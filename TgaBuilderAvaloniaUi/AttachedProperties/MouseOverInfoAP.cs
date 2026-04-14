using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using System.Diagnostics;
using TgaBuilderAvaloniaUi.View;

namespace TgaBuilderAvaloniaUi.AttachedProperties
{
    public class MouseOverInfoAP : AvaloniaObject
    {
        static MouseOverInfoAP()
        {
            InfoTextProperty.Changed.AddClassHandler<AvaloniaObject>(OnInfoTextChanged);
            EnableMouseUpdatesProperty.Changed.AddClassHandler<AvaloniaObject>(OnEnableMouseUpdatesChanged);
        }

        public static readonly AttachedProperty<string?> InfoTextProperty =
            AvaloniaProperty.RegisterAttached<Control, string?>(
                name: "InfoText",
                ownerType: typeof(MouseOverInfoAP),
                defaultValue: string.Empty);

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

        private static void OnInfoTextChanged(AvaloniaObject obj, AvaloniaPropertyChangedEventArgs args)
        {
            if (obj is Control control)
            {
                if (args.NewValue is not null)
                {
                    control.PointerEntered += Control_PointerEntered;
                    control.PointerExited += Control_PointerExited;

                    if (!GetEnableMouseUpdates(control))
                        return;

                    control.PointerMoved += Control_PointerMoved;

                    control.AddHandler(
                        routedEvent: InputElement.PointerWheelChangedEvent,
                        handler: Control_PointerWheelChanged,
                        handledEventsToo: true);

                    control.PointerReleased += Control_PointerReleased;
                }
                else
                {
                    control.PointerEntered -= Control_PointerEntered;
                    control.PointerExited -= Control_PointerExited;

                    control.PointerMoved -= Control_PointerMoved;

                    control.RemoveHandler(
                        routedEvent: InputElement.PointerWheelChangedEvent,
                        handler: Control_PointerWheelChanged);

                    control.PointerReleased -= Control_PointerReleased;
                }
            }
        }

        private static void OnEnableMouseUpdatesChanged(AvaloniaObject obj, AvaloniaPropertyChangedEventArgs args)
        {
            if (obj is Control control && args.NewValue is bool newBoolVall)
            {
                if (newBoolVall)
                {
                    var infoText = GetInfoText(control);
                    if (!string.IsNullOrEmpty(infoText))
                        control.PointerMoved += Control_PointerMoved;
                }
                else
                {
                    control.PointerMoved -= Control_PointerMoved;
                }
            }
        }

        private static void Control_PointerEntered(object? sender, PointerEventArgs e)
        {
            if (sender is Control control && GetMainWindow() is MainWindow mainWindow)
            {
                mainWindow.MouseOverInfoTextBlock.Text = GetInfoText(control);
            }
        }

        private static void Control_PointerExited(object? sender, PointerEventArgs e)
        {
            if (GetMainWindow() is MainWindow mainWindow)
            {
                mainWindow.MouseOverInfoTextBlock.Text = string.Empty;
            }
        }

        private static void Control_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (sender is Control control && GetMainWindow() is MainWindow mainWindow)
            {
                mainWindow.MouseOverInfoTextBlock.Text = GetInfoText(control);
            }
        }

        private static void Control_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (sender is Control control && GetMainWindow() is MainWindow mainWindow)
            {
                mainWindow.MouseOverInfoTextBlock.Text = GetInfoText(control);
            }
        }

        private static void Control_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (sender is Control control && GetMainWindow() is MainWindow mainWindow)
            {
                mainWindow.MouseOverInfoTextBlock.Text = GetInfoText(control);
            }
        }

        private static MainWindow GetMainWindow()
        {
            if (Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                Debug.WriteLine("The application is not running in desktop mode.");
                return new MainWindow();
            }

            if (desktop.MainWindow is not MainWindow mainWindow)
                return new MainWindow();

            return mainWindow;
        }
    }
}
