using System.Windows;
using System.Windows.Input;

namespace TgaBuilderWpfUi.AttachedProperties
{
    public static class ScrollingAP
    {
        public static readonly DependencyProperty ScrollCommandProperty =
            DependencyProperty.RegisterAttached(
                "ScrollCommand",
                typeof(ICommand),
                typeof(ScrollingAP),
                new PropertyMetadata(null));

        public static void SetScrollCommand(UIElement element, ICommand value)
        {
            element.SetValue(ScrollCommandProperty, value);
        }

        public static ICommand GetScrollCommand(UIElement element)
        {
            return (ICommand)element.GetValue(ScrollCommandProperty);
        }

    }
}
