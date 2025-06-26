using System.Windows;
using System.Windows.Input;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace TgaBuilderWpfUi.AttachedProperties
{
    public class WindowDraggerAP
    {
        public static readonly DependencyProperty CanDragWindowProperty =
            DependencyProperty.RegisterAttached("CanDragWindow", typeof(bool), typeof(WindowDraggerAP),
                new PropertyMetadata(false, OnCanDragWindowChanged));

        public static bool GetCanDragWindow(UIElement element)
        {
            return (bool)element.GetValue(CanDragWindowProperty);
        }

        public static void SetCanDragWindow(UIElement element, bool value)
        {
            element.SetValue(CanDragWindowProperty, value);
        }

        private static void OnCanDragWindowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if ((bool)e.NewValue)
                {
                    element.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
                    element.PreviewMouseMove += OnMouseMove;
                    element.PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
                }
                else
                {
                    element.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
                    element.PreviewMouseMove -= OnMouseMove;
                    element.PreviewMouseLeftButtonUp -= OnMouseLeftButtonUp;
                }
            }
        }

        private static Point _startPoint;
        private static bool _isDragging;
        private static Window? _window;

        private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is UIElement element)
            {
                _window = Window.GetWindow(element);
                if (_window != null)
                {
                    _startPoint = e.GetPosition(_window);
                    _isDragging = false;
                }
            }
        }

        private static void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_window == null || e.LeftButton != MouseButtonState.Pressed)
                return;

            var currentPosition = e.GetPosition(_window);
            var diff = currentPosition - _startPoint;

            if (!_isDragging && (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                _isDragging = true;
                _window.DragMove();
            }
        }

        private static void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
        }
    }
}
