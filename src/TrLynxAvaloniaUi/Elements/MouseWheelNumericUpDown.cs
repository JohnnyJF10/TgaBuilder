using Avalonia.Controls;
using Avalonia.Input;
using System;

namespace TrLynxAvaloniaUi.Elements
{
    public class MouseWheelNumericUpDown : NumericUpDown
    {
        protected override Type StyleKeyOverride => typeof(NumericUpDown);

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            if (!IsFocused || !IsPointerOver || !IsEnabled || !AllowSpin || e.Delta.Y == 0)
            {
                base.OnPointerWheelChanged(e);
                return;
            }

            decimal change = Increment > 0 ? Increment : 1m;
            decimal currentValue = Value ?? Minimum;
            decimal nextValue = decimal.Clamp(currentValue + (e.Delta.Y > 0 ? change : -change), Minimum, Maximum);

            if (nextValue == currentValue)
            {
                base.OnPointerWheelChanged(e);
                return;
            }

            Value = nextValue;
            e.Handled = true;
        }
    }
}
