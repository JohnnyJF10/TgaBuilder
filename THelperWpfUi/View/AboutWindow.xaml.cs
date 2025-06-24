using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Navigation;
using THelperLib.Abstraction;
using Wpf.Ui.Controls;

namespace THelperWpfUi.View
{
    public partial class AboutWindow : FluentWindow, IView
    {
        public AboutWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
