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
using TgaBuilderLib.ViewModel;
using TgaBuilderWpfUi.Elements;
using TgaBuilderWpfUi.Services;
using Wpf.Ui.Controls;

namespace TgaBuilderWpfUi.View
{
    /// <summary>
    /// Interaktionslogik für BrickTransitionWindow.xaml
    /// </summary>
    public partial class BrickTransitionWindow : AsyncWindow, ISnackbarOwner
    {
        public BrickTransitionWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is BrickTransitionViewModel vm)
                vm.MarkFinishedCommand.Execute(null);
        }

        private void ColorPickerButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not BrickTransitionViewModel vm) return;
            var dialog = new ColorPickerWindow(vm.ColorSource, vm.ColorTarget);
            if (dialog.ShowDialog() == true)
            {
                vm.ColorSource = dialog.ResultColorSource;
                vm.ColorTarget = dialog.ResultColorTarget;
            }
        }

        public SnackbarPresenter SnackbarPresenter => MessageSnackbarPresenter;

        public SnackbarPresenter MessageSnackbarPresenter { get; private set; }
    }
}
