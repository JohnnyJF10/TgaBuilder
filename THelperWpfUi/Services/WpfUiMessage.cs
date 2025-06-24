using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui.Controls;

namespace THelperWpfUi.Services
{
    internal struct WpfUiMessage
    {
        internal string Title { get; set; }
        internal string Message { get; set; }
        internal ControlAppearance Appearance { get; set; }
        internal SymbolIcon Icon { get; set; }
        internal TimeSpan timeout { get; set; }

        internal WpfUiMessage(
            string title, 
            string message, 
            ControlAppearance appearance, 
            SymbolIcon icon, 
            TimeSpan timeoutTimeSpan)
        {
            Title = title;
            Message = message;
            Appearance = appearance;
            Icon = icon;
            timeout = timeoutTimeSpan;
        }
    }
}
