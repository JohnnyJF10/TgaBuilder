using System.ComponentModel;
using THelperLib.Abstraction;
using THelperWpfUi.Services;
using WPFZoomPanel;
using Application = System.Windows.Application;
using Cursor = System.Windows.Input.Cursor;
using FluentWindow = Wpf.Ui.Controls.FluentWindow;
using Image = System.Windows.Controls.Image;

namespace THelperWpfUi.View
{
    public partial class MainWindow : FluentWindow, IView, ISnackbarOwner
    {
        public Image? CurrentImage { get; set; }
        public ZoomPanel? CurrentPanel { get; set; }
        public Cursor EyedropperCursor = new(Application
            .GetResourceStream(
            new Uri("Resources/pipette.cur", UriKind.Relative))
            .Stream);

        private MouseModifier _modifier = MouseModifier.None;

        private Dictionary<Image, ZoomPanel>? _imagePanelDict;

        public MainWindow(INotifyPropertyChanged mainViewModel)
        {
            PreviewMouseDown += Window_PreviewMouseDown;
            MouseDoubleClick += Window_MouseDoubleClick;
            PreviewMouseMove += Window_PreviewMouseMove;
            PreviewMouseUp += Window_PreviewMouseUp;

            InitializeComponent();
            DataContext = mainViewModel;
        }
    }
}