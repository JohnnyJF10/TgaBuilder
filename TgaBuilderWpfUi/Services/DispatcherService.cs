using System.Windows;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderWpfUi.Services
{
    internal class DispatcherService : IDispatcherService
    {
        public void Invoke(Action action)
            => Application.Current.Dispatcher.Invoke(action);

        public Task InvokeAsync(Action action)
            => Application.Current.Dispatcher.InvokeAsync(action).Task;
    }
}
