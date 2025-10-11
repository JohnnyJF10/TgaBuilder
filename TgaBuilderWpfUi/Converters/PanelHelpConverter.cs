using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using TgaBuilderLib.Enums;

namespace TgaBuilderWpfUi.Converters
{
    internal class PanelHelpConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not PanelHelpType panelInfoType)
                return string.Empty;

            return panelInfoType switch
            {
                PanelHelpType.SourceOnPanZoomInfo => "Source Panel: Ctrl + Mouse: Move, Mouse Wheel: Zoom",
                PanelHelpType.SourceOnPanelInfo => "Source Panel: Left: Select, Right: Animate, Alt: Free selecting, Double Left: Move Grid",
                PanelHelpType.DestinationOnPanZoomInfo => "Destination Panel: Ctrl + Mouse: Move, Mouse Wheel: Zoom",
                PanelHelpType.DestinationOnPanelPickingInfo => "Left: Select, Right: Animate",
                PanelHelpType.DestinationOnPanelPlacingInfo => "Left: Place, Right: Discard",
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
