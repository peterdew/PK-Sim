using OSPSuite.Core.Domain.Formulas;
using OSPSuite.Core.Domain.Populations;

namespace PKSim.Core.Model
{
   public class DistributedParameterValue : ParameterValue
   {
      public double Mean { get; }
      public double Std { get; }
      public DistributionType DistributionType { get; }

      public DistributedParameterValue(string parameterPath, double value, double percentile, double mean, double std, DistributionType distributionType) : base(parameterPath, value, percentile)
      {
         Mean = mean;
         Std = std;
         DistributionType = distributionType;
      }
   }
}