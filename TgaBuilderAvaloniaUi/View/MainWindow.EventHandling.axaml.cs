using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using System.Windows.Input;
using TgaBuilderAvaloniaUi.AttachedProperties;
using TgaBuilderLib.Enums;
using MouseAction = TgaBuilderLib.Enums.MouseAction;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class MainWindow
    {

        private Avalonia.Point _lastPanPosition;
        private Avalonia.Point _lastPointerPosition;

        private static bool IsGridlessModifier(KeyModifiers modifiers)
            => modifiers.HasFlag(KeyModifiers.Alt);

        private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (CurrentImage == null) return;

            var updateKind = e.GetCurrentPoint(CurrentImage).Properties.PointerUpdateKind;
            bool isLeftButton = updateKind == PointerUpdateKind.LeftButtonPressed;

            if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
                return;

            bool isDestination = IsElementFromDestinationPanel(CurrentImage);
            CurrentPanel = GetPanelFromImage(CurrentImage);

            e.Pointer.Capture(CurrentImage);


            _lastPanPosition = e.GetPosition(CurrentPanel);
            if (e.Properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                this.Cursor = new Cursor(StandardCursorType.SizeAll);
                return;
            }


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

            _lastPointerPosition = e.GetPosition(CurrentImage);
        }

        private void Window_DoubleTapped(object? sender, RoutedEventArgs e)
        {
            _modifier = MouseModifier.Double;
        }

        private void Window_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (CurrentImage == null) return;

            var pos = e.GetPosition(CurrentImage);
            int x = (int)pos.X;
            int y = (int)pos.Y;
            bool isDestination = IsElementFromDestinationPanel(CurrentImage);

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
            e.Properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                var posNewPanel = e.GetPosition(CurrentPanel);
                var deltaPos = posNewPanel - _lastPanPosition;
                CurrentPanel.EnablePan = false;
                CurrentPanel.PanDelta(
                    dx: deltaPos.X, 
                    dy: deltaPos.Y,
                    skipTransitions: true);

                _lastPanPosition = posNewPanel;
                return;
            }

            if (CurrentPanel != null && 
                !(e.Properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Control)) &&
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

            _lastPointerPosition = e.GetPosition(CurrentImage);
        }

        private void Window_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (CurrentImage == null) return;

            if (e.InitialPressMouseButton == MouseButton.Middle) return;

            if (CurrentPanel is not null)
                CurrentPanel.EnablePan = false;

            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                _modifier = MouseModifier.Shift;

            var pos = e.GetPosition(CurrentImage);
            int x = (int)pos.X;
            int y = (int)pos.Y;
            bool isDestination = IsElementFromDestinationPanel(CurrentImage);

            if (PanelMouseAP.GetPanelMouseCommand(this) is ICommand mousePanelCommand)
                mousePanelCommand.Execute((x, y, isDestination, MouseAction.DragEnd, _modifier));

            CurrentImage.InvalidateVisual();

            var hit = this.GetVisualsAt(e.GetPosition(this));

            if (hit.Count() > 1 && !hit.Contains(CurrentImage))
                PanelMouseAP.OnPointerExited(CurrentImage);

            e.Pointer.Capture(null);
            _modifier = MouseModifier.None;
            this.Cursor = new Cursor(StandardCursorType.Arrow);
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

            bool isDestination = IsElementFromDestinationPanel(CurrentImage);
            if (PanelMouseAP.GetWheelShiftCommand(this) is ICommand wheelShiftCommand)
            {
                wheelShiftCommand.Execute((isDestination, e.Delta.Y < 0));
                e.Handled = true;
            }
        }

        
    }
}