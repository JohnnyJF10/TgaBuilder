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
using TgaBuilderLib.Utils;

namespace TgaBuilderLib.ViewModel
{
    public class SourceIOViewModel : ViewModelBase
    {
        public SourceIOViewModel(
            Func<ViewIndex, IView> getViewCallback,
            IFileService fileService,
            IMessageService messageService,
            IImageFileManager imageManager,
            ILogger logger,
            IUsageData usageData,
            
            SourceTexturePanelViewModel source)
        {
            _getViewCallback = getViewCallback;
            _fileService = fileService;
            _messageService = messageService;
            _imageManager = imageManager;
            _logger = logger;
            _usageData = usageData;

            Source = source;
        }

        private const FileTypes DEF_FILE_TYPES =
            FileTypes.TGA | FileTypes.BMP | FileTypes.PNG | FileTypes.JPG
            | FileTypes.JPEG | FileTypes.PSD | FileTypes.DDS;

        private const FileTypes TR_FILE_TYPES = FileTypes.PHD | FileTypes.TR2
            | FileTypes.TR4 | FileTypes.TRC | FileTypes.TEN;

        private static bool IsHandleableOpenFileException(Exception e)
            => e is FileNotFoundException
            or DirectoryNotFoundException
            or FileFormatException
            or NotSupportedException
            or InvalidOperationException;

        private readonly Func<ViewIndex, IView> _getViewCallback;

        private readonly IFileService _fileService;
        private readonly IMessageService _messageService;

        private readonly IImageFileManager _imageManager;

        private readonly ILogger _logger;

        private IUsageData _usageData;

        private bool _isLoading;
        private bool _controlsEnabled = true;


        public SourceTexturePanelViewModel Source { get; set; }

        public IEnumerable<string> RecentSourceFileNames => _usageData.RecentInputFiles;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetPropertyPrimitive(ref _isLoading, value, nameof(IsLoading));
        }

        public bool ControlsEnabled
        {
            get => _controlsEnabled;
            set => SetPropertyPrimitive(ref _controlsEnabled, value, nameof(ControlsEnabled));
        }

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

            Source.SetPresenter(batchLoaderVM.Presenter);

            Source.VisualGrid.Reset();
        }

        public async Task Open(string? fileName = null, List<FileTypes>? fileTypes = null)
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

            try
            {
                SetControlsStateForLoading();

                await Task.Run(() => _imageManager.LoadImageFile(
                    fileName: fileName,
                    mode: Abstraction.ResizeMode.SourceResize));

                Source.Presenter = _imageManager.GetLoadedBitmap();
            }
            catch (OperationCanceledException) { }
            catch (Exception e) when (IsHandleableOpenFileException(e))
            {
                _messageService.SendMessage(MessageType.SourceOpenError, ex: e);
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

            _usageData.AddRecentInputFile(fileName);

            Source.VisualGrid.Reset();

            var resMessage = _imageManager.ResultInfo switch
            {
                ResultStatus.Success => MessageType.SourceOpenSuccess,
                ResultStatus.RezisingRequired => MessageType.SourceOpenSuccessButResized,
                ResultStatus.BitmapAreaNotSufficient => MessageType.SourceOpenSuccessButIncomplete,
                _ => MessageType.UnknownError
            };

            Application.Current.Dispatcher.Invoke(() => _messageService.SendMessage(resMessage));
        }

        public async Task Reload()
        {
            if (_usageData.RecentInputFiles.Count == 0)
                return;

            string fileName = _usageData.RecentInputFiles.FirstOrDefault()!;

            try
            {
                SetControlsStateForLoading();

                await Task.Run(() => _imageManager.LoadImageFile(
                    fileName: fileName,
                    mode: Abstraction.ResizeMode.SourceResize));

                Source.MouseLeave();

                Source.Presenter = _imageManager.GetLoadedBitmap();
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
            }
        }

        public async Task OpenTr()
            => await Open(fileTypes: [TR_FILE_TYPES, DEF_FILE_TYPES]);

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
