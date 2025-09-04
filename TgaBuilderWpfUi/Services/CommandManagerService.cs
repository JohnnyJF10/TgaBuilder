using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
