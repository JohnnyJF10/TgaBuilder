using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using System;
using System.Diagnostics;
using TgaBuilderAvaloniaUi.View;

namespace TgaBuilderAvaloniaUi.AttachedProperties
{
    public class MouseOverInfoAP : AvaloniaObject
    {
        static MouseOverInfoAP()
        {
            InfoTextProperty.Changed.AddClassHandler<AvaloniaObject>(OnPropertyChanged);
            EnableMouseUpdatesProperty.Changed.AddClassHandler<AvaloniaObject>(OnPropertyChanged);
        }

        public static readonly AttachedProperty<string?> InfoTextProperty =
            AvaloniaProperty.RegisterAttached<Control, string?>(
                name: "InfoText",
                ownerType: typeof(MouseOverInfoAP),
                defaultValue: null);

        public static readonly AttachedProperty<bool> EnableMouseUpdatesProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>(
                name: "EnableMouseUpdates",
                ownerType: typeof(MouseOverInfoAP),
                defaultValue: false);

        public static string? GetInfoText(AvaloniaObject obj)
            => obj.GetValue(InfoTextProperty);

        public static void SetInfoText(AvaloniaObject obj, string? value)
            => obj.SetValue(InfoTextProperty, value);

        public static bool GetEnableMouseUpdates(AvaloniaObject obj)
            => obj.GetValue(EnableMouseUpdatesProperty);

        public static void SetEnableMouseUpdates(AvaloniaObject obj, bool value)
            => obj.SetValue(EnableMouseUpdatesProperty, value);

        private static void OnPropertyChanged(AvaloniaObject obj, AvaloniaPropertyChangedEventArgs args)
        {
            if (obj is Control control)
            {
                UpdateHandlers(control);
            }
        }

        private static void UpdateHandlers(Control control)
        {
            Cleanup(control);

            var infoText = GetInfoText(control);
            var enableMouse = GetEnableMouseUpdates(control);

            var isActive = !string.IsNullOrEmpty(infoText);

            if (!isActive)
                return;

            control.PointerEntered += Control_PointerEntered;
            control.PointerExited += Control_PointerExited;

            if (enableMouse)
            {
                control.PointerMoved += Control_PointerMoved;

                control.AddHandler(
                    InputElement.PointerWheelChangedEvent,
                    Control_PointerWheelChanged,
                    handledEventsToo: true);

                control.PointerReleased += Control_PointerReleased;
            }

            control.DetachedFromVisualTree -= Control_Detached;
            control.DetachedFromVisualTree += Control_Detached;
        }

        private static void Cleanup(Control control)
        {
            control.PointerEntered -= Control_PointerEntered;
            control.PointerExited -= Control_PointerExited;
            control.PointerMoved -= Control_PointerMoved;
            control.PointerReleased -= Control_PointerReleased;

            control.RemoveHandler(
                InputElement.PointerWheelChangedEvent,
                Control_PointerWheelChanged);

            control.DetachedFromVisualTree -= Control_Detached;
        }

        private static void Control_Detached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (sender is Control control)
            {
                Cleanup(control);
            }
        }

        private static void Control_PointerEntered(object? sender, PointerEventArgs e)
        {
            UpdateText(sender as Control);
        }

        private static void Control_PointerExited(object? sender, PointerEventArgs e)
        {
            var mainWindow = GetMainWindow();
            if (mainWindow != null)
                mainWindow.MouseOverInfoTextBlock.Text = string.Empty;
        }

        private static void Control_PointerMoved(object? sender, PointerEventArgs e)
        {
            UpdateText(sender as Control);
        }

        private static void Control_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            UpdateText(sender as Control);
        }

        private static void Control_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            UpdateText(sender as Control);
        }

        private static void UpdateText(Control? control)
        {
            if (control == null)
                return;

            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                mainWindow.MouseOverInfoTextBlock.Text = GetInfoText(control);
            }
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
