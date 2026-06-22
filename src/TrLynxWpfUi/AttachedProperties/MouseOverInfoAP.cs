using System.Windows;
using System.Windows.Input;
using TrLynxWpfUi.View;

namespace TrLynxWpfUi.AttachedProperties
{
    public static class MouseOverInfoAP
    {
        public static readonly DependencyProperty InfoTextProperty =
            DependencyProperty.RegisterAttached(
                "InfoText",
                typeof(string),
                typeof(MouseOverInfoAP),
                new PropertyMetadata(null, OnPropertyChanged));

        public static readonly DependencyProperty EnableMouesUpdatesProperty =
            DependencyProperty.RegisterAttached(
                "EnableMouseUpdates",
                typeof(bool),
                typeof(MouseOverInfoAP),
                new PropertyMetadata(false, OnPropertyChanged));

        public static string? GetInfoText(DependencyObject obj)
            => (string?)obj.GetValue(InfoTextProperty);

        public static void SetInfoText(DependencyObject obj, string? value)
            => obj.SetValue(InfoTextProperty, value);

        public static bool GetEnableMouseUpdates(DependencyObject obj)
            => (bool)obj.GetValue(EnableMouesUpdatesProperty);

        public static void SetEnableMouseUpdates(DependencyObject obj, bool value)
            => obj.SetValue(EnableMouesUpdatesProperty, value);

        private static void OnPropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element)
                return;

            UpdateHandlers(element);
        }

        private static void UpdateHandlers(UIElement element)
        {
            Cleanup(element);

            var infoText = GetInfoText(element);

            if (string.IsNullOrWhiteSpace(infoText))
                return;

            element.MouseEnter += Element_UpdateText;
            element.MouseLeave += Element_ClearText;

            if (GetEnableMouseUpdates(element))
            {
                element.MouseMove += Element_UpdateText;

                element.PreviewMouseWheel += Element_PreviewMouseWheel;
                element.PreviewMouseUp += Element_PreviewMouseUp;
                element.PreviewMouseRightButtonUp += Element_PreviewMouseRightButtonUp;
            }
        }

        private static void Cleanup(UIElement element)
        {
            element.MouseEnter -= Element_UpdateText;
            element.MouseLeave -= Element_ClearText;

            element.MouseMove -= Element_UpdateText;

            element.PreviewMouseWheel -= Element_PreviewMouseWheel;
            element.PreviewMouseUp -= Element_PreviewMouseUp;
            element.PreviewMouseRightButtonUp -= Element_PreviewMouseRightButtonUp;
        }

        private static void Element_UpdateText(
            object sender,
            MouseEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                UpdateText(element);
            }
        }

        private static void Element_ClearText(
            object sender,
            MouseEventArgs e)
        {
            if (GetMainWindow() is MainWindow mainWindow)
            {
                mainWindow.MouseOverInfoTextBlock.Text = string.Empty;
            }
        }

        private static void Element_PreviewMouseWheel(
            object sender,
            MouseWheelEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                UpdateText(element);
            }
        }

        private static void Element_PreviewMouseUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                UpdateText(element);
            }
        }

        private static void Element_PreviewMouseRightButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                UpdateText(element);
            }
        }

        private static void UpdateText(FrameworkElement? element)
        {
            if (element == null)
                return;

            if (GetMainWindow() is not MainWindow mainWindow)
                return;

            var text = GetInfoText(element);

            if (mainWindow.MouseOverInfoTextBlock.Text != text)
            {
                mainWindow.MouseOverInfoTextBlock.Text = text;
            }
        }

        private static MainWindow? GetMainWindow()
        {
            return Application.Current?.MainWindow as MainWindow;
        }
    }
}