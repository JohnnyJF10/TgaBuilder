using System.Windows.Input;

namespace TgaBuilderLib.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) =>
            _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) =>
            _execute();

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManagerProxy.RequerySuggested += value!;
            remove => CommandManagerProxy.RequerySuggested -= value!;
        }

        public void RaiseCanExecuteChanged() =>
            CommandManagerProxy.InvalidateRequerySuggested();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter == null && typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
                return false;

            if (parameter is T tParam)
                return _canExecute?.Invoke(tParam) ?? true; 
            else
                return true;
        }

        public void Execute(object? parameter)
        {
            if (parameter == null && typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
                throw new InvalidCastException($"Parameter of type 'null' cannot be cast to non-nullable type {typeof(T).Name}.");

            if (parameter is T tParam)
                _execute(tParam);
            else
                throw new InvalidCastException($"Parameter of type '{parameter?.GetType().Name}' cannot be cast to type {typeof(T).Name}.");
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManagerProxy.RequerySuggested += value!;
            remove => CommandManagerProxy.RequerySuggested -= value!;
        }

        public void RaiseCanExecuteChanged() =>
            CommandManagerProxy.InvalidateRequerySuggested();
    }
}
