using System.ComponentModel;
using System.Diagnostics;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;
using TgaBuilderWpfUi.Elements;
using TgaBuilderWpfUi.Services;
using Wpf.Ui.Appearance;
using WPFZoomPanel;
using Application = System.Windows.Application;
using Cursor = System.Windows.Input.Cursor;
using FluentWindow = Wpf.Ui.Controls.FluentWindow;
using Image = System.Windows.Controls.Image;

namespace TgaBuilderWpfUi.View
{
    public partial class MainWindow : AsyncWindow, ISnackbarOwner
    {
        public Image? CurrentImage { get; set; }
        public ZoomPanel? CurrentPanel { get; set; }

        public bool IsLoaded => throw new NotImplementedException();

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
    }
}