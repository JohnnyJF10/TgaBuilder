using Avalonia.Labs.Input;
using System;
using TgaBuilderLib.Commands;

namespace TgaBuilderAvaloniaUi.Services
{
    internal class CommandManagerService : ICommandManagerService
    {
        public event EventHandler RequerySuggested
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void InvalidateRequerySuggested()
            => CommandManager.InvalidateRequerySuggested();
    }
}
