
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Globalization;

using TrLynxAvaloniaUi.Wrappers;

namespace TrLynxAvaloniaUi.Converters
{
    public class WriteableBitmapConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is WriteableBitmapWrapper wrapper)
                return wrapper.InnerBitmap;


            return new WriteableBitmap(new PixelSize(42, 42), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new InvalidOperationException("XAML Binding Mode must be OneWay");
    }
}