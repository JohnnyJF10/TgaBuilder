namespace WPFZoomPanel.Enums
{
    /// <summary>
    /// Defines the current state of the mouse handling logic.
    /// </summary>
    internal enum MouseHandlingMode
    {
        /// <summary>
        /// Not in any special mode.
        /// </summary>
        None,

        /// <summary>
        /// The user is holding down shift and left-clicking or right-clicking
        /// to zoom in or out.
        /// </summary>
        ClickZooming,


        /// <summary>
        /// The user is left-mouse-button-dragging to pan the viewport.
        /// </summary>
        DragPanning,

        /// <summary>
        /// The user is holding down shift and left-mouse-button-dragging to
        /// select a region to zoom to.
        /// </summary>
        DragZooming,
    }
}
