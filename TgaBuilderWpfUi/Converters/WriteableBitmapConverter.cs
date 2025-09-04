
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

using TgaBuilderWpfUi.Wrappers;

namespace TgaBuilderWpfUi.Converters
{
    public class WriteableBitmapConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WriteableBitmapWrapper wrapper)
                return wrapper.InnerWriteableBitmap;

            Debug.WriteLine("WriteableBitmapConverter: Value is not of type WriteableBitmapWrapper. Returning Fallback image.");

            return new WriteableBitmap(42, 42, 96, 96, System.Windows.Media.PixelFormats.Rgb24, null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new InvalidOperationException("XAML Binding Mode must be OneWay");
    }
}