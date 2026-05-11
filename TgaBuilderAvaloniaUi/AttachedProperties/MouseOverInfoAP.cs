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

        private static void OnPropertyChanged(
            AvaloniaObject obj,
            AvaloniaPropertyChangedEventArgs args)
        {
            if (obj is not Control control)
                return;

            UpdateHandlers(control);
        }

        private static void UpdateHandlers(Control control)
        {
            Cleanup(control);

            var infoText = GetInfoText(control);

            if (string.IsNullOrWhiteSpace(infoText))
                return;

            control.PointerEntered += Control_UpdateText;
            control.PointerExited += Control_ClearText;

            if (GetEnableMouseUpdates(control))
            {
                control.PointerMoved += Control_UpdateText;
                control.PointerReleased += Control_UpdateText;

                control.AddHandler(
                    InputElement.PointerWheelChangedEvent,
                    Control_PointerWheelChanged,
                    handledEventsToo: true);
            }
        }

        private static void Cleanup(Control control)
        {
            control.PointerEntered -= Control_UpdateText;
            control.PointerExited -= Control_ClearText;

            control.PointerMoved -= Control_UpdateText;
            control.PointerReleased -= Control_UpdateText;

            control.RemoveHandler(
                InputElement.PointerWheelChangedEvent,
                Control_PointerWheelChanged);
        }

        private static void Control_UpdateText(
            object? sender,
            PointerEventArgs e)
        {
            UpdateText(sender as Control);
        }

        private static void Control_ClearText(
            object? sender,
            PointerEventArgs e)
        {
            if (GetMainWindow() is MainWindow mainWindow)
            {
                mainWindow.MouseOverInfoTextBlock.Text = string.Empty;
            }
        }

        private static void Control_PointerWheelChanged(
            object? sender,
            PointerWheelEventArgs e)
        {
            UpdateText(sender as Control);
        }

        private static void UpdateText(Control? control)
        {
            if (control == null)
                return;

            if (GetMainWindow() is not MainWindow mainWindow)
                return;

            var text = GetInfoText(control);

            if (mainWindow.MouseOverInfoTextBlock.Text != text)
            {
                mainWindow.MouseOverInfoTextBlock.Text = text;
            }
        }

        private static MainWindow? GetMainWindow()
        {
            if (Application.Current?.ApplicationLifetime
                is not IClassicDesktopStyleApplicationLifetime desktop)
                return null;

            return desktop.MainWindow as MainWindow;
        }
    }
}