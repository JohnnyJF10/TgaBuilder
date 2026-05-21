using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using TgaBuilderLib.ViewModel;
using Image = System.Windows.Controls.Image;

namespace TgaBuilderWpfUi.View
{
    /// <summary>
    /// Interaction logic for ModificationsWindow.xaml
    /// </summary>
    public partial class ModificationsWindow : Elements.AsyncWindow
    {
        private readonly Cursor _eyedropperCursor = new(Application
            .GetResourceStream(
            new Uri("Resources/eyedropper.cur", UriKind.Relative))
            .Stream);

        public ModificationsWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is ModificationsViewModel vm)
                vm.MarkFinished();
        }

        private void InputImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (DataContext is ModificationsViewModel vm && vm.IsColorOverlayEyedropperMode)
            {
                var position = e.GetPosition((Image)sender);
                vm.MouseOverInputCommand.Execute((X: (int)position.X, Y: (int)position.Y));
            }
        }

        private void InputImage_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DataContext is ModificationsViewModel vm && vm.IsColorOverlayEyedropperMode)
                Mouse.OverrideCursor = _eyedropperCursor;
        }

        private void InputImage_MouseLeave(object sender, MouseEventArgs e)
        {
            if (DataContext is ModificationsViewModel vm && vm.IsColorOverlayEyedropperMode)
                Mouse.OverrideCursor = null;
        }

        private void InputImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ModificationsViewModel vm && vm.IsColorOverlayEyedropperMode)
            {
                vm.IsColorOverlayEyedropperMode = false;
                Mouse.OverrideCursor = null;
            }
        }
    }
}
