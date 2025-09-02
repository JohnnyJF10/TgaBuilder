
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

using TgaBuilderWpfUi.Wrappers;

public class WriteableBitmapConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is WriteableBitmapWrapper wrapper
        ? wrapper.Inner
        : new WriteableBitmap(42, 42, 96, 96, System.Windows.Media.PixelFormats.Rgb24, null);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new InvalidOperationException("XAML Binding Mode must be OneWay");
}