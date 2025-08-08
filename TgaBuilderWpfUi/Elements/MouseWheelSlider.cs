using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TgaBuilderWpfUi.Elements
{
    public class MouseWheelSlider : Slider
    {
        public MouseWheelSlider()
        {
            PreviewMouseWheel += OnPreviewMouseWheel;
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!IsMouseOver) return;

            double change = SmallChange > 0 ? SmallChange : (Maximum - Minimum) / 10.0;

            if (e.Delta > 0)
                Value = Math.Min(Maximum, Value + change);
            else
                Value = Math.Max(Minimum, Value - change);

            e.Handled = true;
        }
    }
}
