using System.Windows;
using TgaBuilderWpfUi.Elements;
using Color = TgaBuilderLib.Abstraction.Color;
using WpfColor = System.Windows.Media.Color;

namespace TgaBuilderWpfUi.View
{
    public partial class ColorPickerWindow : AsyncWindow
    {
        public Color ResultColorSource { get; private set; }
        public Color ResultColorTarget { get; private set; }

        public ColorPickerWindow(Color initialSource, Color initialTarget)
        {
            InitializeComponent();

            SourceColorPanel.SelectedColor = ToWpfColor(initialSource);
            TargetColorPanel.SelectedColor = ToWpfColor(initialTarget);

            OkButton.Click += OkButton_Click;
            CancelButton.Click += CancelButton_Click;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ResultColorSource = ToLibColor(SourceColorPanel.SelectedColor ?? WpfColor.FromArgb(0, 0, 0, 0));
            ResultColorTarget = ToLibColor(TargetColorPanel.SelectedColor ?? WpfColor.FromArgb(0, 0, 0, 0));
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private static WpfColor ToWpfColor(Color c)
            => c.A.HasValue
                ? WpfColor.FromArgb(c.A.Value, c.R, c.G, c.B)
                : WpfColor.FromRgb(c.R, c.G, c.B);

        private static Color ToLibColor(WpfColor c)
            => c.A < 255
                ? new Color(c.R, c.G, c.B, c.A)
                : new Color(c.R, c.G, c.B);
    }
}
