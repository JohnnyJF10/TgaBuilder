using System.ComponentModel;
using System.Windows;
using TgaBuilderLib.ViewModel;

namespace TgaBuilderWpfUi.View
{
    /// <summary>
    /// Interaction logic for SingleTextureModificationWindow.xaml
    /// </summary>
    public partial class SingleTextureModificationWindow : Elements.AsyncWindow
    {
        public SingleTextureModificationWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is SingleTextureModificationViewModel vm)
                vm.MarkFinished();
        }
    }
}
