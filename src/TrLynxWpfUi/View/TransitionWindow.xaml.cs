using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using TrLynxLib.ViewModel;
using TrLynxWpfUi.Elements;
using Image = System.Windows.Controls.Image;

namespace TrLynxWpfUi.View
{
    /// <summary>
    /// Interaction logic for TransitionWindow.xaml
    /// </summary>
    public partial class TransitionWindow : AsyncWindow
    {
        public TransitionWindow(INotifyPropertyChanged viewModel)
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

            if (DataContext is TransitionViewModel vm)
                vm.MarkFinishedCommand.Execute(null);
        }

        private void Image1_MouseMove(object sender, MouseEventArgs e)
            => DoEyedropperMouseMove(Image1, e, 1);

        private void Image2_MouseMove(object sender, MouseEventArgs e)
            => DoEyedropperMouseMove(Image2, e, 2);

        private void DoEyedropperMouseMove(Image image, MouseEventArgs e, int imageNum)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionInVM;

            if (!tpvm.IsEyedropperMode && !tpvm.IsShadowEyedropperMode)
                return;

            var position = e.GetPosition(image);
            tpvm.MouseOverCommand.Execute((X: (int)position.X, Y: (int)position.Y, imageNum));
        }

        private void Image1_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionInVM;

            if (!tpvm.IsEyedropperMode && !tpvm.IsShadowEyedropperMode)
                return;

            Mouse.OverrideCursor = EyedropperCursor;
        }

        private void Image1_MouseLeave(object sender, MouseEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionInVM;

            if (!tpvm.IsEyedropperMode && !tpvm.IsShadowEyedropperMode)
                return;

            Mouse.OverrideCursor = null;
        }

        private void Image1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionInVM;

            if (!tpvm.IsEyedropperMode && !tpvm.IsShadowEyedropperMode)
                return;

            tpvm.IsEyedropperMode = false;
            tpvm.IsShadowEyedropperMode = false;
            Mouse.OverrideCursor = null;
        }

        private void Image2_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionInVM;

            if (!tpvm.IsEyedropperMode && !tpvm.IsShadowEyedropperMode)
                return;

            Mouse.OverrideCursor = EyedropperCursor;
        }

        private void Image2_MouseLeave(object sender, MouseEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionInVM;

            if (!tpvm.IsEyedropperMode && !tpvm.IsShadowEyedropperMode)
                return;

            Mouse.OverrideCursor = null;
        }

        private void Image2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionInVM;

            if (!tpvm.IsEyedropperMode && !tpvm.IsShadowEyedropperMode)
                return;

            tpvm.IsEyedropperMode = false;
            tpvm.IsShadowEyedropperMode = false;
            Mouse.OverrideCursor = null;
        }

        private void ResultImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionOutVM;

            if (!tpvm.IsAnyManualMode)
                return;

            var position = e.GetPosition(ResultImage);
            int x = (int)position.X;
            int y = (int)position.Y;

            tpvm.RequestLabelIndicatorCommand.Execute((X: x, Y: y));

            if (e.LeftButton == MouseButtonState.Pressed)
                tpvm.ManualPointerDragCommand.Execute((X: x, Y: y));
            else if (tpvm.IsTileRotateMode && ResultImage.IsMouseCaptured)
                ResultImage.ReleaseMouseCapture();
        }

        private void ResultImage_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionOutVM;

            if (!tpvm.IsAnyManualMode)
                return;

            tpvm.IsIndicatorMapVisible = true;
        }

        private void ResultImage_MouseLeave(object sender, MouseEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionOutVM;

            if (!tpvm.IsAnyManualMode)
                return;

            tpvm.IsIndicatorMapVisible = false;

            if (tpvm.IsTileMoveRotateMode || tpvm.IsTileRotateMode)
            {
                if (tpvm.IsTileRotateMode && ResultImage.IsMouseCaptured)
                    return;

                tpvm.EndManipulationCommand.Execute(null);
            }
        }

        private void ResultImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionOutVM;

            if (!tpvm.IsAnyManualMode)
                return;

            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            var position = e.GetPosition(ResultImage);
            tpvm.ManualPointerDownCommand.Execute((X: (int)position.X, Y: (int)position.Y));

            if (tpvm.IsTileRotateMode)
                ResultImage.CaptureMouse();
        }

        private void ResultImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionOutVM;

            if (tpvm.IsTileRotateMode && ResultImage.IsMouseCaptured)
                ResultImage.ReleaseMouseCapture();
        }

        private void ResultImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionOutVM;

            if (!tpvm.IsWheelRotationMode)
                return;

            int notches = e.Delta > 0 ? 1 : (e.Delta < 0 ? -1 : 0);
            if (notches == 0)
                return;

            var position = e.GetPosition(ResultImage);
            tpvm.RotateActiveTileCommand.Execute((X: (int)position.X, Y: (int)position.Y, Notches: notches));

            // Prevent the wheel from also scrolling/zooming the surrounding UI while rotating.
            e.Handled = true;
        }
    }
}
