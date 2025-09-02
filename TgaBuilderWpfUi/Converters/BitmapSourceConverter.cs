using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using TgaBuilderWpfUi.Wrappers;

public class BitmapSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is BitmapSourceWrapper wrapper
        ? wrapper.Inner
        : new WriteableBitmap(42, 42, 96, 96, PixelFormats.Rgb24, null);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new InvalidOperationException("XAML Binding Mode must be OneWay");
}