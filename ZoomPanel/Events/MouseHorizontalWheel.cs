using System.Windows;
using System.Windows.Input;

namespace WPFZoomPanel.Events
{
    /// <summary>
    /// Provides events for handling mouse horizontal wheel scrollin or mouse
    /// wheel tilt
    /// </summary>
    public class MouseHorizontalWheel
    {
        #region Public Fields

        /// <summary>
        /// </summary>
        public static readonly RoutedEvent MouseHorizontalWheelEvent =
          EventManager.RegisterRoutedEvent("MouseHorizontalWheel", RoutingStrategy.Bubble, typeof(RoutedEventHandler),
            typeof(MouseHorizontalWheel));

        #endregion Public Fields

        #region Public Delegates

        public delegate void MouseHorizontalWheelEventHandler(object sender, MouseHorizontalWheelEventArgs e);

        #endregion Public Delegates

        #region Public Methods

        public static void AddMouseHorizontalWheelHandler(DependencyObject d, RoutedEventHandler handler)
        {
            if (d is UIElement uie)
            {
                uie.AddHandler(MouseHorizontalWheelEvent, handler);
            }
        }

        public static void RemoveMouseHorizontalWheelHandler(DependencyObject d, RoutedEventHandler handler)
        {
            if (d is UIElement uie)
                uie?.RemoveHandler(MouseHorizontalWheelEvent, handler);
        }

        #endregion Public Methods
    }

    /// <summary>
    /// Provides event arguments for <see cref="MouseHorizontalWheel" />
    /// </summary>
    public class MouseHorizontalWheelEventArgs : MouseEventArgs
    {
        #region Public Constructors + Destructors

        public MouseHorizontalWheelEventArgs(MouseDevice mouse, int timestamp, int horizontalDelta) : base(mouse, timestamp) => HorizontalDelta = horizontalDelta;

        #endregion Public Constructors + Destructors

        #region Public Properties

        public int HorizontalDelta { get; }

        #endregion Public Properties
    }
}
