using System.Windows;
using System.Windows.Input;
using TgaBuilderWpfUi.View;

namespace TgaBuilderWpfUi.AttachedProperties
{
    public static class MouseOverInfoAP
    {
        public static readonly DependencyProperty InfoTextProperty =
            DependencyProperty.RegisterAttached(
                "InfoText",
                typeof(string),
                typeof(MouseOverInfoAP),
                new PropertyMetadata(null, OnInfoTextChanged));

        public static readonly DependencyProperty EnableMouesUpdatesProperty =
            DependencyProperty.RegisterAttached(
                "EnableMouseUpdates",
                typeof(bool),
                typeof(MouseOverInfoAP),
                new PropertyMetadata(false, OnEnableMouseUpdatesChanged));

        public static string GetInfoText(DependencyObject obj) => (string)obj.GetValue(InfoTextProperty);
        public static void SetInfoText(DependencyObject obj, string value) => obj.SetValue(InfoTextProperty, value);

        public static bool GetEnableMouseUpdates(DependencyObject obj) => (bool)obj.GetValue(EnableMouesUpdatesProperty);
        public static void SetEnableMouseUpdates(DependencyObject obj, bool value) => obj.SetValue(EnableMouesUpdatesProperty, value);

        private static void OnInfoTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if (e.NewValue != null)
                {
                    element.MouseEnter += Element_MouseEnter;
                    element.MouseLeave += Element_MouseLeave;

                    if (!GetEnableMouseUpdates(element))
                        return;

                    element.MouseMove += Element_MouseMove;
                    element.PreviewMouseWheel += Element_PreviewMouseWheel;
                    element.PreviewMouseUp += Element_PreviewMouseUp;
                    element.PreviewMouseRightButtonUp += Element_PreviewMouseRightButtonUp;

                }
                else
                {
                    element.MouseEnter -= Element_MouseEnter;
                    element.MouseLeave -= Element_MouseLeave;

                    element.MouseMove -= Element_MouseMove;
                    element.MouseWheel -= Element_PreviewMouseWheel;
                    element.PreviewMouseUp -= Element_PreviewMouseUp;
                    element.PreviewMouseRightButtonUp -= Element_PreviewMouseRightButtonUp;
                }
            }
        }

        private static void OnEnableMouseUpdatesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                bool isEnabled = (bool)e.NewValue;

                var infoText = GetInfoText(d);
                if (!string.IsNullOrEmpty(infoText))
                {
                    if (isEnabled)
                        element.MouseMove += Element_MouseMove;
                    else
                        element.MouseMove -= Element_MouseMove;
                }
            }
        }

        private static void Element_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element && Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.MouseOverInfoTextBlock.Text = GetInfoText(element);
            }
        }

        private static void Element_MouseLeave(object sender, MouseEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.MouseOverInfoTextBlock.Text = string.Empty;
            }
        }

        private static void Element_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element && Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.MouseOverInfoTextBlock.Text = GetInfoText(element);
            }
        }

        private static void Element_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is FrameworkElement element && Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.MouseOverInfoTextBlock.Text = GetInfoText(element);
            }
        }

        private static void Element_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.MouseOverInfoTextBlock.Text = GetInfoText(element);
            }
        }

        private static void Element_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.MouseOverInfoTextBlock.Text = GetInfoText(element);
            }
        }
    }
}
