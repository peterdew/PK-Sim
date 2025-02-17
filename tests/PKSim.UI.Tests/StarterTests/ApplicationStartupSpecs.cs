﻿using System.Threading;
using FakeItEasy;
using OSPSuite.BDDHelper;
using OSPSuite.BDDHelper.Extensions;
using OSPSuite.Infrastructure.Container.Castle;
using OSPSuite.Presentation.Presenters.Journal;
using OSPSuite.Presentation.Presenters.Main;
using OSPSuite.Presentation.Views;
using OSPSuite.UI.Views;
using OSPSuite.Utility.Container;
using PKSim.Core.Mappers;
using PKSim.Presentation.Presenters.ExpressionProfiles;
using PKSim.Presentation.Presenters.Individuals;
using PKSim.Presentation.Services;
using PKSim.UI.Starter;

namespace PKSim.UI.StarterTests
{
   public class concern_for_ApplicationStartup : StaticContextSpecification
   {
      protected IContainer _container;

      protected override void Context()
      {
         SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
         IoC.InitializeWith(new CastleWindsorContainer());

         // To initialize the local container, the application starter will take some components from the
         // static container
         IoC.Container.RegisterImplementationOf(A.Fake<IMainViewPresenter>());
         IoC.Container.RegisterImplementationOf(A.Fake<IMainView>());
         IoC.Container.RegisterImplementationOf(A.Fake<IJournalPresenter>());
         IoC.Container.RegisterImplementationOf(new BaseShell() as IShell);

         _container = ApplicationStartup.Initialize();
      }
   }

   public class When_resolving_the_database_query_task : concern_for_ApplicationStartup
   {
      private IExpressionProfileProteinDatabaseTask _task;

      protected override void Because()
      {
         _task = _container.Resolve<IExpressionProfileProteinDatabaseTask>();
      }

      [Observation]
      public void the_task_should_be_resolved()
      {
         _task.ShouldBeAnInstanceOf<ExpressionProfileProteinDatabaseTask>();
      }
   }

   public class When_resolving_the_expression_profile_presenter : concern_for_ApplicationStartup
   {
      private ICreateExpressionProfilePresenter _presenter;

      protected override void Because()
      {
         _presenter = _container.Resolve<ICreateExpressionProfilePresenter>();
      }

      [Observation]
      public void the_presenter_should_be_resolved()
      {
         _presenter.ShouldBeAnInstanceOf<CreateExpressionProfilePresenter>();
      }
   }

   public class When_resolving_the_individual_presenter : concern_for_ApplicationStartup
   {
      private ICreateIndividualPresenter _presenter;

      protected override void Because()
      {
         _presenter = _container.Resolve<ICreateIndividualPresenter>();
      }

      [Observation]
      public void the_presenter_should_be_resolved()
      {
         _presenter.ShouldBeAnInstanceOf<CreateIndividualPresenterForMoBi>();
      }

      public class When_resolving_the_individual_mapper : concern_for_ApplicationStartup
      {
         private IIndividualToIndividualBuildingBlockMapper _mapper;

         protected override void Because()
         {
            _mapper = _container.Resolve<IIndividualToIndividualBuildingBlockMapper>();
         }

         [Observation]
         public void the_mapper_should_be_resolved()
         {
            _mapper.ShouldBeAnInstanceOf<IndividualToIndividualBuildingBlockMapper>();
         }
      }

      public class When_resolving_the_building_block_mapper : concern_for_ApplicationStartup
      {
         private IExpressionProfileToExpressionProfileBuildingBlockMapper _mapper;

         protected override void Because()
         {
            _mapper = _container.Resolve<IExpressionProfileToExpressionProfileBuildingBlockMapper>();
         }

         [Observation]
         public void the_mapper_should_be_resolved()
         {
            _mapper.ShouldBeAnInstanceOf<ExpressionProfileToExpressionProfileBuildingBlockMapper>();
         }
      }
   }
}