using System.Windows.Input;

using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Messaging;
using TgaBuilderLib.Utils;

namespace TgaBuilderLib.ViewModel
{
    public class SelectionViewModel : ViewModelBase
    {
        public SelectionViewModel(
            IMediaFactory mediaFactory,
            IClipboardService clipboardService,
            ILogger logger,
            IMessageService messageService,
            IBitmapOperations bitmapOperations,

            IWriteableBitmap presenter)
        {
            _mediaFactory = mediaFactory;
            _clipboardService = clipboardService;
            _logger = logger;
            _messageService = messageService;
            _bitmapOperations = bitmapOperations;

            _presenter = presenter;
        }

        private readonly IMediaFactory _mediaFactory;
        private readonly IClipboardService _clipboardService;
        private readonly ILogger _logger;
        private readonly IMessageService _messageService;
        private readonly IBitmapOperations _bitmapOperations;

        private IWriteableBitmap _presenter;

        private bool _isPlacing;
        private bool _autoCopy;
        private bool _autoPaste;

        private RelayCommand? _copyCommand;
        private RelayCommand? _pasteCommand;
        private RelayCommand? _autoPasteCommand;
        private RelayCommand<Color>? _selectionMonoColorFillCommand;



        public IWriteableBitmap Presenter
        {
            get => _presenter;
            set
            {
                SetProperty(ref _presenter, value, nameof(Presenter));
                if (AutoCopy) Copy();
            }
        }

        public bool IsPlacing
        {
            get => _isPlacing;
            set => SetProperty(ref _isPlacing, value, nameof(IsPlacing));
        }

        public bool AutoCopy
        {
            get => _autoCopy;
            set
            {
                if (_autoCopy == value) return;

                AutoPaste = false;
                _autoCopy = value;

                OnPropertyChanged(nameof(AutoCopy));
            }
        }

        public bool AutoPaste
        {
            get => _autoPaste;
            set
            {
                if (_autoPaste == value) return;

                AutoCopy = false; 
                _autoPaste = value;

                OnPropertyChanged(nameof(AutoPaste));
            }
        }

        public RelayCommand CopyCommand => _copyCommand ??= new(Copy);

        public RelayCommand PasteCommand => _pasteCommand ??= new(Paste);

        public RelayCommand AutoPasteCommand => _autoPasteCommand ??= new(Paste);

        public ICommand SelectionMonoColorFillCommand
            => _selectionMonoColorFillCommand ??= new(SelectionMonoColorFill);


        public void Copy() => _clipboardService.SetImage(Presenter);
        
        public void Paste()
        {
            try
            {
                if (!_clipboardService.ContainsImage())
                {
                    _messageService.SendMessage(MessageType.ClipboardNotContainingImageData, "Clipboard does not contain an image.");
                    return;
                }

                if (_clipboardService.GetImage() is not IReadableBitmap bitmap)
                    throw new InvalidOperationException("Failed to get image from clipboard.");

                Presenter = _mediaFactory.CloneBitmap(bitmap);
                IsPlacing = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                _messageService.SendMessage(
                    MessageType.ClipboardPasteError, 
                    "Failed to paste image from clipboard. Please find more details in the log.", 
                    ex);
            }        
        }

        public void SelectionMonoColorFill(Color color)
        {
            PixelRect rect = new(0, 0,
                Presenter.PixelWidth,
                Presenter.PixelHeight);

            _bitmapOperations.FillRectColor(
                Presenter, rect, color);

            IsPlacing = true;
        }

        internal void FillSelection(IWriteableBitmap presenter, Color color)
        {
            PixelRect rect = new(0, 0,
                Presenter.PixelWidth,
                Presenter.PixelHeight);

            _bitmapOperations.FillRectColor(
                Presenter, rect, color);

            IsPlacing = true;
        }
    }
}
