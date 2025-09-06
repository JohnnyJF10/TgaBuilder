using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.Commands
{
    public static class CommandManagerProxy
    {
        private static ICommandManagerService? _implementation;

        /// <summary>
        /// Needs to be set right after application start.
        /// </summary>
        public static void Initialize(ICommandManagerService implementation)
        {
            _implementation = implementation
                ?? throw new ArgumentNullException(nameof(implementation));
        }

        public static void InvalidateRequerySuggested()
            => _implementation?.InvalidateRequerySuggested();

        public static event EventHandler? RequerySuggested
        {
            add => _implementation!.RequerySuggested += value; 
            remove => _implementation!.RequerySuggested -= value; 
        }

    }
}
