using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Commands;
using TgaBuilderLib.ViewModel;
using TgaBuilderWpfUi.Services;
using TgaBuilderWpfUi.View;
using Wpf.Ui.Appearance;
using Application = System.Windows.Application;

namespace TgaBuilderWpfUi
{

    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            CommandManagerProxy.Initialize(new CommandManagerService());

            var services = new ServiceCollection();
            BuildServicesDI(services);
            var provider = services.BuildServiceProvider();

            MainViewModel mainViewModel = provider.GetRequiredService<MainViewModel>();

            MainWindow mainWindow = provider.GetServices<IView>().ElementAt(0) as MainWindow 
                ?? throw new InvalidOperationException("MainWindow not found in DI container");

            ApplicationThemeManager.GetAppTheme();
            SystemThemeWatcher.Watch(mainWindow);

            mainWindow.Loaded += (_, _) =>
            {
                _ = mainViewModel.SourceViewTab.DefferedFill();
                _ = mainViewModel.DestinationViewTab.DefferedFill();
            };

            mainWindow.ApplicationThemeButton.Click += (_, _) 
                => ChangeTheme();

            mainWindow.Show();


            mainWindow.Closed += (_, _) => Shutdown();
        }

        private void ChangeTheme()
        {
            var Theme = ApplicationThemeManager.GetAppTheme();

            if (Theme == ApplicationTheme.Light)
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
            else
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
        }
    }

}
