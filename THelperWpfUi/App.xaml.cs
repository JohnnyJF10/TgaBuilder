using System.Configuration;
using System.Data;
using System.Windows;
using THelperLib.ViewModel;
using THelperWpfUi.View;
using THelperWpfUi.Services;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;
using Appearance = Wpf.Ui.Appearance;
using Microsoft.Extensions.DependencyInjection;
using THelperLib.Abstraction;

namespace THelperWpfUi
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
