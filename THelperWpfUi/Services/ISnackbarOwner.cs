using Wpf.Ui.Controls;

namespace THelperWpfUi.Services
{
    public interface ISnackbarOwner
    {
        public SnackbarPresenter SnackbarPresenter { get; }
    }
}
