﻿using OSPSuite.BDDHelper;
using OSPSuite.BDDHelper.Extensions;
using FakeItEasy;
using PKSim.Core.Model;
using PKSim.Core.Services;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Builder;

namespace PKSim.Core
{
   public abstract class concern_for_ReactionBuildingBlockCreator : ContextSpecification<IReactionBuildingBlockCreator>
   {
      protected IMoleculesAndReactionsCreator _moleculeAndReactionCreator;
      protected IObjectBaseFactory _objectBaseFactory;

      protected override void Context()
      {
         _moleculeAndReactionCreator = A.Fake<IMoleculesAndReactionsCreator>();
         _objectBaseFactory = A.Fake<IObjectBaseFactory>();
         sut = new ReactionBuildingBlockCreator(_moleculeAndReactionCreator, _objectBaseFactory);
      }
   }

   public class When_creating_the_reaction_building_block_based_on_the_settings_of_a_given_simulation : concern_for_ReactionBuildingBlockCreator
   {
      private Simulation _simulation;
      private ReactionBuildingBlock _reactionBuildingBlock;

      protected override void Context()
      {
         base.Context();
         _simulation = A.Fake<Simulation>();
         _reactionBuildingBlock = A.Fake<ReactionBuildingBlock>();
         A.CallTo(() => _moleculeAndReactionCreator.CreateFor(A<Module>._, _simulation)).Returns((new MoleculeBuildingBlock(), _reactionBuildingBlock));
      }

      [Observation]
      public void should_leverage_the_molecule_and_reaction_creator_to_create_a_reaction_building_block()
      {
         sut.CreateFor(_simulation).ShouldBeEqualTo(_reactionBuildingBlock);
      }
   }

   public class When_creating_a_reaction_building_block_for_an_imported_simulation : concern_for_ReactionBuildingBlockCreator
   {
      private Simulation _simulation;
      private ReactionBuildingBlock _reactionBuildingBlock;

      protected override void Context()
      {
         base.Context();
         _simulation = A.Fake<Simulation>();
         A.CallTo(() => _simulation.IsImported).Returns(true);
         _reactionBuildingBlock = A.Fake<ReactionBuildingBlock>();
         A.CallTo(() => _objectBaseFactory.Create<ReactionBuildingBlock>()).Returns(_reactionBuildingBlock);
      }

      [Observation]
      public void should_return_an_empty_reaction_building_block()
      {
         sut.CreateFor(_simulation).ShouldBeEqualTo(_reactionBuildingBlock);
      }
   }
}