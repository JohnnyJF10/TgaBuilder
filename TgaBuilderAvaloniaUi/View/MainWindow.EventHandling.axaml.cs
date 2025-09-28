using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Windows.Input;
using TgaBuilderAvaloniaUi.AttachedProperties;
using TgaBuilderLib.Enums;
using MouseAction = TgaBuilderLib.Enums.MouseAction;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class MainWindow
    {
        private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (CurrentImage == null) return;

            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                return;

            bool isDestination = CurrentImage.Name == "TargetImage";
            CurrentPanel = GetPanelFromImage(CurrentImage);

            e.Pointer.Capture(CurrentImage);

            var pos = e.GetPosition(CurrentImage);
            int x = (int)pos.X;
            int y = (int)pos.Y;

            _modifier = e.GetCurrentPoint(CurrentImage).Properties.PointerUpdateKind switch
            {
                PointerUpdateKind.LeftButtonPressed when e.KeyModifiers.HasFlag(KeyModifiers.Alt) => MouseModifier.AltLeft,
                PointerUpdateKind.LeftButtonPressed when _modifier == MouseModifier.Double => MouseModifier.Double,
                PointerUpdateKind.LeftButtonPressed => MouseModifier.Left,
                PointerUpdateKind.RightButtonPressed => MouseModifier.Right,
                _ => MouseModifier.None
            };

            if (PanelMouseAP.GetPanelMouseCommand(this) is ICommand mousePanelCommand)
                mousePanelCommand.Execute((x, y, isDestination, MouseAction.DragStart, _modifier));
        }

        private void Window_DoubleTapped(object? sender, RoutedEventArgs e)
        {
            //_modifier = MouseModifier.Double;
            //if (e is PointerReleasedEventArgs pe)
            //    Window_PointerPressed(sender, new PointerPressedEventArgs(
            //        pe.InputDevice, pe.Timestamp, pe.GetCurrentPoint(pe.Source)));
        }

        private void Window_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (CurrentImage == null) return;

            var pos = e.GetPosition(CurrentImage);
            int x = (int)pos.X;
            int y = (int)pos.Y;
            bool isDestination = CurrentImage.Name == "TargetImage";

            if (CurrentImage == e.Pointer.Captured)
            {
                x = Math.Clamp(x, 0, (int)CurrentImage.Bounds.Width - 1);
                y = Math.Clamp(y, 0, (int)CurrentImage.Bounds.Height - 1);

                if (e.KeyModifiers.HasFlag(KeyModifiers.Alt))
                    _modifier = MouseModifier.AltLeft;
            }
            else
            {
                _modifier = e.KeyModifiers.HasFlag(KeyModifiers.Alt) ? MouseModifier.Alt : MouseModifier.None;
            }

            if (CurrentPanel != null && PanelMouseAP.GetScrollCommand(CurrentPanel) is ICommand scrollCommand)
            {
                var panelPos = e.GetPosition(CurrentPanel);
                scrollCommand.Execute((panelPos.X, panelPos.Y));
            }

            if (PanelMouseAP.GetPanelMouseCommand(this) is ICommand mousePanelCommand)
                mousePanelCommand.Execute((x, y, isDestination, MouseAction.Move, _modifier));
        }

        private void Window_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (CurrentImage == null) return;

            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                _modifier = MouseModifier.Shift;

            if (e.GetCurrentPoint(CurrentImage).Properties.PointerUpdateKind == PointerUpdateKind.MiddleButtonReleased)
                _modifier = MouseModifier.Middle;

            var pos = e.GetPosition(CurrentImage);
            int x = (int)pos.X;
            int y = (int)pos.Y;
            bool isDestination = CurrentImage.Name == "TargetImage";

            if (PanelMouseAP.GetPanelMouseCommand(this) is ICommand mousePanelCommand)
                mousePanelCommand.Execute((x, y, isDestination, MouseAction.DragEnd, _modifier));

            e.Pointer.Capture(null);
            _modifier = MouseModifier.None;

            CurrentImage.InvalidateVisual();
        }

        private void MainWindow_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (CurrentImage == null) return;
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                bool isDestination = CurrentImage.Name == "TargetImage";

                if (PanelMouseAP.GetWheelShiftCommand(this) is ICommand wheelShiftCommand)
                    wheelShiftCommand.Execute((isDestination, e.Delta.Y < 0));
            }
            else if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                CurrentPanel = GetPanelFromImage(CurrentImage);
                CurrentPanel.SetMatrix(CurrentPanel.Matrix * Matrix.CreateTranslation(0, 100 * e.Delta.Y));
            }
        }

        private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                var sourceMatrix = SourcePanel.Matrix;
                var targetMatrix = TargetPanel.Matrix;

                SourcePanel.EnableZoom = true;
                SourcePanel.EnablePan = true;
                TargetPanel.EnableZoom = true;
                TargetPanel.EnablePan = true;

                SourcePanel.SetMatrix(sourceMatrix);
                TargetPanel.SetMatrix(targetMatrix);
            }
        }

        private void MainWindow_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                var sourceMatrix = SourcePanel.Matrix;
                var targetMatrix = TargetPanel.Matrix;

                SourcePanel.EnableZoom = false;
                SourcePanel.EnablePan = false;
                TargetPanel.EnableZoom = false;
                TargetPanel.EnablePan = false;

                SourcePanel.SetMatrix(sourceMatrix);
                TargetPanel.SetMatrix(targetMatrix);
            }

        }
    }
}