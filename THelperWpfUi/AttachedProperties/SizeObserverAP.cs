using System.Windows;

namespace THelperWpfUi.AttachedProperties
{
    public static class SizeObserverAP
    {
        // --- Activate the observation ---
        public static readonly DependencyProperty ObserveSizeProperty =
            DependencyProperty.RegisterAttached(
                "ObserveSize",
                typeof(bool),
                typeof(SizeObserverAP),
                new PropertyMetadata(false, OnObserveSizeChanged));

        public static void SetObserveSize(DependencyObject obj, bool value) =>
            obj.SetValue(ObserveSizeProperty, value);

        public static bool GetObserveSize(DependencyObject obj) =>
            (bool)obj.GetValue(ObserveSizeProperty);

        private static void OnObserveSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element && (bool)e.NewValue)
            {
                element.SizeChanged += (s, ev) =>
                {
                    SetObservedWidth(element, element.ActualWidth);
                    SetObservedHeight(element, element.ActualHeight);
                };
            }
        }

        // --- Observed Width ---
        public static readonly DependencyProperty ObservedWidthProperty =
            DependencyProperty.RegisterAttached(
                "ObservedWidth",
                typeof(double),
                typeof(SizeObserverAP),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static void SetObservedWidth(DependencyObject obj, double value) =>
            obj.SetValue(ObservedWidthProperty, value);

        public static double GetObservedWidth(DependencyObject obj) =>
            (double)obj.GetValue(ObservedWidthProperty);

        // --- Observed Height ---
        public static readonly DependencyProperty ObservedHeightProperty =
            DependencyProperty.RegisterAttached(
                "ObservedHeight",
                typeof(double),
                typeof(SizeObserverAP),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static void SetObservedHeight(DependencyObject obj, double value) =>
            obj.SetValue(ObservedHeightProperty, value);

        public static double GetObservedHeight(DependencyObject obj) =>
            (double)obj.GetValue(ObservedHeightProperty);
    }
}
