using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
