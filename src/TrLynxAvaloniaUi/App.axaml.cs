using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using TrLynxAvaloniaUi.Services;
using TrLynxAvaloniaUi.View;
using TrLynxLib.Abstraction;
using TrLynxLib.Commands;
using TrLynxLib.ViewModel;

namespace TrLynxAvaloniaUi
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            CommandManagerProxy.Initialize(new CommandManagerService());

            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var services = new ServiceCollection();
            BuildServicesDI(services);
            var provider = services.BuildServiceProvider();

            MainViewModel mainViewModel = provider.GetRequiredService<MainViewModel>();

            MainWindow mainWindow = provider.GetServices<IView>().ElementAt(0) as MainWindow
                ?? throw new InvalidOperationException("MainWindow not found in DI container");

            // This is required in Avalonia UI as partial changes on Images are not automatically redrawn.
            mainViewModel.VisualInvalidator = new VisualInvalidator(mainWindow.TargetImage);
            var SelectionUserControl = mainWindow.FindControl<MainWindowSelectionUserControl>("SelectionUserControl")
                ?? throw new InvalidOperationException("SelectionUserControl not found in MainWindow visual tree");
            var selectionImage = SelectionUserControl.FindControl<Image>("SelectionImage")
                ?? throw new InvalidOperationException("SelectionImage not found in MainWindow visual tree");
            mainViewModel.Selection.VisualInvalidator = new VisualInvalidator(selectionImage);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = mainWindow;
            }

            mainWindow.ThemeToggleButton.Click += (_, _) => ToggleTheme();

            var clipboardService = provider.GetRequiredService<IClipboardService>();
            if (clipboardService is ClipboardService clipboardServiceImpl && mainWindow.Clipboard is not null)
                clipboardServiceImpl.RegisterClipboard(mainWindow.Clipboard);

            mainWindow.Show();

            mainWindow.Closed += (_, _) =>
            {
                if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
            };

            base.OnFrameworkInitializationCompleted();
        }

        public void ToggleTheme()
        {
            if (Current == null) return;

            var currentVariant = Current.ActualThemeVariant;

            // ActualThemeVariant resolves Default to the actual system theme,
            // so it will be either Light or Dark at runtime.
            Current.RequestedThemeVariant = currentVariant == ThemeVariant.Dark
                ? ThemeVariant.Light
                : ThemeVariant.Dark;
        }

        private async Task PeriodicDebugLogging()
        {
            while (true)
            {
                if (ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                    return;

                if (desktop.MainWindow is not View.MainWindow mainWindow)
                    return;

                if (mainWindow.SourcePanel is not ZoomBorder sourcePanel)
                    return;

                if (mainWindow.TargetPanel is not ZoomBorder targetPanel)
                    return;

                await Task.Delay(500);
            }
        }
    }
}
