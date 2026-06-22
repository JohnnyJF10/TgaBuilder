using Avalonia.Data.Converters;
using Avalonia.Media;
using System;

namespace TrLynxAvaloniaUi.Converters
{
    internal class BoolToStrokeDashStyleConverter : IValueConverter
    {


        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? DashStyle.Dot : null;
            }
            else
            {
                return null;
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
                return true;
            }
        }
    }
}
