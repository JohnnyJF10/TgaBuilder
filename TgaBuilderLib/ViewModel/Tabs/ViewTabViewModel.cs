using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using TgaBuilderLib.Commands;
using Point = System.Windows.Point;

namespace TgaBuilderLib.ViewModel
{
    public class ViewTabViewModel : ViewModelBase
    {
        public ViewTabViewModel(
            PanelVisualSizeViewModel visualPanelSize,
            TexturePanelViewModelBase panel)
        {
            _panel = panel;
            VisualPanelSize = visualPanelSize;
        }

        private const int SCROLL_SPEED_PIX_PER_SEC = 420;
        private const int DRAG_THRESHOLD = 10;
        private const int SCROLLING_THRESHOLD = 30;

        public bool IsScrolling { get; set; } = false;
        private Vector _scrollDirection;

        private TexturePanelViewModelBase _panel;

        private double _offsetX;
        private double _offsetY;

        private double _horizonatlMargin;

        private Stopwatch? _stopwatch;

        private RelayCommand? _FillCommand;
        private RelayCommand? _FitCommand;
        private RelayCommand? _100PercentCommand;
        private RelayCommand<Point>? _scrollCommand;

        double maxX;
        double maxY;

        public PanelVisualSizeViewModel VisualPanelSize { get; set; }

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
            set
            {
                if (value == _panel.Zoom) return;

                _panel.Zoom = value;
                OnPropertyChanged(nameof(Zoom));
            }
        }

        public double HorizonatlMargin
        {
            get => _horizonatlMargin;
            set => SetProperty(ref _horizonatlMargin, value, nameof(HorizonatlMargin));
        }

        public ICommand FillCommand => _FillCommand ??= new RelayCommand(Fill);
        public ICommand FitCommand => _FitCommand ??= new RelayCommand(Fit);
        public ICommand Zoom100Command => _100PercentCommand ??= new RelayCommand(Zoom100);
        public ICommand ScrollCommand => _scrollCommand ??= new RelayCommand<Point>(DoPanelScrolling);

        // A makeshift workaround to avoid a race condition when the panel is resized.
        // WPF seemingly requires time to set everything up appropriately.
        public Task DefferedFill()
            => Task.Run(async () =>
            {
                await Task.Delay(20);
                Fill();
                await Task.Delay(20);
                OffsetY -= 1000;
                OffsetY = 0;
            });

        public void Fill()
        {
            Zoom = VisualPanelSize.ContentWidth < VisualPanelSize.ContentHeight
                ? Math.Max(
                    VisualPanelSize.ViewportWidth / VisualPanelSize.ContentWidth,
                    VisualPanelSize.ViewportHeight / VisualPanelSize.ContentHeight)
                : Math.Min(
                    VisualPanelSize.ViewportWidth / VisualPanelSize.ContentWidth,
                    VisualPanelSize.ViewportHeight / VisualPanelSize.ContentHeight);

            OffsetX = 0;
            OffsetY = 0;
        }

        public void Fit()
        {
            Zoom = VisualPanelSize.ContentWidth < VisualPanelSize.ContentHeight
                ? Math.Min(
                    VisualPanelSize.ViewportWidth / VisualPanelSize.ContentWidth,
                    VisualPanelSize.ViewportHeight / VisualPanelSize.ContentHeight)
                : Math.Max(
                    VisualPanelSize.ViewportWidth / VisualPanelSize.ContentWidth,
                    VisualPanelSize.ViewportHeight / VisualPanelSize.ContentHeight);
        }

        public void Zoom100()
        {
            Zoom = 1.0;
            OffsetX = (VisualPanelSize.ContentWidth - VisualPanelSize.ViewportWidth) / 2;
            OffsetY = (VisualPanelSize.ContentHeight - VisualPanelSize.ViewportHeight) / 2;
        }

        private void DoPanelScrolling(Point pos)
        {
            int posX = (int)pos.X;
            int posY = (int)pos.Y;

            if ((Math.Abs(posX - VisualPanelSize.ViewportWidth) > DRAG_THRESHOLD ||
                Math.Abs(posY - VisualPanelSize.ViewportHeight) > DRAG_THRESHOLD) &&
                _panel.CanScroll)
            {
                Vector scrollVector = new(0, 0);

                if (posY < SCROLLING_THRESHOLD)
                    scrollVector.Y = -1;
                else if (posY > VisualPanelSize.ViewportHeight - SCROLLING_THRESHOLD)
                    scrollVector.Y = 1;

                if (posX < SCROLLING_THRESHOLD)
                    scrollVector.X = -1;
                else if (posX > VisualPanelSize.ViewportWidth - SCROLLING_THRESHOLD)
                    scrollVector.X = 1;

                if (scrollVector.Length > 0)
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

            Stopwatch stopwatch = _stopwatch ?? new();
            stopwatch.Start();

            long lastTicks = stopwatch.ElapsedTicks;

            while (IsScrolling)
            {
                long nowTicks = stopwatch.ElapsedTicks;
                double elapsedSeconds = (nowTicks - lastTicks) / (double)Stopwatch.Frequency;
                lastTicks = nowTicks;

                double deltaX = _scrollDirection.X * SCROLL_SPEED_PIX_PER_SEC * elapsedSeconds / Zoom;
                double deltaY = _scrollDirection.Y * SCROLL_SPEED_PIX_PER_SEC * elapsedSeconds / Zoom;

                OffsetX += deltaX;
                OffsetY += deltaY;

                maxX = Math.Max(0, VisualPanelSize.ContentWidth - (VisualPanelSize.ViewportWidth / Zoom));
                maxY = Math.Max(0, VisualPanelSize.ContentHeight - (VisualPanelSize.ViewportHeight / Zoom));

                OffsetX = Math.Clamp(OffsetX, 0, maxX);
                OffsetY = Math.Clamp(OffsetY, 0, maxY);

                await Task.Delay(3);
            }
        }
    }
}