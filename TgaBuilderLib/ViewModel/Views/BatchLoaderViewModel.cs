using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Enums;
using TgaBuilderLib.FileHandling;
using TgaBuilderLib.Messaging;
using TgaBuilderLib.Utils;

namespace TgaBuilderLib.ViewModel
{
    public class BatchLoaderViewModel : ViewModelBase
    {
        public BatchLoaderViewModel(
            IMediaFactory mediaFactory,
            IFileService fileService,
            IMessageService messageService,

            IUsageData usageData,
            IAsyncFileLoader asyncFileLoader, 
            IBitmapOperations bitmapOperations,
            ILogger logger,

            IWriteableBitmap presenter)   
        {
            _mediaFactory = mediaFactory;
            _fileService = fileService;
            _messageService = messageService;

            _usageData = usageData;
            _asyncFileLoader = asyncFileLoader;
            _logger = logger;

            _bitmapOperations = bitmapOperations;
            _presenter = presenter;
        }

        private const int TEX_MIN_SIZE = 64;
        private const int TEX_MAX_SIZE = 512;
        private const int MAX_PIXEL_HEIGHT = 32768;

        private readonly IMediaFactory _mediaFactory;
        private readonly IAsyncFileLoader _asyncFileLoader;
        private readonly IBitmapOperations _bitmapOperations;
        private readonly ILogger _logger;

        private readonly IFileService _fileService;
        private readonly IMessageService _messageService;

        private IUsageData _usageData;

        private List<string> _allFiles = new();
        private List<string> _selectedFiles = new();

        private CancellationTokenSource _updateTaskCts = new();
        private readonly object _updateTaskLock = new();

        private IWriteableBitmap _presenter;

        private string _selectedFolderPath = string.Empty;
        private int _startTexIndex = 0;
        private int _numTextures = 11;
        private int _textureSize = 128;
        private int _panelWidth = 512;
        private bool _isDropHintVisible = true;
        private BitmapScalingMode _bitmapScalingMode = BitmapScalingMode.NearestNeighbor;
        private double _viewportWidth;
        private double _viewportHeight;
        private double _zoom = 1.0;

        private RelayCommand? _selectTRRFolderCommand;
        private RelayCommand<IView>? _importCommand;
        private RelayCommand<IView>? _cancelCommand;
        private RelayCommand<string>? openRecentFolderCommand;
        private RelayCommand<List<string>>? _fileDropCommand;


        public IEnumerable<string> RecentBatchLoaderFolders => _usageData.RecentBatchLoaderFolders;

        public IWriteableBitmap Presenter
        {
            get => _presenter;
            set => SetPresenter(value);
        }

        public int StartTexIndex
        {
            get => _startTexIndex;
            set => SetStartTexIndex(value);
        }

        public int NumTextures
        {
            get => _numTextures;
            set => SetNumTextures(value);
        }

        public int TextureSize
        {
            get => _textureSize;
            set => SetTextureSize(value);
        }

        public int PanelWidth
        {
            get => _panelWidth;
            set => SetImportPanelWidth(value);
        }

        public int ScalingModeIndex
        {
            get => (int)_bitmapScalingMode - 1;
            set => SetScalingModeIndex(value);  
        }

        public bool IsDropHintVisible
        {
            get => _isDropHintVisible;
            set => SetProperty(ref _isDropHintVisible, value, nameof(IsDropHintVisible));
        }

        public double ViewportWidth 
        { 
            get => _viewportWidth; 
            set => SetViewportSize(value, _viewportHeight);
        }

        public double ViewportHeight 
        { 
            get => _viewportHeight; 
            set => SetViewportSize(_viewportWidth, value);
        }

        public double Zoom
        {
            get => _zoom;
            set => SetZoom(value);
        }

        public double ContentActualWidth
            => Math.Min(Presenter.PixelWidth * Zoom, ViewportWidth);

        public double ContentActualHeight
            => Math.Min(Presenter.PixelHeight * Zoom, ViewportHeight);


        public ICommand SelectTRRFolderCommand
            => _selectTRRFolderCommand ??= new RelayCommand(() => SelectFolder());
        public ICommand ImportCommand 
            => _importCommand ??= new RelayCommand<IView>(Import);
        public ICommand CancelCommand 
            => _cancelCommand ??= new RelayCommand<IView>(Cancel);
        public ICommand OpenRecentFolderCommand
            => openRecentFolderCommand ??= new RelayCommand<string>(SelectFolder);
        public ICommand FileDropCommand
            => _fileDropCommand ??= new RelayCommand<List<string>>(FileDrop);



        public void SetViewportSize(double width, double height)
        {
            if (width < 0 || height < 0)
                return;

            _viewportWidth = width;
            _viewportHeight = height;

            Debug.WriteLine($"Viewport size set to: {width}x{height}");

            OnPropertyChanged(nameof(ViewportWidth));
            OnPropertyChanged(nameof(ViewportHeight));

            OnPropertyChanged(nameof(ContentActualWidth));
            OnPropertyChanged(nameof(ContentActualHeight));
        }

        public void SetStartTexIndex(int index)
        {
            if (index == _startTexIndex) return;

            if (index < 0)
                index = 0;

            if (index >= _allFiles.Count)
                index = _allFiles.Count - 1;

            _startTexIndex = index;

            _selectedFiles = _allFiles
                .Skip(_startTexIndex)
                .Take(_numTextures)
                .ToList();

            OnPropertyChanged(nameof(StartTexIndex));
            _ = UpdatePresenterAsync();
        }

        public void SetNumTextures(int num)
        {
            if (num == _numTextures) 
                return;
            if (num < 0) 
                num = 0;

            int oldVal = _numTextures;

            _numTextures = num;

            _selectedFiles = _allFiles
                .Skip(_startTexIndex)
                .Take(_numTextures)
                .ToList();

            OnPropertyChanged(nameof(NumTextures));
            _ = UpdatePresenterNumChangedAsync(oldVal, num);
        }

        public void SetTextureSize(int size)
        {
            if (size == _textureSize) 
                return;

            size = Math.Clamp(size, TEX_MIN_SIZE, TEX_MAX_SIZE);

            if (size < _textureSize)
                size = NextLowerPowerOfTwo(size);
            else
                size = NextHigherPowerOfTwo(size);
            _textureSize = size;

            OnPropertyChanged(nameof(TextureSize));
            _ = UpdatePresenterAsync();
        }


        private void SetImportPanelWidth(int value)
        {
            if (value == _panelWidth) 
                return;

            value = CalculateNewPanelWidth(value, _panelWidth);

            _panelWidth = value;

            OnPropertyChanged(nameof(PanelWidth));
            _ = UpdatePresenterAsync();
        }

        public void Import(IView view)
        {
            DoHeightResizing();

            view.DialogResult = true;
            view.Close();
        }

        public void Cancel(IView view)
        {
            view.DialogResult = false;
            view.Close();
        }

        private void DoHeightResizing()
        {
            int paddedHeight = (int)Math.Ceiling(Presenter.PixelHeight / 256.0) * 256;

            if (paddedHeight == Presenter.PixelHeight)
                return;

            int stride = PanelWidth * 4;

            IWriteableBitmap paddedBitmap = _mediaFactory.CreateEmptyBitmap(
                width:       PanelWidth,
                height:     paddedHeight,
                hasAlpha:   true);

            byte[] blackPixels = new byte[paddedHeight * stride];
            for (int i = 0; i < blackPixels.Length; i += 4)
            {
                blackPixels[i + 0] = 0; // B
                blackPixels[i + 1] = 0; // G
                blackPixels[i + 2] = 0; // R
                blackPixels[i + 3] = 0; // A
            }

            paddedBitmap.WritePixels(
                rect: new PixelRect(0, 0, PanelWidth, paddedHeight),
                pixels:     blackPixels, 
                stride:     stride, 
                offset:     0);

            // Copy original pixels to the top of the padded bitmap
            var croppedSource = _bitmapOperations.CropBitmap(
                source:     Presenter, 
                rectangle:  new PixelRect(0, 0, PanelWidth, Presenter.PixelHeight));

            int srcStride = PanelWidth * 4;
            byte[] srcPixels = new byte[Presenter.PixelHeight * srcStride];

            croppedSource.CopyPixels(
                pixels: srcPixels, 
                stride: srcStride, 
                offset: 0);

            paddedBitmap.WritePixels(
                rect: new PixelRect(0, 0, PanelWidth, Presenter.PixelHeight),
                pixels:     srcPixels, 
                stride:     srcStride, 
                offset:     0);

            Presenter = paddedBitmap;
        }

        private async Task UpdatePresenterAsync()
        {
            int successCount = 0;
            byte[] data;
            IReadableBitmap createdBitmap;
            IWriteableBitmap loadedBitmap;
            bool allFilesLoadedSuccessfully = true;

            if (TexPanelExceedsMaxDimensions())
                return;

            CancellationToken token;

            lock (_updateTaskLock)
            {
                _updateTaskCts?.Cancel(); 
                _updateTaskCts = new CancellationTokenSource();
                token = _updateTaskCts.Token;
            }

            try
            {
                int texPerRow = PanelWidth / _textureSize;
                int newHeight = ((_selectedFiles.Count - 1) / texPerRow + 1) * _textureSize;

                if (newHeight > Presenter.PixelHeight)
                    Presenter = _bitmapOperations.Resize(Presenter, PanelWidth, newHeight + _textureSize);
                else if (newHeight < Presenter.PixelHeight)
                    Presenter = _bitmapOperations.Resize(Presenter, PanelWidth, newHeight);

                for (int i = 0; i < _selectedFiles.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    var file = _selectedFiles[i];

                    if (!_asyncFileLoader.SupportedExtensions.Contains(Path.GetExtension(file)))
                        continue;

                    try
                    {
                        data = await Task.Run(
                            function: () => _asyncFileLoader.LoadCore(file),
                            cancellationToken: token);

                        createdBitmap = _mediaFactory.CreateBitmapFromRaw(
                            pixelWidth:     _asyncFileLoader.LoadedWidth,
                            pixelHeight:    _asyncFileLoader.LoadedHeight,
                            hasAlpha:       _asyncFileLoader.LoadedHasAlpha,
                            pixels:         data,
                            stride:         _asyncFileLoader.LoadedStride);

                        loadedBitmap = _mediaFactory.CreateRescaledBitmap(
                            source:     _mediaFactory.CloneBitmap(createdBitmap),
                            newWidth:   _textureSize, 
                            newHeight:  _textureSize);
                    }
                    catch (OperationCanceledException) 
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        allFilesLoadedSuccessfully = false;
                        _logger.LogError(ex);
                        continue;
                    }

                    int x = (successCount % texPerRow) * _textureSize;
                    int y = (successCount / texPerRow) * _textureSize;

                    _bitmapOperations.FillRectBitmapNoConvert(
                        source:     loadedBitmap,
                        target:     Presenter,
                        pos:        (x, y));

                    successCount++;

                    await Task.Delay(1, token);
                    await Task.Yield(); 
                }
            }
            catch (OperationCanceledException) {}
            catch (Exception ex) 
            {
                _logger.LogError(ex);
                throw; 
            }

            if (!allFilesLoadedSuccessfully)
            {
                _messageService.SendMessage(MessageType.BatchLoaderPanelLoadIssues);
            }
        }

        private async Task UpdatePresenterNumChangedAsync(int oldNum, int newNum)
        {
            int successCount = oldNum;
            byte[] data;
            IReadableBitmap createdBitmap;
            IWriteableBitmap loadedBitmap;

            if (TexPanelExceedsMaxDimensions())
                return;

            CancellationToken token;

            lock (_updateTaskLock)
            {
                _updateTaskCts?.Cancel(); 
                _updateTaskCts = new CancellationTokenSource();
                token = _updateTaskCts.Token;
            }

            try
            {
                int texPerRow = PanelWidth / _textureSize;
                int newHeight = ((newNum - 1) / texPerRow + 1) * _textureSize;

                if (newNum > oldNum)
                {
                    if (newHeight > Presenter.PixelHeight)
                        Presenter = _bitmapOperations.Resize(Presenter, PanelWidth, newHeight + _textureSize);

                    for (int i = oldNum; i < newNum; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        if (i >= _selectedFiles.Count) break;
                        var file = _selectedFiles[i];

                        if (!_asyncFileLoader.SupportedExtensions.Contains(Path.GetExtension(file)))
                            continue;

                        try
                        {
                            data = await Task.Run(
                                function:          () => _asyncFileLoader.LoadCore(file),
                                cancellationToken: token);

                            createdBitmap = _mediaFactory.CreateBitmapFromRaw(
                                pixelWidth: _asyncFileLoader.LoadedWidth,
                                pixelHeight: _asyncFileLoader.LoadedHeight,
                                hasAlpha: _asyncFileLoader.LoadedHasAlpha,
                                pixels: data,
                                stride: _asyncFileLoader.LoadedStride);

                            loadedBitmap = _mediaFactory.CreateRescaledBitmap(
                                source: _mediaFactory.CloneBitmap(createdBitmap),
                                newWidth: _textureSize,
                                newHeight: _textureSize);

                        }
                        catch (OperationCanceledException) 
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex);
                            continue;
                        }

                        int x = (successCount % texPerRow) * _textureSize;
                        int y = (successCount / texPerRow) * _textureSize;

                        _bitmapOperations.FillRectBitmapNoConvert(
                            source:     loadedBitmap,
                            target:     Presenter,
                            pos:        (x, y));

                        successCount++;

                        await Task.Delay(1, token);
                        await Task.Yield();
                    }
                }
                else if (newNum < oldNum)
                {
                    if (newHeight < Presenter.PixelHeight)
                        Presenter = _bitmapOperations.Resize(Presenter, PanelWidth, newHeight);

                    for (int i = oldNum - 1; i >= newNum; i--)
                    {
                        int x = (i % texPerRow) * _textureSize;
                        int y = (i / texPerRow) * _textureSize;

                        _bitmapOperations.FillRectColor(Presenter, new PixelRect(x, y, _textureSize, _textureSize));
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }

            OnPropertyChanged(nameof(ContentActualHeight));
        }

        private bool TexPanelExceedsMaxDimensions()
        {
            int newHeight = ((_numTextures - 1) / (_presenter.PixelWidth / _textureSize)) * _textureSize;

            if (newHeight > MAX_PIXEL_HEIGHT)
            {
                _messageService.SendMessage(MessageType.BatchLoaderPanelExceedsMaxDimensions);
                return true;
            }
            return false;
        }

        private void SelectFolder(string folderName = "")
        {
            if (String.IsNullOrEmpty(folderName))
                if (_fileService.SelectFolderDialog() == true)
                    folderName = _fileService.SelectedPath;
                else return;

            if (!Directory.Exists(folderName))
            {
                _messageService.SendMessage(MessageType.BatchLoaderFolderSetFail);
                return;
            }

            _selectedFolderPath = folderName;

            string searchPath = Path.Combine(_selectedFolderPath);
        
            _allFiles = Directory.EnumerateFiles(searchPath)
                .Where(file => _asyncFileLoader.SupportedExtensions.Contains(Path.GetExtension(file)))
                .OrderBy(Path.GetFileName)
                .ToList();
        
            if (_allFiles.Count == 0)
            {
                _messageService.SendMessage(MessageType.BatchLoaderFolderSetNoImageFiles);
                return;
            }

            _messageService.SendMessage(MessageType.BatchLoaderFolderSetSuccess);
            _usageData.AddRecentBatchLoaderFolder(folderName);

            _selectedFiles = _allFiles
                .Skip(_startTexIndex)
                .Take(_numTextures)
                .ToList();

            IsDropHintVisible = false;

            _ = UpdatePresenterAsync();
        }

        public void FileDrop(List<string> files)
        {
            _allFiles = files
                .Where(file => _asyncFileLoader.SupportedExtensions.Contains(Path.GetExtension(file)))
                .OrderBy(Path.GetFileName)
                .ToList();

            if (_allFiles.Count == 0)
            {
                _messageService.SendMessage(MessageType.BatchLoaderDropNoImageFiles);
                return;
            }

            _messageService.SendMessage(MessageType.BatchLoaderDropSuccess);

            _selectedFiles = new List<string>(_allFiles);

            _numTextures = _selectedFiles.Count;
            OnPropertyChanged(nameof(NumTextures));
            _startTexIndex = 0;
            OnPropertyChanged(nameof(StartTexIndex));

            IsDropHintVisible = false;

            _ = UpdatePresenterAsync();
        }

        private void SetScalingModeIndex(int value)
        {
            if (value < 0 || value > 3)
                throw new ArgumentOutOfRangeException(nameof(value), "Scaling mode index must be between 0 and 3.");

            _bitmapScalingMode = (BitmapScalingMode)(value + 1);

            OnPropertyChanged(nameof(ScalingModeIndex));
            _ = UpdatePresenterAsync();
        }

        private void SetZoom(double value)
        {
            SetProperty(ref _zoom, value, nameof(Zoom));

            OnPropertyChanged(nameof(ContentActualWidth));
            OnPropertyChanged(nameof(ContentActualHeight));
        }

        private void SetPresenter(IWriteableBitmap value)
        {
            SetProperty(ref _presenter, value, nameof(Presenter));

            OnPropertyChanged(nameof(ContentActualWidth));
            OnPropertyChanged(nameof(ContentActualHeight));
        }

        private int CalculateNewPanelWidth(int proposedValue, int currentValue)
            => proposedValue < currentValue ? proposedValue switch
            {
                < 1024 => 512,
                < 2048 => 1024,
                < 4096 => 2048,
                _      => 4096,
            }
            : proposedValue switch
            {

                > 2048 => 4096,
                > 1024 => 2048,
                > 512  => 1024,
                _      => 512,
            };

        private int NextLowerPowerOfTwo(int n)
        {
            n |= (n >> 1);
            n |= (n >> 2);
            n |= (n >> 4);
            n |= (n >> 8);
            n |= (n >> 16);
            return n - (n >> 1);
        }

        private int NextHigherPowerOfTwo(int n)
        {
            if (n < 1) return 1;
            n--;
            n |= (n >> 1);
            n |= (n >> 2);
            n |= (n >> 4);
            n |= (n >> 8);
            n |= (n >> 16);
            return n + 1;
        }
    }
}
