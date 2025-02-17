﻿using OSPSuite.Presentation.Views;
using PKSim.Presentation.DTO.ExpressionProfiles;
using PKSim.Presentation.Presenters.ExpressionProfiles;

namespace PKSim.Presentation.Views.ExpressionProfiles
{
   public interface IRenameExpressionProfileView : IModalView<IRenameExpressionProfilePresenter>
   {
      void BindTo(ExpressionProfileDTO expressionProfileDTO);
   }
}