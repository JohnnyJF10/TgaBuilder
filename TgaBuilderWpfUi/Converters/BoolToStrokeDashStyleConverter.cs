using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace TgaBuilderWpfUi.Converters
{
    internal class BoolToStrokeDashStyleConverter : IValueConverter
    {


        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? DashStyles.Dot : DashStyles.Solid;
            }
            else
            {
                return DashStyles.Solid;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DashStyle ds)
            {
                return ds == DashStyles.Dot;
            }
            else
            {
                return false;
            }
        }
    }
}
