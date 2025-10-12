using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Navigation;
using TgaBuilderWpfUi.Elements;

namespace TgaBuilderWpfUi.View
{
    public partial class AboutWindow : AsyncWindow
    {
        public AboutWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
