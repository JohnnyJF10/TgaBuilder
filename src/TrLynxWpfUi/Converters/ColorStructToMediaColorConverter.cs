using System.Globalization;
using System.Windows.Data;

namespace TrLynxWpfUi.Converters;

public class ColorStructToMediaColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TrLynxLib.Abstraction.Color color)
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

        return new TrLynxLib.Abstraction.Color(mediaColor.R, mediaColor.G, mediaColor.B, mediaColor.A);
    }
}
