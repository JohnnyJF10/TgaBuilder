using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using TgaBuilderLib.ViewModel;
using TgaBuilderWpfUi.Elements;
using Image = System.Windows.Controls.Image;

namespace TgaBuilderWpfUi.View
{
    /// <summary>
    /// Interaktionslogik für BrickTransitionWindow.xaml
    /// </summary>
    public partial class BrickTransitionWindow : AsyncWindow
    {
        public BrickTransitionWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
        }

        public Cursor EyedropperCursor = new(Application
            .GetResourceStream(
            new Uri("Resources/eyedropper.cur", UriKind.Relative))
            .Stream);

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is BrickTransitionViewModel vm)
                vm.MarkFinishedCommand.Execute(null);
        }

        private void Image1_MouseMove(object sender, MouseEventArgs e) 
            => DoEyedropperMouseMove(Image1, e, 1);

        private void Image2_MouseMove(object sender, MouseEventArgs e) 
            => DoEyedropperMouseMove(Image2, e, 2);

        private void DoEyedropperMouseMove(Image image, MouseEventArgs e, int imageNum)
        {
            if (DataContext is BrickTransitionViewModel vm && vm.IsEyedropperMode)
            {
                var position = e.GetPosition(image);
                vm.MouseOverCommand.Execute((X: (int)position.X, Y: (int)position.Y, imageNum));
            }
        }

        private void Image2_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DataContext is BrickTransitionViewModel vm && vm.IsEyedropperMode)
            {
                Mouse.OverrideCursor = EyedropperCursor;
            }
        }

        private void Image2_MouseLeave(object sender, MouseEventArgs e)
        {
            if (DataContext is BrickTransitionViewModel vm && vm.IsEyedropperMode)
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void Image2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is BrickTransitionViewModel vm && vm.IsEyedropperMode)
            {
                vm.IsEyedropperMode = false;
                Mouse.OverrideCursor = null;
            }
        }

        private void Image1_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DataContext is BrickTransitionViewModel vm && vm.IsEyedropperMode)
            {
                Mouse.OverrideCursor = EyedropperCursor;
            }
        }

        private void Image1_MouseLeave(object sender, MouseEventArgs e)
        {
            if (DataContext is BrickTransitionViewModel vm && vm.IsEyedropperMode)
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void Image1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is BrickTransitionViewModel vm && vm.IsEyedropperMode)
            {
                vm.IsEyedropperMode = false;
                Mouse.OverrideCursor = null;
            }
        }
    }
}
