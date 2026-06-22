using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace TrLynxAvaloniaUi.Converters
{
    internal class ColorStructToBrush : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TrLynxLib.Abstraction.Color color)
            {
                if (color.A.HasValue)
                {
                    return new SolidColorBrush(
                        Color.FromArgb(color.A.Value, color.R, color.G, color.B));
                }
                else
                {
                    return new SolidColorBrush(
                        Color.FromRgb(color.R, color.G, color.B));
                }
            }
            else
            {

                return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return new TrLynxLib.Abstraction.Color(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B);
            }
            else
            {
                return new TrLynxLib.Abstraction.Color(0, 0, 0, 0);
            }
        }
    }
}
