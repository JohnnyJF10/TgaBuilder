using System.ComponentModel;
using System.Windows;
using TgaBuilderLib.ViewModel;

namespace TgaBuilderWpfUi.View
{
    /// <summary>
    /// Interaction logic for ModificationsWindow.xaml
    /// </summary>
    public partial class ModificationsWindow : Elements.AsyncWindow
    {
        public ModificationsWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is ModificationsViewModel vm)
                vm.MarkFinished();
        }
    }
}
