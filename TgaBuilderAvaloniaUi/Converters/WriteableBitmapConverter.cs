
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Diagnostics;
using System.Globalization;

using TgaBuilderAvaloniaUi.Wrappers;

namespace TgaBuilderAvaloniaUi.Converters
{
    public class WriteableBitmapConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is WriteableBitmapWrapper wrapper)
                return wrapper.InnerBitmap;

            Debug.WriteLine("WriteableBitmapConverter: Value is not of type WriteableBitmapWrapper. Returning Fallback image.");

            return new WriteableBitmap(new PixelSize(42, 42), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new InvalidOperationException("XAML Binding Mode must be OneWay");
    }
}