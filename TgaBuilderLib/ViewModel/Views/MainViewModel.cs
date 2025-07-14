﻿using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.FileHandling;
using TgaBuilderLib.Messaging;
using TgaBuilderLib.UndoRedo;
using TgaBuilderLib.Utils;
using TgaBuilderLib.ViewModel.Views;
using MouseAction = TgaBuilderLib.Abstraction.MouseAction;


namespace TgaBuilderLib.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel(
            Func<ViewIndex, IView> getViewCallback,

            IFileService fileService,
            IMessageService messageService,
            IMessageBoxService messageBoxService,

            IImageFileManager imageManager,
            IBitmapOperations bitmapOperations,
            IUndoRedoManager undoRedoManager,
            ILogger logger,

            IUsageData usageData,

            SourceTexturePanelViewModel source,
            TargetTexturePanelViewModel destination,

            SelectionViewModel selection,
            AnimationViewModel animation,

            ViewTabViewModel sourceViewTab,
            ViewTabViewModel destinationViewTab,
            AlphaTabViewModel alphaTab,
            PlacingTabViewModel placing,
            EditTabViewModel edits,
            SizeTabViewModel size
            )
        {
            _getViewCallback = getViewCallback;
            _messageBoxService = messageBoxService;

            _fileService = fileService;
            _messageService = messageService;

            _imageManager = imageManager;
            _bitmapOperations = bitmapOperations;
            _undoRedoManager = undoRedoManager;
            _logger = logger;

            _usageData = usageData;

            Source = source;
            Destination = destination;

            Selection = selection;
            Animation = animation;

            SourceViewTab = sourceViewTab;
            DestinationViewTab = destinationViewTab;
            AlphaTab = alphaTab;
            PlacingTab = placing;
            EditsTab = edits;
            SizeTab = size;

            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);
                CheckUsageDataLoading();
            });
        }

        private const FileTypes DEF_FILE_TYPES =
            FileTypes.TGA | FileTypes.BMP | FileTypes.PNG | FileTypes.JPG
            | FileTypes.JPEG | FileTypes.PSD | FileTypes.DDS;

        private const FileTypes TR_FILE_TYPES = FileTypes.PHD | FileTypes.TR2
            | FileTypes.TR4 | FileTypes.TRC | FileTypes.TEN;

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
        private readonly IBitmapOperations _bitmapOperations;
        private readonly IUndoRedoManager _undoRedoManager;
        private readonly ILogger _logger;

        private string _lastDestinationFilePath = string.Empty;
        private IUsageData _usageData;

        private PanelMouseCommand? _mousePanelCommand;
        private RelayCommand? _batchLoaderCommand;
        private RelayCommand? _aboutCommand;
        private RelayCommand? _copyEntireCommand;
        private RelayCommand? _newCommand;
        private RelayCommand? _openSourceCommand;
        private RelayCommand<List<string>>? _fileDropSourceCommand;
        private RelayCommand? _reloadSourceCommand;
        private RelayCommand<string>? _openRecentSourceCommand;
        private RelayCommand? _openSourceTrCommand;
        private RelayCommand<string>? _openRecentDestinationCommand;
        private RelayCommand? _openDestinationCommand;
        private RelayCommand<List<string>>? _fileDropDestinationCommand;
        private RelayCommand? _saveCommand;
        private RelayCommand? _saveAsCommand;
        private RelayCommand? _undoCommand;
        private RelayCommand? _redoCommand;
        private RelayCommand? _copyCommand;
        private RelayCommand? _pasteCommand;
        private RelayCommand<bool>? _enterPanelCommand;
        private RelayCommand? _leavePanelCommand;
        private RelayCommand<SolidColorBrush>? selectionMonoColorFillCommand;


        public IEnumerable<string> RecentSourceFileNames => _usageData.RecentInputFiles;
        public IEnumerable<string> RecentDestinationFileNames => _usageData.RecentOutputFiles;

        public SourceTexturePanelViewModel Source { get; set; }
        public TargetTexturePanelViewModel Destination { get; set; }

        public AlphaTabViewModel AlphaTab { get; set; }
        public PlacingTabViewModel PlacingTab { get; set; }
        public EditTabViewModel EditsTab { get; set; }
        public SizeTabViewModel SizeTab { get; set; }
        public ViewTabViewModel SourceViewTab { get; set; }
        public ViewTabViewModel DestinationViewTab { get; set; }

        public SelectionViewModel Selection { get; set; }
        public AnimationViewModel Animation { get; set; }

        public bool TrImportRepackingSelected
        {
            get => _imageManager.TrImportRepackingSelected;
            set
            {
                if (value == _imageManager.TrImportRepackingSelected)
                    return;
                _imageManager.TrImportRepackingSelected = value;
                OnCallerPropertyChanged();
            }
        }

        public int TrImportHorPageNum
        {
            get => _imageManager.TrImportHorPageNum;
            set
            {
                if (value == _imageManager.TrImportHorPageNum)
                    return;

                var newValue = CalculateNewPageXValue(value, _imageManager.TrImportHorPageNum);
                _imageManager.TrImportHorPageNum = newValue;
                OnCallerPropertyChanged();
            }
        }

        public ICommand MousePanelCommand
            => _mousePanelCommand ??= new PanelMouseCommand(HandleMouseOnPanel);

        public ICommand BatchLoaderCommand
            => _batchLoaderCommand ??= new RelayCommand(BatchLoader);

        public ICommand AboutCommand
            => _aboutCommand ??= new RelayCommand(About);

        public ICommand CopyEntireCommand
            => _copyEntireCommand ??= new RelayCommand(CopyEntire);

        public ICommand NewCommand
            => _newCommand ??= new RelayCommand(NewFile);

        public ICommand OpenSourceCommand
            => _openSourceCommand ??= new RelayCommand(() => OpenSource());

        public ICommand OpenSourceTrCommand
            => _openSourceTrCommand ??= new RelayCommand(() => OpenSource(
                fileTypes: new List<FileTypes> { TR_FILE_TYPES, DEF_FILE_TYPES }));

        public ICommand ReloadSourceCommand
            => _reloadSourceCommand ??= new RelayCommand(ReloadSource);

        public ICommand OpenRecentSourceCommand
            => _openRecentSourceCommand ??= new RelayCommand<string>(fn => OpenSource(fn));

        public ICommand OpenRecentDestinationCommand
            => _openRecentDestinationCommand ??= new RelayCommand<string>(OpenDestination);

        public ICommand FileDropSourceCommand
            => _fileDropSourceCommand ??= new RelayCommand<List<string>>(
                files => OpenSource(files.FirstOrDefault()));

        public ICommand SaveCommand 
            => _saveCommand ??= new RelayCommand(() => Save(_lastDestinationFilePath));

        public ICommand SaveAsCommand 
            => _saveAsCommand ??= new RelayCommand(() => Save());

        public ICommand OpenDestinationCommand 
            => _openDestinationCommand ??= new RelayCommand(() => OpenDestination());

        public ICommand FileDropDestinationCommand
            => _fileDropDestinationCommand ??= new RelayCommand<List<string>>(
                files => OpenDestination(files.FirstOrDefault()));

        public ICommand UndoCommand
            => _undoCommand ??= new RelayCommand(
                () => _undoRedoManager.Undo(), () => _undoRedoManager.CanUndo);

        public ICommand RedoCommand
            => _redoCommand ??= new RelayCommand(
                () => _undoRedoManager.Redo(), () => _undoRedoManager.CanRedo);

        public ICommand CopyCommand
            => _copyCommand ??= new RelayCommand(Selection.Copy);

        public ICommand PasteCommand
            => _pasteCommand ??= new RelayCommand(Selection.Paste);

        public ICommand EnterPanelCommand
            => _enterPanelCommand ??= new RelayCommand<bool>(EnterPanel);

        public ICommand LeavePanelCommand 
            => _leavePanelCommand ??= new RelayCommand(LeavePanel);

        public ICommand SelectionMonoColorFillCommand 
            => selectionMonoColorFillCommand ??= new RelayCommand<SolidColorBrush>(SelectionMonoColorFill);



        public void HandleMouseOnPanel(int x, int y, bool isTarget, MouseAction action, MouseModifier modifier)
        {
            TexturePanelViewModelBase panel = isTarget ? Destination : Source;

            if (AlphaTab.IsEyedropperMode)
            {
                modifier = MouseModifier.Eyedropper;
            }

            switch (action, modifier) 
            {
                case (MouseAction.Move, MouseModifier.Alt):
                    panel.AltMove(x, y);
                    return;

                case (MouseAction.Move, MouseModifier.AltLeft):
                    panel.AltDrag(x, y);
                    return;

                case (MouseAction.Move, MouseModifier.None):
                    panel.MouseMove(x, y);
                    return;

                case (MouseAction.Move, MouseModifier.Left):
                    panel.Drag(x, y);
                    return;

                case (MouseAction.Move, MouseModifier.Right):
                    panel.RightDrag(x, y);
                    return;

                case (MouseAction.Move, MouseModifier.Double):
                    panel.DoubleDrag(x, y);
                    return;

                case (MouseAction.Move, MouseModifier.Eyedropper):
                    panel.EyedropperMove(x, y);
                    AlphaTab.DoColorPicking();
                    return;


                case (MouseAction.DragEnd, MouseModifier.Left):
                    panel.DragEnd();
                    EndScrolling();
                    return;

                case (MouseAction.DragEnd, MouseModifier.Right):
                    panel.RightDragEnd();
                    EndScrolling();
                    return;

                case (MouseAction.DragEnd, MouseModifier.AltLeft):
                    panel.DragEnd();
                    EndScrolling();
                    return;

                case (MouseAction.DragEnd, MouseModifier.Double):
                    panel.DoubleDragEnd();
                    EndScrolling();
                    return;

                case (MouseAction.DragEnd, MouseModifier.Eyedropper):
                    HanldeEyedropperEnd();
                    return;

                default:
                    break;
            }
        }

        private void HanldeEyedropperEnd()
        {
            AlphaTab.EndColorPicking();

            Source.EyedropperEnd();
        }

        public void BatchLoader()
        {
            var batchLoaderView = _getViewCallback(ViewIndex.BatchLoader);
            if (batchLoaderView.DataContext is not BatchLoaderViewModel batchLoaderVM) return;

            batchLoaderView.ShowDialog();

            if (batchLoaderView.DialogResult != true) return;

            Source.SetPresenter(batchLoaderVM.Presenter);

            Source.VisualGrid.Reset();
            _ = SourceViewTab.DefferedFill();
        }

        public void CopyEntire()
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
                        if (Save(_lastDestinationFilePath)) break;
                        else return;
                    case YesNoCancel.No: break;
                    case YesNoCancel.Cancel: return;
                }
            }

            Destination.Presenter = _bitmapOperations.GetTargetFromSource(Source.Presenter);
            Destination.RefreshPresenter();

            _lastDestinationFilePath = string.Empty; 

            _undoRedoManager.ClearAllNewFile();

            _ = DestinationViewTab.DefferedFill();
        }

        public void NewFile()
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
                        if (Save(_lastDestinationFilePath)) break;
                        else return;
                    case YesNoCancel.No: break;
                    case YesNoCancel.Cancel: return;
                }
            }

            Destination.NewFile();

            _lastDestinationFilePath = string.Empty;

            _undoRedoManager.ClearAllNewFile();

            _ = DestinationViewTab.DefferedFill();
        }


        public void OpenSource(string? fileName = null, List<FileTypes>? fileTypes = null)
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
                Source.OpenFile(fileName);
            }
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

            _usageData.AddRecentInputFile(fileName);

            Source.VisualGrid.Reset();
            _ = SourceViewTab.DefferedFill();

            var resMessage = _imageManager.ResultInfo switch
            {
                ResultStatus.Success                    => MessageType.SourceOpenSuccess,
                ResultStatus.RezisingRequired           => MessageType.SourceOpenSuccessButResized,
                ResultStatus.BitmapAreaNotSufficient    => MessageType.SourceOpenSuccessButIncomplete,
                _ => MessageType.UnknownError
            };
            _messageService.SendMessage(resMessage);
        }

        public void ReloadSource()
        {
            if (_usageData.RecentInputFiles.Count == 0)
                return;

            string fileName = _usageData.RecentInputFiles.FirstOrDefault()!;

            try
            {
                Source.OpenFile(fileName);
            }
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
        }

        public void OpenDestination(string? fileName = null)
        {
            if (_undoRedoManager.IsTargetDirty())
            {
                var result = _messageBoxService.ShowYesNoCancelMessageBox(
                    Header:     "Save changes?" , 
                    Message:    "Do you want to save changes?")
                    .Result;
                switch (result)
                {
                    case YesNoCancel.Yes: 
                        if (Save(_lastDestinationFilePath)) break;
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
                Destination.OpenFile(fileName);
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

            _usageData.AddRecentOutputFile(fileName);

            _lastDestinationFilePath = IsFileWriteable(fileName) ? fileName : string.Empty; 

            _undoRedoManager.ClearAllNewFile();

            _ = DestinationViewTab.DefferedFill();

            var resMessage = _imageManager.ResultInfo switch
            {
                ResultStatus.Success                    => MessageType.DestinationOpenSuccess,
                ResultStatus.RezisingRequired           => MessageType.DestinationOpenSuccessButResized,
                ResultStatus.BitmapAreaNotSufficient    => MessageType.DestinationOpenSuccessButIncomplete,
                _                                       => MessageType.UnknownError
            };
            _messageService.SendMessage(resMessage);
        }

        public bool Save(string? fileName = null)
        {
            if (String.IsNullOrEmpty(fileName) || !IsFileWriteable(fileName))
                if (_fileService.SaveFileDialog(WRITEABLE_FILE_TYPES) == true)
                    fileName = _fileService.SelectedPath;
                else return false;

            try
            {
                Destination.SaveFile(fileName);
            }
            catch (Exception e) when (IsHandleableSaveFileException(e))
            {
                _messageService.SendMessage(MessageType.DestinationSaveError);
                _logger.LogError(e);
                return false;
            }

            _lastDestinationFilePath = fileName;
            _usageData.AddRecentOutputFile(fileName);

            _undoRedoManager.TakeStatusSnapshot();

            _messageService.SendMessage(MessageType.DestinationSaveSuccess);
            return true;
        }

        public void About()
        {
            var aboutView = _getViewCallback(ViewIndex.About);

            if (aboutView.DataContext is not AboutViewModel aboutVM) 
                return;

            aboutView.ShowDialog();
        }

        public void EnterPanel(bool isTargetPanel)
        {
            if (isTargetPanel)
                Destination.MouseEnter();
            else
                Source.MouseEnter();
        }

        public void LeavePanel()
        {
            Source.MouseLeave();
            Destination.MouseLeave();

            EndScrolling();
        }


        public void SelectionMonoColorFill(SolidColorBrush brush)
        {
            Int32Rect rect = new(0,0,
                Selection.Presenter.PixelWidth,
                Selection.Presenter.PixelHeight);

            _bitmapOperations.FillRectColor(
                Selection.Presenter, rect, brush.Color);

            Selection.IsPlacing = true;
        }

        private bool IsFileWriteable(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath)?.TrimStart('.').ToLower() ?? ""; 

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

        private void CheckUsageDataLoading()
        {
            if (_usageData.WasLoadingUnsuccessful)
                Application.Current.Dispatcher.Invoke(() =>
                    _messageService.SendMessage(MessageType.UsageDataLoadError));
        }

        private void EndScrolling()
        {
            SourceViewTab.IsScrolling = false;
            DestinationViewTab.IsScrolling = false;
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
