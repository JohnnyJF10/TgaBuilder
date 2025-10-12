using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System.Runtime.CompilerServices;

namespace TgaBuilderAvaloniaUi.Elements
{
    public class PickerShape : Control
    {
        public static readonly StyledProperty<int> SizeProperty =
            AvaloniaProperty.Register<PickerShape, int>(nameof(Size), 64);

        public static readonly StyledProperty<IBrush> StrokeProperty =
            AvaloniaProperty.Register<PickerShape, IBrush>(nameof(Stroke), Brushes.Red);

        public static readonly StyledProperty<double> StrokeThicknessProperty =
            AvaloniaProperty.Register<PickerShape, double>(nameof(StrokeThickness), 1.0);

        public int Size
        {
            get => GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public IBrush Stroke
        {
            get => GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        public double StrokeThickness
        {
            get => GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        static PickerShape()
        {
            SizeProperty.Changed.AddClassHandler<PickerShape>((x, e) => x.InvalidateVisual());
            StrokeProperty.Changed.AddClassHandler<PickerShape>((x, e) => x.InvalidateVisual());
            StrokeThicknessProperty.Changed.AddClassHandler<PickerShape>((x, e) => x.InvalidateVisual());
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var pen = new Pen(Stroke, (float)StrokeThickness);
            var geometry = new StreamGeometry();

            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(0, Size * 0.3), false);
                ctx.LineTo(new Point(0, 0));
                ctx.LineTo(new Point(Size * 0.3, 0));

                ctx.BeginFigure(new Point(Size * 0.7, 0), false);
                ctx.LineTo(new Point(Size, 0));
                ctx.LineTo(new Point(Size, Size * 0.3));

                ctx.BeginFigure(new Point(Size, Size * 0.7), false);
                ctx.LineTo(new Point(Size, Size));
                ctx.LineTo(new Point(Size * 0.7, Size));

                ctx.BeginFigure(new Point(Size * 0.3, Size), false);
                ctx.LineTo(new Point(0, Size));
                ctx.LineTo(new Point(0, Size * 0.7));
            }

            context.DrawGeometry(null, pen, geometry);
        }
    }
}
