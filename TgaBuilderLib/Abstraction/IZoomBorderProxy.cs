namespace TgaBuilderLib.Abstraction
{
    /// <summary>
    /// Abstraction over the Avalonia ZoomBorder control, allowing the ViewModel
    /// to invoke zoom/pan operations without referencing any view-layer types.
    /// </summary>
    public interface IZoomBorderProxy
    {
        /// <summary>
        /// Apply a full view transformation (zoom + translate) via a matrix.
        /// </summary>
        void ApplyTransform(double zoom, double translateX, double translateY);

        /// <summary>
        /// Center the viewport on a given content point at a given zoom level.
        /// </summary>
        void CenterOn(double centerX, double centerY, double zoom);

        /// <summary>
        /// Apply an incremental pan step in screen-space coordinates.
        /// </summary>
        void PanStep(double deltaX, double deltaY);

        /// <summary>
        /// Apply a zoom step at the center of the viewport.
        /// Values greater than 1 zoom in, less than 1 zoom out.
        /// </summary>
        void ZoomStep(double zoomDelta);

        /// <summary>
        /// Reset the view to its initial state.
        /// </summary>
        void ResetView();
    }
}
