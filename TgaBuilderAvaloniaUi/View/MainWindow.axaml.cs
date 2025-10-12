using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Notification;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TgaBuilderAvaloniaUi.Elements;
using TgaBuilderLib.Enums;
using TgaBuilderLib.ViewModel;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class MainWindow : AsyncWindow
    {
        public Image? CurrentImage { get; set; }
        public ZoomBorder? CurrentPanel { get; set; }

        public INotificationMessageManager Manager { get; } = new NotificationMessageManager();

        public bool IsLoaded => throw new NotImplementedException();

        public Cursor EyedropperCursor = new Cursor(StandardCursorType.Hand);

        private MouseModifier _modifier = MouseModifier.None;

        private Dictionary<Image, ZoomBorder>? _imagePanelDict;

        public MainWindow(INotifyPropertyChanged mainViewModel)
        {
            AddHandler(InputElement.PointerPressedEvent, Window_PointerPressed, handledEventsToo: true);
            AddHandler(InputElement.PointerReleasedEvent, Window_PointerReleased, handledEventsToo: true);
            AddHandler(InputElement.PointerMovedEvent, Window_PointerMoved, handledEventsToo: true);
            AddHandler(InputElement.DoubleTappedEvent, Window_DoubleTapped, handledEventsToo: true);

            InitializeComponent();
            base.DataContext = mainViewModel;
        }

        [Obsolete("For designer use only")]
        public MainWindow()
        {
            var serviceProvider = GlobalServiceProvider.Instance;

            var mainViewModel = serviceProvider.GetRequiredService<MainViewModel>()
                ?? throw new InvalidOperationException("MainViewModel not found in DI container");
            InitializeComponent();
            base.DataContext = mainViewModel;
        }
    }
}