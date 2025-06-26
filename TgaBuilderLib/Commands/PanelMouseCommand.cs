using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using MouseAction = TgaBuilderLib.Abstraction.MouseAction;

namespace TgaBuilderLib.Commands
{
    public class PanelMouseCommand : ICommand
    {
        private readonly Action<int, int, bool, MouseAction, MouseModifier> _execute;
        private readonly Func<int, int, bool, MouseAction, MouseModifier, bool>? _canExecute;

        public PanelMouseCommand(
            Action<int, int, bool, MouseAction, MouseModifier> execute, 
            Func<int, int, bool, MouseAction, MouseModifier, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null)
                return true;

            if (parameter is ValueTuple<int, int, bool, MouseAction, MouseModifier> 
                (int x, int y, bool isTarget, MouseAction action, MouseModifier modifier) args)
                return _canExecute(x, y, isTarget, action, modifier);

            return false;
        }

        public void Execute(object? parameter)
        {
            if (parameter is ValueTuple<int, int, bool, MouseAction, MouseModifier> 
                (int x, int y, bool isTarget, MouseAction action, MouseModifier modifier) args)
                _execute(x, y, isTarget, action, modifier);
        }

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
