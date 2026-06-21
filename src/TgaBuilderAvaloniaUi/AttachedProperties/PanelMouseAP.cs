using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using System;
using System.Diagnostics;
using System.Windows.Input;
using TgaBuilderAvaloniaUi.View;

namespace TgaBuilderAvaloniaUi.AttachedProperties
{
    public class PanelMouseAP : AvaloniaObject
    {
        static PanelMouseAP()
        {
            CheckMouseEnterLeaveProperty.Changed.AddClassHandler<Control>(OnCheckMouseEnterLeavePropertyChanged);
        }

        public static readonly AttachedProperty<ICommand?> PanelMouseCommandProperty =
            AvaloniaProperty.RegisterAttached<Control, ICommand?>(
                name: "PanelMouseCommand",
                ownerType: typeof(PanelMouseAP),
                defaultValue: null);

        public static void SetPanelMouseCommand(Control element, ICommand value)
        {
            element.SetValue(PanelMouseCommandProperty, value);
        }

        public static ICommand? GetPanelMouseCommand(Control element)
        {
            return element.GetValue(PanelMouseCommandProperty);
        }

        public static readonly AttachedProperty<bool> CheckMouseEnterLeaveProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>(
                name: "CheckMouseEnterLeave",
                ownerType: typeof(PanelMouseAP),
                defaultValue: false);

        public static void SetCheckMouseEnterLeave(Control element, bool value)
        {
            element.SetValue(CheckMouseEnterLeaveProperty, value);
        }

        public static bool GetCheckMouseEnterLeave(Control element)
        {
            return (bool)element.GetValue(CheckMouseEnterLeaveProperty);
        }

        private static void OnCheckMouseEnterLeavePropertyChanged(AvaloniaObject obj, AvaloniaPropertyChangedEventArgs args)
        {
            if (obj is Image image && args.NewValue is bool newBoolVal)
            {
                Cleanup(image);

                if (newBoolVal)
                {
                    image.PointerEntered += Image_PointerEntered;
                    image.PointerExited += Image_PointerExited;

                    // Lifecycle Hook
                    image.DetachedFromVisualTree -= Image_Detached;
                    image.DetachedFromVisualTree += Image_Detached;
                }
            }
        }

        private static void Image_Detached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (sender is Image image)
            {
                Cleanup(image);
            }
        }

        private static void Cleanup(Image image)
        {
            image.PointerEntered -= Image_PointerEntered;
            image.PointerExited -= Image_PointerExited;

            image.DetachedFromVisualTree -= Image_Detached;
        }

        private static void Image_PointerEntered(object? sender, PointerEventArgs e)
        {
            if (sender is Image image)
            {
                var mainWindow = GetMainWindow();

                mainWindow.CurrentImage = image;
                mainWindow.SetPanelFromImage(image);

                if (mainWindow.CurrentPanel is not null)
                    mainWindow.CurrentPanel.EnableAnimations = false;

                bool isDestination = GetIsTargetPanel(image);

                var EnterPanelCommand = GetEnterPanelCommand(mainWindow);

                if (EnterPanelCommand is not null && EnterPanelCommand.CanExecute(isDestination))
                    EnterPanelCommand.Execute(isDestination);
            }
        }

        private static void Image_PointerExited(object? sender, PointerEventArgs e)
        {
            if (sender is Image image && e.Pointer.Captured != image)
            {
                OnPointerExited(image);
            }
        }

        internal static void OnPointerExited(Image image)
        {
            bool isDestination = GetIsTargetPanel(image);

            var mainWindow = GetMainWindow();

            if (mainWindow.CurrentPanel is not null)
                mainWindow.CurrentPanel.EnableAnimations = true;

            mainWindow.CurrentImage = null;
            mainWindow.CurrentPanel = null;

            var LeavePanelCommand = GetLeavePanelCommand(mainWindow);

            if (LeavePanelCommand is not null && LeavePanelCommand.CanExecute(isDestination))
                LeavePanelCommand.Execute(isDestination);
        }

        public static readonly AttachedProperty<ICommand?> LeavePanelCommandProperty =
            AvaloniaProperty.RegisterAttached<Control, ICommand?>(
                name: "LeavePanelCommand",
                ownerType: typeof(PanelMouseAP),
                defaultValue: null);

        public static void SetLeavePanelCommand(Control element, ICommand value)
        {
            element.SetValue(LeavePanelCommandProperty, value);
        }
        public static ICommand? GetLeavePanelCommand(Control element)
        {
            return element.GetValue(LeavePanelCommandProperty);
        }

        public static readonly AttachedProperty<ICommand?> EnterPanelCommandProperty =
            AvaloniaProperty.RegisterAttached<Control, ICommand?>(
                name: "EnterPanelCommand",
                ownerType: typeof(PanelMouseAP),
                defaultValue: null);

        public static void SetEnterPanelCommand(Control element, ICommand value)
        {
            element.SetValue(EnterPanelCommandProperty, value);
        }
        public static ICommand? GetEnterPanelCommand(Control element)
        {
            return element.GetValue(EnterPanelCommandProperty);
        }

        public static readonly AttachedProperty<bool> IsTargetPanelProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>(
                name: "IsTargetPanel",
                ownerType: typeof(PanelMouseAP),
                defaultValue: false);

        public static void SetIsTargetPanel(Control element, bool value)
        {
            element.SetValue(IsTargetPanelProperty, value);
        }
        public static bool GetIsTargetPanel(Control element)
        {
            return element.GetValue(IsTargetPanelProperty);
        }

        public static readonly AttachedProperty<ICommand?> ScrollCommandProperty =
            AvaloniaProperty.RegisterAttached<Control, ICommand?>(
                name: "ScrollCommand",
                ownerType: typeof(PanelMouseAP),
                defaultValue: null);

        public static void SetScrollCommand(Control element, ICommand value)
        {
            element.SetValue(ScrollCommandProperty, value);
        }

        public static ICommand? GetScrollCommand(Control element)
        {
            return element.GetValue(ScrollCommandProperty);
        }

        public static readonly AttachedProperty<ICommand?> WheelShiftCommandProperty =
            AvaloniaProperty.RegisterAttached<Control, ICommand?>(
                name: "WheelShiftCommand",
                ownerType: typeof(PanelMouseAP),
                defaultValue: null);

        public static void SetWheelShiftCommand(Control element, ICommand value)
        {
            element.SetValue(WheelShiftCommandProperty, value);
        }

        public static ICommand? GetWheelShiftCommand(Control element)
        {
            return element.GetValue(WheelShiftCommandProperty);
        }

        public static readonly AttachedProperty<ICommand?> EndScrollCommandProperty =
            AvaloniaProperty.RegisterAttached<Control, ICommand?>(
                name: "EndScrollCommand",
                ownerType: typeof(PanelMouseAP),
                defaultValue: null);

        public static void SetEndScrollCommand(Control element, ICommand value)
        {
            element.SetValue(EndScrollCommandProperty, value);
        }

        public static ICommand? GetEndScrollCommand(Control element)
        {
            return element.GetValue(EndScrollCommandProperty);
        }

        private static MainWindow GetMainWindow()
        {
            if (Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                throw new InvalidOperationException("This attached property can only be used in a classic desktop application.");

            if (desktop.MainWindow is not MainWindow mainWindow)
                throw new InvalidOperationException("The main window of the application must be of type MainWindow.");

            return mainWindow;
        }
    }
}
