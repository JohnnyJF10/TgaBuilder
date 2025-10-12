using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
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
                name:           "PanelMouseCommand",
                ownerType:      typeof(PanelMouseAP),
                defaultValue:   null);

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
                if (newBoolVal)
                {
                    image.PointerEntered += Image_PointerEntered;
                    image.PointerExited += Image_PointerExited;
                }
                else
                {
                    image.PointerEntered -= Image_PointerEntered;
                    image.PointerExited -= Image_PointerExited;
                }
            }
        }

        private static void Image_PointerEntered(object? sender, PointerEventArgs e)
        {
            if (sender is Image image)
            {
                var mainWindow = GetMainWindow();

                mainWindow.CurrentImage = image;
                mainWindow.SetPanelFromImage(image);

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

            mainWindow.CurrentImage = null;
            mainWindow.CurrentPanel = null;

            var LeavePanelCommand = GetLeavePanelCommand(mainWindow);

            if (LeavePanelCommand is not null && LeavePanelCommand.CanExecute(isDestination))
                LeavePanelCommand.Execute(isDestination);

            mainWindow.Cursor = new Cursor(StandardCursorType.Arrow);
        }

        public static readonly AttachedProperty<ICommand?> LeavePanelCommandProperty =
            AvaloniaProperty.RegisterAttached<Control, ICommand?>(
                name:           "LeavePanelCommand",
                ownerType:      typeof(PanelMouseAP),
                defaultValue:   null);

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
                name:           "EnterPanelCommand",
                ownerType:      typeof(PanelMouseAP),
                defaultValue:   null);

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
                name:           "IsTargetPanel",
                ownerType:      typeof(PanelMouseAP),
                defaultValue:   false);

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
                name:           "ScrollCommand",
                ownerType:      typeof(PanelMouseAP),
                defaultValue:   null);

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
                name:           "WheelShiftCommand",
                ownerType:      typeof(PanelMouseAP),
                defaultValue:   null);

        public static void SetWheelShiftCommand(Control element, ICommand value)
        {
            element.SetValue(WheelShiftCommandProperty, value);
        }

        public static ICommand? GetWheelShiftCommand(Control element)
        {
            return element.GetValue(WheelShiftCommandProperty);
        }

        private static MainWindow GetMainWindow()
        {
            if (Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                Debug.WriteLine("The application is not running in desktop mode.");
                return new MainWindow();
            }

            if (desktop.MainWindow is not MainWindow mainWindow)
            {
                Debug.WriteLine("Could not get the MainWindow.");
                return new MainWindow();
            }

            return mainWindow;
        }
    }
}
