using Avalonia.Data.Converters;
using Avalonia.Media;
using System;

namespace TgaBuilderAvaloniaUi.Converters
{
    internal class BoolToStrokeDashStyleConverter : IValueConverter
    {


        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? DashStyle.Dot : DashStyle.Dash;
            }
            else
            {
                return DashStyle.Dash;
            }
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DashStyle ds)
            {
                return ds == DashStyle.Dot;
            }
            else
            {
                return false;
            }
        }
    }
}
