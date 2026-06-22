using System.Globalization;
using System.Windows.Data;

namespace TrLynxWpfUi.Converters
{
    internal class WpfColorToColorStruct : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Media.Color wpfColor)
            {
                if (wpfColor.A < 255)
                {
                    return new TrLynxLib.Abstraction.Color(wpfColor.R, wpfColor.G, wpfColor.B, wpfColor.A);
                }
                else
                {
                    return new TrLynxLib.Abstraction.Color(wpfColor.R, wpfColor.G, wpfColor.B);
                }
            }
            else
            {
                return new TrLynxLib.Abstraction.Color(0, 0, 0, 0);
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TrLynxLib.Abstraction.Color color)
            {
                if (color.A.HasValue)
                {
                    return System.Windows.Media.Color.FromArgb(color.A.Value, color.R, color.G, color.B);
                }
                else
                {
                    return System.Windows.Media.Color.FromRgb(color.R, color.G, color.B);
                }
            }
            else
            {
                return System.Windows.Media.Colors.Transparent;
            }
        }
    }
}
