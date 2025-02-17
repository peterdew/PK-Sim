using System.Collections.Generic;
using OSPSuite.Utility.Collections;
using PKSim.Core.Model;

namespace PKSim.Core
{
   /// <inheritdoc />
   public interface IApplicationSettings : OSPSuite.Core.IApplicationSettings
   {
      IEnumerable<SpeciesDatabaseMap> SpeciesDataBaseMaps { get; }
      void AddSpeciesDatabaseMap(SpeciesDatabaseMap speciesDatabaseMap);
      void RemoveSpeciesDatabaseMap(string speciesName);
      bool HasExpressionsDatabaseFor(Species species);
      bool HasExpressionsDatabaseFor(string speciesName);
      SpeciesDatabaseMap SpeciesDatabaseMapsFor(string speciesName);

      /// <summary>
      ///    Full path to MoBi exe. This path will be used if MoBi cannot be found using standard registry mechanism. Can be null
      /// </summary>
      string MoBiPath { get; set; }
   }

   public class ApplicationSettings : OSPSuite.Core.ApplicationSettings, IApplicationSettings
   {
      private string _moBiPath;

      private readonly Cache<string, SpeciesDatabaseMap> _allMaps = new Cache<string, SpeciesDatabaseMap>(x => x.Species);

      public IEnumerable<SpeciesDatabaseMap> SpeciesDataBaseMaps => _allMaps;

      public void AddSpeciesDatabaseMap(SpeciesDatabaseMap speciesDatabaseMap)
      {
         _allMaps.Add(speciesDatabaseMap);
      }

      public void RemoveSpeciesDatabaseMap(string species)
      {
         _allMaps.Remove(species);
      }

      public bool HasExpressionsDatabaseFor(string speciesName)
      {
         return _allMaps.Contains(speciesName);
      }

      public bool HasExpressionsDatabaseFor(Species species)
      {
         return HasExpressionsDatabaseFor(species.Name);
      }

      public SpeciesDatabaseMap SpeciesDatabaseMapsFor(string speciesName)
      {
         if (!_allMaps.Contains(speciesName))
         {
            AddSpeciesDatabaseMap(new SpeciesDatabaseMap {Species = speciesName});
         }
         return _allMaps[speciesName];
      }

      public virtual string MoBiPath
      {
         get => _moBiPath;
         set => SetProperty(ref _moBiPath, value);
      }
   }
}