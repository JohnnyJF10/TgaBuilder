using System.ComponentModel;
using THelperLib.Abstraction;
using THelperWpfUi.Services;
using Wpf.Ui.Controls;

namespace THelperWpfUi.View
{
    public partial class BatchLoaderWindow : FluentWindow, IView, ISnackbarOwner
    {
        public BatchLoaderWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        public SnackbarPresenter SnackbarPresenter => MessageSnackbarPresenter;
    }
}
