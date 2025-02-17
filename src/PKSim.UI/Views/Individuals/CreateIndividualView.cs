﻿using System.Drawing;
using OSPSuite.Assets;
using OSPSuite.UI.Views;
using PKSim.Assets;
using PKSim.Presentation.Presenters.Individuals;
using PKSim.Presentation.Views.Individuals;
using PKSim.UI.Views.Core;

namespace PKSim.UI.Views.Individuals
{
   public partial class CreateIndividualView : BuildingBlockWizardView, ICreateIndividualView
   {
      public CreateIndividualView(BaseShell shell) : base(shell)
      {
         InitializeComponent();
         ClientSize = new Size(UIConstants.Size.INDIVIDUAL_VIEW_WIDTH, UIConstants.Size.INDIVIDUAL_VIEW_HEIGHT);
      }

      public void AttachPresenter(ICreateIndividualPresenter presenter)
      {
         WizardPresenter = presenter;
      }

      public override void InitializeResources()
      {
         base.InitializeResources();
         ApplicationIcon = ApplicationIcons.Individual;
         Caption = PKSimConstants.UI.CreateIndividual;
      }
   }
}