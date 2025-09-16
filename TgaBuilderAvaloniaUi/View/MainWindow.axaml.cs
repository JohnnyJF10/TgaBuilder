using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using TgaBuilderAvaloniaUi.Elements;
using TgaBuilderAvaloniaUi.Services;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class MainWindow : AsyncWindow
    {
        public Image? CurrentImage { get; set; }
        public ZoomBorder? CurrentPanel { get; set; }

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
            AddHandler(InputElement.PointerWheelChangedEvent, MainWindow_PointerWheelChanged, handledEventsToo: true);

            InitializeComponent();
            base.DataContext = mainViewModel;
        }
    }
}