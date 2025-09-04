
using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Enums;
using TgaBuilderLib.FileHandling;
using TgaBuilderLib.Messaging;
using TgaBuilderLib.UndoRedo;
using TgaBuilderLib.Utils;
using MouseAction = TgaBuilderLib.Enums.MouseAction;


namespace TgaBuilderLib.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel(
            Func<ViewIndex, IView> getViewCallback,
            IMessageService messageService,
            IUndoRedoManager undoRedoManager,
            IDispatcherService dispatcherService,

            SourceTexturePanelViewModel source,
            TargetTexturePanelViewModel destination,

            SelectionViewModel selection,
            AnimationViewModel animation,

            SourceIOViewModel sourceIO,
            TargetIOViewModel destinationIO,

            ViewTabViewModel sourceViewTab,
            ViewTabViewModel destinationViewTab,

            PlacingTabViewModel placing,
            EditTabViewModel edits,
            SizeTabViewModel size,

            FormatTabViewModel sourceFormat,
            FormatTabViewModel targetFormat,

            IUsageData? usageData = null)
        {
            _getViewCallback = getViewCallback;
            _messageService = messageService;
            _undoRedoManager = undoRedoManager;
            _dispatcherService = dispatcherService;

            Source = source;
            Destination = destination;

            Selection = selection;
            Animation = animation;

            SourceIO = sourceIO;
            TargetIO = destinationIO;

            SourceViewTab = sourceViewTab;
            DestinationViewTab = destinationViewTab;

            SourceFormatTab = sourceFormat;
            DestinationFormatTab = targetFormat;

            PlacingTab = placing;
            EditsTab = edits;
            SizeTab = size;

            if (usageData != null)
                _ = CheckUsageDataLoading(usageData);

            SourceFormatTab.EyedroppingRequested += (_, _)
                => _currnetlyEyedroppingTab = SourceFormatTab;

            DestinationFormatTab.EyedroppingRequested += (_, _)
                => _currnetlyEyedroppingTab = DestinationFormatTab;
        }

        private readonly Func<ViewIndex, IView> _getViewCallback;
        private readonly IMessageService _messageService;
        private readonly IUndoRedoManager _undoRedoManager;
        private readonly IDispatcherService _dispatcherService;

        private FormatTabViewModel? _currnetlyEyedroppingTab;

        private bool _panelInfoVisible;
        private bool _tileInfoVisible = true;

        private string _pixelInfo = string.Empty;
        private string _tileInfo = string.Empty;
        private string _panelInfo = string.Empty;
        private string _panelHelp = string.Empty;


        private PanelMouseCommand? _mousePanelCommand;
        private RelayCommand? _batchLoaderCommand;
        private RelayCommand? _aboutCommand;
        private RelayCommand? _entireToSourceCommand;
        private AsyncCommand? _entireToTargetCommand;
        private AsyncCommand? _newCommand;
        private RelayCommand? _openSourceCommand;
        private RelayCommand? _openPreviousSourceCommand;
        private RelayCommand? _openNextSourceCommand;
        private RelayCommand<List<string>>? _fileDropSourceCommand;
        private RelayCommand? _reloadSourceCommand;
        private RelayCommand<string>? _openRecentSourceCommand;
        private RelayCommand? _openSourceTrCommand;
        private RelayCommand<string>? _openRecentDestinationCommand;
        private RelayCommand? _openDestinationCommand;
        private RelayCommand<List<string>>? _fileDropDestinationCommand;
        private RelayCommand? _saveCommand;
        private RelayCommand? _saveAsCommand;
        private RelayCommand? _cancelSourceIOCommand;
        private RelayCommand? _cancelDestinationIOCommand;
        private RelayCommand? _undoCommand;
        private RelayCommand? _redoCommand;
        private RelayCommand? _copyCommand;
        private RelayCommand? _pasteCommand;
        private RelayCommand<bool>? _enterPanelCommand;
        private RelayCommand<bool>? _leavePanelCommand;
        private RelayCommand<(bool, bool)>? _wheelShiftCommand;


        public SourceTexturePanelViewModel Source { get; set; }
        public TargetTexturePanelViewModel Destination { get; set; }

        public SourceIOViewModel SourceIO { get; set; }
        public TargetIOViewModel TargetIO { get; set; }

        public PlacingTabViewModel PlacingTab { get; set; }
        public EditTabViewModel EditsTab { get; set; }
        public SizeTabViewModel SizeTab { get; set; }

        public FormatTabViewModel SourceFormatTab { get; set; }
        public FormatTabViewModel DestinationFormatTab { get; set; }

        public ViewTabViewModel SourceViewTab { get; set; }
        public ViewTabViewModel DestinationViewTab { get; set; }

        public SelectionViewModel Selection { get; set; }
        public AnimationViewModel Animation { get; set; }

        public bool PanelInfoVisible
        {
            get => _panelInfoVisible;
            set => SetCallerProperty(ref _panelInfoVisible, value);
        }

        public bool TileInfoVisible 
        { 
            get => _tileInfoVisible;
            set
            {
                SetCallerProperty(ref _tileInfoVisible, value);
                OnPropertyChanged(nameof(SelectionInfoVisible));
            }
        }

        public bool SelectionInfoVisible => !TileInfoVisible;

        public string PixelInfo
        {
            get => _pixelInfo;
            set => SetCallerProperty(ref _pixelInfo, value);
        }
        public string TileInfo
        {
            get => _tileInfo;
            set => SetCallerProperty(ref _tileInfo, value);
        }
        public string PanelInfo
        {
            get => _panelInfo;
            set => SetCallerProperty(ref _panelInfo, value);
        }
        public string PanelHelp
        {
            get => _panelHelp;
            set => SetCallerProperty(ref _panelHelp, value);
        }

        public string DebugNote =>
#if DEBUG
            "Debug ";
#else
            string.Empty;
#endif


        public ICommand MousePanelCommand  => _mousePanelCommand 
            ??= new(HandleMouseOnPanel);

        public ICommand BatchLoaderCommand => _batchLoaderCommand 
            ??= new(SourceIO.BatchLoader);

        public ICommand AboutCommand => _aboutCommand 
            ??= new(About);

        public ICommand EntireToSourceCommand => _entireToSourceCommand
            ??= new(() => SourceIO.CopyEntire(Destination.Presenter));

        public ICommand EntireToTargetCommand => _entireToTargetCommand 
            ??= new(() => TargetIO.CopyEntire(Source.Presenter));

        public ICommand NewCommand => _newCommand 
            ??= new(TargetIO.NewFile);

        public ICommand OpenSourceCommand => _openSourceCommand 
            ??= new(() => SourceIO.SetupOpenTask());

        public ICommand OpenPreviousSourceCommand => _openPreviousSourceCommand 
            ??= new(() => SourceIO.OpenPreviosFile(), () => SourceIO.LastFolderFileNames.Count > 0);

        public ICommand OpenNextSourceCommand => _openNextSourceCommand 
            ??= new(() => SourceIO.OpenNextFile(), () => SourceIO.LastFolderFileNames.Count > 0);

        public ICommand OpenSourceTrCommand => _openSourceTrCommand 
            ??= new(SourceIO.OpenTr);

        public ICommand ReloadSourceCommand => _reloadSourceCommand 
            ??= new(SourceIO.SetupReloadTask, () => SourceIO.LastFolderFileNames.Count > 0);

        public ICommand OpenRecentSourceCommand => _openRecentSourceCommand 
            ??= new(fn => SourceIO.SetupOpenTask(fn));

        public ICommand OpenRecentDestinationCommand => _openRecentDestinationCommand 
            ??= new(fn => TargetIO.SetupOpenTask(fn));

        public ICommand FileDropSourceCommand => _fileDropSourceCommand 
            ??= new ( files => SourceIO.SetupOpenTask(files.FirstOrDefault()));

        public ICommand SaveCommand => _saveCommand 
            ??= new(TargetIO.SaveCurrent);

        public ICommand SaveAsCommand => _saveAsCommand 
            ??= new(() => TargetIO.SetupSaveTask());

        public ICommand OpenDestinationCommand
            => _openDestinationCommand 
            ??= new(() => TargetIO.SetupOpenTask());

        public ICommand FileDropDestinationCommand => _fileDropDestinationCommand 
            ??= new(files => TargetIO.SetupOpenTask(files.FirstOrDefault()));

        public ICommand CancelSourceIOCommand => _cancelSourceIOCommand 
            ??= new(SourceIO.CancelOpen);

        public ICommand CancelDestinationIOCommand => _cancelDestinationIOCommand 
            ??= new(TargetIO.CancelOpen);

        public ICommand UndoCommand => _undoCommand 
            ??= new(() => _undoRedoManager.Undo(), () => _undoRedoManager.CanUndo);

        public ICommand RedoCommand => _redoCommand 
            ??= new(() => _undoRedoManager.Redo(), () => _undoRedoManager.CanRedo);

        public ICommand CopyCommand => _copyCommand 
            ??= new(Selection.Copy);

        public ICommand PasteCommand => _pasteCommand 
            ??= new(Selection.Paste);

        public ICommand EnterPanelCommand => _enterPanelCommand 
            ??= new(EnterPanel);

        public ICommand LeavePanelCommand => _leavePanelCommand 
            ??= new (LeavePanel);

        public ICommand WheelShiftCommand => _wheelShiftCommand
            ??= new(args => WheelShift(args.Item1, args.Item2));



        public void HandleMouseOnPanel(int x, int y, bool isTarget, MouseAction action, MouseModifier modifier)
        {
            TexturePanelViewModelBase panel = isTarget ? Destination : Source;

            if (_currnetlyEyedroppingTab is not null)
            {
                modifier = MouseModifier.Eyedropper;
            }

            panel.XPointer = x;
            panel.YPointer = y;

            switch (action, modifier) 
            {
                case (MouseAction.DragStart, MouseModifier.Left):
                    TileInfoVisible = false;
                    break;

                case (MouseAction.Move, MouseModifier.Alt):
                    panel.AltMove();
                    break;

                case (MouseAction.Move, MouseModifier.AltLeft):
                    panel.AltDrag();
                    break;

                case (MouseAction.Move, MouseModifier.None):
                    panel.MouseMove();
                    break;

                case (MouseAction.Move, MouseModifier.Left):
                    panel.Drag();
                    break;

                case (MouseAction.Move, MouseModifier.Right):
                    panel.RightDrag();
                    break;

                case (MouseAction.Move, MouseModifier.Double):
                    panel.DoubleDrag();
                    break;

                case (MouseAction.Move, MouseModifier.Eyedropper):
                    panel.EyedropperMove();
                    _currnetlyEyedroppingTab?.DoColorPicking();
                    break;


                case (MouseAction.DragEnd, MouseModifier.Left):
                    panel.DragEnd();
                    TileInfoVisible = true;
                    EndScrolling();
                    break;

                case (MouseAction.DragEnd, MouseModifier.Right):
                    panel.RightDragEnd();
                    EndScrolling();
                    break;

                case (MouseAction.DragEnd, MouseModifier.AltLeft):
                    panel.DragEnd();
                    EndScrolling();
                    break;

                case (MouseAction.DragEnd, MouseModifier.Double):
                    panel.DoubleDragEnd();
                    EndScrolling();
                    break;

                case (MouseAction.DragEnd, MouseModifier.Eyedropper):
                    HanldeEyedropperEnd();
                    break;

                default:
                    break;
            }
            PixelInfo = panel.PixelInfo;
            TileInfo = panel.TileInfo;
            PanelInfo = panel.PanelInfo;
            PanelHelp = panel.PanelHelp;
        }

        private void HanldeEyedropperEnd()
        {
            _currnetlyEyedroppingTab?.EndColorPicking();

            _currnetlyEyedroppingTab = null;
        }

        public void About()
        {
            var aboutView = _getViewCallback(ViewIndex.About);

            aboutView.ShowDialog();
        }

        public void EnterPanel(bool isTargetPanel)
        {
            if (isTargetPanel)
                Destination.MouseEnter();
            else
                Source.MouseEnter();

            PanelInfoVisible = true;
        }

        public void LeavePanel(bool isTargetPanel)
        {
            PixelInfo = string.Empty;
            TileInfo = string.Empty;

            if (isTargetPanel)
            {
                Destination.MouseLeave();
                PanelInfo = $"{Destination.Presenter.PixelWidth} x {Destination.Presenter.PixelHeight}px, " +
                            $"{(Destination.Presenter.HasAlpha ? 32 : 24)} bpp";
                PanelHelp = $"Destination Panel: Ctrl + Mouse: Move, Mouse Wheel: Zoom";
            }
            else
            {
                Source.MouseLeave();
                PanelInfo = $"{Source.Presenter.PixelWidth} x {Source.Presenter.PixelHeight}px, " +
                            $"{(Source.Presenter.HasAlpha ? 32 : 24)} bpp";
                PanelHelp = $"Source Panel: Ctrl + Mouse: Move, Mouse Wheel: Zoom";
            }

            PanelInfoVisible = false;

            EndScrolling();
        }

        private void EndScrolling()
        {
            SourceViewTab.IsScrolling = false;
            DestinationViewTab.IsScrolling = false;

            SourceIO.IsDropHintVisible = false;
            TargetIO.IsDropHintVisible = false;
        }

        private void WheelShift(bool isTarget, bool isNegative)
        {
            TexturePanelViewModelBase panel = isTarget ? Destination : Source;

            panel.SelectedPickerSize += isNegative ? -1 : 1;

            TileInfo = panel.TileInfo;
        }

        private async Task CheckUsageDataLoading(IUsageData usageData)
        {
            await Task.Delay(1000);

            if (usageData.WasLoadingUnsuccessful)
                _dispatcherService.Invoke(() =>
                    _messageService.SendMessage(MessageType.UsageDataLoadError));
        }
    }
}
