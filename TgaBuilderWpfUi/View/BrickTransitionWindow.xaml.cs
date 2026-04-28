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
        private double _collapsedWindowHeight;
        private double _collapsedWindowMinHeight;

        public BrickTransitionWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
            _collapsedWindowHeight = Height;
            _collapsedWindowMinHeight = MinHeight;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is BrickTransitionViewModel vm)
                vm.MarkFinishedCommand.Execute(null);
        }

        private void OptionsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            _collapsedWindowHeight = ActualHeight;
            _collapsedWindowMinHeight = MinHeight;

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, (Action)(() =>
            {
                if (sender is Expander { Content: FrameworkElement content } && content.ActualHeight > 0)
                {
                    double extra = content.ActualHeight;
                    MinHeight = _collapsedWindowMinHeight + extra;
                    Height = _collapsedWindowHeight + extra;
                }
            }));
        }

        private void OptionsExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            Height = _collapsedWindowHeight;
            MinHeight = _collapsedWindowMinHeight;
        }

        public SnackbarPresenter SnackbarPresenter => MessageSnackbarPresenter;

        public SnackbarPresenter MessageSnackbarPresenter { get; private set; }
    }
}

