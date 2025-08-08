using System.ComponentModel.Design;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using TgaBuilderLib.Enums;
using TgaBuilderWpfUi.AttachedProperties;
using FluentWindow = Wpf.Ui.Controls.FluentWindow;
using MouseAction = TgaBuilderLib.Enums.MouseAction;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace TgaBuilderWpfUi.View
{
    public partial class MainWindow : FluentWindow
    {
        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not MainWindow window) return;
            if (Keyboard.Modifiers == ModifierKeys.Control) return;
            if (CurrentImage == null) return;

            bool isDestination = CurrentImage.Name == "TargetImage";

            CurrentPanel = GetPanelFromImage(CurrentImage);

            CurrentImage.CaptureMouse();

            int x = (int)e.GetPosition(CurrentImage).X;
            int y = (int)e.GetPosition(CurrentImage).Y;

            _modifier = e.ChangedButton switch
            {
                MouseButton.Left when Keyboard.Modifiers == ModifierKeys.Alt => MouseModifier.AltLeft,
                MouseButton.Left when _modifier == MouseModifier.Double => MouseModifier.Double,
                MouseButton.Left => MouseModifier.Left,
                MouseButton.Right => MouseModifier.Right,
                _ => MouseModifier.None
            };

            if (PanelMouseAP.GetPanelMouseCommand(window) is ICommand mousePanelCommand)
                mousePanelCommand.Execute((x, y, isDestination, MouseAction.DragStart, _modifier));
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _modifier = MouseModifier.Double;
            Window_PreviewMouseDown(sender, e);
        }

        private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not MainWindow window) return;
            if (CurrentImage == null) return;

            int x = (int)e.GetPosition(CurrentImage).X;
            int y = (int)e.GetPosition(CurrentImage).Y;
            bool isDestination = CurrentImage.Name == "TargetImage";

            if (CurrentImage.IsMouseCaptured)
            {
                x = Math.Clamp(x, 0, (int)CurrentImage.ActualWidth - 1);
                y = Math.Clamp(y, 0, (int)CurrentImage.ActualHeight - 1);

                if (Keyboard.Modifiers == ModifierKeys.Alt)
                    _modifier = MouseModifier.AltLeft;
            }
            else
            {
                _modifier = Keyboard.Modifiers == ModifierKeys.Alt
                    ? MouseModifier.Alt : MouseModifier.None;
            }

            if (CurrentPanel != null &&
                PanelMouseAP.GetScrollCommand(CurrentPanel) is ICommand scrollCommand)
                scrollCommand.Execute(e.GetPosition(CurrentPanel));

            if (PanelMouseAP.GetPanelMouseCommand(window) is ICommand mousePanelCommand)
                mousePanelCommand.Execute((x, y, isDestination, MouseAction.Move, _modifier));
        }

        private void Window_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (CurrentImage is null || sender is not Window window) return;

            int x = ((int)e.GetPosition(CurrentImage).X);
            int y = ((int)e.GetPosition(CurrentImage).Y);
            bool isDestination = CurrentImage.Name == "TargetImage";

            if (PanelMouseAP.GetPanelMouseCommand(window) is ICommand mousePanelCommand)
                mousePanelCommand.Execute((x, y, isDestination, MouseAction.DragEnd, _modifier));

            CurrentImage.ReleaseMouseCapture();
            _modifier = MouseModifier.None;
        }

        private void MainWindow_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not MainWindow window) return;

            if (CurrentImage == null) return;

            if (Keyboard.Modifiers != ModifierKeys.Shift) return;

            bool isDestination = CurrentImage.Name == "TargetImage";

            if (PanelMouseAP.GetWheelShiftCommand(window) is ICommand wheelShiftCommand)
                wheelShiftCommand.Execute((isDestination, e.Delta < 0));
        }
    }
}