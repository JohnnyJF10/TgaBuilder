using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace TgaBuilderWpfUi.Converters;

public class ColorStructToMediaColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TgaBuilderLib.Abstraction.Color color)
            return null;

        return new System.Windows.Media.Color
        {
            R = color.R,
            G = color.G,
            B = color.B,
            A = color.A ?? 255
        };

    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not System.Windows.Media.Color mediaColor)
            return null;

        return new TgaBuilderLib.Abstraction.Color(mediaColor.R, mediaColor.G, mediaColor.B, mediaColor.A);
    }
}
