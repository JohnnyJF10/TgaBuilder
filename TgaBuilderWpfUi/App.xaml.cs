using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.ViewModel;
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

            var services = new ServiceCollection();
            BuildServicesDI(services);
            var provider = services.BuildServiceProvider();

            var mainViewModel = provider.GetRequiredService<MainViewModel>();

            var mainWindow = provider.GetServices<IView>().ElementAt(0) as MainWindow;

            ApplicationThemeManager.GetAppTheme();
            SystemThemeWatcher.Watch(mainWindow);

            MainWindow.Loaded += (_, _) =>
            {
                _ = mainViewModel.SourceViewTab.DefferedFill();
                _ = mainViewModel.DestinationViewTab.DefferedFill();
            };

            MainWindow.Show();


            MainWindow.Closed += (_, _) => Shutdown();
        }
    }

}
