using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace TgaBuilderAvaloniaUi.Converters
{
    public class ZoomToOffsetConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values[0] is not double size) return 0.0f;
            if (values[1] is not double zoom) return 0.0f;

            return size - size * zoom;
        }
    }
}
