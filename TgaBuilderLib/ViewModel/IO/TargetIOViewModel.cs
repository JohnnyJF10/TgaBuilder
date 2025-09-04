using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Enums;
using TgaBuilderLib.FileHandling;
using TgaBuilderLib.Messaging;
using TgaBuilderLib.UndoRedo;
using TgaBuilderLib.Utils;
using ResizeMode = TgaBuilderLib.Enums.ResizeMode;

namespace TgaBuilderLib.ViewModel
{
    public class TargetIOViewModel : IOViewModelBase
    {
        public TargetIOViewModel(
            Func<ViewIndex, IView> getViewCallback,
            IMediaFactory mediaFactory,
            IDispatcherService dispatcherService,
            IFileService fileService,
            IMessageService messageService,
            IMessageBoxService messageBoxService,
            IImageFileManager imageManager,
            ILogger logger,
            IUndoRedoManager undoRedoManager,
            IUsageData usageData,
            TexturePanelViewModelBase panel)
            : base(getViewCallback, fileService, messageService, imageManager, logger, usageData, dispatcherService, panel)
        {
            _mediaFactory = mediaFactory;
            _messageBoxService = messageBoxService;
            _undoRedoManager = undoRedoManager;
        }

        private const FileTypes DEF_FILE_TYPES =
            FileTypes.TGA | FileTypes.BMP | FileTypes.PNG | FileTypes.JPG
            | FileTypes.JPEG | FileTypes.PSD | FileTypes.DDS;


        private const FileTypes WRITEABLE_FILE_TYPES =
            FileTypes.TGA | FileTypes.BMP | FileTypes.PNG | FileTypes.JPG
            | FileTypes.JPEG;

        private static bool IsHandleableSaveFileException(Exception e)
            => e is FileNotFoundException
            or DirectoryNotFoundException
            or ArgumentException
            or ArgumentNullException
            or FormatException
            or NotSupportedException
            or InvalidOperationException;

        private readonly IMediaFactory _mediaFactory;
        private readonly IUndoRedoManager _undoRedoManager;
        private readonly IMessageBoxService _messageBoxService;


        public IEnumerable<string> RecentDestinationFileNames => _usageData.RecentOutputFiles;




        public void SetupOpenTask(string? fileName = null, List<FileTypes>? fileTypes = null)
        {
            if (_ioTask != null && !_ioTask.IsCompleted)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _ioTask.Wait();
            }

            _ioTask = Open(fileName);
        }

        public void SetupSaveTask(string? fileName = null)
        {
            if (_ioTask != null && !_ioTask.IsCompleted)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _ioTask.Wait();
            }
            _ioTask = Save(fileName);
        }

        public void SaveCurrent() => SetupSaveTask(_lastFilePath);

        public async Task CopyEntire(IWriteableBitmap bitmap)
        {
            if (_undoRedoManager.IsTargetDirty())
            {
                var result = _messageBoxService.ShowYesNoCancelMessageBox(
                    Header: "Save changes?",
                    Message: "Do you want to save changes?")
                    .Result;
                switch (result)
                {
                    case YesNoCancel.Yes:
                        if (await Save(_lastFilePath)) break;
                        else return;
                    case YesNoCancel.No: break;
                    case YesNoCancel.Cancel: return;
                }
            }

            _panel.Presenter = _imageManager.GetDestinationConfirmBitmap(bitmap);
            _panel.RefreshPresenter();

            _lastFilePath = string.Empty;
            OnPropertyChanged(nameof(LastFileName));

            _undoRedoManager.ClearAllNewFile();
        }

        public async Task NewFile()
        {
            if (_undoRedoManager.IsTargetDirty())
            {
                var result = _messageBoxService.ShowYesNoCancelMessageBox(
                    Header: "Save changes?",
                    Message: "Do you want to save changes?")
                    .Result;
                switch (result)
                {
                    case YesNoCancel.Yes:
                        if (await Save(_lastFilePath)) break;
                        else return;
                    case YesNoCancel.No: break;
                    case YesNoCancel.Cancel: return;
                }
            }

            _panel.Presenter = _mediaFactory.CreateEmptyBitmap(
                width: 256,
                height: 1536,
                hasAlpha: true);

            _lastFilePath = "";
            OnPropertyChanged(nameof(LastFileName));

            _undoRedoManager.ClearAllNewFile();
        }

        private async Task Open(string? fileName = null)
        {
            if (_undoRedoManager.IsTargetDirty())
            {
                var result = _messageBoxService.ShowYesNoCancelMessageBox(
                    Header: "Save changes?",
                    Message: "Do you want to save changes?")
                    .Result;
                switch (result)
                {
                    case YesNoCancel.Yes:
                        if (await Save(_lastFilePath)) break;
                        else return;
                    case YesNoCancel.No: break;
                    case YesNoCancel.Cancel: return;
                }
            }

            if (String.IsNullOrEmpty(fileName))
                if (_fileService.OpenFileDialog(DEF_FILE_TYPES) == true)
                    fileName = _fileService.SelectedPath;
                else return;

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            try
            {
                SetControlsStateForLoading();

                await Task.Run(() => _imageManager.LoadImageFile(
                    fileName: fileName,
                    mode: ResizeMode.SourceResize,
                    cancellationToken: token));

                _panel.Presenter = _imageManager.GetLoadedBitmap();
            }
            catch (OperationCanceledException)
            {
                _messageService.SendMessage(MessageType.DestinationOpenCancelledByUser);
                return;
            }
            catch (Exception e) when (IsHandleableOpenFileException(e))
            {
                _messageService.SendMessage(MessageType.DestinationOpenError, ex: e);
                _logger.LogError(e);
                return;
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

            _usageData.AddRecentOutputFile(fileName);

            _lastFilePath = IsFileWriteable(fileName) ? fileName : string.Empty;
            OnPropertyChanged(nameof(LastFileName));

            _undoRedoManager.ClearAllNewFile();

            var resMessage = _imageManager.ResultInfo switch
            {
                ResultStatus.Success => MessageType.DestinationOpenSuccess,
                ResultStatus.RezisingRequired => MessageType.DestinationOpenSuccessButResized,
                ResultStatus.BitmapAreaNotSufficient => MessageType.DestinationOpenSuccessButIncomplete,
                _ => MessageType.UnknownError
            };
            _dispatcherService.Invoke(() => _messageService.SendMessage(resMessage));
        }

        private async Task<bool> Save(string? fileName = null)
        {
            if (String.IsNullOrEmpty(fileName) || !IsFileWriteable(fileName))
                if (_fileService.SaveFileDialog(WRITEABLE_FILE_TYPES) == true)
                    fileName = _fileService.SelectedPath;
                else return false;

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            try
            {
                SetControlsStateForLoading();

                _imageManager.SaveImageFile(
                    fileName: fileName,
                    bitmap: _panel.Presenter);
                
                await Task.Run(() => _imageManager.WriteImageFile(fileName, token));
            }
            catch (OperationCanceledException)
            {
                _messageService.SendMessage(MessageType.DestinationSaveCancelledByUser);
                return false;
            }
            catch (Exception e) when (IsHandleableSaveFileException(e))
            {
                _messageService.SendMessage(MessageType.DestinationSaveError);
                _logger.LogError(e);
                return false;
            }
            finally
            {
                _imageManager.ClearLoadedData();
                SetControlsStateAfterLoading();
                _cancellationTokenSource?.Dispose();
            }

            _lastFilePath = fileName;
            OnPropertyChanged(nameof(LastFileName));

            _usageData.AddRecentOutputFile(fileName);

            _undoRedoManager.TakeStatusSnapshot();

            _messageService.SendMessage(MessageType.DestinationSaveSuccess);
            return true;
        }

        private bool IsFileWriteable(string filePath)
        {
            string extension = Path.GetExtension(filePath)?.TrimStart('.').ToLower() ?? "";

            foreach (FileTypes type in Enum.GetValues(typeof(FileTypes)))
            {
                if (type == FileTypes.None)
                    continue;

                if (WRITEABLE_FILE_TYPES.HasFlag(type)
                    && type.ToString().ToLower() == extension)
                    return true;
            }
            return false;
        }
    }
}
