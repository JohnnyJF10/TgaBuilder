using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using System;
using System.Globalization;

namespace TgaBuilderAvaloniaUi.Elements
{
    public class MouseWheelSlider : Slider
    {
        private readonly DispatcherTimer _toolTipTimer;

        protected override Type StyleKeyOverride => typeof(Slider);

        public MouseWheelSlider()
        {
            _toolTipTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            _toolTipTimer.Tick += OnToolTipTimerTick;
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            if (!IsFocused || !IsPointerOver || !IsEnabled || e.Delta.Y == 0)
            {
                base.OnPointerWheelChanged(e);
                return;
            }

            double change = SmallChange > 0 ? SmallChange : (Maximum - Minimum) / 10.0;
            if (change <= 0)
            {
                base.OnPointerWheelChanged(e);
                return;
            }

            Value = Math.Clamp(Value + Math.Sign(e.Delta.Y) * change, Minimum, Maximum);

            ShowValueToolTip();
            e.Handled = true;
        }

        protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
        {
            ToolTip.SetIsOpen(this, false);
            _toolTipTimer.Stop();
            base.OnDetachedFromVisualTree(e);
        }

        private void ShowValueToolTip()
        {
            if (ToolTip.GetTip(this) is null)
                ToolTip.SetTip(this, Value.ToString(CultureInfo.CurrentCulture));

            ToolTip.SetIsOpen(this, true);
            _toolTipTimer.Stop();
            _toolTipTimer.Start();
        }

        private void OnToolTipTimerTick(object? sender, EventArgs e)
        {
            _toolTipTimer.Stop();
            ToolTip.SetIsOpen(this, false);
        }
    }
}
