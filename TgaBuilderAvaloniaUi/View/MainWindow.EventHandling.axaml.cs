using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
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

            if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
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

            e.Pointer.Capture(null);
            _modifier = MouseModifier.None;

            CurrentImage.InvalidateVisual();

            var hit = this.GetVisualsAt(e.GetPosition(this));

            if (hit.Count() > 0 && hit.ElementAt(0) != CurrentImage)
                PanelMouseAP.OnPointerExited(CurrentImage);
        }

        private void DestinationFormatSwitch_Click(object? sender, RoutedEventArgs e)
        {
            if (DestinationFormatSwitch.IsChecked == false)
                DestinationFormatSwitch.IsChecked = true;
        }
    }
}