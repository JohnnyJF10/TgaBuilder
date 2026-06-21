using Wpf.Ui.Controls;

namespace TgaBuilderWpfUi.Services
{
    public interface ISnackbarOwner
    {
        public SnackbarPresenter SnackbarPresenter { get; }
    }
}
