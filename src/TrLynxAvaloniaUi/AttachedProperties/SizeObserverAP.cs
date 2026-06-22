using Avalonia;
using Avalonia.Controls;

namespace TrLynxAvaloniaUi.AttachedProperties
{
    public class SizeObserverAP : AvaloniaObject
    {
        static SizeObserverAP()
        {
            // Override the metadata of the ObserveSizeProperty to add a property changed callback
            ObserveSizeProperty.Changed.AddClassHandler<Control>(OnObserveSizeChanged);
        }

        // --- Activate the observation ---
        public static readonly AttachedProperty<bool> ObserveSizeProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>(
                name: "ObserveSize",
                ownerType: typeof(SizeObserverAP),
                defaultValue: false);

        public static void SetObserveSize(Control obj, bool value) =>
            obj.SetValue(ObserveSizeProperty, value);

        public static bool GetObserveSize(Control obj) =>
            (bool)obj.GetValue(ObserveSizeProperty);

        private static void OnObserveSizeChanged(Control control, AvaloniaPropertyChangedEventArgs args)
        {
            if (args.NewValue is bool newBoolVal)
            {
                control.SizeChanged += (s, ev) =>
                {
                    SetObservedWidth(control, control.Bounds.Width);
                    SetObservedHeight(control, control.Bounds.Height);
                };
            }
        }

        // --- Observed Width ---
        public static readonly StyledProperty<double> ObservedWidthProperty =
            AvaloniaProperty.RegisterAttached<Control, double>(
                name: "ObservedWidth",
                ownerType: typeof(SizeObserverAP),
                defaultValue: 0.0,
                inherits: false);

        public static void SetObservedWidth(Control obj, double value) =>
            obj.SetValue(ObservedWidthProperty, value);

        public static double GetObservedWidth(Control obj) =>
            (double)obj.GetValue(ObservedWidthProperty);

        // --- Observed Height ---
        public static readonly StyledProperty<double> ObservedHeightProperty =
            AvaloniaProperty.RegisterAttached<Control, double>(
                name: "ObservedHeight",
                ownerType: typeof(SizeObserverAP),
                defaultValue: 0.0,
                inherits: false);

        public static void SetObservedHeight(Control obj, double value) =>
            obj.SetValue(ObservedHeightProperty, value);

        public static double GetObservedHeight(Control obj) =>
            (double)obj.GetValue(ObservedHeightProperty);
    }
}
