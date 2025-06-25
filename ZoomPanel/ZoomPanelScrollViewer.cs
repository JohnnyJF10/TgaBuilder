using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFZoomPanel.Commands;
using WPFZoomPanel.Enums;

namespace WPFZoomPanel
{
    public class ZoomPanelScrollViewer : ScrollViewer
    {
        // private ZoomAndPanControl _zoomAndPanControl;

        #region Public Fields

        public static readonly DependencyProperty MinimumZoomTypeProperty = DependencyProperty.Register("MinimumZoomType", typeof(MinimumZoomType), typeof(ZoomPanelScrollViewer), new FrameworkPropertyMetadata(MinimumZoomType.MinimumZoom));

        public static readonly DependencyProperty MousePositionProperty = DependencyProperty.Register("MousePosition", typeof(Point?), typeof(ZoomPanelScrollViewer), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty UseAnimationsProperty = DependencyProperty.Register("UseAnimations", typeof(bool), typeof(ZoomPanelScrollViewer), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty ViewportZoomProperty = DependencyProperty.Register("ViewportZoom", typeof(double), typeof(ZoomPanelScrollViewer), new FrameworkPropertyMetadata(1.0));

        public static readonly DependencyProperty ZoomAndPanContentProperty = DependencyProperty.Register("ZoomAndPanContent", typeof(ZoomPanel), typeof(ZoomPanelScrollViewer), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ZoomAndPanInitialPositionProperty = DependencyProperty.Register("ZoomAndPanInitialPosition", typeof(ZoomPanelInitialPosition), typeof(ZoomPanelScrollViewer), new FrameworkPropertyMetadata(ZoomPanelInitialPosition.Default));

        // Added by Moravuscz
        public static readonly DependencyProperty MouseDragZoomModifierProperty = DependencyProperty.Register("MouseDragZoomModifier", typeof(ModifierKeys), typeof(ZoomPanelScrollViewer), new FrameworkPropertyMetadata(ModifierKeys.Shift));

        // Added by Moravuscz
        public static readonly DependencyProperty ScrollLeftRightModifierProperty = DependencyProperty.Register("ScrollLeftRightModifier", typeof(ModifierKeys), typeof(ZoomPanelScrollViewer), new FrameworkPropertyMetadata(ModifierKeys.Shift));

        // Added by Moravuscz
        public static readonly DependencyProperty ScrollUpDownModifierProperty = DependencyProperty.Register("ScrollUpDownModifier", typeof(ModifierKeys), typeof(ZoomPanelScrollViewer), new FrameworkPropertyMetadata(ModifierKeys.None));

        // Added by Moravuscz
        public static readonly DependencyProperty ScrollZoomModifierProperty = DependencyProperty.Register("ScrollZoomModifier", typeof(ModifierKeys), typeof(ZoomPanelScrollViewer), new FrameworkPropertyMetadata(ModifierKeys.Control));

        #endregion Public Fields

        #region Private Fields

        private RelayCommand? _fillCommand;

        private RelayCommand? _fitCommand;

        private RelayCommand? _redoZoomCommand;

        private RelayCommand? _undoZoomCommand;

        private RelayCommand? _zoomInCommand;

        private RelayCommand? _zoomOutCommand;

        private RelayCommand<double>? _zoomPercentCommand;

        private RelayCommand<double>? _zoomRatioFromMinimumCommand;

        #endregion Private Fields

        #region Public Constructors + Destructors

        /// <summary>
        /// Static constructor to define metadata for the control (and link it
        /// to the style in Generic.xaml).
        /// </summary>
        static ZoomPanelScrollViewer() => DefaultStyleKeyProperty.OverrideMetadata(typeof(ZoomPanelScrollViewer), new FrameworkPropertyMetadata(typeof(ZoomPanelScrollViewer)));

        #endregion Public Constructors + Destructors

        #region Public Events

        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion Public Events

        #region Public Properties

        /// <summary>
        /// Command to implement the zoom to fill
        /// </summary>
        public ICommand FillCommand => _fillCommand ?? (_fillCommand =
                new RelayCommand(
                    () => ZoomAndPanContent.FillCommand.Execute(null),
                    () => ZoomAndPanContent?.FillCommand.CanExecute(null) ?? true));

        /// <summary>
        /// Command to implement the zoom to fit
        /// </summary>
        public ICommand FitCommand => _fitCommand ?? (_fitCommand =
                new RelayCommand(
                    () => ZoomAndPanContent.FitCommand.Execute(null),
                    () => ZoomAndPanContent?.FitCommand.CanExecute(null) ?? true));

        /// <summary>
        /// Get/set the maximum value for 'ViewportZoom'.
        /// </summary>
        public MinimumZoomType MinimumZoomType
        {
            get => (MinimumZoomType)GetValue(MinimumZoomTypeProperty);
            set => SetValue(MinimumZoomTypeProperty, value);
        }

        /// <summary>
        /// Get/set the MinimumZoom value for 'ViewportZoom'.
        /// </summary>
        public Point? MousePosition
        {
            get => (Point?)GetValue(MousePositionProperty);
            set => SetValue(MousePositionProperty, value);
        }

        /// <summary>
        /// Command to implement Redo
        /// </summary>
        public ICommand RedoZoomCommand => _redoZoomCommand ?? (_redoZoomCommand =
                new RelayCommand(
                    () => ZoomAndPanContent.RedoZoomCommand.Execute(null),
                    () => ZoomAndPanContent?.RedoZoomCommand.CanExecute(null) ?? true));

        /// <summary>
        /// Command to implement Undo
        /// </summary>
        public ICommand UndoZoomCommand => _undoZoomCommand ?? (_undoZoomCommand =
                new RelayCommand(
                    () => ZoomAndPanContent.UndoZoomCommand.Execute(null),
                    () => ZoomAndPanContent?.UndoZoomCommand.CanExecute(null) ?? true));

        /// <summary>
        /// Disables animations if set to false
        /// </summary>
        public bool UseAnimations
        {
            get => (bool)GetValue(UseAnimationsProperty);
            set => SetValue(UseAnimationsProperty, value);
        }

        /// <summary>
        /// Get/set the current scale (or zoom factor) of the content.
        /// </summary>
        public double ViewportZoom
        {
            get => (double)GetValue(ViewportZoomProperty);
            set => SetValue(ViewportZoomProperty, value);
        }

        /// <summary>
        /// Get/set the maximum value for 'ViewportZoom'.
        /// </summary>
        public ZoomPanel ZoomAndPanContent
        {
            get => (ZoomPanel)GetValue(ZoomAndPanContentProperty);
            set => SetValue(ZoomAndPanContentProperty, value);
        }

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
        /// Command to implement the zoom in by 91%
        /// </summary>
        public ICommand ZoomInCommand => _zoomInCommand ?? (_zoomInCommand =
                new RelayCommand(
                    () => ZoomAndPanContent.ZoomInCommand.Execute(null),
                    () => ZoomAndPanContent?.ZoomInCommand.CanExecute(null) ?? true));

        /// <summary>
        /// Command to implement the zoom out by 110%
        /// </summary>
        public ICommand ZoomOutCommand => _zoomOutCommand ?? (_zoomOutCommand =
                new RelayCommand(
                    () => ZoomAndPanContent.ZoomOutCommand.Execute(null),
                    () => ZoomAndPanContent?.ZoomOutCommand.CanExecute(null) ?? true));

        /// <summary>
        /// Command to implement the zoom to 100%
        /// </summary>
        public ICommand ZoomPercentCommand => _zoomPercentCommand ?? (_zoomPercentCommand =
                new RelayCommand<double>(
                    value => ZoomAndPanContent.ZoomPercentCommand.Execute(value),
                    value => ZoomAndPanContent?.ZoomPercentCommand.CanExecute(value) ?? true));

        /// <summary>
        /// Command to implement the zoom to 100%
        /// </summary>
        public ICommand ZoomRatioFromMinimumCommand => _zoomRatioFromMinimumCommand ?? (_zoomRatioFromMinimumCommand =
                new RelayCommand<double>(
                    value => ZoomAndPanContent.ZoomRatioFromMinimumCommand.Execute(value),
                    value => ZoomAndPanContent?.ZoomRatioFromMinimumCommand.CanExecute(value) ?? true));

        public ModifierKeys MouseDragZoomModifier
        {
            get => (ModifierKeys)GetValue(MouseDragZoomModifierProperty);
            set => SetValue(MouseDragZoomModifierProperty, value);
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

        public ModifierKeys ScrollZoomModifier
        {
            get => (ModifierKeys)GetValue(ScrollZoomModifierProperty);
            set => SetValue(ScrollZoomModifierProperty, value);
        }

        #endregion Public Properties

        #region Public Methods

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (Template.FindName("PART_ZoomAndPanControl", this) is not ZoomPanel ZoomAndPanContent)
                return; 

            OnPropertyChanged(new DependencyPropertyChangedEventArgs(ZoomAndPanContentProperty, null, ZoomAndPanContent));
            RefreshProperties();
        }

        #endregion Public Methods

        #region Private Methods

        private void RefreshProperties()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FillCommand)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FitCommand)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZoomPercentCommand)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZoomRatioFromMinimumCommand)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZoomInCommand)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZoomOutCommand)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UndoZoomCommand)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RedoZoomCommand)));
        }

        #endregion Private Methods
    }
}
