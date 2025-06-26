using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFZoomPanel.Commands;
using WPFZoomPanel.Enums;
using WPFZoomPanel.Events;
using WPFZoomPanel.Helpers;

namespace WPFZoomPanel
{
    public partial class ZoomPanel
    {
        #region Private Fields

        private RelayCommand? _fillCommand;

        private RelayCommand? _fitCommand;

        /// <summary>
        /// Records which mouse button clicked during mouse dragging.
        /// </summary>
        private MouseButton _mouseButtonDown;

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
        /// The point that was clicked relative to the ZoomAndPanControl.
        /// </summary>
        private Point _origZoomAndPanControlMouseDownPoint;

        /// <summary>
        /// The control for creating a zoom border
        /// </summary>
        private Border _partDragZoomBorder = new();

        /// <summary>
        /// The control for containing a zoom border
        /// </summary>
        private Canvas _partDragZoomCanvas = new();

        private RelayCommand? _zoomInCommand;

        private RelayCommand? _zoomOutCommand;

        private RelayCommand<double>? _zoomPercentCommand;

        private RelayCommand<double>? _zoomRatioFromMinimumCommand;

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Command to implement the zoom to fill
        /// </summary>
        public ICommand FillCommand => _fillCommand ?? (_fillCommand = new RelayCommand(() =>
        {
            SaveZoom();
            AnimatedZoomToCentered(FillZoomValue);
            RaiseCanExecuteChanged();
        },
            () => !InternalViewportZoom.IsWithinOnePercent(FillZoomValue) && FillZoomValue >= MinimumZoomClamped));

        /// <summary>
        /// Command to implement the zoom to fit
        /// </summary>
        public ICommand FitCommand => _fitCommand ?? (_fitCommand = new RelayCommand(() =>
        {
            SaveZoom();
            AnimatedZoomTo(FitZoomValue);
            RaiseCanExecuteChanged();
        }, 
            () => !InternalViewportZoom.IsWithinOnePercent(FitZoomValue) && FitZoomValue >= MinimumZoomClamped));

        /// <summary>
        /// Command to implement the zoom in by 91%
        /// </summary>
        public ICommand ZoomInCommand => _zoomInCommand ?? (_zoomInCommand = new RelayCommand(() =>
        {
            DelayedSaveZoom1500Miliseconds();
            ZoomIn(new Point(ContentZoomFocusX, ContentZoomFocusY));
        },
            () => InternalViewportZoom < MaximumZoom));

        /// <summary>
        /// Command to implement the zoom out by 110%
        /// </summary>
        public ICommand ZoomOutCommand => _zoomOutCommand ?? (_zoomOutCommand = new RelayCommand(() =>
        {
            DelayedSaveZoom1500Miliseconds();
            ZoomOut(new Point(ContentZoomFocusX, ContentZoomFocusY));
        }, 
            () => InternalViewportZoom > MinimumZoomClamped));

        /// <summary>
        /// Command to implement the zoom to a percentage where 100 (100%) is
        /// the default and shows the image at a zoom where 1 pixel is 1 pixel.
        /// Other percentages specified with the command parameter. 50 (i.e.
        /// 50%) would display 4 times as much of the image
        /// </summary>
        public ICommand ZoomPercentCommand
            => _zoomPercentCommand ?? (_zoomPercentCommand = new RelayCommand<double>(value =>
            {
                SaveZoom();
                double adjustedValue = value == 0 ? 1 : value / 100;
                AnimatedZoomTo(adjustedValue);
                RaiseCanExecuteChanged();
            }, value =>
            {
                double adjustedValue = value == 0 ? 1 : value / 100;
                return !InternalViewportZoom.IsWithinOnePercent(adjustedValue) && adjustedValue >= MinimumZoomClamped;
            }));

        // Math.Abs(InternalViewportZoom - ((value == 0) ? 1.0 : value / 100)) >
        // .01 * InternalViewportZoom
        /// <summary>
        /// Command to implement the zoom ratio where 1 is is the the specified
        /// minimum. 2 make the image twices the size, and is the default. Other
        /// values are specified with the CommandParameter.
        /// </summary>
        public ICommand ZoomRatioFromMinimumCommand
            => _zoomRatioFromMinimumCommand ?? (_zoomRatioFromMinimumCommand = new RelayCommand<double>(value =>
            {
                SaveZoom();
                double adjustedValue = (value == 0 ? 2 : value) * MinimumZoomClamped;
                AnimatedZoomTo(adjustedValue);
                RaiseCanExecuteChanged();
            }, value =>
            {
                double adjustedValue = (value == 0 ? 2 : value) * MinimumZoomClamped;
                return !InternalViewportZoom.IsWithinOnePercent(adjustedValue) && adjustedValue >= MinimumZoomClamped;
            }));

        #endregion Public Properties

        #region Protected Methods

        /// <summary>
        /// When content is renewed, set event to set the initial position as specified
        /// </summary>
        /// <param name="oldContent"></param>
        /// <param name="newContent"></param>
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            if (oldContent != null) { ((FrameworkElement)oldContent).SizeChanged -= SetZoomAndPanInitialPosition; }
            if (newContent != null) { ((FrameworkElement)newContent).SizeChanged += SetZoomAndPanInitialPosition; }
        }

        /// <summary>
        /// Event raised on mouse down in the ZoomAndPanControl.
        /// </summary>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            SaveZoom();
            _ = _content.Focus();
            _ = Keyboard.Focus(_content);

            _mouseButtonDown = e.ChangedButton;
            _origZoomAndPanControlMouseDownPoint = e.GetPosition(this);
            _origContentMouseDownPoint = e.GetPosition(_content);

            if (e.ClickCount == 2)
            {
                if (UseDoubleClickSnapping && (Keyboard.Modifiers & MouseDragZoomModifier) == 0)
                {
                    //AnimatedSnapTo(e.GetPosition(_content));
                    if (UseAnimations) { AnimatedSnapTo(e.GetPosition(_content)); }
                    else { SnapTo(e.GetPosition(_content)); }

                }
            }
            else
            {
                if ((Keyboard.Modifiers == MouseDragZoomModifier) && (_mouseButtonDown == MouseButton.Left || _mouseButtonDown == MouseButton.Right))
                {
                    // Shift + left- or right-down initiates zooming mode.
                    _mouseHandlingMode = MouseHandlingMode.ClickZooming;
                }
                else if ((Keyboard.Modifiers == MousePanModifier) && (_mouseButtonDown == MouseButton.Left))
                {
                    // Just a plain old left-down initiates panning mode.
                    _mouseHandlingMode = MouseHandlingMode.DragPanning;
                }

                if (_mouseHandlingMode != MouseHandlingMode.None)
                {
                    // Capture the mouse so that we eventually receive the mouse up event.
                    if (e.MouseDevice.Captured == null)
                    {
                        _ = CaptureMouse();
                    }
                }
            }
        }

        /// <summary>
        /// Event raised on mouse move in the ZoomAndPanControl.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Point oldContentMousePoint = MousePosition;
            Point curContentMousePoint = e.GetPosition(_content);
            MousePosition = curContentMousePoint.FilterClamp(_content.ActualWidth - 1, _content.ActualHeight - 1);
            OnPropertyChanged(new DependencyPropertyChangedEventArgs(MousePositionProperty, oldContentMousePoint, curContentMousePoint));
            if (_mouseHandlingMode == MouseHandlingMode.DragPanning)
            {
                // The user is left-dragging the mouse. Pan the viewport by the
                // appropriate amount.
                Vector dragOffset = curContentMousePoint - _origContentMouseDownPoint;

                ContentOffsetX -= dragOffset.X;
                ContentOffsetY -= dragOffset.Y;

                e.Handled = true;
            }
            else if (_mouseHandlingMode == MouseHandlingMode.ClickZooming)
            {
                Point curZoomAndPanControlMousePoint = e.GetPosition(this);
                Vector dragOffset = curZoomAndPanControlMousePoint - _origZoomAndPanControlMouseDownPoint;
                double dragThreshold = 10;
                if (_mouseButtonDown == MouseButton.Left &&
                    (Math.Abs(dragOffset.X) > dragThreshold ||
                     Math.Abs(dragOffset.Y) > dragThreshold))
                {
                    // When Shift + left-down zooming mode and the user drags
                    // beyond the drag threshold, initiate drag zooming mode
                    // where the user can drag out a rectangle to select the
                    // area to zoom in on.
                    _mouseHandlingMode = MouseHandlingMode.DragZooming;
                    InitDragZoomRect(_origContentMouseDownPoint, curContentMousePoint);
                }
            }
            else if (_mouseHandlingMode == MouseHandlingMode.DragZooming)
            {
                // When in drag zooming mode continously update the position of
                // the rectangle that the user is dragging out.
                curContentMousePoint = e.GetPosition(this);
                SetDragZoomRect(_origZoomAndPanControlMouseDownPoint, curContentMousePoint);
            }
        }

        /// <summary>
        /// Event raised on mouse up in the ZoomAndPanControl.
        /// </summary>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (_mouseHandlingMode != MouseHandlingMode.None)
            {
                if (_mouseHandlingMode == MouseHandlingMode.ClickZooming)
                {
                    if (_mouseButtonDown == MouseButton.Left)
                    {
                        // Shift + left-click zooms in on the content.
                        ZoomIn(_origContentMouseDownPoint);
                    }
                    else if (_mouseButtonDown == MouseButton.Right)
                    {
                        // Shift + left-click zooms out from the content.
                        ZoomOut(_origContentMouseDownPoint);
                    }
                }
                else if (_mouseHandlingMode == MouseHandlingMode.DragZooming)
                {
                    Point finalContentMousePoint = e.GetPosition(_content);
                    // When drag-zooming has finished we zoom in on the
                    // rectangle that was highlighted by the user.
                    ApplyDragZoomRect(finalContentMousePoint);
                }

                _mouseHandlingMode = MouseHandlingMode.None;
            }
            if (e.MouseDevice.Captured != null) { ReleaseMouseCapture(); }
        }

        /// <summary>
        /// Event raised on mouse wheel moved in the ZoomAndPanControl.
        /// </summary>
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            DelayedSaveZoom750Miliseconds();
            e.Handled = true;

            if (Keyboard.Modifiers == ScrollZoomModifier)
            {
                if (e.Delta > 0) { ZoomIn(e.GetPosition(_content)); }
                if (e.Delta < 0) { ZoomOut(e.GetPosition(_content)); }
            }
            if (Keyboard.Modifiers == ScrollLeftRightModifier)
            {
                ContentOffsetX -= e.Delta / InternalViewportZoom;
            }

            if (Keyboard.Modifiers == ScrollUpDownModifier)
            {
                ContentOffsetY -= e.Delta / InternalViewportZoom;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// When the user has finished dragging out the rectangle the zoom
        /// operation is applied.
        /// </summary>
        private void ApplyDragZoomRect(Point finalContentMousePoint)
        {
            Rect rect = ViewporTgaBuilders.Clip(finalContentMousePoint, _origContentMouseDownPoint, new Point(0, 0),
                new Point(_partDragZoomCanvas.ActualWidth, _partDragZoomCanvas.ActualHeight));
            AnimatedZoomTo(rect);
            // new Rect(contentX, contentY, contentWidth, contentHeight));
            FadeOutDragZoomRect();
        }

        // Fade out the drag zoom rectangle.
        private void FadeOutDragZoomRect()
        {
            AnimationHelper.StartAnimation(_partDragZoomBorder, OpacityProperty, 0.0, 0.1,
                delegate { _partDragZoomCanvas.Visibility = Visibility.Collapsed; }, UseAnimations);
        }

        /// <summary>
        /// Initialise the rectangle that the use is dragging out.
        /// </summary>
        private void InitDragZoomRect(Point pt1, Point pt2)
        {
            _partDragZoomCanvas.Visibility = Visibility.Visible;
            _partDragZoomBorder.Opacity = 1;
            SetDragZoomRect(pt1, pt2);
        }

        /// <summary>
        /// Scroll the view horizontally when mouse tilt/horizontal scrol wheel
        /// is used
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseHorizontalWheel(object sender, MouseHorizontalWheelEventArgs e) => ContentOffsetX += e.HorizontalDelta / InternalViewportZoom;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void RaiseCanExecuteChanged()
        {
            _zoomPercentCommand?.RaiseCanExecuteChanged();
            _zoomOutCommand?.RaiseCanExecuteChanged();
            _zoomInCommand?.RaiseCanExecuteChanged();
            _fitCommand?.RaiseCanExecuteChanged();
            _fillCommand?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Update the position and size of the rectangle that user is dragging out.
        /// </summary>
        private void SetDragZoomRect(Point pt1, Point pt2)
        {
            // Update the coordinates of the rectangle that is being dragged out
            // by the user. The we offset and rescale to convert from content coordinates.
            Rect rect = ViewporTgaBuilders.Clip(pt1, pt2, new Point(0, 0),
                new Point(_partDragZoomCanvas.ActualWidth, _partDragZoomCanvas.ActualHeight));
            ViewporTgaBuilders.PositionBorderOnCanvas(_partDragZoomBorder, rect);
        }

        /// <summary>
        /// When content is renewed, set the initial position as specified
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetZoomAndPanInitialPosition(object sender, SizeChangedEventArgs e)
        {
            switch (ZoomAndPanInitialPosition)
            {
                case ZoomPanelInitialPosition.Default:
                    break;

                case ZoomPanelInitialPosition.FitScreen:
                    InternalViewportZoom = FitZoomValue;
                    break;

                case ZoomPanelInitialPosition.FillScreen:
                    InternalViewportZoom = FillZoomValue;
                    ContentOffsetX = (_content.ActualWidth - ViewportWidth / InternalViewportZoom) / 2;
                    ContentOffsetY = (_content.ActualHeight - ViewportHeight / InternalViewportZoom) / 2;
                    break;

                case ZoomPanelInitialPosition.OneHundredPercentCentered:
                    InternalViewportZoom = 1.0;
                    ContentOffsetX = (_content.ActualWidth - ViewportWidth) / 2;
                    ContentOffsetY = (_content.ActualHeight - ViewportHeight) / 2;
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"{e.NewSize} is out of range");
            }
        }

        private void ZoomAndPanControl_EventHandlers_OnApplyTemplate()
        {
            _partDragZoomBorder = (Template.FindName("PART_DragZoomBorder", this) as Border) ?? new();
            _partDragZoomCanvas = (Template.FindName("PART_DragZoomCanvas", this) as Canvas) ?? new();
        }

        /// <summary>
        /// Zoom the viewport in, centering on the specified point (in content coordinates).
        /// </summary>
        private void ZoomIn(Point contentZoomCenter) => ZoomAboutPoint(InternalViewportZoom * 1.1, contentZoomCenter);

        /// <summary>
        /// Zoom the viewport out, centering on the specified point (in content coordinates).
        /// </summary>
        private void ZoomOut(Point contentZoomCenter) => ZoomAboutPoint(InternalViewportZoom * 0.90909090909, contentZoomCenter);

        #endregion Private Methods
    }
}