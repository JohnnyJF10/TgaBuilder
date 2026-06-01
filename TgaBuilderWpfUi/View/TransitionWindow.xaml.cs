using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using TgaBuilderLib.ViewModel;
using TgaBuilderWpfUi.Elements;
using Image = System.Windows.Controls.Image;

namespace TgaBuilderWpfUi.View
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

            var tpvm = tvm.TransitionsPresentersVM;

            if (!tpvm.IsEyedropperMode && !tpvm.IsShadowEyedropperMode)
                return;

            var position = e.GetPosition(image);
            tpvm.MouseOverCommand.Execute((X: (int)position.X, Y: (int)position.Y, imageNum));
        }

        private void Image1_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionsPresentersVM;

            if (!tpvm.IsEyedropperMode && !tpvm.IsShadowEyedropperMode)
                return;

            Mouse.OverrideCursor = EyedropperCursor;
        }

        private void Image1_MouseLeave(object sender, MouseEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionsPresentersVM;

            if (!tpvm.IsEyedropperMode && !tpvm.IsShadowEyedropperMode)
                return;

            Mouse.OverrideCursor = null;
        }

        private void Image1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionsPresentersVM;

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

            var tpvm = tvm.TransitionsPresentersVM;

            if (!tpvm.IsEyedropperMode && !tpvm.IsShadowEyedropperMode)
                return;

            Mouse.OverrideCursor = EyedropperCursor;
        }

        private void Image2_MouseLeave(object sender, MouseEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionsPresentersVM;

            if (!tpvm.IsEyedropperMode && !tpvm.IsShadowEyedropperMode)
                return;

            Mouse.OverrideCursor = null;
        }

        private void Image2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionsPresentersVM;

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

            var tpvm = tvm.TransitionsPresentersVM;

            if (!tpvm.IsExplicitTileVisibilityDrawMode && !tpvm.IsExplicitTileVisibilityEraseMode)
                return;

            if (tpvm.RequestLabelIndicatorCommand is ICommand requestLabelIndicatorCommand)
                requestLabelIndicatorCommand.Execute((X: (int)e.GetPosition(ResultImage).X, Y: (int)e.GetPosition(ResultImage).Y));

            if (e.LeftButton == MouseButtonState.Pressed && tpvm.SetExplicitTileVisibilityCommand is ICommand setExplicitTileVisibilityCommand)
                setExplicitTileVisibilityCommand.Execute((X: (int)e.GetPosition(ResultImage).X, Y: (int)e.GetPosition(ResultImage).Y));
        }

        private void ResultImage_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionsPresentersVM;

            if (!tpvm.IsExplicitTileVisibilityDrawMode && !tpvm.IsExplicitTileVisibilityEraseMode)
                return;

            tpvm.IsIndicatorMapVisible = true;
        }

        private void ResultImage_MouseLeave(object sender, MouseEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionsPresentersVM;

            if (!tpvm.IsExplicitTileVisibilityDrawMode && !tpvm.IsExplicitTileVisibilityEraseMode)
                return;

            tpvm.IsIndicatorMapVisible = false;
        }

        private void ResultImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not TransitionViewModel tvm)
                return;

            var tpvm = tvm.TransitionsPresentersVM;

            if (!tpvm.IsExplicitTileVisibilityDrawMode && !tpvm.IsExplicitTileVisibilityEraseMode)
                return;

            if (e.LeftButton == MouseButtonState.Pressed && tpvm.SetExplicitTileVisibilityCommand is ICommand setExplicitTileVisibilityCommand)
                setExplicitTileVisibilityCommand.Execute((X: (int)e.GetPosition(ResultImage).X, Y: (int)e.GetPosition(ResultImage).Y));
        }
    }
}
