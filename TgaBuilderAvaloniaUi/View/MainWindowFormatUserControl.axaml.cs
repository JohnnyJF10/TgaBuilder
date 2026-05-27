using Avalonia;
using Avalonia.Controls;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class MainWindowFormatUserControl : UserControl
    {
        public static readonly StyledProperty<bool> IsTargetProperty =
            AvaloniaProperty.Register<MainWindowFormatUserControl, bool>(nameof(IsTarget));

        public bool IsTarget
        {
            get => GetValue(IsTargetProperty);
            set => SetValue(IsTargetProperty, value);
        }

        public MainWindowFormatUserControl()
        {
            InitializeComponent();
        }

        private void FormatSwitch_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (IsTarget && FormatSwitch.IsChecked == false)
                FormatSwitch.IsChecked = true;
        }
    }
}
