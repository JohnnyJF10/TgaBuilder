using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace TrLynxAvaloniaUi.Converters;

public class ColorStructToColorHexConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TrLynxLib.Abstraction.Color color)
            return null;
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";

    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string)
            return null;

        if (string.IsNullOrEmpty((string)value))
            return null;

        var str = ((string)value).TrimStart('#');
        if (str.Length != 8)
            return null;

        if (byte.TryParse(str.Substring(0, 2), NumberStyles.HexNumber, culture, out var r) &&
            byte.TryParse(str.Substring(2, 2), NumberStyles.HexNumber, culture, out var g) &&
            byte.TryParse(str.Substring(4, 2), NumberStyles.HexNumber, culture, out var b) &&
            byte.TryParse(str.Substring(6, 2), NumberStyles.HexNumber, culture, out var a))
        {
            return new TrLynxLib.Abstraction.Color(r, g, b, a);
        }

        return null;
    }
}