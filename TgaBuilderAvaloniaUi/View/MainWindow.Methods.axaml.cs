
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using TgaBuilderLib.ViewModel;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class MainWindow
    {
        public ZoomBorder GetPanelFromImage(Image image)
        {
            if ((_imagePanelDict ??= new()).TryGetValue(image, out var panel))
                return panel;
            else
            {
                var FE = image.GetVisualParent();
                while (FE.GetType() != typeof(ZoomBorder))
                {
                    FE = FE.GetVisualParent();
                    if (FE == null) throw new Exception("ZoomBorder not found");
                }
                _imagePanelDict[image] = (ZoomBorder)FE;
                return (ZoomBorder)FE;
            }
        }

        public void SetPanelFromImage(Image image)
            => CurrentPanel = GetPanelFromImage(image);

        public void RegisterZoomBorderCallbacks(ReadOnlyViewTabViewModel viewTab, ZoomBorder panel)
        {
            viewTab.ApplyTransformCallback = (zoom, translateX, translateY) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var matrix = Matrix.CreateScale(zoom, zoom) *
                                 Matrix.CreateTranslation(translateX, translateY);
                    panel.SetMatrix(matrix);
                });
            };

            viewTab.PanStepCallback = (deltaX, deltaY) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    panel.SetMatrix(panel.Matrix * Matrix.CreateTranslation(deltaX, deltaY));
                });
            };

            viewTab.ZoomStepCallback = (zoomDelta) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var currentMatrix = panel.Matrix;
                    double centerX = panel.Bounds.Width / 2;
                    double centerY = panel.Bounds.Height / 2;

                    var matrix = currentMatrix *
                                 Matrix.CreateTranslation(-centerX, -centerY) *
                                 Matrix.CreateScale(zoomDelta, zoomDelta) *
                                 Matrix.CreateTranslation(centerX, centerY);
                    panel.SetMatrix(matrix);
                });
            };

            viewTab.ResetViewCallback = () =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    panel.ResetMatrix();
                });
            };
        }

        public void RegisterPresenterChangedCallback(
            TexturePanelViewModelBase panelVm,
            ZoomBorder zoomBorder,
            ScrollViewer scrollViewer)
        {
            panelVm.PresenterChangedCallback = () =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    zoomBorder.ResetMatrix();
                    scrollViewer.InvalidateMeasure();
                    scrollViewer.InvalidateArrange();
                });
            };
        }

        public void RegisterScrollViewScrollSpeedModification(ScrollViewer scrollViewer)
        {
            scrollViewer.AddHandler(InputElement.PointerWheelChangedEvent, (sender, e) =>
            {
                if (e.KeyModifiers.HasFlag(KeyModifiers.Control)
                    || e.KeyModifiers.HasFlag(KeyModifiers.Alt)
                    || e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    return;

                if (sender is ScrollViewer sv)
                {
                    var delta = e.Delta.Y;

                    double speedFactor = 3.0;

                    sv.Offset = new Vector(
                        sv.Offset.X,
                        sv.Offset.Y - delta * 50 * speedFactor
                    );

                    Window_PointerMoved(this, new PointerEventArgs(
                        routedEvent: InputElement.PointerMovedEvent,
                        source: sender,
                        pointer: e.GetCurrentPoint(sv).Pointer,
                        rootVisual: sv,
                        rootVisualPosition: e.GetCurrentPoint(sv).Position,
                        timestamp: e.Timestamp,
                        properties: e.GetCurrentPoint(sv).Properties,
                        modifiers: e.KeyModifiers));

                    e.Handled = true;
                }
            }, RoutingStrategies.Tunnel);
        }

        public void SourceFitButton_Click(object? sender, RoutedEventArgs e)
        {
            var zoom = SourcePanel.Bounds.Width / SourceImage.Bounds.Width;

            var centerX = SourceImage.Bounds.Width / 2;

            var centerY = SourceImage.Bounds.Height > SourcePanel.Bounds.Height
                ? SourcePanel.Bounds.Height / zoom / 2
                : SourceImage.Bounds.Height / 2;
            
            SourcePanel.CenterOn(new Point(centerX, centerY), zoom);
        }

        public void SourceFillButton_Click(object? sender, RoutedEventArgs e)
        {
            var zoom = SourcePanel.Bounds.Height / SourceImage.Bounds.Height;
           
            var centerX = SourceImage.Bounds.Width > SourcePanel.Bounds.Width
                ? SourcePanel.Bounds.Width / zoom / 2
                : SourceImage.Bounds.Width / 2;

            var centerY = SourceImage.Bounds.Height / 2;

            SourcePanel.CenterOn(new Point(centerX, centerY), zoom);
        }

        public void Source100Button_Click(object? sender, RoutedEventArgs e)
        {
            var centerX = SourceImage.Bounds.Width > SourcePanel.Bounds.Width
                ? SourcePanel.Bounds.Width / 2
                : SourceImage.Bounds.Width / 2;

            var centerY = SourceImage.Bounds.Height > SourcePanel.Bounds.Height
                ? SourcePanel.Bounds.Height / 2
                : SourceImage.Bounds.Height / 2;

            SourcePanel.CenterOn(new Point(centerX, centerY), 1);
        }

        public void TargetFitButton_Click(object? sender, RoutedEventArgs e)
        {
            var zoom = TargetPanel.Bounds.Width / TargetImage.Bounds.Width;

            var centerX = TargetImage.Bounds.Width / 2;

            var centerY = TargetImage.Bounds.Height > TargetPanel.Bounds.Height
                ? TargetPanel.Bounds.Height / zoom / 2
                : TargetImage.Bounds.Height / 2;
            
            TargetPanel.CenterOn(new Point(centerX, centerY), zoom);
        }

        public void TargetFillButton_Click(object? sender, RoutedEventArgs e)
        {
            var zoom = TargetPanel.Bounds.Height / TargetImage.Bounds.Height;

            var centerX = TargetImage.Bounds.Width > TargetPanel.Bounds.Width
                ? TargetPanel.Bounds.Width / zoom / 2
                : TargetImage.Bounds.Width / 2;

            var centerY = TargetImage.Bounds.Height / 2;

            TargetPanel.CenterOn(new Point(centerX, centerY), zoom);
        }

        public void Target100Button_Click(object? sender, RoutedEventArgs e)
        {
            var centerX = TargetImage.Bounds.Width > TargetPanel.Bounds.Width
                ? TargetPanel.Bounds.Width / 2
                : TargetImage.Bounds.Width / 2;

            var centerY = TargetImage.Bounds.Height > TargetPanel.Bounds.Height
                ? TargetPanel.Bounds.Height / 2
                : TargetImage.Bounds.Height / 2;

            TargetPanel.CenterOn(new Point(centerX, centerY), 1);
        }
    }
}
