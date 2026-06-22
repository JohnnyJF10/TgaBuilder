using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
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
