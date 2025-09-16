using System.ComponentModel;
using System.Diagnostics;
using TgaBuilderAvaloniaUi.Elements;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class AboutWindow : AsyncWindow
    {
        public AboutWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
        }
    }
}
