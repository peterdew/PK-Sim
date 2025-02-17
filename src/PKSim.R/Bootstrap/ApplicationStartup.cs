using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using OSPSuite.Core;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Services;
using OSPSuite.Utility.Container;
using OSPSuite.Utility.Events;
using OSPSuite.Utility.Exceptions;
using PKSim.Assets;
using PKSim.CLI.Core.MinimalImplementations;
using PKSim.Core;
using PKSim.Core.Model;
using PKSim.Core.Services;
using PKSim.Infrastructure;
using CoreRegister = PKSim.Core.CoreRegister;
using IContainer = OSPSuite.Utility.Container.IContainer;
using ICoreUserSettings = PKSim.Core.ICoreUserSettings;

namespace PKSim.R.Bootstrap
{
   internal class ApplicationStartup
   {
      private static IContainer _container;

      public static IContainer Initialize()
      {
         if (_container != null)
            return _container;

         redirectAssemblies();

         _container = new ApplicationStartup().performInitialization();
         return _container;
      }

      private IContainer performInitialization()
      {
         // We do not want to register the IoC container to avoid static pollution that may collide with OSPSuite-R
         var container = InfrastructureRegister.Initialize(registerContainerAsStatic: false);

         using (container.OptimizeDependencyResolution())
         {
            container.RegisterImplementationOf(new SynchronizationContext());
            container.AddRegister(x => x.FromType<RRegister>());
            container.AddRegister(x => x.FromType<CoreRegister>());
            container.AddRegister(x => x.FromType<InfrastructureRegister>());
            InfrastructureRegister.RegisterSerializationDependencies(container);
            registerMinimalImplementations(container);
         }

         var configuration = container.Resolve<IPKSimConfiguration>();
         var currentPath = new FileInfo(Assembly.GetAssembly(GetType()).Location).DirectoryName;
         if(!File.Exists(configuration.PKSimDbPath))
            configuration.PKSimDbPath = Path.Combine(currentPath, CoreConstants.PK_SIM_DB_FILE);

         if (!File.Exists(configuration.PKParametersFilePath))
            configuration.PKParametersFilePath = Path.Combine(currentPath, Constants.Files.PK_PARAMETERS_FILE_NAME);

         if (!File.Exists(configuration.DimensionFilePath))
            configuration.DimensionFilePath = Path.Combine(currentPath, Constants.Files.DIMENSIONS_FILE_NAME);

         // This will log the version of PKSIm used to R
         Console.WriteLine(PKSimConstants.Information.InitializingPKSim(CoreConstants.PRODUCT_NAME, configuration.FullVersion));

         // Serialization mapping will require access to PKSim DB and as such, it needs to be performed after the DB was set.
         InfrastructureRegister.LoadDefaultEntities(container);
         return container;
      }

      private void registerMinimalImplementations(IContainer container)
      {
         container.Register<ICoreWorkspace, IWorkspace, CLIWorkspace>(LifeStyle.Singleton);
         container.Register<ICoreUserSettings, CLIUserSettings>();
         container.Register<IProgressUpdater, NoneProgressUpdater>();
         container.Register<IDisplayUnitRetriever, CLIDisplayUnitRetriever>();
         container.Register<IOntogenyTask, CLIIndividualOntogenyTask>();
         container.Register<IExceptionManager, CLIExceptionManager>();
         container.Register<IDialogCreator, CLIDialogCreator>();
      }

      private static void redirectAssemblies()
      {
          redirectAssembly("NHibernate", new Version(5, 2, 0, 0), "aa95f207798dfdb4");
          redirectAssembly("Microsoft.Extensions.Options", new Version(3, 1, 0, 0), "adb9793829ddae60");
          redirectAssembly("Microsoft.Extensions.Logging.Abstractions", new Version(3, 1, 0, 0), "adb9793829ddae60");
      }

      private static void redirectAssembly(string shortName, Version targetVersion, string publicKeyToken)
      {
         Assembly Handler(object sender, ResolveEventArgs args)
         {
            var requestedAssembly = new AssemblyName(args.Name);
            if (requestedAssembly.Name != shortName) return null; 

            requestedAssembly.Version = targetVersion;
            requestedAssembly.SetPublicKeyToken(new AssemblyName("x, PublicKeyToken=" + publicKeyToken).GetPublicKeyToken());
            requestedAssembly.CultureInfo = CultureInfo.InvariantCulture;

            //once found, not need to react to event anymore
            AppDomain.CurrentDomain.AssemblyResolve -= Handler;

            return Assembly.Load(requestedAssembly);
         }

         AppDomain.CurrentDomain.AssemblyResolve += Handler;
      }
   }
}