using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace TgaBuilderAvaloniaUi.Converters;
internal class EnumToBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;

        return value.Equals(parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked)
        {
            return parameter;
        }
        return BindingOperations.DoNothing;
    }
}