using System.Diagnostics;
using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Commands;

namespace TgaBuilderLib.ViewModel
{
    /// <summary>
    /// View tab implementation for Avalonia where zoom and offsets are read-only
    /// from the PanAndZoom control. Uses matrix transformation callbacks to
    /// apply view changes (zoom, pan) through the view's dispatcher service.
    /// </summary>
    public class ReadOnlyViewTabViewModel : ViewModelBase, IViewTabViewModel
    {
        public ReadOnlyViewTabViewModel(
            PanelVisualSizeViewModel visualPanelSize,
            TexturePanelViewModelBase panel)
        {
            _panel = panel;
            VisualPanelSize = visualPanelSize;

            _panel.PresenterChanged += (_, _) => _ = DefferedFill();
            VisualPanelSize.PropertyChanged += (_, _) => OnContentActualSizeChanged();
        }

        private const int SCROLL_SPEED_PIX_PER_SEC = 150;
        private const int DRAG_THRESHOLD = 10;
        private const int SCROLLING_THRESHOLD = 30;
        private const double ZOOM_STEP_FACTOR = 1.2;

        public bool IsScrolling { get; set; } = false;
        private (double X, double Y) _scrollDirection;

        private TexturePanelViewModelBase _panel;

        private double _offsetX;
        private double _offsetY;

        private double _horizonatlMargin;

        private RelayCommand? _FillCommand;
        private RelayCommand? _FitCommand;
        private RelayCommand? _100PercentCommand;
        private RelayCommand? _zoomInCommand;
        private RelayCommand? _zoomOutCommand;
        private RelayCommand<(double X, double Y)>? _scrollCommand;
        private RelayCommand? _endScrollCommand;

        public PanelVisualSizeViewModel VisualPanelSize { get; set; }

        public double ContentActualWidth
            => Math.Min(_panel.Presenter.PixelWidth * Zoom, VisualPanelSize.ViewportWidth);

        public double ContentActualHeight
            => Math.Min(_panel.Presenter.PixelHeight * Zoom, VisualPanelSize.ViewportHeight);

        public double OffsetX
        {
            get => _offsetX;
            set => SetProperty(ref _offsetX, value, nameof(OffsetX));
        }
        public double OffsetY
        {
            get => _offsetY;
            set => SetProperty(ref _offsetY, value, nameof(OffsetY));
        }
        public double Zoom
        {
            get => _panel.Zoom;
            set => SetPanelZoom(value);
        }

        public double HorizonatlMargin
        {
            get => _horizonatlMargin;
            set => SetProperty(ref _horizonatlMargin, value, nameof(HorizonatlMargin));
        }

        public ICommand FillCommand => _FillCommand ??= new RelayCommand(Fill);
        public ICommand FitCommand => _FitCommand ??= new RelayCommand(Fit);
        public ICommand Zoom100Command => _100PercentCommand ??= new RelayCommand(Zoom100);
        public ICommand ScrollCommand => _scrollCommand ??= new(DoPanelScrolling);
        public ICommand EndScrollCommand => _endScrollCommand ??= new RelayCommand(() => IsScrolling = false);

        /// <summary>
        /// Command to zoom in by a fixed step factor.
        /// </summary>
        public ICommand ZoomInCommand => _zoomInCommand ??= new RelayCommand(ZoomIn);

        /// <summary>
        /// Command to zoom out by a fixed step factor.
        /// </summary>
        public ICommand ZoomOutCommand => _zoomOutCommand ??= new RelayCommand(ZoomOut);

        /// <summary>
        /// Proxy for the ZoomBorder control, allowing the ViewModel to invoke
        /// zoom/pan operations without referencing any view-layer types.
        /// </summary>
        public IZoomBorderProxy? ZoomBorderProxy { get; set; }

        public async Task DefferedFill()
        {
            await Task.Delay(20);

            ZoomBorderProxy?.ResetView();

            var zoom = VisualPanelSize.ViewportHeight / _panel.Presenter.PixelHeight;

            _panel.Zoom = zoom;
            OnPropertyChanged(nameof(Zoom));
            OnContentActualSizeChanged();
            await Task.Delay(20);

            var centerX = _panel.Presenter.PixelWidth > VisualPanelSize.ViewportWidth
                ? VisualPanelSize.ViewportWidth / zoom / 2
                : _panel.Presenter.PixelWidth / 2.0;

            var centerY = _panel.Presenter.PixelHeight / 2.0;

            ZoomBorderProxy?.CenterOn(centerX, centerY, zoom);
        }

        public void Fill()
        {
            var zoom = VisualPanelSize.ViewportHeight / _panel.Presenter.PixelHeight;

            _panel.Zoom = zoom;
            OnPropertyChanged(nameof(Zoom));
            OnContentActualSizeChanged();

            var centerX = _panel.Presenter.PixelWidth > VisualPanelSize.ViewportWidth
                ? VisualPanelSize.ViewportWidth / zoom / 2
                : _panel.Presenter.PixelWidth / 2.0;

            var centerY = _panel.Presenter.PixelHeight / 2.0;

            ZoomBorderProxy?.CenterOn(centerX, centerY, zoom);
        }

        public void Fit()
        {
            var zoom = VisualPanelSize.ViewportWidth / _panel.Presenter.PixelWidth;

            _panel.Zoom = zoom;
            OnPropertyChanged(nameof(Zoom));
            OnContentActualSizeChanged();

            var centerX = _panel.Presenter.PixelWidth / 2.0;

            var centerY = _panel.Presenter.PixelHeight > VisualPanelSize.ViewportHeight
                ? VisualPanelSize.ViewportHeight / zoom / 2
                : _panel.Presenter.PixelHeight / 2.0;

            ZoomBorderProxy?.CenterOn(centerX, centerY, zoom);
        }

        public void Zoom100()
        {
            _panel.Zoom = 1.0;
            OnPropertyChanged(nameof(Zoom));
            OnContentActualSizeChanged();

            var centerX = _panel.Presenter.PixelWidth > VisualPanelSize.ViewportWidth
                ? VisualPanelSize.ViewportWidth / 2
                : _panel.Presenter.PixelWidth / 2.0;

            var centerY = _panel.Presenter.PixelHeight > VisualPanelSize.ViewportHeight
                ? VisualPanelSize.ViewportHeight / 2
                : _panel.Presenter.PixelHeight / 2.0;

            ZoomBorderProxy?.CenterOn(centerX, centerY, 1.0);
        }

        public void ZoomIn()
        {
            ZoomBorderProxy?.ZoomStep(ZOOM_STEP_FACTOR);
        }

        public void ZoomOut()
        {
            ZoomBorderProxy?.ZoomStep(1.0 / ZOOM_STEP_FACTOR);
        }

        private void SetPanelZoom(double zoom)
        {
            if (zoom == _panel.Zoom)
                return;

            _panel.Zoom = zoom;

            OnContentActualSizeChanged();
            OnPropertyChanged(nameof(Zoom));
        }

        private void OnContentActualSizeChanged()
        {
            OnPropertyChanged(nameof(ContentActualWidth));
            OnPropertyChanged(nameof(ContentActualHeight));
        }

        private void DoPanelScrolling((double X, double Y) pos)
        {
            int posX = (int)pos.X;
            int posY = (int)pos.Y;

            if ((Math.Abs(posX - VisualPanelSize.ViewportWidth) > DRAG_THRESHOLD ||
                Math.Abs(posY - VisualPanelSize.ViewportHeight) > DRAG_THRESHOLD) &&
                _panel.CanScroll)
            {
                (double X, double Y) scrollVector = new(0, 0);

                if (posY < SCROLLING_THRESHOLD)
                    scrollVector.Y = -1;
                else if (posY > VisualPanelSize.ViewportHeight - SCROLLING_THRESHOLD)
                    scrollVector.Y = 1;

                if (posX < SCROLLING_THRESHOLD)
                    scrollVector.X = -1;
                else if (posX > VisualPanelSize.ViewportWidth - SCROLLING_THRESHOLD)
                    scrollVector.X = 1;

                if (scrollVector.X != 0 || scrollVector.Y != 0)
                {
                    _scrollDirection = scrollVector;
                    if (!IsScrolling)
                    {
                        _ = StartScrollingAsync();
                    }
                }
                else IsScrolling = false;
            }
            else IsScrolling = false;
        }

        private async Task StartScrollingAsync()
        {
            IsScrolling = true;

            while (IsScrolling)
            {
                double deltaX = -_scrollDirection.X * SCROLL_SPEED_PIX_PER_SEC;
                double deltaY = -_scrollDirection.Y * SCROLL_SPEED_PIX_PER_SEC;
                ZoomBorderProxy?.PanStep(deltaX, deltaY);
                await Task.Delay(1000);
            }
        }
    }
}
