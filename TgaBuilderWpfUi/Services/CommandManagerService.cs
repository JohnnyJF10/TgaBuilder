using System.Windows.Input;
using TgaBuilderLib.Commands;

namespace TgaBuilderWpfUi.Services
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
