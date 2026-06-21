using Avalonia;
using Avalonia.Controls.PanAndZoom;

namespace TgaBuilderAvaloniaUi.AttachedProperties
{
    public class PanelAttributesAP : AvaloniaObject
    {
        static PanelAttributesAP()
        {
            ZoomAtrProperty.Changed.AddClassHandler<ZoomBorder>(OnZoomAtrPropertyChanged);
            OffsetXAtrProperty.Changed.AddClassHandler<ZoomBorder>(OnOffsetXAtrPropertyChanged);
            OffsetYAtrProperty.Changed.AddClassHandler<ZoomBorder>(OnOffsetYAtrPropertyChanged);
        }

        public static readonly AttachedProperty<double> ZoomAtrProperty =
            AvaloniaProperty.RegisterAttached<ZoomBorder, double>(
                name: "ZoomAtr",
                ownerType: typeof(PanelAttributesAP),
                defaultValue: 1);

        public static void SetZoomAtr(ZoomBorder element, double value)
        {
            element.SetValue(ZoomAtrProperty, value);
        }
        public static double GetZoomAtr(ZoomBorder element)
        {
            return (double)element.GetValue(ZoomAtrProperty);
        }

        public static readonly AttachedProperty<double> OffsetXAtrProperty =
            AvaloniaProperty.RegisterAttached<ZoomBorder, double>(
                name: "OffsetXAtr",
                ownerType: typeof(PanelAttributesAP),
                defaultValue: 0);
        public static void SetOffsetXAtr(ZoomBorder element, double value)
        {
            element.SetValue(OffsetXAtrProperty, value);
        }
        public static double GetOffsetXAtr(ZoomBorder element)
        {
            return (double)element.GetValue(OffsetXAtrProperty);
        }

        public static readonly AttachedProperty<double> OffsetYAtrProperty =
            AvaloniaProperty.RegisterAttached<ZoomBorder, double>(
                name: "OffsetYAtr",
                ownerType: typeof(PanelAttributesAP),
                defaultValue: 0);
        public static void SetOffsetYAtr(ZoomBorder element, double value)
        {
            element.SetValue(OffsetYAtrProperty, value);
        }
        public static double GetOffsetYAtr(ZoomBorder element)
        {
            return (double)element.GetValue(OffsetYAtrProperty);
        }

        private static void OnZoomAtrPropertyChanged(AvaloniaObject obj, AvaloniaPropertyChangedEventArgs args)
        {
            if (obj is ZoomBorder panel && args.NewValue is double newZoom && args.OldValue is double oldZoom)
            {
                panel.SetMatrix(panel.Matrix * Matrix.CreateScale(newZoom / oldZoom, newZoom / oldZoom));
            }
        }

        private static void OnOffsetXAtrPropertyChanged(AvaloniaObject obj, AvaloniaPropertyChangedEventArgs args)
        {
            if (obj is ZoomBorder panel && args.NewValue is double newOffsetX && args.OldValue is double oldOffsetX)
            {
                var zoom = GetZoomAtr(panel);
                panel.SetMatrix(panel.Matrix * Matrix.CreateTranslation(newOffsetX - oldOffsetX, 0));
            }
        }

        private static void OnOffsetYAtrPropertyChanged(AvaloniaObject obj, AvaloniaPropertyChangedEventArgs args)
        {
            if (obj is ZoomBorder panel && args.NewValue is double newOffsetY && args.OldValue is double oldOffsetY)
            {
                var zoom = GetZoomAtr(panel);
                panel.SetMatrix(panel.Matrix * Matrix.CreateTranslation(0, newOffsetY - oldOffsetY));
            }
        }
    }
}
