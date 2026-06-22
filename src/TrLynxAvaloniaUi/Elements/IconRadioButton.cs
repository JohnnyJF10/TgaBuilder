using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace TrLynxAvaloniaUi.Elements
{
    internal class IconRadioButton : RadioButton
    {
        public static readonly StyledProperty<Geometry> IconProperty =
            AvaloniaProperty.Register<IconRadioButton, Geometry>(nameof(Icon));

        public Geometry Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
    }
}
