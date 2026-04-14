using Avalonia.Data.Converters;
using System;
using System.Diagnostics;
using System.Globalization;

namespace TgaBuilderAvaloniaUi.Converters
{
    internal class SignSwitchConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                double multiplier = parameter is double paramDouble ? paramDouble : 1.0;
                return doubleValue * -1.0 * multiplier;
            }
            else if (value is int intValue)
            {
                return intValue * -1;
            }
            else
            {
                Debug.WriteLine($"SignSwitchConverter: Unsupported type {value?.GetType().Name}");
                return value;
            }
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                double multiplier = parameter is double paramDouble ? paramDouble : 1.0;
                return doubleValue * -1.0 / multiplier;
            }
            else if (value is int intValue)
            {
                return intValue * -1;
            }
            else
            {
                Debug.WriteLine($"SignSwitchConverter: Unsupported type {value?.GetType().Name}");
                return value;
            }
        }
    }
}
