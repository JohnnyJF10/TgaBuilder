using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using TrLynxAvaloniaUi.Elements;
using TrLynxAvaloniaUi.View;
using TrLynxLib.Abstraction;

namespace TrLynxAvaloniaUi.Services
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
