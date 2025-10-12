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
    public class BitmapSourceConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is BitmapWrapper swrapper)
                return swrapper.InnerBitmap;
        
            if (value is WriteableBitmapWrapper wwrapper)
                return wwrapper.InnerBitmap;
        
            return new WriteableBitmap(
                new PixelSize(42, 42),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);
        }
        
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new InvalidOperationException("XAML Binding Mode must be OneWay");
    }
}