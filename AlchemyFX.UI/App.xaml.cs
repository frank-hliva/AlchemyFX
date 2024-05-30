using System;
using System.Configuration;
using System.Data;
using System.IO.IsolatedStorage;
using System.IO;
using System.Windows;
using Unity;
using Unity.Lifetime;
using Unity.Injection;
using System.Xml.Linq;
using Unity.Strategies;
using LiteDB;
using LiteDB.FSharp;
using AlchemyFX;
using AlchemyFX.Data;
using AlchemyFX.Data.Model;
using AlchemyFX.UI;
using AlchemyFX.UI.Pages;
using AlchemyFX.UI.Dialogs;
using System.ComponentModel.Design;
using AlchemyFX.View;
using System.Reflection;
using System.Windows.Input;
using System.Globalization;
using MoonSharp.Interpreter;

namespace AlchemyFX.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IUnityContainer serviceContainer = new UnityContainer();

        protected void InitDefaultCulture()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentUICulture;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            InitDefaultCulture();

            var launchWindow = LaunchWindow.create();
            LaunchWindow.show(launchWindow);
            base.OnStartup(e);
            serviceContainer.RegisterApplicationContext();

            serviceContainer.RegisterSingleton<WindowStorage>();
            serviceContainer.RegisterSingleton<LocationsStorage>();
            serviceContainer.RegisterInstance<GetDefaultDistro>(() => WSL.Distro.getDefault().Id);
            serviceContainer.RegisterSingleton<AppConfig>();

            serviceContainer.RegisterSingleton<ITheme, ThemeDefinition>();

            serviceContainer.RegisterSingleton<TaskProfile>(
                new InjectionConstructor(typeof(LocationsStorage))
            );

            serviceContainer.RegisterSingleton<LocationBuilder>();
            serviceContainer.RegisterSingleton<WSL.IEnvironment, WSL.EnvironmentConfigAdapter>();

            serviceContainer.RegisterLiteDatabase(ResolveDatabaseFile());

            serviceContainer.RegisterSingleton<IProfileRepository, ProfileRepository>();

            serviceContainer.RegisterSingleton<TaskMarkdownEditorViewModel>();
            serviceContainer.RegisterSingleton<TaskViewModel>();
            serviceContainer.RegisterSingleton<ProfileViewModel>();
            serviceContainer.RegisterSingleton<ApplicationViewModel>();

            serviceContainer.RegisterSingleton<WSL.Command>();
            serviceContainer.RegisterType<WSL.Path>();

            serviceContainer.RegisterSingleton<GlobalCommandMediator>();
            serviceContainer.RegisterSingleton<TaskPage>();
            serviceContainer.RegisterSingleton<ProfilesPage>();

            serviceContainer.RegisterSingleton<MainWindow>();

            InitializeHome();

            serviceContainer.Resolve<MainWindow>().Show();
            LaunchWindow.close(launchWindow);
        }

        protected void InitializeHome()
        {
            HomeDirectory.resolveStructure();

            var lua = new MoonSharp.Interpreter.Script();
            //lua.Globals["serviceContainer"] = serviceContainer;
            var initScript = HomeDirectory.Scripts.Init.resolve();
            lua.DoString(initScript);
        }

        protected string ResolveDatabaseFile()
        {
            var databaseFilePath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                ApplicationDatabase.Name
            );
            if (!File.Exists(databaseFilePath))
            {
                using (var fileStream = File.Create(databaseFilePath))
                {
                }
            }
            return databaseFilePath;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            var appConfig = serviceContainer.Resolve<AppConfig>();
            if (appConfig.IsFirstStart)
            {
                appConfig.IsFirstStart = false;
            }
            serviceContainer.Resolve<LiteDatabase>().Dispose();
            base.OnExit(e);
        }
    }
}
