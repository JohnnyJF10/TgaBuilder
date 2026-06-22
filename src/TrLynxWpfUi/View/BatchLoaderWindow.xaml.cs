using System.ComponentModel;
using TrLynxWpfUi.Elements;
using TrLynxWpfUi.Services;
using Wpf.Ui.Controls;

namespace TrLynxWpfUi.View
{
    public partial class BatchLoaderWindow : AsyncWindow, ISnackbarOwner
    {
        public BatchLoaderWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
        }

        public SnackbarPresenter SnackbarPresenter => MessageSnackbarPresenter;
    }
}
