
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Diagnostics;
using System.Windows.Input;
using TgaBuilderAvaloniaUi.AttachedProperties;
using TgaBuilderAvaloniaUi.Services;
using TgaBuilderLib.Enums;
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
            viewTab.ZoomBorderProxy = new ZoomBorderProxy(panel, InvalidatePointerPosition);
        }

        public void InvalidatePointerPosition(double dx, double dy, ZoomBorder panel)
        {
            bool isDestination = string.Equals(panel.Name, "TargetPanel");
            Image curImage = isDestination ? TargetImage : SourceImage;

            int newPixX, newPixY;
            int signX = dx > 0 ? -1 : 1;
            int signY = dy > 0 ? -1 : 1;

            newPixX = dx == 0
                ? (int)_lastPointerPosition.X
                : (int)(((curImage.Bounds.Width + (signX * panel.Bounds.Width)) * 0.5 - panel.OffsetX) / panel.ZoomX);

            newPixY = dy == 0
                ? (int)_lastPointerPosition.Y
                : (int)(((curImage.Bounds.Height + (signY * panel.Bounds.Height)) * 0.5 - panel.OffsetY) / panel.ZoomY);
            
            if (PanelMouseAP.GetPanelMouseCommand(this) is ICommand mousePanelCommand)
                mousePanelCommand.Execute((newPixX, newPixY, isDestination, MouseAction.Move, _modifier));
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
    }
}
