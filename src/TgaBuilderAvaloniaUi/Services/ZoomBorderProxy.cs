using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Threading;
using System;
using TgaBuilderAvaloniaUi.View;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderAvaloniaUi.Services
{
    /// <summary>
    /// View-layer proxy that implements <see cref="IZoomBorderProxy"/> by
    /// forwarding every call to a concrete <see cref="ZoomBorder"/> on the
    /// Avalonia UI thread.
    /// </summary>
    public class ZoomBorderProxy : IZoomBorderProxy
    {
        private readonly ZoomBorder _zoomBorder;
        private readonly Action<double, double, ZoomBorder>? _invalidatePointerPositionCallback;

        public ZoomBorderProxy(ZoomBorder zoomBorder, Action<double, double, ZoomBorder>? invalidatePointerPositionCallback = null)
        {
            _zoomBorder = zoomBorder;
            _invalidatePointerPositionCallback = invalidatePointerPositionCallback;
        }

        public void ApplyTransform(double zoom, double translateX, double translateY)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var matrix = Matrix.CreateScale(zoom, zoom) *
                             Matrix.CreateTranslation(translateX, translateY);
                _zoomBorder.SetMatrix(matrix, skipTransitions: true);
            });
        }

        public void CenterOn(double centerX, double centerY, double zoom)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _zoomBorder.CenterOn(new Point(centerX, centerY), zoom);
            });
        }

        public void PanStep(double deltaX, double deltaY)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _zoomBorder.PanDelta(
                    dx: deltaX,
                    dy: deltaY,
                    skipTransitions: true);
            });
            _invalidatePointerPositionCallback?.Invoke(deltaX, deltaY, _zoomBorder);
        }

        public void ZoomStep(double zoomDelta)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var currentMatrix = _zoomBorder.Matrix;
                double centerX = _zoomBorder.Bounds.Width / 2;
                double centerY = _zoomBorder.Bounds.Height / 2;

                var matrix = currentMatrix *
                             Matrix.CreateTranslation(-centerX, -centerY) *
                             Matrix.CreateScale(zoomDelta, zoomDelta) *
                             Matrix.CreateTranslation(centerX, centerY);
                _zoomBorder.SetMatrix(matrix);
            });
        }

        public void ResetView()
        {
            Dispatcher.UIThread.Post(() =>
            {
                _zoomBorder.ResetMatrix();
            });
        }
    }
}
