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
using TgaBuilderWpfUi.Elements;
using TgaBuilderWpfUi.Services;
using Wpf.Ui.Controls;

namespace TgaBuilderWpfUi.View
{
    /// <summary>
    /// Interaktionslogik für SmoothTransitionWindow.xaml
    /// </summary>
    public partial class SmoothTransitionWindow : AsyncWindow, ISnackbarOwner
    {
        public SmoothTransitionWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
        }

        public SnackbarPresenter SnackbarPresenter => MessageSnackbarPresenter;

        public SnackbarPresenter MessageSnackbarPresenter { get; private set; }
    }
}
