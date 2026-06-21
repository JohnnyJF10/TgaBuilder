using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace TgaBuilderAvaloniaUi.Converters
{
    internal class TupleToPointConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is (int x, int y))
                return new Point(x, y);

            if (value is (double dx, double dy))
                return new Point(dx, dy);

            return new Point(0, 0);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Point p)
                return (p.X, p.Y);

            return (0, 0);
        }
    }
}
