using System.Collections.Generic;
using System.Linq;
using PKSim.Core;
using PKSim.Core.Model;
using PKSim.Core.Repositories;
using PKSim.Core.Services;

using PKSim.Presentation.Views.Individuals;
using OSPSuite.Presentation.Presenters;

namespace PKSim.Presentation.Presenters.Individuals
{
   public interface IOntogenySelectionPresenter : IPresenter<IOntogenySelectionView>, ICommandCollectorPresenter
   {
      void SelectedOntogenyIs(Ontogeny ontogeny);
      IEnumerable<Ontogeny> AllOntogenies();

      /// <summary>
      ///    Show the ontogeny data for the selected ontogeny
      /// </summary>
      void ShowOntogeny();

      /// <summary>
      ///    Refresh the view (for instance the show ontogeny button needs to be activated deactivated in response to specific
      ///    events)
      /// </summary>
      void RefreshView();

      /// <summary>
      ///    Load a user defined ontogeny from file
      /// </summary>
      void LoadOntogeny();

      void Edit(IndividualMolecule individualMolecule, ISimulationSubject simulationSubject);
      void DisableEdit();
   }

   public class OntogenySelectionPresenter : AbstractCommandCollectorPresenter<IOntogenySelectionView, IOntogenySelectionPresenter>,
      IOntogenySelectionPresenter
   {
      private readonly IOntogenyTask _ontogenyTask;
      private IndividualMolecule _individualMolecule;
      private readonly IEnumerable<Ontogeny> _allOntogenies;
      private ISimulationSubject _simulationSubject;

      public OntogenySelectionPresenter(IOntogenySelectionView view, IOntogenyRepository ontogenyRepository, IOntogenyTask ontogenyTask)
         : base(view)
      {
         _ontogenyTask = ontogenyTask;
         _allOntogenies = ontogenyRepository.AllFor(CoreConstants.Species.HUMAN);
      }

      public void Edit(IndividualMolecule individualMolecule, ISimulationSubject simulationSubject)
      {
         _simulationSubject = simulationSubject;
         _individualMolecule = individualMolecule;
         rebind();
      }

      public void DisableEdit()
      {
         _view.ReadOnly = true;
      }

      public void SelectedOntogenyIs(Ontogeny ontogeny)
      {
         AddCommand(_ontogenyTask.SetOntogenyForMolecule(_individualMolecule, ontogeny, _simulationSubject));
         updateShowOntogenyButton();
      }

      private void updateShowOntogenyButton()
      {
         _view.ShowOntogenyEnabled = _individualMolecule.Ontogeny.IsDefined();
      }

      public IEnumerable<Ontogeny> AllOntogenies()
      {
         var allOntogenies = new List<Ontogeny> {new NullOntogeny()};
         if (_individualMolecule.Ontogeny.IsDefined())
            allOntogenies.Add(_individualMolecule.Ontogeny);

         return allOntogenies.Union(_allOntogenies);
      }

      private Ontogeny selectedOntogeny => _individualMolecule.Ontogeny;

      public void ShowOntogeny()
      {
         _ontogenyTask.ShowOntogenyData(selectedOntogeny);
      }

      public void RefreshView()
      {
         updateShowOntogenyButton();
         _view.AdjustHeight();
      }

      public void LoadOntogeny()
      {
         _ontogenyTask.LoadOntogenyForMolecule(_individualMolecule, _simulationSubject);
         rebind();
      }

      private void rebind()
      {
         updateShowOntogenyButton();
         _view.BindTo(_individualMolecule);
      }
   }
}