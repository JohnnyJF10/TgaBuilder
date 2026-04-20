using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace TgaBuilderAvaloniaUi.Elements
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
