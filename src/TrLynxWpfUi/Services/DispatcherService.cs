using System.Windows;
using TrLynxLib.Abstraction;

namespace TrLynxWpfUi.Services
{
    internal class DispatcherService : IDispatcherService
    {
        public void Invoke(Action action)
            => Application.Current.Dispatcher.Invoke(action);

        public Task InvokeAsync(Action action)
            => Application.Current.Dispatcher.InvokeAsync(action).Task;
    }
}
