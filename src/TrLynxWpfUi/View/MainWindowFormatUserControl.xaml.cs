using System.Windows;
using System.Windows.Controls;

namespace TrLynxWpfUi.View
{
    public partial class MainWindowFormatUserControl : UserControl
    {
        public static readonly DependencyProperty IsTargetProperty = DependencyProperty.Register(
            nameof(IsTarget), typeof(bool), typeof(MainWindowFormatUserControl), new PropertyMetadata(false));

        public bool IsTarget
        {
            get => (bool)GetValue(IsTargetProperty);
            set => SetValue(IsTargetProperty, value);
        }

        public MainWindowFormatUserControl()
        {
            InitializeComponent();
        }
    }
}
