using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace TgaBuilderAvaloniaUi.Converters
{
    internal class WpfColorToColorStruct : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Color wpfColor)
            {
                if (wpfColor.A < 255)
                {
                    return new TgaBuilderLib.Abstraction.Color(wpfColor.R, wpfColor.G, wpfColor.B, wpfColor.A);
                }
                else
                {
                    return new TgaBuilderLib.Abstraction.Color(wpfColor.R, wpfColor.G, wpfColor.B);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("WpfColorToColorStruct: Value is not of type Color. Returning Fallback color.");
                return new TgaBuilderLib.Abstraction.Color(0, 0, 0, 0);
            }
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TgaBuilderLib.Abstraction.Color color)
            {
                if (color.A.HasValue)
                {
                    return Color.FromArgb(color.A.Value, color.R, color.G, color.B);
                }
                else
                {
                    return Color.FromRgb(color.R, color.G, color.B);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("WpfColorToColorStruct: Value is not of type Color. Returning Fallback color.");
                return Colors.Transparent;
            }
        }
    }
}
