using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.IO;
using System.Runtime.InteropServices;
using TgaBuilderAvaloniaUi.Elements;
using TgaBuilderAvaloniaUi.View;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderAvaloniaUi.Services
{
    internal class CursorSetter : ICursorSetter
    {
        private readonly Cursor _eyedropperCursor;

        public CursorSetter()
        {
            _eyedropperCursor = CursorProvider.EyedropperCursor;
        }

        public void SetDefaultCursor()
        {
            if (GetMainWindow() is { } mainWindow)
                mainWindow.Cursor = CursorProvider.DefaultCursor;
        }

        public void SetEyedropperCursor()
        {
            if (GetMainWindow() is { } mainWindow)
                mainWindow.Cursor = _eyedropperCursor;
        }

        private static MainWindow? GetMainWindow()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is MainWindow mainWindow)
                return mainWindow;
            return null;
        }
    }
}
