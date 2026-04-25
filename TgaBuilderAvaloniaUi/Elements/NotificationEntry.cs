using Avalonia.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TgaBuilderAvaloniaUi.Elements
{
    public class NotificationEntry : INotifyPropertyChanged
    {
        private bool _isLast;
        private bool _isDismissing;

        public string Title { get; init; } = "";
        public string Message { get; init; } = "";
        public IBrush AccentBrush { get; init; } = Brushes.Gray;
        public int TimeoutSeconds { get; init; } = 5;

        public bool IsLast
        {
            get => _isLast;
            set
            {
                if (_isLast == value) return;
                _isLast = value;
                OnPropertyChanged();
            }
        }

        public bool IsDismissing
        {
            get => _isDismissing;
            private set
            {
                if (_isDismissing == value) return;
                _isDismissing = value;
                OnPropertyChanged();
            }
        }

        public void BeginDismiss() => IsDismissing = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
