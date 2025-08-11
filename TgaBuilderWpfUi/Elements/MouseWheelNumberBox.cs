using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TgaBuilderWpfUi.Elements
{
    public class MouseWheelNumberBox : Wpf.Ui.Controls.NumberBox
    {
        public MouseWheelNumberBox()
        {
            PreviewMouseWheel += OnPreviewMouseWheel;
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!IsMouseOver) 
                return;

            if (e.Delta > 0)
                Value++;
            else
                Value--;

            e.Handled = true;
        }
    }
}
