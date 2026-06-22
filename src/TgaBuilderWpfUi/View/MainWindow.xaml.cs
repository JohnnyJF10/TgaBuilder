using System.ComponentModel;
using TgaBuilderLib.Enums;
using TgaBuilderWpfUi.Elements;
using TgaBuilderWpfUi.Services;
using WPFZoomPanel;
using Application = System.Windows.Application;
using Cursor = System.Windows.Input.Cursor;
using Image = System.Windows.Controls.Image;

namespace TgaBuilderWpfUi.View
{
    public partial class MainWindow : AsyncWindow, ISnackbarOwner
    {
        public Image? CurrentImage { get; set; }
        public ZoomPanel? CurrentPanel { get; set; }


        public Cursor EyedropperCursor = new(Application
            .GetResourceStream(
            new Uri("Resources/eyedropper.cur", UriKind.Relative))
            .Stream);

        private MouseModifier _modifier = MouseModifier.None;

        private Dictionary<Image, ZoomPanel>? _imagePanelDict;

        public MainWindow(INotifyPropertyChanged mainViewModel)
        {
            base.PreviewMouseDown += Window_PreviewMouseDown;
            base.MouseDoubleClick += Window_MouseDoubleClick;
            base.PreviewMouseMove += Window_PreviewMouseMove;
            base.PreviewMouseUp += Window_PreviewMouseUp;
            base.PreviewMouseWheel += MainWindow_PreviewMouseWheel;

            InitializeComponent();
            base.DataContext = mainViewModel;
        }

        public static List<FileTypes> SupportedFileTypes
            => new List<FileTypes>
        {
            FileTypes.TGA,
            FileTypes.BMP,
            FileTypes.PNG,
            FileTypes.JPEG,
            FileTypes.JPG,
            FileTypes.KRA,
            FileTypes.PSD
        };

        public static List<string> SupportedFileTypesStrings
            => new List<string>
        {
            "TGA (Targa Image)",
            "BMP (Bitmap Image)",
            "PNG (Portable Network Graphics)",
            "JPEG (JPEG Image)",
            "JPG (JPG Image)",
            "KRA (Krita Project)",
            "PSD (Photoshop Document)"
        };
    }
}