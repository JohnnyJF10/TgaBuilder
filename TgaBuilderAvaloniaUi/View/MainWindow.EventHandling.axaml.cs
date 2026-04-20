using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using TgaBuilderAvaloniaUi.AttachedProperties;
using TgaBuilderLib.Enums;
using MouseAction = TgaBuilderLib.Enums.MouseAction;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class MainWindow
    {
        private static bool IsGridlessModifier(KeyModifiers modifiers)
            => modifiers.HasFlag(KeyModifiers.Alt);

        private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (CurrentImage == null) return;

            var updateKind = e.GetCurrentPoint(CurrentImage).Properties.PointerUpdateKind;
            bool isLeftButton = updateKind == PointerUpdateKind.LeftButtonPressed;

            // Ctrl+Left initiates ZoomBorder panning (matching WPF Ctrl-pan behaviour).
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && isLeftButton)
            {
                CurrentPanel = GetPanelFromImage(CurrentImage);
                _isCtrlPanning = true;
                _ctrlPanLastPoint = e.GetPosition(CurrentPanel);
                e.Pointer.Capture(CurrentPanel);
                return;
            }

            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                return;

            if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
                return;

            bool isDestination = CurrentImage.Name == "TargetImage";
            CurrentPanel = GetPanelFromImage(CurrentImage);

            e.Pointer.Capture(CurrentImage);

            var pos = e.GetPosition(CurrentImage);
            int x = (int)pos.X;
            int y = (int)pos.Y;

            _modifier = updateKind switch
            {
                PointerUpdateKind.LeftButtonPressed when IsGridlessModifier(e.KeyModifiers) => MouseModifier.AltLeft,
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
            _modifier = MouseModifier.Double;
        }

        private void Window_PointerMoved(object? sender, PointerEventArgs e)
        {
            // Handle Ctrl+Left panning: translate the ZoomBorder matrix.
            if (_isCtrlPanning && CurrentPanel != null)
            {
                var currentPoint = e.GetPosition(CurrentPanel);
                double dx = currentPoint.X - _ctrlPanLastPoint.X;
                double dy = currentPoint.Y - _ctrlPanLastPoint.Y;
                CurrentPanel.SetMatrix(
                    CurrentPanel.Matrix * Avalonia.Matrix.CreateTranslation(dx, dy));
                _ctrlPanLastPoint = currentPoint;
                return;
            }

            if (CurrentImage == null) return;

            var pos = e.GetPosition(CurrentImage);
            int x = (int)pos.X;
            int y = (int)pos.Y;
            bool isDestination = CurrentImage.Name == "TargetImage";

            if (CurrentImage == e.Pointer.Captured)
            {
                x = Math.Clamp(x, 0, (int)CurrentImage.Bounds.Width - 1);
                y = Math.Clamp(y, 0, (int)CurrentImage.Bounds.Height - 1);

                if (IsGridlessModifier(e.KeyModifiers))
                    _modifier = MouseModifier.AltLeft;
            }
            else
            {
                _modifier = IsGridlessModifier(e.KeyModifiers) ? MouseModifier.Alt : MouseModifier.None;
            }

            if (CurrentPanel != null && 
                !e.Properties.IsMiddleButtonPressed &&
                PanelMouseAP.GetScrollCommand(CurrentPanel) is ICommand scrollCommand &&
                PanelMouseAP.GetEndScrollCommand(CurrentPanel) is ICommand endScrollCommand)
            {
                var panelPos = e.GetPosition(CurrentPanel);
                var imagePos = e.GetPosition(CurrentImage);
                if (imagePos.X > 0 && imagePos.Y > 0 && imagePos.X < CurrentImage.Bounds.Width && imagePos.Y < CurrentImage.Bounds.Height)
                    scrollCommand.Execute((panelPos.X, panelPos.Y));
                else
                    endScrollCommand.Execute(null);
                
            }

            if (PanelMouseAP.GetPanelMouseCommand(this) is ICommand mousePanelCommand)
                mousePanelCommand.Execute((x, y, isDestination, MouseAction.Move, _modifier));
        }

        private void Window_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            // End Ctrl+Left panning.
            if (_isCtrlPanning)
            {
                _isCtrlPanning = false;
                e.Pointer.Capture(null);
                return;
            }

            if (CurrentImage == null)
                return;

            if (e.InitialPressMouseButton == MouseButton.Middle)
                return;

            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                _modifier = MouseModifier.Shift;

            var pos = e.GetPosition(CurrentImage);
            int x = (int)pos.X;
            int y = (int)pos.Y;
            bool isDestination = CurrentImage.Name == "TargetImage";

            if (PanelMouseAP.GetPanelMouseCommand(this) is ICommand mousePanelCommand)
                mousePanelCommand.Execute((x, y, isDestination, MouseAction.DragEnd, _modifier));

            CurrentImage.InvalidateVisual();

            var hit = this.GetVisualsAt(e.GetPosition(this));

            if (hit.Count() > 1 && !hit.Contains(CurrentImage))
                PanelMouseAP.OnPointerExited(CurrentImage);

            e.Pointer.Capture(null);
            _modifier = MouseModifier.None;
        }

        private void DestinationFormatSwitch_Click(object? sender, RoutedEventArgs e)
        {
            if (DestinationFormatSwitch.IsChecked == false)
                DestinationFormatSwitch.IsChecked = true;
        }

        private void Window_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (CurrentImage == null) return;

            if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift)) return;

            bool isDestination = CurrentImage.Name == "TargetImage";

            if (PanelMouseAP.GetWheelShiftCommand(this) is ICommand wheelShiftCommand)
            {
                wheelShiftCommand.Execute((isDestination, e.Delta.Y < 0));
                e.Handled = true;
            }
        }

        
    }
}