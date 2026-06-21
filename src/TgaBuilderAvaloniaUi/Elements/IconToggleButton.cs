using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace TgaBuilderAvaloniaUi.Elements
{
    internal class IconToggleButton : ToggleButton
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
