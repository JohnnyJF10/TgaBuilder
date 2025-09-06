using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Enums;
using TgaBuilderLib.FileHandling;
using TgaBuilderLib.Messaging;
using TgaBuilderLib.Utils;

namespace TgaBuilderLib.ViewModel
{
    public class SourceIOViewModel : IOViewModelBase
    {
        public SourceIOViewModel(
            Func<ViewIndex, IView> getViewCallback,
            IFileService fileService,
            IMessageService messageService,
            IImageFileManager imageManager,
            ILogger logger,
            IUsageData usageData,
            IDispatcherService dispatcherService,
            TexturePanelViewModelBase panel)
            : base(getViewCallback, fileService, messageService, imageManager, logger, usageData, dispatcherService, panel) {}

        private const FileTypes DEF_FILE_TYPES =
            FileTypes.TGA | FileTypes.BMP | FileTypes.PNG | FileTypes.JPG
            | FileTypes.JPEG | FileTypes.PSD | FileTypes.DDS;

        private const FileTypes TR_FILE_TYPES = FileTypes.PHD | FileTypes.TR2
            | FileTypes.TR4 | FileTypes.TRC | FileTypes.TEN;


        private Dictionary<FileTypes, string>? _extensions;

        private string _previousFile = string.Empty;
        private string _nextFile = string.Empty;


        public IList<string> LastFolderFileNames { get; private set; } = new List<string>();

        public IEnumerable<string> RecentSourceFileNames => _usageData.RecentInputFiles;

        public string ReloadFileText => "Reload " + Path.GetFileName(_lastFilePath);
        public string NextFileOpenText => "Open " + Path.GetFileName(_nextFile);
        public string PreviousFileOpenText => "Open " + Path.GetFileName(_previousFile);


        public bool TrImportRepackingSelected
        {
            get => _imageManager.TrImportRepackingSelected;
            set => SetTrImportRepackingSelected(value);
        }

        public int TrImportHorPageNum
        {
            get => _imageManager.TrImportHorPageNum;
            set => SetTrImportHorPageNum(value);
        }



        public void SetTrImportRepackingSelected(bool value)
        {
            if (value == _imageManager.TrImportRepackingSelected)
                return;
            _imageManager.TrImportRepackingSelected = value;
            OnPropertyChanged(nameof(TrImportRepackingSelected));
        }

        public void SetTrImportHorPageNum(int num)
        {
            if (num == _imageManager.TrImportHorPageNum)
                return;

            var newValue = CalculateNewPageXValue(num, _imageManager.TrImportHorPageNum);
            _imageManager.TrImportHorPageNum = newValue;
            OnPropertyChanged(nameof(TrImportHorPageNum));
        }

        public void BatchLoader()
        {
            var batchLoaderView = _getViewCallback(ViewIndex.BatchLoader);
            if (batchLoaderView.DataContext is not BatchLoaderViewModel batchLoaderVM) 
                return;

            batchLoaderView.ShowDialog();

            if (batchLoaderView.DialogResult != true) 
                return;

            _panel.SetPresenter(batchLoaderVM.Presenter);

            ResetVisualGrid();
        }

        public void SetupOpenTask(string? fileName = null, List<FileTypes>? fileTypes = null)
        {
            if (_ioTask != null && !_ioTask.IsCompleted)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _ioTask.Wait();
            }

            _ioTask = Open(fileName, fileTypes);
        }

        public void SetupReloadTask()
        {
            if (_ioTask != null && !_ioTask.IsCompleted)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _ioTask.Wait();
            }

            _ioTask = Reload();
        }

        public void OpenTr() => SetupOpenTask(fileTypes: new() { TR_FILE_TYPES, DEF_FILE_TYPES });

        private async Task Open(string? fileName = null, List<FileTypes>? fileTypes = null)
        {
            if (fileTypes == null)
                fileTypes = new List<FileTypes>
                {
                    DEF_FILE_TYPES,
                    TR_FILE_TYPES
                };

            if (String.IsNullOrEmpty(fileName))
                if (_fileService.OpenFileDialog(fileTypes) == true)
                    fileName = _fileService.SelectedPath;
                else return;

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            try
            {
                SetControlsStateForLoading();

                await Task.Run(() => _imageManager.LoadImageFile(
                    fileName: fileName,
                    mode: Enums.ResizeMode.SourceResize,
                    cancellationToken: token));

                _panel.Presenter = _imageManager.GetLoadedBitmap();
            }
            catch (OperationCanceledException) 
            {
                _messageService.SendMessage(MessageType.SourceOpenCancelledByUser);
                return;
            }
            catch (Exception e) when (IsHandleableOpenFileException(e))
            {
                _messageService.SendMessage(MessageType.SourceOpenError, ex: e);
                _logger.LogError(e);
                return;
            }
            catch (Exception e)
            {
                _messageService.SendMessage(MessageType.UnknownError, ex: e);
                _logger.LogError(e);
                throw;
            }
            finally
            {
                _imageManager.ClearLoadedData();
                SetControlsStateAfterLoading();
                _cancellationTokenSource?.Dispose();
            }

            _usageData.AddRecentInputFile(fileName);

            GetPreviousAndNextFileNames(fileName);

            ResetVisualGrid();

            SendLoadStatus();
        }

        private void SendLoadStatus()
        {
            var resMessage = _imageManager.ResultInfo switch
            {
                ResultStatus.Success => MessageType.SourceOpenSuccess,
                ResultStatus.RezisingRequired => MessageType.SourceOpenSuccessButResized,
                ResultStatus.BitmapAreaNotSufficient => MessageType.SourceOpenSuccessButIncomplete,
                _ => MessageType.UnknownError
            };

            _dispatcherService.Invoke(() => _messageService.SendMessage(resMessage));
        }

        private async Task Reload()
        {
            if (_usageData.RecentInputFiles.Count == 0)
                return;

            string fileName = _usageData.RecentInputFiles.FirstOrDefault()!;

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            try
            {
                SetControlsStateForLoading();

                await Task.Run(() => _imageManager.LoadImageFile(
                    fileName: fileName,
                    mode: Enums.ResizeMode.SourceResize,
                    cancellationToken: token));

                _panel.MouseLeave();

                _panel.Presenter = _imageManager.GetLoadedBitmap();
            }
            catch (OperationCanceledException) { }
            catch (Exception e) when (IsHandleableOpenFileException(e))
            {
                _messageService.SendMessage(MessageType.SourceOpenError, ex: e);
                _logger.LogError(e);
            }
            catch (Exception e)
            {
                _logger.LogError(e);
                throw;
            }
            finally
            {
                _imageManager.ClearLoadedData();
                SetControlsStateAfterLoading();
                _cancellationTokenSource?.Dispose();
            }
        }

        public void OpenPreviosFile() => SetupOpenTask(_previousFile);

        public void OpenNextFile() => SetupOpenTask(_nextFile);

        private void GetPreviousAndNextFileNames(string fileName)
        {
            _lastFilePath = fileName;

            LastFolderFileNames = GetFilesWithSpecificExtensions(
                Path.GetDirectoryName(fileName) ?? string.Empty,
                DEF_FILE_TYPES | TR_FILE_TYPES);

            int currentIndex = LastFolderFileNames.IndexOf(_lastFilePath);

            if (currentIndex < 1)
                _previousFile = LastFolderFileNames[LastFolderFileNames.Count - 1];
            else
                _previousFile = LastFolderFileNames[currentIndex - 1];
            
            if (currentIndex < 0 || currentIndex >= LastFolderFileNames.Count - 1)
                _nextFile = LastFolderFileNames[0];
            else
                _nextFile = LastFolderFileNames[currentIndex + 1];

            OnPropertyChanged(nameof(LastFileName));
            OnPropertyChanged(nameof(ReloadFileText));
            OnPropertyChanged(nameof(PreviousFileOpenText));
            OnPropertyChanged(nameof(NextFileOpenText));
        }

        private List<string> GetFilesWithSpecificExtensions(string directory, FileTypes fileTypes)
        {
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException("The specified directory does not exist: " + directory);

            var extensions = _extensions ?? Enum.GetValues(typeof(FileTypes))
                .Cast<FileTypes>()
                .Where(ft => ft != FileTypes.None)
                .ToDictionary(ft => ft, ft => $"*.{ft.ToString().ToLower()}");

            List<string> files = new List<string>();

            foreach (var entry in extensions)
            {
                if (fileTypes.HasFlag(entry.Key))
                {
                    files.AddRange(Directory.GetFiles(directory, entry.Value, SearchOption.TopDirectoryOnly));
                }
            }

            return files;
        }

        public void CopyEntire(IWriteableBitmap bitmap)
        {
            _panel.Presenter = _imageManager.GetDestinationConfirmBitmap(bitmap);
            _panel.RefreshPresenter();
        }

        private void ResetVisualGrid()
        {
            if (_panel is SourceTexturePanelViewModel sourcePanel)
                sourcePanel.VisualGrid.Reset();
        }

        private int CalculateNewPageXValue(int proposedValue, int currentValue)
            => proposedValue < currentValue ? proposedValue switch
            {
                < 2 => 1,
                < 4 => 2,
                < 8 => 4,
                _ => 8
            }
            : proposedValue switch
            {
                > 8 => 16,
                > 4 => 8,
                > 2 => 4,
                _ => 2
            };

    }
}
