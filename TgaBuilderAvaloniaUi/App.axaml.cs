using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TgaBuilderAvaloniaUi.Services;
using TgaBuilderAvaloniaUi.View;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Commands;
using TgaBuilderLib.ViewModel;

namespace TgaBuilderAvaloniaUi
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
            mainViewModel.VisualInvalidator = new ImageVisualInvalidator(mainWindow.TargetImage);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = mainWindow;
            }

            mainWindow.Loaded += (_, _) =>
            {
                //_ = mainViewModel.SourceViewTab.DefferedFill();
                //_ = mainViewModel.DestinationViewTab.DefferedFill();
                _ = PeriodicDebugLogging();
            };

            mainWindow.ThemeToggleButton.Click += (_, _) => ToggleTheme();

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

        if (mainWindow.SourceScrollViewer is not ScrollViewer sourceScroll)
            return;

        if (mainWindow.TargetScrollViewer is not ScrollViewer targetScroll)
            return;


        Debug.WriteLine(
            $"Periodic Debug Log - " +
            $"SourceZoom: {sourcePanel.ZoomX:F2}, " +
            //$"SourceOffsetX: {sourcePanel.OffsetX:F2}, " +
            //$"SourceOffsetY: {sourcePanel.OffsetY:F2}, " +
            $"SourceScrollX: {sourceScroll.Offset.X:F2}, " +
            $"SourceScrollY: {sourceScroll.Offset.Y:F2}, " +
            $"TargetZoom: {targetPanel.ZoomX:F2}, " +
            //$"TargetOffsetX: {targetPanel.OffsetX:F2}, " +
            //$"TargetOffsetY: {targetPanel.OffsetY:F2}"
            $"TargetScrollX: {targetScroll.Offset.X:F2}, " +
            $"TargetScrollY: {targetScroll.Offset.Y:F2}"
        );
            
        

        await Task.Delay(500);
        }
    }
    }
}