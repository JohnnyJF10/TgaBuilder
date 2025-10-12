using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace TgaBuilderAvaloniaUi.Elements
{
    public class IconButton : Button
    {
        public static readonly StyledProperty<Geometry> IconProperty =
            AvaloniaProperty.Register<IconButton, Geometry>(nameof(Icon));

        public Geometry Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
    }
}
