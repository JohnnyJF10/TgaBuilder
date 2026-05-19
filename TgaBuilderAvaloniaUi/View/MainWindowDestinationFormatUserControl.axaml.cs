using Avalonia.Controls;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class MainWindowDestinationFormatUserControl : UserControl
    {
        public MainWindowDestinationFormatUserControl()
        {
            InitializeComponent();
        }

    private void DestinationFormatSwitch_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DestinationFormatSwitch.IsChecked == false)
            DestinationFormatSwitch.IsChecked = true;
    }
    }
}
