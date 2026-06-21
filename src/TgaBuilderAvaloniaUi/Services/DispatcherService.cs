using Avalonia.Threading;
using System;
using System.Threading.Tasks;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderAvaloniaUi.Services
{
    internal class DispatcherService : IDispatcherService
    {
        public void Invoke(Action action)

            => Dispatcher.UIThread.Post(action);


        public Task InvokeAsync(Action action)
        {
            Action a = () => Dispatcher.UIThread.InvokeAsync(action);

            return Task.Run(a);
        }

    }
}
