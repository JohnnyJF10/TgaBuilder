using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace TgaBuilderWpfUi.Converters
{
    internal class ColorStructToBrush : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        { 
            if (value is TgaBuilderLib.Abstraction.Color color)
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
                Debug.WriteLine("ColorStructToWpfColor: Value is not of type Color. Returning Fallback color.");

                return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return new TgaBuilderLib.Abstraction.Color(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B);
            }
            else
            {
                Debug.WriteLine("ColorStructToWpfColor: Value is not of type SolidColorBrush. Returning Fallback color.");
                return new TgaBuilderLib.Abstraction.Color(0, 0, 0, 0);
            }
        }
    }
}
