using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPFZoomPanel.Enums;
using WPFZoomPanel.Helpers;

namespace WPFZoomPanel
{
    /// <summary>
    /// A class that wraps up zooming and panning of it's content.
    /// </summary>
    public class ZoomPanelMinimap : ContentControl
    {
        #region Public Fields

        public static readonly DependencyProperty VisualProperty = DependencyProperty.Register("Visual",
                    typeof(FrameworkElement), typeof(ZoomPanelMinimap), new FrameworkPropertyMetadata(null, OnVisualChanged));

        #endregion Public Fields

        #region Private Fields

        /// <summary>
        /// The control for creating a drag border
        /// </summary>
        private Border _dragBorder = new();

        /// <summary>
        /// Specifies the current state of the mouse handling logic.
        /// </summary>
        private MouseHandlingMode _mouseHandlingMode = MouseHandlingMode.None;

        /// <summary>
        /// The point that was clicked relative to the content that is contained
        /// within the ZoomAndPanControl.
        /// </summary>
        private Point _origContentMouseDownPoint;

        /// <summary>
        /// The control for creating a drag border
        /// </summary>
        private Border _sizingBorder = new();

        /// <summary>
        /// The control for containing a zoom border
        /// </summary>
        private Canvas _viewportCanvas = new();

        #endregion Private Fields

        #region Public Constructors + Destructors

        /// <summary>
        /// Static constructor to define metadata for the control (and link it
        /// to the style in Generic.xaml).
        /// </summary>
        static ZoomPanelMinimap() => DefaultStyleKeyProperty.OverrideMetadata(typeof(ZoomPanelMinimap), new FrameworkPropertyMetadata(typeof(ZoomPanelMinimap)));

        #endregion Public Constructors + Destructors

        #region Public Properties

        /// <summary>
        /// The X coordinate of the content focus, this is the point that we are
        /// focusing on when zooming.
        /// </summary>
        public FrameworkElement Visual
        {
            get => (FrameworkElement)GetValue(VisualProperty);
            set => SetValue(VisualProperty, value);
        }

        #endregion Public Properties

        #region Public Methods

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _dragBorder     = (Template.FindName("PART_DraggingBorder", this) as Border) ?? new();
            _sizingBorder   = (Template.FindName("PART_SizingBorder", this) as Border) ?? new();
            _viewportCanvas = (Template.FindName("PART_Content", this) as Canvas) ?? new();
            SetBackground(Visual);
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Event raised with the double click command
        /// </summary>
        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                ZoomPanel zoomAndPanControl = GetZoomAndPanControl();
                zoomAndPanControl.SaveZoom();
                zoomAndPanControl.AnimatedSnapTo(e.GetPosition(_viewportCanvas));
            }
        }

        /// <summary>
        /// Event raised on mouse down in the ZoomAndPanControl.
        /// </summary>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            GetZoomAndPanControl().SaveZoom();
            _mouseHandlingMode = MouseHandlingMode.DragPanning;
            _origContentMouseDownPoint = e.GetPosition(_viewportCanvas);

            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                // Shift + left- or right-down initiates zooming mode.
                _mouseHandlingMode = MouseHandlingMode.DragZooming;
                _dragBorder.Visibility = Visibility.Hidden;
                _sizingBorder.Visibility = Visibility.Visible;
                Canvas.SetLeft(_sizingBorder, _origContentMouseDownPoint.X);
                Canvas.SetTop(_sizingBorder, _origContentMouseDownPoint.Y);
                _sizingBorder.Width = 0;
                _sizingBorder.Height = 0;
            }
            else
            {
                // Just a plain old left-down initiates panning mode.
                _mouseHandlingMode = MouseHandlingMode.DragPanning;
            }

            if (_mouseHandlingMode != MouseHandlingMode.None)
            {
                // Capture the mouse so that we eventually receive the mouse up event.
                _viewportCanvas.CaptureMouse();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Event raised on mouse move in the ZoomAndPanControl.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_mouseHandlingMode == MouseHandlingMode.DragPanning)
            {
                Point curContentPoint = e.GetPosition(_viewportCanvas);
                Vector rectangleDragVector = curContentPoint - _origContentMouseDownPoint;
                // When in 'dragging rectangles' mode update the position of the
                // rectangle as the user drags it.
                _origContentMouseDownPoint = e.GetPosition(_viewportCanvas).Clamp();
                Canvas.SetLeft(_dragBorder, Canvas.GetLeft(_dragBorder) + rectangleDragVector.X);
                Canvas.SetTop(_dragBorder, Canvas.GetTop(_dragBorder) + rectangleDragVector.Y);
            }
            else if (_mouseHandlingMode == MouseHandlingMode.DragZooming)
            {
                Point curContentPoint = e.GetPosition(_viewportCanvas);
                Rect rect = ViewportHelpers.Clip(curContentPoint, _origContentMouseDownPoint, new Point(0, 0), new Point(_viewportCanvas.Width, _viewportCanvas.Height));
                ViewportHelpers.PositionBorderOnCanvas(_sizingBorder, rect);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Event raised on mouse up in the ZoomAndPanControl.
        /// </summary>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (_mouseHandlingMode == MouseHandlingMode.DragZooming)
            {
                ZoomPanel zoomAndPanControl = GetZoomAndPanControl();
                Point curContentPoint = e.GetPosition(_viewportCanvas);
                Rect rect = ViewportHelpers.Clip(curContentPoint, _origContentMouseDownPoint, new Point(0, 0),
                    new Point(_viewportCanvas.Width, _viewportCanvas.Height));
                zoomAndPanControl.AnimatedZoomTo(rect);
                _dragBorder.Visibility = Visibility.Visible;
                _sizingBorder.Visibility = Visibility.Hidden;
            }
            _mouseHandlingMode = MouseHandlingMode.None;
            _viewportCanvas.ReleaseMouseCapture();
            e.Handled = true;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (ActualWidth > 0 && _viewportCanvas != null)
            {
                _sizingBorder.BorderThickness = _dragBorder.BorderThickness = new Thickness(
                   _viewportCanvas.ActualWidth / ActualWidth * BorderThickness.Left,
                   _viewportCanvas.ActualWidth / ActualWidth * BorderThickness.Top,
                   _viewportCanvas.ActualWidth / ActualWidth * BorderThickness.Right,
                   _viewportCanvas.ActualWidth / ActualWidth * BorderThickness.Bottom);
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is not FrameworkElement element) return;

            ZoomPanelMinimap c = (ZoomPanelMinimap)d;
            c.SetBackground(element);
        }

        private ZoomPanel GetZoomAndPanControl()
        {
            ZoomPanel zoomAndPanControl = (DataContext as ZoomPanel) 
                ?? (DataContext as ZoomPanelScrollViewer)?.ZoomAndPanContent 
                ?? new();
            if (zoomAndPanControl == null)
            {
                throw new NullReferenceException("DataContext is not of type ZoomAndPanControl");
            }

            return zoomAndPanControl;
        }

        private void SetBackground(FrameworkElement frameworkElement)
        {
            frameworkElement = frameworkElement ?? (DataContext as ContentControl)?.Content as FrameworkElement ?? new();
            VisualBrush visualBrush = new VisualBrush
            {
                Visual = frameworkElement,
                ViewboxUnits = BrushMappingMode.RelativeToBoundingBox,
                ViewportUnits = BrushMappingMode.RelativeToBoundingBox,
                TileMode = TileMode.None,
                Stretch = Stretch.Fill
            };

            if (frameworkElement != null)
            {
                frameworkElement.SizeChanged += (s, e) =>
            {
                _viewportCanvas.Height = frameworkElement.ActualHeight;
                _viewportCanvas.Width = frameworkElement.ActualWidth;
                _viewportCanvas.Background = visualBrush;
            };
            }
        }

        #endregion Private Methods
    }
}
