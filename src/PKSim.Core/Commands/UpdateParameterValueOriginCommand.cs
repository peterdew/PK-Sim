﻿using OSPSuite.Core.Commands.Core;
using OSPSuite.Core.Domain;
using Command = OSPSuite.Assets.Command;

namespace PKSim.Core.Commands
{
   public class UpdateParameterValueOriginCommand : EditParameterCommand
   {
      private ValueOrigin _valueOrigin;
      private ValueOrigin _oldValueOrigin;

      public UpdateParameterValueOriginCommand(IParameter parameter, ValueOrigin valueOrigin) : base(parameter)
      {
         _valueOrigin = valueOrigin;
      }

      protected override ICommand<IExecutionContext> GetInverseCommand(IExecutionContext context)
      {
         return new UpdateParameterValueOriginCommand(_parameter, _oldValueOrigin).AsInverseFor(this);
      }

      protected override void ExecuteUpdateParameter(IParameter parameter, IExecutionContext context)
      {
         _oldValueOrigin = parameter.ValueOrigin.Clone();
         UpdateParameter(context);
         Description = Command.UpdateValueOriginFrom(_oldValueOrigin.ToString(), _valueOrigin.ToString(), ObjectType, context.DisplayNameFor(parameter), BuildingBlockType, BuildingBlockName);
      }

      protected override void UpdateParameter(IParameter parameter, IExecutionContext context)
      {
         parameter?.UpdateValueOriginFrom(_valueOrigin);
      }

      protected override void ClearReferences()
      {
         base.ClearReferences();
         _valueOrigin = null;
      }
   }
}