using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace WPFZoomPanel.Converters
{
    public class ZoomAdjustConverter : MarkupExtension, IValueConverter
    {
        #region Public Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double doubleValue = (double)value;
            return Math.Log(doubleValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double doubleValue = (double)value;
            return Math.Exp(doubleValue);
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        #endregion Public Methods
    }
}
