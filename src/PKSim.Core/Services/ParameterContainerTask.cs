using System;
using System.Collections.Generic;
using System.Linq;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Builder;
using OSPSuite.Core.Domain.Formulas;
using PKSim.Core.Model;
using PKSim.Core.Repositories;
using IParameterFactory = PKSim.Core.Model.IParameterFactory;

namespace PKSim.Core.Services
{
   public interface IParameterContainerTask
   {
      void AddIndividualParametersTo<TContainer>(TContainer parameterContainer, OriginData originData) where TContainer : IContainer;
      void AddIndividualParametersTo<TContainer>(TContainer parameterContainer, OriginData originData, string parameterName) where TContainer : IContainer;

      void AddActiveProcessParametersTo(CompoundProcess process);
      void AddProcessBuilderParametersTo(IContainer process);
      void AddFormulationParametersTo(Formulation formulation);
      void AddMoleculeParametersTo(MoleculeBuilder molecule, IFormulaCache formulaCache);
      void AddApplicationParametersTo(IContainer container);
      void AddSchemaItemParametersTo(ISchemaItem schemaItem);
      void AddEventParametersTo(IContainer container);
      void AddCompoundParametersTo(Compound compound);
      void AddDiseaseStateParametersTo(DiseaseState diseaseState);

      void AddParametersToSpatialStructureContainer<TContainer>(TContainer parameterContainer, OriginData originData, ModelProperties modelProperties, IFormulaCache formulaCache) where TContainer : IContainer;
      void AddApplicationTransportParametersTo(TransportBuilder applicationTransportBuilder, string applicationName, string formulationName, IFormulaCache formulaCache);
   }

   public class ParameterContainerTask : IParameterContainerTask
   {
      private readonly IParameterFactory _parameterFactory;
      private readonly IIndividualParameterBySpeciesRepository _individualParameterBySpeciesRepository;
      private readonly IIndividualParametersSameFormulaOrValueForAllSpeciesRepository _sameFormulaOrValueForAllSpeciesRepository;
      private readonly IParameterQuery _parameterQuery;

      public ParameterContainerTask(
         IParameterQuery parameterQuery,
         IParameterFactory parameterFactory,
         IIndividualParameterBySpeciesRepository individualParameterBySpeciesRepository,
         IIndividualParametersSameFormulaOrValueForAllSpeciesRepository sameFormulaOrValueForAllSpeciesRepository
      )
      {
         _parameterQuery = parameterQuery;
         _parameterFactory = parameterFactory;
         _individualParameterBySpeciesRepository = individualParameterBySpeciesRepository;
         _sameFormulaOrValueForAllSpeciesRepository = sameFormulaOrValueForAllSpeciesRepository;
      }

      public void AddIndividualParametersTo<TContainer>(TContainer parameterContainer, OriginData originData) where TContainer : IContainer
      {
         addParametersTo(parameterContainer, originData, originData.AllCalculationMethods().Select(cm => cm.Name), param => param.BuildingBlockType == PKSimBuildingBlockType.Individual);
      }

      public void AddIndividualParametersTo<TContainer>(TContainer parameterContainer, OriginData originData, string parameterName) where TContainer : IContainer
      {
         addParametersTo(parameterContainer, originData, originData.AllCalculationMethods().Select(cm => cm.Name),
            param => param.BuildingBlockType == PKSimBuildingBlockType.Individual && string.Equals(param.ParameterName, parameterName));
      }

      public void AddDiseaseStateParametersTo(DiseaseState diseaseState) => addParametersTo(diseaseState, calculationMethods: CoreConstants.CalculationMethod.ForDiseaseStates);

      public void AddParametersToSpatialStructureContainer<TContainer>(TContainer parameterContainer, OriginData originData, ModelProperties modelProperties, IFormulaCache formulaCache) where TContainer : IContainer
      {
         bool addParameter(ParameterMetaData parameterMetaData)
         {
            //all non individual parameters are added to the spatial structure by default
            //otherwise, we add all parameters that are used by all species. The value used will be set based on the fact that the parameter has a shared value for all parameters or species or not
            return parameterMetaData.BuildingBlockType != PKSimBuildingBlockType.Individual || _individualParameterBySpeciesRepository.UsedForAllSpecies(parameterMetaData);
         }
         
         void parameterValueModifier(ParameterMetaData parameterMetaData, IParameter parameter)
         {
            //here we know that the parameter fulfills the criteria above but we really only care about parameters that are individual parameters
            if (parameterMetaData.BuildingBlockType != PKSimBuildingBlockType.Individual)
               return;

            //In case of a parameter that is used by all species, we need to verify that either the formula or the value is common 
            //to all species. Otherwise, we set the value to NaN
            var isSame = _sameFormulaOrValueForAllSpeciesRepository.IsSameFormulaOrValue(parameterMetaData);
            if (!isSame)
               parameter.Formula = new ConstantFormula(double.NaN);
         }

         addParametersTo(parameterContainer, originData, modelProperties.AllCalculationMethods().Select(cm => cm.Name), addParameter, parameterValueModifier, formulaCache);
      }

      public void AddActiveProcessParametersTo(CompoundProcess process) => addParametersTo(process, calculationMethods: CoreConstants.CalculationMethod.ForProcesses);

      public void AddApplicationTransportParametersTo(TransportBuilder applicationTransportBuilder, string applicationName, string formulationName, IFormulaCache formulaCache)
      {
         //assuming that all application transports are located directly under
         //the application container
         var formulation = new Container().WithName(formulationName);
         var application = new Container().WithName(applicationName);
         var transport = new Container().WithName(applicationTransportBuilder.Name);

         formulation.Add(application);
         application.Add(transport);

         addParametersTo(transport, calculationMethods: CoreConstants.CalculationMethod.ForApplications, formulaCache: formulaCache);

         updateProcessesParameters(transport, applicationTransportBuilder);
      }

      public void AddProcessBuilderParametersTo(IContainer processBuilder)
      {
         var container = new Container().WithName(processBuilder.Name);
         addParametersTo(container, calculationMethods: CoreConstants.CalculationMethod.ForProcesses);
         updateProcessesParameters(container, processBuilder);
      }

      private static void updateProcessesParameters(IContainer container, IContainer processesBuilder)
      {
         foreach (var parameter in container.AllParameters())
         {
            var parameterName = parameter.Name;
            //parameter already available in process. nothing to do
            if (processesBuilder.ContainsName(parameterName))
               continue;

            processesBuilder.Add(parameter);
         }
      }

      public void AddFormulationParametersTo(Formulation formulation)
      {
         string oldName = formulation.Root.Name;
         formulation.Root.Name = formulation.Name;
         addParametersTo(formulation.Root, calculationMethods: CoreConstants.CalculationMethod.ForFormulations);
         formulation.Root.Name = oldName;
      }

      public void AddMoleculeParametersTo(MoleculeBuilder molecule, IFormulaCache formulaCache)
         => addParametersTo(molecule, calculationMethods: CoreConstants.CalculationMethod.ForCompounds, formulaCache: formulaCache);

      public void AddApplicationParametersTo(IContainer container)
      {
         addParametersTo(container, calculationMethods: CoreConstants.CalculationMethod.ForApplications);
         //parameter input dose should not be added to the container 
         var inputDose = container.Parameter(CoreConstants.Parameters.INPUT_DOSE);
         if (inputDose != null)
            container.RemoveChild(inputDose);
      }

      public void AddSchemaItemParametersTo(ISchemaItem schemaItem)
         => addParametersTo(schemaItem, calculationMethods: CoreConstants.CalculationMethod.ForSchemaItems);

      public void AddEventParametersTo(IContainer container)
         => addParametersTo(container, calculationMethods: CoreConstants.CalculationMethod.ForEvents);

      public void AddCompoundParametersTo(Compound compound)
      {
         //reset compound name so that formula can be resolved
         var oldName = compound.Root.Name;
         compound.Root.Name = CoreConstants.ContainerName.Drug;
         addParametersTo(compound.Root, calculationMethods: CoreConstants.CalculationMethod.ForCompounds, predicate: x => x.BuildingBlockType == PKSimBuildingBlockType.Compound);
         compound.Root.Name = oldName;
      }

      /// <summary>
      ///    Add all parameters defined in the database for the given <paramref name="originData" /> and
      ///    <paramref name="calculationMethods" /> in <paramref name="parameterContainer" />
      /// </summary>
      /// <param name="parameterContainer">Container where all parameters will be added</param>
      /// <param name="originData">Origin data used to retrieve constant parameter values</param>
      /// <param name="calculationMethods">Calculation methods used to retrieve rate parameter values</param>
      /// <param name="predicate">Optional predicate used to filter out some parameter from the query</param>
      /// <param name="parameterValueModifier">
      ///    Optional action that will allow the caller to manipulate the default value created
      ///    for the parameter
      /// </param>
      /// <param name="formulaCache">Formula cache where the formula will be defined for a rate parameter</param>
      private void addParametersTo(
         IContainer parameterContainer,
         OriginData originData = null,
         IEnumerable<string> calculationMethods = null,
         Func<ParameterMetaData, bool> predicate = null,
         Action<ParameterMetaData, IParameter> parameterValueModifier = null,
         IFormulaCache formulaCache = null)
      {
         var predicateToUse = predicate ?? (x => true);
         var parameterValueModifierToUse = parameterValueModifier ?? ((_, _) => { });

         //RATE PARAMETERS
         foreach (var parameterRateMetaData in _parameterQuery.ParameterRatesFor(parameterContainer, calculationMethods, predicateToUse))
         {
            //parameter already available,
            var parameter = _parameterFactory.CreateFor(parameterRateMetaData, formulaCache);
            parameterValueModifierToUse(parameterRateMetaData, parameter);
            parameterContainer.Add(parameter);
         }

         //CONSTANT PARAMETERS
         foreach (var parameterValueMetaData in _parameterQuery.ParameterValuesFor(parameterContainer, originData, predicateToUse))
         {
            var parameter = _parameterFactory.CreateFor(parameterValueMetaData);
            parameterValueModifierToUse(parameterValueMetaData, parameter);
            parameterContainer.Add(parameter);
         }

         //DISTRIBUTION PARAMETERS
         foreach (var parameterDistributionMetaData in _parameterQuery.ParameterDistributionsFor(parameterContainer, originData, predicateToUse).GroupBy(dist => dist.ParameterName))
         {
            var parameter = _parameterFactory.CreateFor(parameterDistributionMetaData.ToList(), originData);

            //Parameter might be distributed only for a few species
            if (parameterContainer.ContainsName(parameter.Name))
               parameterContainer.RemoveChild(parameterContainer.Parameter(parameter.Name));

            //no need to use the parameter modifier here as they are unique by species by definition
            parameterContainer.Add(parameter);
         }
      }
   }
}