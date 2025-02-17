using System.Collections.Generic;
using System.Linq;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Formulas;
using OSPSuite.Core.Domain.Populations;
using OSPSuite.Core.Domain.Services;
using OSPSuite.Utility.Container;
using PKSim.Core.Mappers;
using PKSim.Core.Model;
using PKSim.Core.Snapshots.Mappers;
using OriginData = PKSim.Core.Snapshots.OriginData;

namespace PKSim.Matlab
{
   public interface IMatlabIndividualFactory
   {
      ParameterValue[] CreateIndividual(OriginData matlabOriginData, IEnumerable<MoleculeOntogeny> moleculeOntogenies);
      ParameterValue[] CreateIndividual(OriginData matlabOriginData);
      DistributedParameterValue[] DistributionsFor(OriginData matlabOriginData, IEnumerable<MoleculeOntogeny> moleculeOntogenies);
      DistributedParameterValue[] DistributionsFor(OriginData matlabOriginData);
   }

   public class MatlabIndividualFactory : IMatlabIndividualFactory
   {
      private readonly OriginDataMapper _originDataMapper;
      private readonly IIndividualFactory _individualFactory;
      private readonly IIndividualToIndividualValuesMapper _individualValuesMapper;
      private readonly IOntogenyFactorsRetriever _ontogenyFactorsRetriever;
      private readonly IEntityPathResolver _entityPathResolver;

      static MatlabIndividualFactory()
      {
         ApplicationStartup.Initialize();
      }

      public MatlabIndividualFactory()
         : this(
            IoC.Resolve<OriginDataMapper>(),
            IoC.Resolve<IIndividualFactory>(),
            IoC.Resolve<IIndividualToIndividualValuesMapper>(),
            IoC.Resolve<IOntogenyFactorsRetriever>(),
            IoC.Resolve<IEntityPathResolver>()
         )
      {
      }

      internal MatlabIndividualFactory(
         OriginDataMapper originDataMapper,
         IIndividualFactory individualFactory,
         IIndividualToIndividualValuesMapper individualValuesMapper,
         IOntogenyFactorsRetriever ontogenyFactorsRetriever,
         IEntityPathResolver entityPathResolver)
      {
         _originDataMapper = originDataMapper;
         _individualFactory = individualFactory;
         _individualValuesMapper = individualValuesMapper;
         _ontogenyFactorsRetriever = ontogenyFactorsRetriever;
         _entityPathResolver = entityPathResolver;
      }

      public ParameterValue[] CreateIndividual(OriginData matlabOriginData, IEnumerable<MoleculeOntogeny> moleculeOntogenies)
      {
         var originData = originDataFrom(matlabOriginData);
         var individual = _individualFactory.CreateAndOptimizeFor(originData);
         var individualProperties = _individualValuesMapper.MapFrom(individual);
         var allIndividualParameters = individualProperties.ParameterValues.ToList();
         allIndividualParameters.AddRange(_ontogenyFactorsRetriever.FactorsFor(originData, moleculeOntogenies));
         return allIndividualParameters.ToArray();
      }

      public ParameterValue[] CreateIndividual(OriginData matlabOriginData)
      {
         return CreateIndividual(matlabOriginData, Enumerable.Empty<MoleculeOntogeny>());
      }

        public DistributedParameterValue[] DistributionsFor(OriginData matlabOriginData, IEnumerable<MoleculeOntogeny> moleculeOntogenies)
      {
         var originData = originDataFrom(matlabOriginData);
         var individual = _individualFactory.CreateAndOptimizeFor(originData);
         return individual.GetAllChildren<IDistributedParameter>().Select(distributedParameterValueFrom).ToArray();
      }

      public DistributedParameterValue[] DistributionsFor(OriginData matlabOriginData)
      {
         return DistributionsFor(matlabOriginData, Enumerable.Empty<MoleculeOntogeny>());
      }

      private Core.Model.OriginData originDataFrom(OriginData matlabOriginData) => _originDataMapper.MapToModel(matlabOriginData, new SnapshotContext()).Result;

      private DistributedParameterValue distributedParameterValueFrom(IDistributedParameter parameter)
      {
         var parameterPath = _entityPathResolver.PathFor(parameter);
         var distributionType = parameter.Formula.DistributionType;
         double p1 = 0, p2 = 0;
         if (distributionType == DistributionType.Normal || distributionType == DistributionType.LogNormal)
         {
            p1 = parameter.MeanParameter.Value;
            p2 = parameter.DeviationParameter.Value;
         }
         else if (distributionType == DistributionType.Uniform)
         {
            p1 = parameter.Parameter(Constants.Distribution.MINIMUM).Value;
            p2 = parameter.Parameter(Constants.Distribution.MAXIMUM).Value;
         }

         return new DistributedParameterValue(parameterPath, parameter.Value, parameter.Percentile, p1, p2, distributionType);
      }
   }
}