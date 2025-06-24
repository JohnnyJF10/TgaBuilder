using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui.Controls;

namespace THelperWpfUi.Services
{
    public interface ISnackbarOwner
    {
        public SnackbarPresenter SnackbarPresenter { get; }
    }
}
