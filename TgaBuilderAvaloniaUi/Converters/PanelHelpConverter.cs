using Avalonia.Data.Converters;
using System;
using System.Globalization;
using TgaBuilderLib.Enums;

namespace TgaBuilderAvaloniaUi.Converters
{
    internal class PanelHelpConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not PanelHelpType panelInfoType)
                return string.Empty;

            return panelInfoType switch
            {
                PanelHelpType.SourceOnPanZoomInfo => "Source Panel: Mouse Middle: Move, Mouse Wheel: Zoom",
                PanelHelpType.SourceOnPanelInfo => "Source Panel: Left: Select, Right: Animate, Alt: Free selecting, Double Left: Move Grid",
                PanelHelpType.DestinationOnPanZoomInfo => "Destination Panel: Mouse Middle: Move, Mouse Wheel: Zoom",
                PanelHelpType.DestinationOnPanelPickingInfo => "Left: Select, Right: Animate",
                PanelHelpType.DestinationOnPanelPlacingInfo => "Left: Place, Right: Discard",
                _ => string.Empty,
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
