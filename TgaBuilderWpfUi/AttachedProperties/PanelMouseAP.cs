﻿using System.Windows;
using System.Windows.Input;
using TgaBuilderWpfUi.View;
using Image = System.Windows.Controls.Image;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace TgaBuilderWpfUi.AttachedProperties
{
    public static class PanelMouseAP
    {
        public static readonly DependencyProperty PanelMouseCommandProperty =
            DependencyProperty.RegisterAttached(
                "PanelMouseCommand",
                typeof(ICommand),
                typeof(PanelMouseAP),
                new PropertyMetadata(null));

        public static void SetPanelMouseCommand(UIElement element, ICommand value)
        {
            element.SetValue(PanelMouseCommandProperty, value);
        }

        public static ICommand GetPanelMouseCommand(UIElement element)
        {
            return (ICommand)element.GetValue(PanelMouseCommandProperty);
        }

        public static readonly DependencyProperty CheckMouseEnterLeaveProperty =
            DependencyProperty.RegisterAttached(
                "CheckMouseEnterLeave",
                typeof(bool),
                typeof(PanelMouseAP),
                new PropertyMetadata(false, OnCheckMouseEnterLeavePropertyChanged));

        public static void SetCheckMouseEnterLeave(UIElement element, bool value)
        {
            element.SetValue(CheckMouseEnterLeaveProperty, value);
        }

        public static bool GetCheckMouseEnterLeave(UIElement element)
        {
            return (bool)element.GetValue(CheckMouseEnterLeaveProperty);
        }

        private static void OnCheckMouseEnterLeavePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Image image)
            {
                if ((bool)e.NewValue)
                {
                    image.MouseEnter += Image_MouseEnter;
                    image.MouseLeave += Image_MouseLeave;
                }
                else
                {
                    image.MouseEnter -= Image_MouseEnter;
                    image.MouseLeave -= Image_MouseLeave;
                }
            }
        }

        private static void Image_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Image image && Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.CurrentImage = image;
                mainWindow.SetPanelFromImage(image);

                bool isDestination = GetIsTargetPanel(image);
                ICommand EnterPanelCommand = GetEnterPanelCommand(mainWindow);
                if (EnterPanelCommand is not null && EnterPanelCommand.CanExecute(isDestination))
                    EnterPanelCommand.Execute(isDestination);
            }
        }

        private static void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Image image && Application.Current.MainWindow is MainWindow mainWindow)
            {
                bool isDestination = GetIsTargetPanel(image);

                mainWindow.CurrentImage = null;
                mainWindow.CurrentPanel = null;
                ICommand LeavePanelCommand = GetLeavePanelCommand(mainWindow);
                if (LeavePanelCommand is not null && LeavePanelCommand.CanExecute(isDestination))
                    LeavePanelCommand.Execute(isDestination);

                Mouse.OverrideCursor = null;
            }
        }

        public static readonly DependencyProperty LeavePanelCommandProperty =
            DependencyProperty.RegisterAttached(
                "LeavePanelCommand",
                typeof(ICommand),
                typeof(PanelMouseAP),
                new PropertyMetadata(null));
        public static void SetLeavePanelCommand(UIElement element, ICommand value)
        {
            element.SetValue(LeavePanelCommandProperty, value);
        }
        public static ICommand GetLeavePanelCommand(UIElement element)
        {
            return (ICommand)element.GetValue(LeavePanelCommandProperty);
        }

        public static readonly DependencyProperty EnterPanelCommandProperty =
            DependencyProperty.RegisterAttached(
                "EnterPanelCommand",
                typeof(ICommand),
                typeof(PanelMouseAP),
                new PropertyMetadata(null));
        public static void SetEnterPanelCommand(UIElement element, ICommand value)
        {
            element.SetValue(EnterPanelCommandProperty, value);
        }
        public static ICommand GetEnterPanelCommand(UIElement element)
        {
            return (ICommand)element.GetValue(EnterPanelCommandProperty);
        }

        public static readonly DependencyProperty IsTargetPanelProperty =
            DependencyProperty.RegisterAttached(
                "IsTargetPanel",
                typeof(bool),
                typeof(PanelMouseAP),
                new PropertyMetadata(null));
        public static void SetIsTargetPanel(UIElement element, bool value)
        {
            element.SetValue(IsTargetPanelProperty, value);
        }
        public static bool GetIsTargetPanel(UIElement element)
        {
            return (bool)element.GetValue(IsTargetPanelProperty);
        }

        public static readonly DependencyProperty ScrollCommandProperty =
            DependencyProperty.RegisterAttached(
                "ScrollCommand",
                typeof(ICommand),
                typeof(PanelMouseAP),
                new PropertyMetadata(null));

        public static void SetScrollCommand(UIElement element, ICommand value)
        {
            element.SetValue(ScrollCommandProperty, value);
        }

        public static ICommand GetScrollCommand(UIElement element)
        {
            return (ICommand)element.GetValue(ScrollCommandProperty);
        }

        public static readonly DependencyProperty WheelShiftCommandProperty =
            DependencyProperty.RegisterAttached(
                "WheelShiftCommand",
                typeof(ICommand),
                typeof(PanelMouseAP),
                new PropertyMetadata(null));

        public static void SetWheelShiftCommand(UIElement element, ICommand value)
        {
            element.SetValue(WheelShiftCommandProperty, value);
        }

        public static ICommand GetWheelShiftCommand(UIElement element)
        {
            return (ICommand)element.GetValue(WheelShiftCommandProperty);
        }

    }
}
