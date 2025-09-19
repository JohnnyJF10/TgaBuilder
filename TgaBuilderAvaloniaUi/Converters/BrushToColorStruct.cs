using Avalonia.Data.Converters;
using Avalonia.Media;
using System;

namespace TgaBuilderAvaloniaUi.Converters
{
    internal class BrushToColorStruct : IValueConverter
        {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                var color = brush.Color;
                if (color.A < 255)
                {
                    return new TgaBuilderLib.Abstraction.Color(color.R, color.G, color.B, color.A);
                }
                else
                {
                    return new TgaBuilderLib.Abstraction.Color(color.R, color.G, color.B);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("BrushToColorStruct: Value is not of type SolidColorBrush. Returning Fallback color.");
                System.Diagnostics.Debug.WriteLine("Type: " + (value?.GetType().FullName ?? "null"));
                return new TgaBuilderLib.Abstraction.Color(0, 0, 0, 0);
            }
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is TgaBuilderLib.Abstraction.Color color)
            {
                if (color.A.HasValue)
                {
                    var wpfColor = Color.FromArgb(color.A.Value, color.R, color.G, color.B);
                    return new SolidColorBrush(wpfColor);
                }
                else
                {
                    var wpfColor = Color.FromRgb(color.R, color.G, color.B);
                    return new SolidColorBrush(wpfColor);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("BrushToColorStruct: Value is not of type Color. Returning Fallback brush.");
                return new SolidColorBrush(Colors.Transparent);
            }
        }
    }
}
