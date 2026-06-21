using System.ComponentModel;
using TgaBuilderWpfUi.Elements;
using TgaBuilderWpfUi.Services;
using Wpf.Ui.Controls;

namespace TgaBuilderWpfUi.View
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
