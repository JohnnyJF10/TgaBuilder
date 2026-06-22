using Wpf.Ui.Controls;

namespace TrLynxWpfUi.Services
{
    public interface ISnackbarOwner
    {
        public SnackbarPresenter SnackbarPresenter { get; }
    }
}
