using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderAvaloniaUi.Converters;

internal class ColorStructToMediaColor : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TgaBuilderLib.Abstraction.Color color)
            return null;
        var mediaColor = Avalonia.Media.Color.FromArgb(
            color.A ?? 255,
            color.R,
            color.G,
            color.B);
        return mediaColor;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Avalonia.Media.Color mediaColor)
            return null;
        return new TgaBuilderLib.Abstraction.Color(mediaColor.R, mediaColor.G, mediaColor.B, mediaColor.A);
    }
}
