using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.FileHandling;
using TgaBuilderLib.Messaging;
using TgaBuilderLib.Utils;

namespace TgaBuilderLib.ViewModel
{
    public class BatchLoaderViewModel : ViewModelBase
    {
        public BatchLoaderViewModel(
            IFileService fileService,
            IMessageService messageService,

            IUsageData usageData,
            IAsyncFileLoader asyncFileLoader, 
            IBitmapOperations bitmapOperations,
            ILogger logger)
        {
            _fileService = fileService;
            _messageService = messageService;

            _usageData = usageData;
            _asyncFileLoader = asyncFileLoader;
            _logger = logger;

            _bitmapOperations = bitmapOperations;
        }

        private const int TEX_MIN_SIZE = 64;
        private const int TEX_MAX_SIZE = 512;
        private const int MAX_PIXEL_HEIGHT = 32768;

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

        private WriteableBitmap _presenter = new(
            pixelWidth:     512,
            pixelHeight:    1536,
            dpiX:           96,
            dpiY:           96,
            pixelFormat:    PixelFormats.Bgra32,
            palette:        null);

        private string _selectedFolderPath = string.Empty;
        private int _startTexIndex = 0;
        private int _numTextures = 11;
        private int _textureSize = 128;
        private int _panelWidth = 512;
        private bool _isDropHintVisible = true;

        private RelayCommand? _selectTRRFolderCommand;
        private RelayCommand<IView>? _importCommand;
        private RelayCommand<IView>? _cancelCommand;
        private RelayCommand<string>? openRecentFolderCommand;
        private RelayCommand<List<string>>? _fileDropCommand;


        public IEnumerable<string> RecentBatchLoaderFolders => _usageData.RecentBatchLoaderFolders;

        public WriteableBitmap Presenter
        {
            get => _presenter;
            set => SetProperty(ref _presenter, value, nameof(Presenter));
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

        public bool IsDropHintVisible
        {
            get => _isDropHintVisible;
            set => SetProperty(ref _isDropHintVisible, value, nameof(IsDropHintVisible));
        }

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

            WriteableBitmap paddedBitmap = new WriteableBitmap(
                pixelWidth: PanelWidth,
                pixelHeight: paddedHeight,
                dpiX: 96,
                dpiY: 96,
                pixelFormat: PixelFormats.Bgra32,
                palette: null);

            byte[] blackPixels = new byte[paddedHeight * stride];
            for (int i = 0; i < blackPixels.Length; i += 4)
            {
                blackPixels[i + 0] = 0; // B
                blackPixels[i + 1] = 0; // G
                blackPixels[i + 2] = 0; // R
                blackPixels[i + 3] = 0; // A
            }

            paddedBitmap.WritePixels(
                sourceRect: new Int32Rect(0, 0, PanelWidth, paddedHeight),
                pixels:     blackPixels, 
                stride:     stride, 
                offset:     0);

            // Copy original pixels to the top of the padded bitmap
            CroppedBitmap croppedSource = new CroppedBitmap(
                source:     Presenter, 
                sourceRect: new Int32Rect(0, 0, PanelWidth, Presenter.PixelHeight));

            int srcStride = PanelWidth * 4;
            byte[] srcPixels = new byte[Presenter.PixelHeight * srcStride];

            croppedSource.CopyPixels(
                pixels: srcPixels, 
                stride: srcStride, 
                offset: 0);

            paddedBitmap.WritePixels(
                sourceRect: new Int32Rect(0, 0, PanelWidth, Presenter.PixelHeight),
                pixels:     srcPixels, 
                stride:     srcStride, 
                offset:     0);

            Presenter = paddedBitmap;
        }

        private async Task UpdatePresenterAsync()
        {
            int successCount = 0;
            byte[] data;
            WriteableBitmap loadedBitmap;
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

                    loadedBitmap = _bitmapOperations.CreateBitmapAndResize(
                        data:           data,
                        width:          _asyncFileLoader.LoadedWidth,
                        height:         _asyncFileLoader.LoadedHeight,
                        stride:         _asyncFileLoader.LoadedStride,
                        pixelFormat:    _asyncFileLoader.LoadedPixelFormat,
                        targetWidth:    _textureSize, 
                        targetHeight:   _textureSize, 
                        scalingMode:    BitmapScalingMode.NearestNeighbor);

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
            WriteableBitmap loadedBitmap;

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


                        loadedBitmap = _bitmapOperations.CreateBitmapAndResize(
                            data:           data,
                            width:          _asyncFileLoader.LoadedWidth,
                            height:         _asyncFileLoader.LoadedHeight,
                            stride:         _asyncFileLoader.LoadedStride,
                            pixelFormat:    _asyncFileLoader.LoadedPixelFormat,
                            targetWidth:    _textureSize,
                            targetHeight:   _textureSize,
                            scalingMode:    BitmapScalingMode.NearestNeighbor);

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

                        _bitmapOperations.FillRectColor(Presenter, new Int32Rect(x, y, _textureSize, _textureSize));
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
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
