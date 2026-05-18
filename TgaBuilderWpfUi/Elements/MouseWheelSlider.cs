using System.Globalization;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace TgaBuilderWpfUi.Elements
{
    public class MouseWheelSlider : Slider
    {
        private readonly ToolTip _valueToolTip;
        private readonly DispatcherTimer _toolTipTimer;

        public MouseWheelSlider()
        {
            _valueToolTip = new ToolTip
            {
                PlacementTarget = this,
                StaysOpen = true,
            };

            _toolTipTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            _toolTipTimer.Tick += OnToolTipTimerTick;
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if (!IsFocused || !IsMouseOver || !IsEnabled || e.Delta == 0)
            {
                base.OnPreviewMouseWheel(e);
                return;
            }

            double change = SmallChange > 0 ? SmallChange : (Maximum - Minimum) / 10.0;
            if (change <= 0)
            {
                base.OnPreviewMouseWheel(e);
                return;
            }

            if (e.Delta > 0)
                Value = Math.Min(Maximum, Value + change);
            else
                Value = Math.Max(Minimum, Value - change);

            ShowValueToolTip();
            e.Handled = true;
        }

        private void ShowValueToolTip()
        {
            _valueToolTip.Content = Value.ToString($"F{AutoToolTipPrecision}", CultureInfo.CurrentCulture);
            _valueToolTip.Placement = AutoToolTipPlacement == AutoToolTipPlacement.TopLeft
                ? PlacementMode.Top
                : PlacementMode.Bottom;
            _valueToolTip.IsOpen = true;
            _toolTipTimer.Stop();
            _toolTipTimer.Start();
        }

        private void OnToolTipTimerTick(object? sender, EventArgs e)
        {
            _toolTipTimer.Stop();
            _valueToolTip.IsOpen = false;
        }
    }
}
