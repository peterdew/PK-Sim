using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OSPSuite.Assets;
using OSPSuite.Core.Commands;
using OSPSuite.Core.Commands.Core;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Data;
using OSPSuite.Core.Domain.ParameterIdentifications;
using OSPSuite.Core.Domain.Services;
using OSPSuite.Core.Events;
using OSPSuite.Core.Serialization.Xml;
using OSPSuite.Core.Services;
using OSPSuite.Presentation.Core;
using OSPSuite.Utility.Extensions;
using PKSim.Assets;
using PKSim.Core;
using PKSim.Core.Model;
using PKSim.Core.Services;
using PKSim.Presentation.Presenters;
using PKSim.Presentation.Presenters.Snapshots;
using IObservedDataTask = PKSim.Core.Services.IObservedDataTask;

namespace PKSim.Infrastructure.Services
{
   public class ObservedDataTask : OSPSuite.Core.Domain.Services.ObservedDataTask, IObservedDataTask
   {
      private readonly IPKSimProjectRetriever _projectRetriever;
      private readonly IExecutionContext _executionContext;
      private readonly IApplicationController _applicationController;
      private readonly ITemplateTask _templateTask;
      private readonly IParameterChangeUpdater _parameterChangeUpdater;
      private readonly IPKMLPersistor _pkmlPersistor;
      private readonly IOutputMappingMatchingTask _OutputMappingMatchingTask;

      public ObservedDataTask(
         IPKSimProjectRetriever projectRetriever,
         IExecutionContext executionContext,
         IDialogCreator dialogCreator,
         IApplicationController applicationController,
         IDataRepositoryExportTask dataRepositoryTask,
         ITemplateTask templateTask,
         IContainerTask containerTask,
         IParameterChangeUpdater parameterChangeUpdater,
         IPKMLPersistor pkmlPersistor,
         IObjectTypeResolver objectTypeResolver,
         IOutputMappingMatchingTask OutputMappingMatchingTask)
         : base(dialogCreator, executionContext, dataRepositoryTask, containerTask,
         objectTypeResolver)
      {
         _projectRetriever = projectRetriever;
         _executionContext = executionContext;
         _applicationController = applicationController;
         _templateTask = templateTask;
         _parameterChangeUpdater = parameterChangeUpdater;
         _pkmlPersistor = pkmlPersistor;
         _OutputMappingMatchingTask = OutputMappingMatchingTask;
      }

      public override void Rename(DataRepository observedData)
      {
         using (var renamePresenter = _applicationController.Start<IRenameObservedDataPresenter>())
         {
            if (!renamePresenter.Edit(observedData))
               return;

            _executionContext.AddToHistory(new RenameObservedDataCommand(observedData, renamePresenter.Name).Run(_executionContext));
         }
      }

      public override void UpdateMolWeight(DataRepository observedData)
      {
         _parameterChangeUpdater.UpdateMolWeightIn(observedData);
      }

      public void SaveToTemplate(DataRepository observedData)
      {
         _templateTask.SaveToTemplate(observedData, TemplateType.ObservedData);
      }

      public void ExportToPkml(DataRepository observedData)
      {
         var file = _dialogCreator.AskForFileToSave(PKSimConstants.UI.ExportObservedDataToPkml, Constants.Filter.PKML_FILE_FILTER,
            Constants.DirectoryKey.MODEL_PART, observedData.Name);
         if (string.IsNullOrEmpty(file)) return;

         _pkmlPersistor.SaveToPKML(observedData, file);
      }

      public void LoadFromSnapshot()
      {
         using (var presenter = _applicationController.Start<ILoadFromSnapshotPresenter<DataRepository>>())
         {
            var observedData = presenter.LoadModelFromSnapshot();
            observedData?.Each(AddObservedDataToProject);
         }
      }

      public void AddObservedDataToAnalysable(IReadOnlyList<DataRepository> observedDataList, IAnalysable analysable)
      {
         AddObservedDataToAnalysable(observedDataList, analysable, showData: false);
      }

      public void AddObservedDataToAnalysable(IReadOnlyList<DataRepository> observedDataList, IAnalysable analysable, bool showData)
      {
         var simulation = analysable as Simulation;
         if (simulation == null)
            return;

         var observedDataToAdd = observedDataList.Where(x => !simulation.UsesObservedData(x)).ToList();
         if (!observedDataToAdd.Any())
            return;

         observedDataToAdd.Each(simulation.AddUsedObservedData);
         observedDataList.Each(observedData => _OutputMappingMatchingTask.AddMatchingOutputMapping(observedData, simulation));

         _executionContext.PublishEvent(new ObservedDataAddedToAnalysableEvent(simulation, observedDataToAdd, showData));
         _executionContext.PublishEvent(new SimulationStatusChangedEvent(simulation));
      }

      private IEnumerable<ParameterIdentification> findParameterIdentificationsUsing(UsedObservedData usedObservedData)
      {
         var observedData = observedDataFrom(usedObservedData);
         var simulation = usedObservedData.Simulation;

         return from parameterIdentification in allParameterIdentifications()
            let outputMappings = parameterIdentification.AllOutputMappingsFor(simulation)
            where outputMappings.Any(x => x.UsesObservedData(observedData))
            select parameterIdentification;
      }

      private IReadOnlyCollection<ParameterIdentification> allParameterIdentifications()
      {
         return _projectRetriever.Current.AllParameterIdentifications;
      }

      private void removeUsedObservedDataFromSimulation(IEnumerable<UsedObservedData> usedObservedDatas, Simulation simulation)
      {
         _executionContext.Load(simulation);

         var observedDataList = observedDataListFrom(usedObservedDatas);
         observedDataList.Each(simulation.RemoveUsedObservedData);
         observedDataList.Each(simulation.RemoveOutputMappings);

         _executionContext.PublishEvent(new ObservedDataRemovedFromAnalysableEvent(simulation, observedDataList));
         _executionContext.PublishEvent(new SimulationStatusChangedEvent(simulation));
      }

      private IReadOnlyList<DataRepository> observedDataListFrom(IEnumerable<UsedObservedData> usedObservedDatas)
      {
         return usedObservedDatas.Select(observedDataFrom).ToList();
      }

      private DataRepository observedDataFrom(UsedObservedData usedObservedDatas)
      {
         return _projectRetriever.CurrentProject.ObservedDataBy(usedObservedDatas.Id);
      }

      public async Task LoadObservedDataFromTemplateAsync()
      {
         var observedDataList = await _templateTask.LoadFromTemplateAsync<DataRepository>(TemplateType.ObservedData);
         observedDataList.Each(AddObservedDataToProject);
      }
   }
}