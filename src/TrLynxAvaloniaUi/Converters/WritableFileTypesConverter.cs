using Avalonia.Data.Converters;
using System;
using System.Globalization;
using TrLynxLib.Enums;
using TrLynxLib.FileHandling;

namespace TrLynxAvaloniaUi.Converters;

internal class WritableFileTypesConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return null;

        if (value is not FileTypes fileTypes)
            return null;

        if (FileTypeRegistry.Lookup[fileTypes] is not { } fileType)
            return null;

        return fileType.DisplayName;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
