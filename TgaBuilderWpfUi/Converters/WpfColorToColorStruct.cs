using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace TgaBuilderWpfUi.Converters
{
    internal class WpfColorToColorStruct : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Media.Color wpfColor)
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
                System.Diagnostics.Debug.WriteLine("WpfColorToColorStruct: Value is not of type System.Windows.Media.Color. Returning Fallback color.");
                return new TgaBuilderLib.Abstraction.Color(0, 0, 0, 0);
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TgaBuilderLib.Abstraction.Color color)
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
                System.Diagnostics.Debug.WriteLine("WpfColorToColorStruct: Value is not of type Color. Returning Fallback color.");
                return System.Windows.Media.Colors.Transparent;
            }
        }
    }
}
