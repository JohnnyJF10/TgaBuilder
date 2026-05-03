using System;
using Avalonia.Media;
using TgaBuilderAvaloniaUi.Elements;
using Color = TgaBuilderLib.Abstraction.Color;

namespace TgaBuilderAvaloniaUi.View;

public partial class ColorPickerWindow : AsyncWindow
{
    public bool Confirmed { get; private set; }
    public Color ResultColorSource { get; private set; }
    public Color ResultColorTarget { get; private set; }

    public ColorPickerWindow(Color initialSource, Color initialTarget)
    {
        InitializeComponent();

        SourceColorView.Color = ToAvaloniaColor(initialSource);
        TargetColorView.Color = ToAvaloniaColor(initialTarget);

        OkButton.Click += (_, _) =>
        {
            ResultColorSource = ToLibColor(SourceColorView.Color);
            ResultColorTarget = ToLibColor(TargetColorView.Color);
            Confirmed = true;
            Close(true);
        };

        CancelButton.Click += (_, _) => Close(false);
    }

    [Obsolete("For designer use only")]
    public ColorPickerWindow()
        : this(new Color(128, 128, 128), new Color(0, 0, 0))
    {
    }

    private static Avalonia.Media.Color ToAvaloniaColor(Color c)
        => c.A.HasValue
            ? Avalonia.Media.Color.FromArgb(c.A.Value, c.R, c.G, c.B)
            : Avalonia.Media.Color.FromRgb(c.R, c.G, c.B);

    private static Color ToLibColor(Avalonia.Media.Color c)
        => c.A < 255
            ? new Color(c.R, c.G, c.B, c.A)
            : new Color(c.R, c.G, c.B);
}
