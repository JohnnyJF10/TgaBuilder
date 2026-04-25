using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using TgaBuilderAvaloniaUi.Elements;
using TgaBuilderAvaloniaUi.Services;
using TgaBuilderLib.ViewModel;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class BatchLoaderWindow : AsyncWindow
    {
        private Avalonia.Point _lastPanPosition;

        public BatchLoaderWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
            InitializeVisualInvalidator(viewModel);
            RegisterScrollViewScrollSpeedModification(BatchLoaderScrollViewer);
        }

        [Obsolete("For designer use only")]
        public BatchLoaderWindow()
        {
            var serviceProvider = GlobalServiceProvider.Instance;

            var vm = serviceProvider.GetRequiredService<BatchLoaderViewModel>()
                ?? throw new InvalidOperationException("BatchLoaderViewModel not found in DI container");
            InitializeComponent();
            base.DataContext = vm;
        }

        private void InitializeVisualInvalidator(INotifyPropertyChanged viewModel)
        {
            if (viewModel is BatchLoaderViewModel vm)
            {
                vm.VisualInvalidator = new VisualInvalidator(BatchLoadedImage);
                vm.ZoomBorderProxy = new ZoomBorderProxy(BatchLoaderZoomPanel);
            }
        }

        private void BatchLoaderZoomPanel_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.Properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                _lastPanPosition = e.GetPosition(BatchLoaderZoomPanel);
                e.Pointer.Capture(BatchLoaderZoomPanel);
                e.Handled = true;
            }
        }

        private void BatchLoaderZoomPanel_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (e.Properties.IsLeftButtonPressed
                && e.KeyModifiers.HasFlag(KeyModifiers.Control)
                && e.Pointer.Captured == BatchLoaderZoomPanel)
            {
                var pos = e.GetPosition(BatchLoaderZoomPanel);
                var delta = pos - _lastPanPosition;
                BatchLoaderZoomPanel.PanDelta(dx: delta.X, dy: delta.Y, skipTransitions: true);
                _lastPanPosition = pos;
                e.Handled = true;
            }
        }

        private void BatchLoaderZoomPanel_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.Pointer.Captured == BatchLoaderZoomPanel)
                e.Pointer.Capture(null);
        }

        private void ScrollViewer_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Alt))
                return;

            if (sender is ScrollViewer sv)
            {
                var delta = e.Delta.Y;

                double speedFactor = 150.0;

                if (delta > 0 && sv.Offset.Y < 0.00001)
                    delta = 0;

                if (delta < 0 && sv.Offset.Y > sv.ScrollBarMaximum.Y - 0.00001)
                    delta = 0;

                if (sv.Content is ZoomBorder zb)
                {
                    zb.Pan(
                        x: zb.OffsetX,
                        y: zb.OffsetY + delta * speedFactor,
                        skipTransitions: true);
                }

                e.Handled = true;
            }
        }

                public void RegisterScrollViewScrollSpeedModification(ScrollViewer scrollViewer)
        {
            scrollViewer.AddHandler(InputElement.PointerWheelChangedEvent, (sender, e) =>
            {
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    e.Handled = true;
                    return;
                }

                if (e.KeyModifiers.HasFlag(KeyModifiers.Control)
                    || e.KeyModifiers.HasFlag(KeyModifiers.Alt))
                    return;

                if (sender is ScrollViewer sv)
                {
                    var delta = e.Delta.Y;

                    double speedFactor = 150.0;

                    if (delta > 0 && sv.Offset.Y < 0.00001)
                        delta = 0;

                    if (delta < 0 && sv.Offset.Y > sv.ScrollBarMaximum.Y - 0.00001)
                        delta = 0;

                    if (sv.Content is ZoomBorder zb)
                    {
                        zb.Pan(
                            x: zb.OffsetX, 
                            y: zb.OffsetY + delta * speedFactor, 
                            skipTransitions: true);
                    }

                    e.Handled = true;
                }
            }, RoutingStrategies.Tunnel);
        }
    }
}
