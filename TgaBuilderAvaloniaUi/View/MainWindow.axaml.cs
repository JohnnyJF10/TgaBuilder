using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using TgaBuilderAvaloniaUi.Elements;
using TgaBuilderAvaloniaUi.Services;
using TgaBuilderLib.Enums;
using TgaBuilderLib.ViewModel;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class MainWindow : AsyncWindow
    {
        public Image? CurrentImage { get; set; }
        public ZoomBorder? CurrentPanel { get; set; }

        public NotificationManager Manager { get; }

        private MouseModifier _modifier = MouseModifier.None;

        private Dictionary<Image, ZoomBorder>? _imagePanelDict;

        public MainWindow(INotifyPropertyChanged mainViewModel, NotificationManager manager)
        {
            Manager = manager;

            AddHandler(InputElement.PointerPressedEvent, Window_PointerPressed, handledEventsToo: true);
            AddHandler(InputElement.PointerReleasedEvent, Window_PointerReleased, handledEventsToo: true);
            AddHandler(InputElement.PointerMovedEvent, Window_PointerMoved, handledEventsToo: true);
            AddHandler(InputElement.DoubleTappedEvent, Window_DoubleTapped, handledEventsToo: true);
            AddHandler(InputElement.PointerWheelChangedEvent, Window_PointerWheelChanged, handledEventsToo: true);

            InitializeComponent();
            base.DataContext = mainViewModel;


            if (mainViewModel is MainViewModel vm)
            {
                this.Opened += (_, _) =>
                {
                    var sourcePanel = this.FindControl<ZoomBorder>("SourcePanel");
                    var targetPanel = this.FindControl<ZoomBorder>("TargetPanel");
                    var sourceScrollViewer = this.FindControl<ScrollViewer>("SourceScrollViewer");
                    var targetScrollViewer = this.FindControl<ScrollViewer>("TargetScrollViewer");
                    if (sourcePanel != null && vm.SourceViewTab is ReadOnlyViewTabViewModel sourceVm)
                        RegisterZoomBorderCallbacks(sourceVm, sourcePanel);
                    if (targetPanel != null && vm.DestinationViewTab is ReadOnlyViewTabViewModel targetVm)
                        RegisterZoomBorderCallbacks(targetVm, targetPanel);
                    if (sourcePanel != null && sourceScrollViewer != null)
                        RegisterPresenterChangedCallback(vm.Source, sourcePanel, sourceScrollViewer);
                    if (targetPanel != null && targetScrollViewer != null)
                        RegisterPresenterChangedCallback(vm.Destination, targetPanel, targetScrollViewer);
                    if (sourceScrollViewer != null)
                        RegisterScrollViewScrollSpeedModification(sourceScrollViewer);
                    if (targetScrollViewer != null)
                        RegisterScrollViewScrollSpeedModification(targetScrollViewer);
                };
            }
        }

        [Obsolete("For designer use only")]
        public MainWindow()
        {
            var serviceProvider = GlobalServiceProvider.Instance;

            var mainViewModel = serviceProvider.GetRequiredService<MainViewModel>()
                ?? throw new InvalidOperationException("MainViewModel not found in DI container");
            Manager = serviceProvider.GetRequiredService<NotificationManager>();
            InitializeComponent();
            base.DataContext = mainViewModel;
        }
    }
}