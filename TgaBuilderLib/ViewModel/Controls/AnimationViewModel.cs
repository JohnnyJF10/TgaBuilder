using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;

namespace TgaBuilderLib.ViewModel
{
    public class AnimationViewModel : ViewModelBase
    {
        public AnimationViewModel(IBitmapOperations bitmapOperations)
        {
            _bitmapOperations = bitmapOperations;
        }
        private readonly SemaphoreSlim _animationSemaphore = new SemaphoreSlim(1, 1);
        private CancellationTokenSource? _cancellationTokenSource;

        private Task? _animationTask;

        private Stopwatch? _stopwatch;
        private List<Int32Rect> _frameRects = new();


        private IBitmapOperations _bitmapOperations;

        private BitmapSource? _presenter;
        private BitmapSource? _spriteSheet;

        private bool _isVisible;
        private bool _isPlayVisible = false;
        private bool _isPauseVisible = true;

        private int _speed = 100;
        private int _offsetTop;

        private RelayCommand? _startCommand;
        private RelayCommand? _stopCommand;
        private RelayCommand? _closeCommand;

        public BitmapSource? Presenter
        {
            get => _presenter;
            set => SetProperty(ref _presenter, value, nameof(Presenter));
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value, nameof(IsVisible));
        }

        public bool IsPlaying => _animationTask != null && !_cancellationTokenSource?.IsCancellationRequested == true;

        public int Speed
        {
            get => _speed;
            set => SetProperty(ref _speed, value, nameof(Speed));
        }

        public int OffsetTop
        {
            get => _offsetTop;
            set => SetProperty(ref _offsetTop, value, nameof(OffsetTop));
        }

        public bool IsPlayVisible
        {
            get => _isPlayVisible;
            set => SetProperty(ref _isPlayVisible, value, nameof(IsPlayVisible));
        }

        public bool IsPauseVisible
        {
            get => _isPauseVisible;
            set => SetProperty(ref _isPauseVisible, value, nameof(IsPauseVisible));
        }

        public RelayCommand StartCommand => _startCommand ??= new RelayCommand(Start);

        public RelayCommand StopCommand => _stopCommand ??= new RelayCommand(Stop);

        public RelayCommand CloseCommand => _closeCommand ??= new RelayCommand(Close);


        private int _delayValAnimRange => 200 - _speed;
        private int _delayValTexScrolling => 10 - _speed / 12;



        public void Start()
        {
            if (!IsPlaying)
            {
                _animationTask = Animate();

                IsPlayVisible = false;
                IsPauseVisible = true;
            }
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();

            _animationTask = null;

            IsPlayVisible = true;
            IsPauseVisible = false;
        }

        public void Close()
        {
            Stop();

            Presenter = null;
            IsVisible = false;
        }

        public void SetupAnimation(BitmapSource spriteSheet, (int, int) anchor1, (int, int) anchor2, int tileSize)
        {
                if (IsPlaying && _animationTask != null)
                {
                    Stop();
                }

                _spriteSheet = spriteSheet;
                int index1 = GetTexIndex(anchor1, tileSize, spriteSheet.PixelWidth);
                int index2 = GetTexIndex(anchor2, tileSize, spriteSheet.PixelWidth);

                _frameRects = index1 < index2
                    ? CalcFrameRects(anchor1, index2 - index1, tileSize, spriteSheet.PixelWidth)
                    : CalcFrameRects(anchor2, index1 - index2, tileSize, spriteSheet.PixelWidth);

                if (_frameRects.Count == 0) return;

                Presenter = new WriteableBitmap(tileSize, tileSize, 96, 96, spriteSheet.Format, null);
                IsVisible = true;
                IsPlayVisible = false;
                IsPauseVisible = true;

                _animationTask = Animate();
            }

        private async Task Animate()
        {
            if (_spriteSheet == null || _frameRects.Count == 0) return;

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            Task animationTask = _frameRects.Count > 1
                ? AnimateAnimRange(token)
                : AnimateTexScrolling(token);

            try
            {
                await animationTask;
            }
            catch (OperationCanceledException)
            {
                // Animation paused / ended...
            }
            finally
            {
                OffsetTop = 0;
                _stopwatch?.Stop();
            }
        }

        private async Task AnimateAnimRange(CancellationToken token)
        {
            OffsetTop = 0;

            while (!token.IsCancellationRequested)
            {
                foreach (var rect in _frameRects)
                {
                    if (token.IsCancellationRequested) break;

                    var frameBitmap = new CroppedBitmap(_spriteSheet, rect);
                    Presenter = frameBitmap;

                    await Task.Delay(_delayValAnimRange, token);
                }
            }
        }

        private async Task AnimateTexScrolling(CancellationToken token)
        {
            if (_spriteSheet is null) return;

            var rect = _frameRects[0];
            var rectSize = rect.Width;

            var texScrollingSource = new WriteableBitmap(
                pixelWidth: rectSize,
                pixelHeight: 2 * rectSize,
                dpiX: _spriteSheet.DpiX,
                dpiY: _spriteSheet.DpiY,
                pixelFormat: _spriteSheet.Format,
                palette: null);
            
            WriteableBitmap scrollTex = new(new CroppedBitmap(_spriteSheet, rect));

            _bitmapOperations.FillRectBitmapNoConvert(
                source: scrollTex, 
                target: texScrollingSource,
                pos: (0,0));

            _bitmapOperations.FillRectBitmapNoConvert(
                source: scrollTex, 
                target: texScrollingSource,
                pos: (0,rectSize));

            int offset = 100;
            Presenter = texScrollingSource;

            Stopwatch stopwatch = _stopwatch ?? new();
            stopwatch.Start();

            long lastTicks = stopwatch.ElapsedTicks;

            while (!token.IsCancellationRequested)
            {
                long nowTicks = stopwatch.ElapsedTicks;
                double elapsedSeconds = (nowTicks - lastTicks) / (double)Stopwatch.Frequency;
                lastTicks = nowTicks;

                double deltaY = 1 + 5 * Speed * elapsedSeconds;

                offset -= (int)deltaY;
                if (offset <= 0) 
                    offset = 100;

                OffsetTop = -offset;

                await Task.Delay(5, token);
            }
        }

        private List<Int32Rect> CalcFrameRects((int x, int y) startAnchor, int requiredTiles, int tileSize, int panelWidth)
        {
            int x = startAnchor.x, y = startAnchor.y;
            var res = new List<Int32Rect>();
            for (int i = 0; i <= requiredTiles; i++)
            {
                res.Add(new Int32Rect(x, y, tileSize, tileSize));
                x += tileSize;
                if (x >= panelWidth)  
                {
                    x = 0;
                    y += tileSize;
                }
            }
            return res;
        }

        private int GetTexIndex((int x, int y) anchor, int tileSize, int panelWidth)
        {
            return (anchor.x / tileSize) + (anchor.y / tileSize) * (panelWidth / tileSize);
        }
    }
}
