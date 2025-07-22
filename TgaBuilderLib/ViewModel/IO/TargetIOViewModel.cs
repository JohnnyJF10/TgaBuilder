using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Commands;
using TgaBuilderLib.FileHandling;
using TgaBuilderLib.Messaging;
using TgaBuilderLib.UndoRedo;
using TgaBuilderLib.Utils;
using ResizeMode = TgaBuilderLib.Abstraction.ResizeMode;

namespace TgaBuilderLib.ViewModel
{
    public class TargetIOViewModel : ViewModelBase
    {
        public TargetIOViewModel(
            Func<ViewIndex, IView> getViewCallback,
            IFileService fileService,
            IMessageService messageService,
            IMessageBoxService messageBoxService,
            IImageFileManager imageManager,
            ILogger logger,
            IUndoRedoManager undoRedoManager,
            IUsageData usageData,

            TargetTexturePanelViewModel destination)
        {
            _getViewCallback = getViewCallback;
            _fileService = fileService;
            _messageService = messageService;
            _messageBoxService = messageBoxService;
            _imageManager = imageManager;
            _logger = logger;
            _undoRedoManager = undoRedoManager;
            _usageData = usageData;

            Destination = destination;
        }

        private const FileTypes DEF_FILE_TYPES =
            FileTypes.TGA | FileTypes.BMP | FileTypes.PNG | FileTypes.JPG
            | FileTypes.JPEG | FileTypes.PSD | FileTypes.DDS;


        private const FileTypes WRITEABLE_FILE_TYPES =
            FileTypes.TGA | FileTypes.BMP | FileTypes.PNG | FileTypes.JPG
            | FileTypes.JPEG;

        private static bool IsHandleableOpenFileException(Exception e)
            => e is FileNotFoundException
            or DirectoryNotFoundException
            or FileFormatException
            or NotSupportedException
            or InvalidOperationException;

        private static bool IsHandleableSaveFileException(Exception e)
            => e is FileNotFoundException
            or DirectoryNotFoundException
            or ArgumentException
            or ArgumentNullException
            or FileFormatException
            or NotSupportedException
            or InvalidOperationException;

        private readonly Func<ViewIndex, IView> _getViewCallback;

        private readonly IFileService _fileService;
        private readonly IMessageService _messageService;
        private readonly IMessageBoxService _messageBoxService;
        private readonly IImageFileManager _imageManager;

        private readonly ILogger _logger;
        private readonly IUndoRedoManager _undoRedoManager;
        private IUsageData _usageData;

        private bool _isLoading;
        private bool _controlEnabled = true;
        private string _lastDestinationFilePath = string.Empty;


        public TargetTexturePanelViewModel Destination { get; set; }

        public IEnumerable<string> RecentDestinationFileNames => _usageData.RecentOutputFiles;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetPropertyPrimitive(ref _isLoading, value, nameof(IsLoading));
        }
        public bool ControlsEnabled
        {
            get => _controlEnabled;
            set => SetPropertyPrimitive(ref _controlEnabled, value, nameof(ControlsEnabled));
        }


        public async Task CopyEntire(WriteableBitmap bitmap)
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
                        if (await Save(_lastDestinationFilePath)) break;
                        else return;
                    case YesNoCancel.No: break;
                    case YesNoCancel.Cancel: return;
                }
            }

            Destination.Presenter = _imageManager.GetDestinationConfirmBitmap(bitmap);
            Destination.RefreshPresenter();

            _lastDestinationFilePath = string.Empty;

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
                        if (await Save(_lastDestinationFilePath)) break;
                        else return;
                    case YesNoCancel.No: break;
                    case YesNoCancel.Cancel: return;
                }
            }

            Destination.Presenter = new WriteableBitmap(
                pixelWidth: 256,
                pixelHeight: 1536,
                dpiX: 96,
                dpiY: 96,
                pixelFormat: PixelFormats.Rgb24,
                palette: null);

            _lastDestinationFilePath = string.Empty;

            _undoRedoManager.ClearAllNewFile();
        }

        public async Task Open(string? fileName = null)
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
                        if (await Save(_lastDestinationFilePath)) break;
                        else return;
                    case YesNoCancel.No: break;
                    case YesNoCancel.Cancel: return;
                }
            }

            if (String.IsNullOrEmpty(fileName))
                if (_fileService.OpenFileDialog(DEF_FILE_TYPES) == true)
                    fileName = _fileService.SelectedPath;
                else return;

            try
            {
                SetControlsStateForLoading();

                await Task.Run(() => _imageManager.LoadImageFile(
                    fileName: fileName,
                    targetFormat: PixelFormats.Rgb24,
                    mode: ResizeMode.SourceResize));

                Destination.Presenter = _imageManager.GetLoadedBitmap();
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
            }

            _usageData.AddRecentOutputFile(fileName);

            _lastDestinationFilePath = IsFileWriteable(fileName) ? fileName : string.Empty;

            _undoRedoManager.ClearAllNewFile();

            var resMessage = _imageManager.ResultInfo switch
            {
                ResultStatus.Success => MessageType.DestinationOpenSuccess,
                ResultStatus.RezisingRequired => MessageType.DestinationOpenSuccessButResized,
                ResultStatus.BitmapAreaNotSufficient => MessageType.DestinationOpenSuccessButIncomplete,
                _ => MessageType.UnknownError
            };
            Application.Current.Dispatcher.Invoke(() => _messageService.SendMessage(resMessage));
        }

        public async Task<bool> Save(string? fileName = null)
        {
            if (String.IsNullOrEmpty(fileName) || !IsFileWriteable(fileName))
                if (_fileService.SaveFileDialog(WRITEABLE_FILE_TYPES) == true)
                    fileName = _fileService.SelectedPath;
                else return false;

            try
            {
                SetControlsStateForLoading();

                _imageManager.SaveImageFile(
                    fileName: fileName,
                    bitmap: Destination.Presenter);
                
                await Task.Run(() => _imageManager.WriteImageFile(fileName));
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
            }

            _lastDestinationFilePath = fileName;
            _usageData.AddRecentOutputFile(fileName);

            _undoRedoManager.TakeStatusSnapshot();

            _messageService.SendMessage(MessageType.DestinationSaveSuccess);
            return true;
        }

        public async Task<bool> SaveCurrent() => await Save(_lastDestinationFilePath);

        private void SetControlsStateForLoading()
        {
            IsLoading = true;
            ControlsEnabled = false;
        }

        private void SetControlsStateAfterLoading()
        {
            IsLoading = false;
            ControlsEnabled = true;
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
