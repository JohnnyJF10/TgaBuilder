using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace TgaBuilderWpfUi.Converters
{
    internal class BrushToColorStruct : IValueConverter
        {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is System.Windows.Media.SolidColorBrush brush)
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
                return new TgaBuilderLib.Abstraction.Color(0, 0, 0, 0);
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is TgaBuilderLib.Abstraction.Color color)
            {
                if (color.A.HasValue)
                {
                    var wpfColor = System.Windows.Media.Color.FromArgb(color.A.Value, color.R, color.G, color.B);
                    return new System.Windows.Media.SolidColorBrush(wpfColor);
                }
                else
                {
                    var wpfColor = System.Windows.Media.Color.FromRgb(color.R, color.G, color.B);
                    return new System.Windows.Media.SolidColorBrush(wpfColor);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("BrushToColorStruct: Value is not of type Color. Returning Fallback brush.");
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
            }
        }
    }
}
