using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using WPFZoomPanel.Enums;
using WPFZoomPanel.Events;
using WPFZoomPanel.Helpers;

namespace WPFZoomPanel
{
    /// <summary>
    /// A class that wraps up zooming and panning of it's content.
    /// </summary>
    public partial class ZoomPanel : ContentControl, IScrollInfo, INotifyPropertyChanged
    {
        #region Public Fields

        public static readonly DependencyProperty AnimationDurationProperty = DependencyProperty.Register("AnimationDuration", typeof(double), typeof(ZoomPanel), new FrameworkPropertyMetadata(0.4));

        public static readonly DependencyProperty ContentOffsetXProperty = DependencyProperty.Register("ContentOffsetX", typeof(double), typeof(ZoomPanel), new FrameworkPropertyMetadata(0.0, ContentOffsetX_PropertyChanged, ContentOffsetX_Coerce));

        public static readonly DependencyProperty ContentOffsetYProperty = DependencyProperty.Register("ContentOffsetY", typeof(double), typeof(ZoomPanel), new FrameworkPropertyMetadata(0.0, ContentOffsetY_PropertyChanged, ContentOffsetY_Coerce));

        public static readonly DependencyProperty ContentViewportHeightProperty = DependencyProperty.Register("ContentViewportHeight", typeof(double), typeof(ZoomPanel), new FrameworkPropertyMetadata(0.0));

        public static readonly DependencyProperty ContentViewportWidthProperty = DependencyProperty.Register("ContentViewportWidth", typeof(double), typeof(ZoomPanel), new FrameworkPropertyMetadata(0.0));

        public static readonly DependencyProperty ContentZoomFocusXProperty = DependencyProperty.Register("ContentZoomFocusX", typeof(double), typeof(ZoomPanel), new FrameworkPropertyMetadata(0.0));

        public static readonly DependencyProperty ContentZoomFocusYProperty = DependencyProperty.Register("ContentZoomFocusY", typeof(double), typeof(ZoomPanel), new FrameworkPropertyMetadata(0.0));

        public static readonly DependencyProperty IsMouseWheelScrollingEnabledProperty = DependencyProperty.Register("IsMouseWheelScrollingEnabled", typeof(bool), typeof(ZoomPanel), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty MaximumZoomProperty = DependencyProperty.Register("MaximumZoom", typeof(double), typeof(ZoomPanel), new FrameworkPropertyMetadata(10.0, MinimumOrMaximumZoom_PropertyChanged));

        public static readonly DependencyProperty MinimumZoomProperty = DependencyProperty.Register("MinimumZoom", typeof(double), typeof(ZoomPanel), new FrameworkPropertyMetadata(0.1, MinimumOrMaximumZoom_PropertyChanged));

        public static readonly DependencyProperty MinimumZoomTypeProperty = DependencyProperty.Register("MinimumZoomType", typeof(MinimumZoomType), typeof(ZoomPanel), new FrameworkPropertyMetadata(MinimumZoomType.MinimumZoom));

        public static readonly DependencyProperty MousePositionProperty = DependencyProperty.Register("MousePosition", typeof(Point), typeof(ZoomPanel), new FrameworkPropertyMetadata(new Point(), MinimumOrMaximumZoom_PropertyChanged));

        // Added by jonson
        public static readonly DependencyProperty MousePanModifierProperty = DependencyProperty.Register("MousePanModifier", typeof(ModifierKeys), typeof(ZoomPanel), new FrameworkPropertyMetadata(ModifierKeys.Control));

        // Added by Moravuscz
        public static readonly DependencyProperty MouseDragZoomModifierProperty = DependencyProperty.Register("MouseDragZoomModifier", typeof(ModifierKeys), typeof(ZoomPanel), new FrameworkPropertyMetadata(ModifierKeys.Shift));

        // Added by Moravuscz
        public static readonly DependencyProperty ScrollLeftRightModifierProperty = DependencyProperty.Register("ScrollLeftRightModifier", typeof(ModifierKeys), typeof(ZoomPanel), new FrameworkPropertyMetadata(ModifierKeys.Shift));

        // Added by Moravuscz
        public static readonly DependencyProperty ScrollUpDownModifierProperty = DependencyProperty.Register("ScrollUpDownModifier", typeof(ModifierKeys), typeof(ZoomPanel), new FrameworkPropertyMetadata(ModifierKeys.None));

        // Added by Moravuscz
        public static readonly DependencyProperty ScrollZoomModifierProperty = DependencyProperty.Register("ScrollZoomModifier", typeof(ModifierKeys), typeof(ZoomPanel), new FrameworkPropertyMetadata(ModifierKeys.Control));

        // Added by jonson
        public static readonly DependencyProperty UseDoubleClickSnappingProperty = DependencyProperty.Register("UseDoubleClickSnapping", typeof(bool), typeof(ZoomPanel), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty UseAnimationsProperty = DependencyProperty.Register("UseAnimations", typeof(bool), typeof(ZoomPanel), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty ViewportZoomFocusXProperty = DependencyProperty.Register("ViewportZoomFocusX", typeof(double), typeof(ZoomPanel), new FrameworkPropertyMetadata(0.0));

        public static readonly DependencyProperty ViewportZoomFocusYProperty = DependencyProperty.Register("ViewportZoomFocusY", typeof(double), typeof(ZoomPanel), new FrameworkPropertyMetadata(0.0));

        public static readonly DependencyProperty ViewportZoomProperty = DependencyProperty.Register("ViewportZoom", typeof(double), typeof(ZoomPanel), new FrameworkPropertyMetadata(1.0, ViewportZoom_PropertyChanged));

        public static readonly DependencyProperty ZoomAndPanInitialPositionProperty = DependencyProperty.Register("ZoomAndPanInitialPosition", typeof(ZoomPanelInitialPosition), typeof(ZoomPanel), new FrameworkPropertyMetadata(ZoomPanelInitialPosition.Default, ZoomAndPanInitialPositionChanged));

        #endregion Public Fields

        #region Private Fields

        private static readonly DependencyProperty _internalViewportZoomProperty = DependencyProperty.Register("InternalViewportZoom", typeof(double), typeof(ZoomPanel), new FrameworkPropertyMetadata(1.0, InternalViewportZoom_PropertyChanged, InternalViewportZoom_Coerce));
        /// <summary>
        /// The height of the viewport in content coordinates, clamped to the
        /// height of the content.
        /// </summary>
        private double _constrainedContentViewportHeight = 0.0;

        /// <summary>
        /// The width of the viewport in content coordinates, clamped to the
        /// width of the content.
        /// </summary>
        private double _constrainedContentViewportWidth = 0.0;

        /// <summary>
        /// Reference to the underlying content, which is named PART_Content in
        /// the template.
        /// </summary>
        private FrameworkElement _content = new();

        /// <summary>
        /// The transform that is applied to the content to offset it by
        /// 'ContentOffsetX' and 'ContentOffsetY'.
        /// </summary>
        private TranslateTransform _contentOffsetTransform = new();

        /// <summary>
        /// The transform that is applied to the content to scale it by 'ViewportZoom'.
        /// </summary>
        private ScaleTransform _contentZoomTransform = new();

        private CurrentZoomTypeEnum _currentZoomTypeEnum;
        /// <summary>
        /// Normally when content offsets changes the content focus is
        /// automatically updated. This syncronization is disabled when
        /// 'disableContentFocusSync' is set to 'true'. When we are zooming in
        /// or out we 'disableContentFocusSync' is set to 'true' because we are
        /// zooming in or out relative to the content focus we don't want to
        /// update the focus.
        /// </summary>
        private bool _disableContentFocusSync = false;

        /// <summary>
        /// Used to disable syncronization between IScrollInfo interface and ContentOffsetX/ContentOffsetY.
        /// </summary>
        private bool _disableScrollOffsetSync = false;

        /// <summary>
        /// Enable the update of the content offset as the content scale
        /// changes. This enabled for zooming about a point (google-maps style
        /// zooming) and zooming to a rect.
        /// </summary>
        private bool _enableContentOffsetUpdateFromScale = false;

        /// <summary>
        /// Records the unscaled extent of the content. This is calculated
        /// during the measure and arrange.
        /// </summary>
        private Size _unScaledExtent = new Size(0, 0);

        // These data members are for the implementation of the IScrollInfo
        // interface. This interface works with the ScrollViewer such that when
        // ZoomAndPanControl is wrapped (in XAML) with a ScrollViewer the
        // IScrollInfo interface allows the ZoomAndPanControl to handle the the
        // scrollbar offsets.
        //
        // The IScrollInfo properties and member functions are implemented in ZoomAndPanControl_IScrollInfo.cs.
        //
        // There is a good series of articles showing how to implement
        // IScrollInfo starting here: http://blogs.msdn.com/bencon/archive/2006/01/05/509991.aspx
        /// <summary>
        /// Records the size of the viewport (in viewport coordinates) onto the
        /// content. This is calculated during the measure and arrange.
        /// </summary>
        private Size _viewport = new Size(0, 0);

        private bool _wndHooked = false;

        #endregion Private Fields

        #region Public Constructors + Destructors

        /// <summary>
        /// Static constructor to define metadata for the control (and link it
        /// to the style in Generic.xaml).
        /// </summary>
        static ZoomPanel() => DefaultStyleKeyProperty.OverrideMetadata(typeof(ZoomPanel), new FrameworkPropertyMetadata(typeof(ZoomPanel)));

        #endregion Public Constructors + Destructors

        #region Public Events

        /// <summary>
        /// Event raised when the ContentOffsetX property has changed.
        /// </summary>
        public event EventHandler? ContentOffsetXChanged;

        /// <summary>
        /// Event raised when the ContentOffsetY property has changed.
        /// </summary>
        public event EventHandler? ContentOffsetYChanged;

        /// <summary>
        /// Event raised when the ViewportZoom property has changed.
        /// </summary>
        public event EventHandler? ContentZoomChanged;

        public event MouseHorizontalWheel.MouseHorizontalWheelEventHandler? MouseHorizontalWheel;

        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion Public Events

        #region Private Enums

        private enum CurrentZoomTypeEnum { Fill, Fit, Other }

        #endregion Private Enums

        #region Public Properties

        /// <summary>
        /// The duration of the animations (in seconds) started by calling
        /// AnimatedZoomTo and the other animation methods.
        /// </summary>
        public double AnimationDuration
        {
            get => (double)GetValue(AnimationDurationProperty);
            set => SetValue(AnimationDurationProperty, value);
        }

        /// <summary>
        /// Get/set the X offset (in content coordinates) of the view on the content.
        /// </summary>
        public double ContentOffsetX
        {
            get => (double)GetValue(ContentOffsetXProperty);
            set => SetValue(ContentOffsetXProperty, value);
        }

        /// <summary>
        /// Get/set the Y offset (in content coordinates) of the view on the content.
        /// </summary>
        public double ContentOffsetY
        {
            get => (double)GetValue(ContentOffsetYProperty);
            set => SetValue(ContentOffsetYProperty, value);
        }

        /// <summary>
        /// Get the viewport height, in content coordinates.
        /// </summary>
        public double ContentViewportHeight
        {
            get => (double)GetValue(ContentViewportHeightProperty);
            set => SetValue(ContentViewportHeightProperty, value);
        }

        /// <summary>
        /// Get the viewport width, in content coordinates.
        /// </summary>
        public double ContentViewportWidth
        {
            get => (double)GetValue(ContentViewportWidthProperty);
            set => SetValue(ContentViewportWidthProperty, value);
        }

        /// <summary>
        /// The X coordinate of the content focus, this is the point that we are
        /// focusing on when zooming.
        /// </summary>
        public double ContentZoomFocusX
        {
            get => (double)GetValue(ContentZoomFocusXProperty);
            set => SetValue(ContentZoomFocusXProperty, value);
        }

        /// <summary>
        /// The Y coordinate of the content focus, this is the point that we are
        /// focusing on when zooming.
        /// </summary>
        public double ContentZoomFocusY
        {
            get => (double)GetValue(ContentZoomFocusYProperty);
            set => SetValue(ContentZoomFocusYProperty, value);
        }

        /// <summary>
        /// The Fill Zoom of the viewport.
        /// </summary>
        public double FillZoomValue => ViewportHelpers.FillZoom(ActualWidth, ActualHeight, _content?.ActualWidth, _content?.ActualHeight);

        /// <summary>
        /// The Fit Zoom of the viewport.
        /// </summary>
        public double FitZoomValue => ViewportHelpers.FitZoom(ActualWidth, ActualHeight, _content?.ActualWidth, _content?.ActualHeight);

        /// <summary>
        /// Set to 'true' to enable the mouse wheel to scroll the zoom and pan
        /// control. This is set to 'false' by default.
        /// </summary>
        public bool IsMouseWheelScrollingEnabled
        {
            get => (bool)GetValue(IsMouseWheelScrollingEnabledProperty);
            set => SetValue(IsMouseWheelScrollingEnabledProperty, value);
        }

        /// <summary>
        /// Get/set the maximum value for 'ViewportZoom'.
        /// </summary>
        public double MaximumZoom
        {
            get => (double)GetValue(MaximumZoomProperty);
            set => SetValue(MaximumZoomProperty, value);
        }

        /// <summary>
        /// Get/set the MinimumZoom value for 'ViewportZoom'.
        /// </summary>
        public double MinimumZoom
        {
            get => (double)GetValue(MinimumZoomProperty);
            set => SetValue(MinimumZoomProperty, value);
        }

        public double MinimumZoomClamped => ((MinimumZoomType == MinimumZoomType.FillScreen) ? FillZoomValue
                                              : (MinimumZoomType == MinimumZoomType.FitScreen) ? FitZoomValue
                                              : MinimumZoom).ToRealNumber();

        /// <summary>
        /// Get/set the maximum value for 'ViewportZoom'.
        /// </summary>
        public MinimumZoomType MinimumZoomType
        {
            get => (MinimumZoomType)GetValue(MinimumZoomTypeProperty);
            set => SetValue(MinimumZoomTypeProperty, value);
        }

        public ModifierKeys MousePanModifier
        {
            get => (ModifierKeys)GetValue(MousePanModifierProperty);
            set => SetValue(MousePanModifierProperty, value);
        }

        public ModifierKeys MouseDragZoomModifier
        {
            get => (ModifierKeys)GetValue(MouseDragZoomModifierProperty);
            set => SetValue(MouseDragZoomModifierProperty, value);
        }

        public Point MousePosition
        {
            get => (Point)GetValue(MousePositionProperty);
            set => SetValue(MousePositionProperty, value);
        }

        public ModifierKeys ScrollLeftRightModifier
        {
            get => (ModifierKeys)GetValue(ScrollLeftRightModifierProperty);
            set => SetValue(ScrollLeftRightModifierProperty, value);
        }

        public ModifierKeys ScrollUpDownModifier
        {
            get => (ModifierKeys)GetValue(ScrollUpDownModifierProperty);
            set => SetValue(ScrollUpDownModifierProperty, value);
        }

        /// <summary>
        /// This is used for binding a slider to control the zoom. Cannot use
        /// the InternalUseAnimations because of all the assumptions in when the
        /// this property is changed. THIS IS NOT USED FOR THE ANIMATIONS
        /// </summary>
        public bool UseAnimations
        {
            get => (bool)GetValue(UseAnimationsProperty);
            set => SetValue(UseAnimationsProperty, value);
        }

        /// <summary>
        /// This is used for binding a slider to control the zoom. Cannot use
        /// the InternalViewportZoom because of all the assumptions in when the
        /// this property is changed. THIS IS NOT USED FOR THE ANIMATIONS
        /// </summary>
        public double ViewportZoom
        {
            get => (double)GetValue(ViewportZoomProperty);
            set => SetValue(ViewportZoomProperty, value);
        }

        /// <summary>
        /// The X coordinate of the viewport focus, this is the point in the
        /// viewport (in viewport coordinates) that the content focus point is
        /// locked to while zooming in.
        /// </summary>
        public double ViewportZoomFocusX
        {
            get => (double)GetValue(ViewportZoomFocusXProperty);
            set => SetValue(ViewportZoomFocusXProperty, value);
        }

        /// <summary>
        /// The Y coordinate of the viewport focus, this is the point in the
        /// viewport (in viewport coordinates) that the content focus point is
        /// locked to while zooming in.
        /// </summary>
        public double ViewportZoomFocusY
        {
            get => (double)GetValue(ViewportZoomFocusYProperty);
            set => SetValue(ViewportZoomFocusYProperty, value);
        }

        // Definitions for dependency properties.
        /// <summary>
        /// This allows the same property name be used for direct and indirect
        /// access to this control.
        /// </summary>
        public ZoomPanel ZoomAndPanContent => this;

        /// <summary>
        /// The duration of the animations (in seconds) started by calling
        /// AnimatedZoomTo and the other animation methods.
        /// </summary>
        public ZoomPanelInitialPosition ZoomAndPanInitialPosition
        {
            get => (ZoomPanelInitialPosition)GetValue(ZoomAndPanInitialPositionProperty);
            set => SetValue(ZoomAndPanInitialPositionProperty, value);
        }

        /// <summary>
        /// This is used for binding a slider to control the zoom.
        /// </summary>
        public ModifierKeys ScrollZoomModifier
        {
            get => (ModifierKeys)GetValue(ScrollZoomModifierProperty);
            set => SetValue(ScrollZoomModifierProperty, value);
        }

        /// <summary>
        /// Whether double click snapping is enabled or not.
        /// </summary>
        public bool UseDoubleClickSnapping
        {
            get => (bool)GetValue(UseDoubleClickSnappingProperty);
            set => SetValue(UseDoubleClickSnappingProperty, value);
        }

        #endregion Public Properties

        #region Private Properties

        /// <summary>
        /// This is required for the animations, but has issues if set by
        /// something like a slider.
        /// </summary>
        private double InternalViewportZoom
        {
            get => (double)GetValue(_internalViewportZoomProperty);
            set => SetValue(_internalViewportZoomProperty, value);
        }

        #endregion Private Properties

        #region Public Methods

        /// <summary>
        /// Called when a template has been applied to the control.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _content = (Template.FindName("PART_Content", this) as FrameworkElement) ?? new();
            if (_content != null)
            {
                // Setup the transform on the content so that we can scale it by 'ViewportZoom'.
                _contentZoomTransform = new ScaleTransform(InternalViewportZoom, InternalViewportZoom);

                // Setup the transform on the content so that we can translate
                // it by 'ContentOffsetX' and 'ContentOffsetY'.
                _contentOffsetTransform = new TranslateTransform();
                UpdateTranslationX();
                UpdateTranslationY();

                // Setup a transform group to contain the translation and scale
                // transforms, and then assign this to the content's 'RenderTransform'.
                TransformGroup transformGroup = new TransformGroup();
                transformGroup.Children.Add(_contentOffsetTransform);
                transformGroup.Children.Add(_contentZoomTransform);
                _content.RenderTransform = transformGroup;
                ZoomAndPanControl_EventHandlers_OnApplyTemplate();
            }
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Arrange the control and it's children.
        /// </summary>
        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            Size size = base.ArrangeOverride(DesiredSize);

            if (_content.DesiredSize != _unScaledExtent)
            {
                // Use the size of the child as the un-scaled extent content.
                _unScaledExtent = _content.DesiredSize;
                ScrollOwner?.InvalidateScrollInfo();
            }

            // Update the size of the viewport onto the content based on the
            // passed in 'arrangeBounds'.
            UpdateViewportSize(arrangeBounds);

            return size;
        }

        /// <summary>
        /// Measure the control and it's children.
        /// </summary>
        protected override Size MeasureOverride(Size constraint)
        {
            Size infiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
            Size childSize = base.MeasureOverride(infiniteSize);

            if (childSize != _unScaledExtent)
            {
                // Use the size of the child as the un-scaled extent content.
                _unScaledExtent = childSize;
                ScrollOwner?.InvalidateScrollInfo();
            }

            // Update the size of the viewport onto the content based on the
            // passed in 'constraint'.
            UpdateViewportSize(constraint);
            double width = constraint.Width;
            double height = constraint.Height;
            if (double.IsInfinity(width))
            {
                width = childSize.Width;
            }

            if (double.IsInfinity(height))
            {
                height = childSize.Height;
            }

            UpdateTranslationX();
            UpdateTranslationY();
            return new Size(width, height);
        }

        protected virtual void MouseHorizontalWheelHandler(int delta) => MouseHorizontalWheel?.Invoke(this, new MouseHorizontalWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, delta));



        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="drawingContext"><inheritdoc/></param>
        /// <remarks>Implements window message interception for horizontal scrolling. Based on <see href="https://blog.walterlv.com/post/handle-horizontal-scrolling-of-touchpad-en.html"/> except we use a custom event</remarks>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // Unfortunately we can't really get a parent window's handle until pretty much everything is done loading and rendering
            if (!DesignerProperties.GetIsInDesignMode(this)) // Disable hook attempt in designer/Blend
            {
                if (!_wndHooked) //we don't need to hook the window's messages on every render of the control
                {
                    PresentationSource source = PresentationSource.FromVisual(Window.GetWindow(this)); //get owner Window
                    ((HwndSource)source)?.AddHook(WndHook); //Add a hook to the window to liston to events
                    MouseHorizontalWheel += OnMouseHorizontalWheel; // subscribe to custom event
                    _wndHooked = true;
                }
            }
        }

        /// <summary>
        /// Need to update zoom values if size changes, and update ViewportZoom
        /// if too low
        /// </summary>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (sizeInfo.NewSize.Width <= 1 || sizeInfo.NewSize.Height <= 1)
            {
                return;
            }

            switch (_currentZoomTypeEnum)
            {
                case CurrentZoomTypeEnum.Fit:
                    InternalViewportZoom = ViewportHelpers.FitZoom(sizeInfo.NewSize.Width, sizeInfo.NewSize.Height,
                        _content?.ActualWidth, _content?.ActualHeight);
                    break;

                case CurrentZoomTypeEnum.Fill:
                    InternalViewportZoom = ViewportHelpers.FillZoom(sizeInfo.NewSize.Width, sizeInfo.NewSize.Height,
                        _content?.ActualWidth, _content?.ActualHeight);
                    break;
            }
            if (InternalViewportZoom < MinimumZoomClamped)
            {
                InternalViewportZoom = MinimumZoomClamped;
            }
            // INotifyPropertyChanged property update
            OnPropertyChanged(nameof(MinimumZoomClamped));
            OnPropertyChanged(nameof(FillZoomValue));
            OnPropertyChanged(nameof(FitZoomValue));
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Method called to clamp the 'ContentOffsetX' value to its valid range.
        /// </summary>
        private static object ContentOffsetX_Coerce(DependencyObject d, object baseValue)
        {
            ZoomPanel c = (ZoomPanel)d;
            double value = (double)baseValue;
            double minOffsetX = 0.0;
            double maxOffsetX = Math.Max(0.0, c._unScaledExtent.Width - c._constrainedContentViewportWidth);
            value = Math.Min(Math.Max(value, minOffsetX), maxOffsetX);
            return value;
        }

        /// <summary>
        /// Event raised when the 'ContentOffsetX' property has changed value.
        /// </summary>
        private static void ContentOffsetX_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ZoomPanel c = (ZoomPanel)o;

            c.UpdateTranslationX();

            if (!c._disableContentFocusSync)
            {
                // Normally want to automatically update content focus when
                // content offset changes. Although this is disabled using
                // 'disableContentFocusSync' when content offset changes due to
                // in-progress zooming.
                c.UpdateContentZoomFocusX();
            }
            // Raise an event to let users of the control know that the content
            // offset has changed.
            c.ContentOffsetXChanged?.Invoke(c, EventArgs.Empty);

            if (!c._disableScrollOffsetSync)
            {
                // Notify the owning ScrollViewer that the scrollbar offsets
                // should be updated.
                c.ScrollOwner?.InvalidateScrollInfo();
            }
        }

        /// <summary>
        /// Method called to clamp the 'ContentOffsetY' value to its valid range.
        /// </summary>
        private static object ContentOffsetY_Coerce(DependencyObject d, object baseValue)
        {
            ZoomPanel c = (ZoomPanel)d;
            double value = (double)baseValue;
            double minOffsetY = 0.0;
            double maxOffsetY = Math.Max(0.0, c._unScaledExtent.Height - c._constrainedContentViewportHeight);
            value = Math.Min(Math.Max(value, minOffsetY), maxOffsetY);
            return value;
        }

        /// <summary>
        /// Event raised when the 'ContentOffsetY' property has changed value.
        /// </summary>
        private static void ContentOffsetY_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ZoomPanel c = (ZoomPanel)o;

            c.UpdateTranslationY();

            if (!c._disableContentFocusSync)
            {
                // Normally want to automatically update content focus when
                // content offset changes. Although this is disabled using
                // 'disableContentFocusSync' when content offset changes due to
                // in-progress zooming.
                c.UpdateContentZoomFocusY();
            }

            if (!c._disableScrollOffsetSync)
            {
                // Notify the owning ScrollViewer that the scrollbar offsets
                // should be updated.
                c.ScrollOwner?.InvalidateScrollInfo();
            }
            // Raise an event to let users of the control know that the content
            // offset has changed.
            c.ContentOffsetYChanged?.Invoke(c, EventArgs.Empty);
        }

        /// <summary>
        /// Method called to clamp the 'ViewportZoom' value to its valid range.
        /// </summary>
        private static object InternalViewportZoom_Coerce(DependencyObject dependencyObject, object baseValue)
        {
            ZoomPanel c = (ZoomPanel)dependencyObject;
            double value = Math.Max((double)baseValue, c.MinimumZoomClamped);
            switch (c.MinimumZoomType)
            {
                case MinimumZoomType.FitScreen:
                    value = Math.Min(Math.Max(value, c.FitZoomValue), c.MaximumZoom);
                    break;

                case MinimumZoomType.FillScreen:
                    value = Math.Min(Math.Max(value, c.FillZoomValue), c.MaximumZoom);
                    break;

                case MinimumZoomType.MinimumZoom:
                    value = Math.Min(Math.Max(value, c.MinimumZoom), c.MaximumZoom);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(dependencyObject), $"{c.MinimumZoomType} is Out of Range");
            }
            return value;
        }

        /// <summary>
        /// Event raised when the 'ViewportZoom' property has changed value.
        /// </summary>
        private static void InternalViewportZoom_PropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ZoomPanel c = (ZoomPanel)dependencyObject;

            if (c._contentZoomTransform != null)
            {
                // Update the content scale transform whenever 'ViewportZoom' changes.
                c._contentZoomTransform.ScaleX = c.InternalViewportZoom;
                c._contentZoomTransform.ScaleY = c.InternalViewportZoom;
            }

            // Update the size of the viewport in content coordinates.
            c.UpdateContentViewportSize();

            if (c._enableContentOffsetUpdateFromScale)
            {
                try
                {
                    // Disable content focus syncronization. We are about to
                    // update content offset whilst zooming to ensure that the
                    // viewport is focused on our desired content focus point.
                    // Setting this to 'true' stops the automatic update of the
                    // content focus when content offset changes.
                    c._disableContentFocusSync = true;

                    // Whilst zooming in or out keep the content offset
                    // up-to-date so that the viewport is always focused on the
                    // content focus point (and also so that the content focus
                    // is locked to the viewport focus point - this is how the
                    // google maps style zooming works).
                    double viewportOffsetX = c.ViewportZoomFocusX - (c.ViewportWidth / 2);
                    double viewportOffsetY = c.ViewportZoomFocusY - (c.ViewportHeight / 2);
                    double contentOffsetX = viewportOffsetX / c.InternalViewportZoom;
                    double contentOffsetY = viewportOffsetY / c.InternalViewportZoom;
                    c.ContentOffsetX = (c.ContentZoomFocusX - (c.ContentViewportWidth / 2)) - contentOffsetX;
                    c.ContentOffsetY = (c.ContentZoomFocusY - (c.ContentViewportHeight / 2)) - contentOffsetY;
                }
                finally
                {
                    c._disableContentFocusSync = false;
                }
            }
            c.ContentZoomChanged?.Invoke(c, EventArgs.Empty);
            c.ViewportZoom = c.InternalViewportZoom;
            c.OnPropertyChanged(new DependencyPropertyChangedEventArgs(ViewportZoomProperty, c.ViewportZoom, c.InternalViewportZoom));
            c.ScrollOwner?.InvalidateScrollInfo();
            c.SetCurrentZoomTypeEnum();
            c.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Event raised 'MinimumZoom' or 'MaximumZoom' has changed.
        /// </summary>
        private static void MinimumOrMaximumZoom_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ZoomPanel c = (ZoomPanel)o;
            c.InternalViewportZoom = Math.Min(Math.Max(c.InternalViewportZoom, c.MinimumZoomClamped), c.MaximumZoom);
        }

        private static void ViewportZoom_PropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ZoomPanel c = (ZoomPanel)dependencyObject;
            double newZoom = (double)e.NewValue;
            if (c.InternalViewportZoom != newZoom)
            {
                Point centerPoint = new Point(c.ContentOffsetX + (c._constrainedContentViewportWidth / 2), c.ContentOffsetY + (c._constrainedContentViewportHeight / 2));
                c.ZoomAboutPoint(newZoom, centerPoint);
            }
        }

        private static void ZoomAndPanInitialPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //ZoomAndPanControl zoomAndPanControl = (ZoomAndPanControl)d;
        }

        /// <summary>
        /// Reset the viewport zoom focus to the center of the viewport.
        /// </summary>
        private void ResetViewportZoomFocus()
        {
            ViewportZoomFocusX = ViewportWidth / 2;
            ViewportZoomFocusY = ViewportHeight / 2;
        }

        private void SetCurrentZoomTypeEnum()
        {
            if (ViewportZoom.IsWithinOnePercent(FitZoomValue))
            {
                _currentZoomTypeEnum = CurrentZoomTypeEnum.Fit;
            }
            else if (ViewportZoom.IsWithinOnePercent(FillZoomValue))
            {
                _currentZoomTypeEnum = CurrentZoomTypeEnum.Fill;
            }
            else
            {
                _currentZoomTypeEnum = CurrentZoomTypeEnum.Other;
            }
        }

        /// <summary>
        /// Update the size of the viewport in content coordinates after the
        /// viewport size or 'ViewportZoom' has changed.
        /// </summary>
        private void UpdateContentViewportSize()
        {
            ContentViewportWidth = ViewportWidth / InternalViewportZoom;
            ContentViewportHeight = ViewportHeight / InternalViewportZoom;

            _constrainedContentViewportWidth = Math.Min(ContentViewportWidth, _unScaledExtent.Width);
            _constrainedContentViewportHeight = Math.Min(ContentViewportHeight, _unScaledExtent.Height);

            UpdateTranslationX();
            UpdateTranslationY();
        }

        /// <summary>
        /// Update the X coordinate of the zoom focus point in content coordinates.
        /// </summary>
        private void UpdateContentZoomFocusX() => ContentZoomFocusX = ContentOffsetX + (_constrainedContentViewportWidth / 2);

        /// <summary>
        /// Update the Y coordinate of the zoom focus point in content coordinates.
        /// </summary>
        private void UpdateContentZoomFocusY() => ContentZoomFocusY = ContentOffsetY + (_constrainedContentViewportHeight / 2);

        /// <summary>
        /// Update the X coordinate of the translation transformation.
        /// </summary>
        private void UpdateTranslationX()
        {
            if (_contentOffsetTransform != null)
            {
                double scaledContentWidth = _unScaledExtent.Width * InternalViewportZoom;
                if (scaledContentWidth < ViewportWidth)
                {
                    // When the content can fit entirely within the viewport,
                    // center it.
                    _contentOffsetTransform.X = (ContentViewportWidth - _unScaledExtent.Width) / 2;
                }
                else
                {
                    _contentOffsetTransform.X = -ContentOffsetX;
                }
            }
        }

        /// <summary>
        /// Update the Y coordinate of the translation transformation.
        /// </summary>
        private void UpdateTranslationY()
        {
            if (_contentOffsetTransform != null)
            {
                double scaledContentHeight = _unScaledExtent.Height * InternalViewportZoom;
                if (scaledContentHeight < ViewportHeight)
                {
                    // When the content can fit entirely within the viewport,
                    // center it.
                    _contentOffsetTransform.Y = (ContentViewportHeight - _unScaledExtent.Height) / 2;
                }
                else
                {
                    _contentOffsetTransform.Y = -ContentOffsetY;
                }
            }
        }

        /// <summary>
        /// Update the viewport size from the specified size.
        /// </summary>
        private void UpdateViewportSize(Size newSize)
        {
            if (_viewport == newSize)
            {
                return;
            }

            _viewport = newSize;

            // Update the viewport size in content coordiates.
            UpdateContentViewportSize();

            // Initialise the content zoom focus point.
            UpdateContentZoomFocusX();
            UpdateContentZoomFocusY();

            // Reset the viewport zoom focus to the center of the viewport.
            ResetViewportZoomFocus();

            // Update content offset from itself when the size of the viewport
            // changes. This ensures that the content offset remains properly
            // clamped to its valid range.
            ContentOffsetX = ContentOffsetX;
            ContentOffsetY = ContentOffsetY;

            // Tell that owning ScrollViewer that scrollbar data has changed.
            ScrollOwner?.InvalidateScrollInfo();
        }

        /// <summary>
        /// Hook to listen to Window messages from system
        /// </summary>
        private IntPtr WndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x020E: //Mouse Horizontal Wheel Scroll/Mouse Wheel Tilt
                    int delta = ((int)wParam.ToInt64()) >> 16;
                    MouseHorizontalWheelHandler(delta);
                    return (IntPtr)1;
            }
            return IntPtr.Zero;
        }

        #endregion Private Methods
    }
}