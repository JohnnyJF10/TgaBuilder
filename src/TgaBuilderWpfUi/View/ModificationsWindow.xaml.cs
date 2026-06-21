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
            if (DataContext is not ModificationsViewModel mvm || sender is not Image image)
                return;

            var mivm = mvm.ModificationInVM;

            if (!mivm.IsEyedropperMode)
                return;

            var position = e.GetPosition(image);
            mivm.MouseOverCommand.Execute((X: (int)position.X, Y: (int)position.Y));
        }

        private void InputImage_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DataContext is not ModificationsViewModel mvm || sender is not Image)
                return;

            if (!mvm.ModificationInVM.IsEyedropperMode)
                return;

            Mouse.OverrideCursor = _eyedropperCursor;
        }

        private void InputImage_MouseLeave(object sender, MouseEventArgs e)
        {
            if (DataContext is not ModificationsViewModel mvm || sender is not Image)
                return;

            if (!mvm.ModificationInVM.IsEyedropperMode)
                return;

            Mouse.OverrideCursor = null;
        }

        private void InputImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not ModificationsViewModel mvm || sender is not Image)
                return;

            var mivm = mvm.ModificationInVM;

            if (!mivm.IsEyedropperMode)
                return;

            mivm.IsEyedropperMode = false;
            Mouse.OverrideCursor = null;
        }
    }
}
