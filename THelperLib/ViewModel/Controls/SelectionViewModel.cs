using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using THelperLib.Abstraction;
using THelperLib.Commands;
using THelperLib.Messaging;
using THelperLib.Utils;
using Clipboard = System.Windows.Clipboard;

namespace THelperLib.ViewModel
{
    public class SelectionViewModel : ViewModelBase
    {
        public SelectionViewModel(
            ILogger logger,
            IMessageService messageService,
            WriteableBitmap presenter)
        {
            _logger = logger;
            _messageService = messageService;

            _presenter = presenter;
        }

        private readonly ILogger _logger;
        private readonly IMessageService _messageService;

        private WriteableBitmap _presenter;

        private bool _isPlacing;
        private bool _autoCopy;
        private bool _autoPaste;

        private RelayCommand? _copyCommand;
        private RelayCommand? _pasteCommand;
        private RelayCommand? _autoPasteCommand;


        public WriteableBitmap Presenter
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

        public RelayCommand CopyCommand => _copyCommand ??= new RelayCommand(Copy);

        public RelayCommand PasteCommand => _pasteCommand ??= new RelayCommand(Paste);

        public RelayCommand AutoPasteCommand => _autoPasteCommand ??= new RelayCommand(Paste);


        public void Copy()
        {
            BitmapSource bitmapSource = BitmapFrame.Create(Presenter);
            Clipboard.SetImage(bitmapSource);
        }

        public void Paste()
        {
            try
            {
                if (!Clipboard.ContainsImage())
                {
                    _messageService.SendMessage(MessageType.ClipboardNotContainingImageData, "Clipboard does not contain an image.");
                    return;
                }

                if (Clipboard.GetDataObject().GetFormats().Contains("FileDrop"))
                {
                    BitmapSource bitmapSource = Clipboard.GetImage();

                    var width = bitmapSource.PixelWidth;
                    var height = bitmapSource.PixelHeight;
                    var stride = width * 4;
                    var pixelData = new byte[height * stride];

                    bitmapSource.CopyPixels(pixelData, stride, 0);

                    WriteableBitmap wb = new WriteableBitmap(
                        pixelWidth: width,
                        pixelHeight: height,
                        dpiX: bitmapSource.DpiX,
                        dpiY: bitmapSource.DpiY,
                        pixelFormat: PixelFormats.Bgra32, palette: null);

                    wb.WritePixels(
                        sourceRect: new Int32Rect(0, 0, width, height),
                        pixels: pixelData,
                        stride: stride,
                        offset: 0);

                    Presenter = wb;
                    IsPlacing = true;
                }
                else
                {
                    BitmapSource bitmapSource = Clipboard.GetImage();

                    FormatConvertedBitmap convertedBitmap = new FormatConvertedBitmap();
                    convertedBitmap.BeginInit();
                    convertedBitmap.Source = bitmapSource;
                    convertedBitmap.DestinationFormat = PixelFormats.Rgb24;
                    convertedBitmap.EndInit();

                    Presenter = new WriteableBitmap(convertedBitmap);
                    IsPlacing = true;
                }


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
    }
}
