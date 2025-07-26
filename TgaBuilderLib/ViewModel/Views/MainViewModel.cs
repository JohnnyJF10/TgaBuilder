using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.FileHandling;
using TgaBuilderLib.Messaging;
using TgaBuilderLib.UndoRedo;
using TgaBuilderLib.Utils;
using MouseAction = TgaBuilderLib.Abstraction.MouseAction;


namespace TgaBuilderLib.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel(
            Func<ViewIndex, IView> getViewCallback,
            IMessageService messageService,
            IUndoRedoManager undoRedoManager,
            ILogger logger,

            SourceTexturePanelViewModel source,
            TargetTexturePanelViewModel destination,

            SelectionViewModel selection,
            AnimationViewModel animation,

            SourceIOViewModel sourceIO,
            TargetIOViewModel destinationIO,

            ViewTabViewModel sourceViewTab,
            ViewTabViewModel destinationViewTab,
            AlphaTabViewModel alpha,
            PlacingTabViewModel placing,
            EditTabViewModel edits,
            SizeTabViewModel size,
            FormatTabViewModel format,

            IUsageData? usageData = null)
        {
            _getViewCallback = getViewCallback;

            _messageService = messageService;

            _undoRedoManager = undoRedoManager;
            _logger = logger;


            Source = source;
            Destination = destination;

            Selection = selection;
            Animation = animation;

            SourceIO = sourceIO;
            TargetIO = destinationIO;

            SourceViewTab = sourceViewTab;
            DestinationViewTab = destinationViewTab;
            AlphaTab = alpha;
            PlacingTab = placing;
            EditsTab = edits;
            SizeTab = size;
            FormatTab = format;

            if (usageData != null)
                _ = CheckUsageDataLoading(usageData);
        }

        private readonly Func<ViewIndex, IView> _getViewCallback;

        private readonly IMessageService _messageService;

        private readonly IUndoRedoManager _undoRedoManager;
        private readonly ILogger _logger;


        private PanelMouseCommand? _mousePanelCommand;
        private RelayCommand? _batchLoaderCommand;
        private RelayCommand? _aboutCommand;
        private AsyncCommand? _copyEntireCommand;
        private AsyncCommand? _newCommand;
        private AsyncCommand? _openSourceCommand;
        private AsyncCommand<List<string>>? _fileDropSourceCommand;
        private AsyncCommand? _reloadSourceCommand;
        private AsyncCommand<string>? _openRecentSourceCommand;
        private AsyncCommand? _openSourceTrCommand;
        private AsyncCommand<string>? _openRecentDestinationCommand;
        private AsyncCommand? _openDestinationCommand;
        private AsyncCommand<List<string>>? _fileDropDestinationCommand;
        private AsyncCommand? _saveCommand;
        private AsyncCommand? _saveAsCommand;
        private RelayCommand? _undoCommand;
        private RelayCommand? _redoCommand;
        private RelayCommand? _copyCommand;
        private RelayCommand? _pasteCommand;
        private RelayCommand<bool>? _enterPanelCommand;
        private RelayCommand? _leavePanelCommand;



        public SourceTexturePanelViewModel Source { get; set; }
        public TargetTexturePanelViewModel Destination { get; set; }

        public SourceIOViewModel SourceIO { get; set; }
        public TargetIOViewModel TargetIO { get; set; }

        public AlphaTabViewModel AlphaTab { get; set; }
        public PlacingTabViewModel PlacingTab { get; set; }
        public EditTabViewModel EditsTab { get; set; }
        public SizeTabViewModel SizeTab { get; set; }
        public FormatTabViewModel FormatTab { get; set; }
        public ViewTabViewModel SourceViewTab { get; set; }
        public ViewTabViewModel DestinationViewTab { get; set; }

        public SelectionViewModel Selection { get; set; }
        public AnimationViewModel Animation { get; set; }


        public ICommand MousePanelCommand  => _mousePanelCommand ??= new(HandleMouseOnPanel);

        public ICommand BatchLoaderCommand => _batchLoaderCommand ??= new(SourceIO.BatchLoader);

        public ICommand AboutCommand => _aboutCommand ??= new(About);

        public ICommand CopyEntireCommand
            => _copyEntireCommand ??= new(() => TargetIO.CopyEntire(Source.Presenter));

        public ICommand NewCommand => _newCommand ??= new(TargetIO.NewFile);

        public ICommand OpenSourceCommand => _openSourceCommand ??= new(() => SourceIO.Open());

        public ICommand OpenSourceTrCommand => _openSourceTrCommand ??= new(SourceIO.OpenTr);

        public ICommand ReloadSourceCommand => _reloadSourceCommand ??= new(SourceIO.Reload);

        public ICommand OpenRecentSourceCommand
            => _openRecentSourceCommand ??= new(fn => SourceIO.Open(fn));

        public ICommand OpenRecentDestinationCommand
            => _openRecentDestinationCommand ??= new (TargetIO.Open);

        public ICommand FileDropSourceCommand
            => _fileDropSourceCommand ??= new ( files => SourceIO.Open(files.FirstOrDefault()));

        public ICommand SaveCommand
            => _saveCommand ??= new(() => TargetIO.SaveCurrent());

        public ICommand SaveAsCommand
            => _saveAsCommand ??= new(() => TargetIO.Save());

        public ICommand OpenDestinationCommand
            => _openDestinationCommand ??= new(() => TargetIO.Open());

        public ICommand FileDropDestinationCommand
            => _fileDropDestinationCommand ??= new(files => TargetIO.Open(files.FirstOrDefault()));

        public ICommand UndoCommand
            => _undoCommand ??= new(() => _undoRedoManager.Undo(), () => _undoRedoManager.CanUndo);

        public ICommand RedoCommand
            => _redoCommand ??= new(() => _undoRedoManager.Redo(), () => _undoRedoManager.CanRedo);

        public ICommand CopyCommand => _copyCommand ??= new(Selection.Copy);

        public ICommand PasteCommand => _pasteCommand ??= new(Selection.Paste);

        public ICommand EnterPanelCommand => _enterPanelCommand ??= new(EnterPanel);

        public ICommand LeavePanelCommand => _leavePanelCommand ??= new (LeavePanel);



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
        }

        public void LeavePanel()
        {
            Source.MouseLeave();
            Destination.MouseLeave();

            EndScrolling();
        }

        private void EndScrolling()
        {
            SourceViewTab.IsScrolling = false;
            DestinationViewTab.IsScrolling = false;
        }

        private async Task CheckUsageDataLoading(IUsageData usageData)
        {
            await Task.Delay(1000);

            if (usageData.WasLoadingUnsuccessful)
                Application.Current.Dispatcher.Invoke(() =>
                    _messageService.SendMessage(MessageType.UsageDataLoadError));
        }
    }
}
