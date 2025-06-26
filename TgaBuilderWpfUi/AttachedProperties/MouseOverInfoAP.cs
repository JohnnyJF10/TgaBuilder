using System.Windows;
using TgaBuilderWpfUi.View;
using Application = System.Windows.Application;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

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

        public static string GetInfoText(DependencyObject obj)
        {
            return (string)obj.GetValue(InfoTextProperty);
        }

        public static void SetInfoText(DependencyObject obj, string value)
        {
            obj.SetValue(InfoTextProperty, value);
        }

        private static void OnInfoTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if (e.NewValue is string)
                {
                    element.MouseEnter += Element_MouseEnter;
                    element.MouseLeave += Element_MouseLeave;
                }
                else
                {
                    element.MouseEnter -= Element_MouseEnter;
                    element.MouseLeave -= Element_MouseLeave;
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
            if (sender is FrameworkElement element && Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.MouseOverInfoTextBlock.Text = string.Empty;
            }
        }
    }
}
