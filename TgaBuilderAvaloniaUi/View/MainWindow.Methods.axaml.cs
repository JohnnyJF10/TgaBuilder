
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
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
    }
}
