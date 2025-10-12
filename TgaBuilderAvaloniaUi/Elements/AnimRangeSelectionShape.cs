using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace TgaBuilderAvaloniaUi.Elements
{
    public class AnimRangeSelectionShape : Control
    {
        // --- Properties ---

        public static readonly StyledProperty<int> TopStepPositionProperty =
            AvaloniaProperty.Register<AnimRangeSelectionShape, int>(nameof(TopStepPosition), 0);

        public int TopStepPosition
        {
            get => GetValue(TopStepPositionProperty);
            set => SetValue(TopStepPositionProperty, value);
        }

        public static readonly StyledProperty<int> BottomStepPositionProperty =
            AvaloniaProperty.Register<AnimRangeSelectionShape, int>(nameof(BottomStepPosition), 0);

        public int BottomStepPosition
        {
            get => GetValue(BottomStepPositionProperty);
            set => SetValue(BottomStepPositionProperty, value);
        }

        public static readonly StyledProperty<int> StepHeightProperty =
            AvaloniaProperty.Register<AnimRangeSelectionShape, int>(nameof(StepHeight), 0);

        public int StepHeight
        {
            get => GetValue(StepHeightProperty);
            set => SetValue(StepHeightProperty, value);
        }

        public static readonly StyledProperty<IBrush> FillColorProperty =
            AvaloniaProperty.Register<AnimRangeSelectionShape, IBrush>(nameof(FillColor), Brushes.Transparent);

        public IBrush FillColor
        {
            get => GetValue(FillColorProperty);
            set => SetValue(FillColorProperty, value);
        }

        public static readonly StyledProperty<double> StrokeThicknessProperty =
            AvaloniaProperty.Register<AnimRangeSelectionShape, double>(nameof(StrokeThickness), 1.0);

        public double StrokeThickness
        {
            get => GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public static readonly StyledProperty<IBrush> StrokeColorProperty =
            AvaloniaProperty.Register<AnimRangeSelectionShape, IBrush>(nameof(StrokeColor), Brushes.Black);

        public IBrush StrokeColor
        {
            get => GetValue(StrokeColorProperty);
            set => SetValue(StrokeColorProperty, value);
        }

        // --- Constructor ---
        static AnimRangeSelectionShape()
        {
            AffectsRender<AnimRangeSelectionShape>(
                WidthProperty,
                HeightProperty,
                TopStepPositionProperty,
                BottomStepPositionProperty,
                StepHeightProperty,
                FillColorProperty,
                StrokeThicknessProperty,
                StrokeColorProperty);
        }

        // --- Rendering ---
        public override void Render(DrawingContext drawingContext)
        {
            base.Render(drawingContext);

            var pen = new Pen(StrokeColor, StrokeThickness);

            if (Height == 0 && BottomStepPosition > TopStepPosition + StepHeight)
            {
                // Top rectangle
                drawingContext.DrawLine(pen, new Point(0, 0), new Point(TopStepPosition + StepHeight, 0));
                drawingContext.DrawLine(pen, new Point(TopStepPosition + StepHeight, 0), new Point(TopStepPosition + StepHeight, StepHeight));
                drawingContext.DrawLine(pen, new Point(TopStepPosition + StepHeight, StepHeight), new Point(0, StepHeight));
                drawingContext.DrawLine(pen, new Point(0, StepHeight), new Point(0, 0));

                // Bottom rectangle
                drawingContext.DrawLine(pen, new Point(BottomStepPosition - StepHeight, Height - StepHeight), new Point(Width, Height - StepHeight));
                drawingContext.DrawLine(pen, new Point(Width, Height - StepHeight), new Point(Width, Height));
                drawingContext.DrawLine(pen, new Point(Width, Height), new Point(BottomStepPosition - StepHeight, Height));
                drawingContext.DrawLine(pen, new Point(BottomStepPosition - StepHeight, Height), new Point(BottomStepPosition - StepHeight, Height - StepHeight));
                return;
            }

            if (Height == StepHeight)
            {
                if (BottomStepPosition > TopStepPosition)
                {
                    drawingContext.DrawLine(pen, new Point(TopStepPosition, 0), new Point(BottomStepPosition, 0));
                    drawingContext.DrawLine(pen, new Point(BottomStepPosition, 0), new Point(BottomStepPosition, StepHeight));
                    drawingContext.DrawLine(pen, new Point(BottomStepPosition, StepHeight), new Point(TopStepPosition, StepHeight));
                    drawingContext.DrawLine(pen, new Point(TopStepPosition, StepHeight), new Point(TopStepPosition, 0));
                }
                else
                {
                    drawingContext.DrawLine(pen, new Point(BottomStepPosition - StepHeight, 0), new Point(TopStepPosition + StepHeight, 0));
                    drawingContext.DrawLine(pen, new Point(TopStepPosition + StepHeight, 0), new Point(TopStepPosition + StepHeight, StepHeight));
                    drawingContext.DrawLine(pen, new Point(TopStepPosition + StepHeight, StepHeight), new Point(BottomStepPosition - StepHeight, StepHeight));
                    drawingContext.DrawLine(pen, new Point(BottomStepPosition - StepHeight, StepHeight), new Point(BottomStepPosition - StepHeight, 0));
                }
                return;
            }

            if (Height - StepHeight == StepHeight && BottomStepPosition < TopStepPosition)
            {
                // Top rectangle
                drawingContext.DrawLine(pen, new Point(TopStepPosition, 0), new Point(Width, 0));
                drawingContext.DrawLine(pen, new Point(Width, 0), new Point(Width, StepHeight));
                drawingContext.DrawLine(pen, new Point(Width, StepHeight), new Point(TopStepPosition, StepHeight));
                drawingContext.DrawLine(pen, new Point(TopStepPosition, StepHeight), new Point(TopStepPosition, 0));

                // Bottom rectangle
                drawingContext.DrawLine(pen, new Point(0, Height - StepHeight), new Point(BottomStepPosition, Height - StepHeight));
                drawingContext.DrawLine(pen, new Point(BottomStepPosition, Height - StepHeight), new Point(BottomStepPosition, Height));
                drawingContext.DrawLine(pen, new Point(BottomStepPosition, Height), new Point(0, Height));
                drawingContext.DrawLine(pen, new Point(0, Height), new Point(0, Height - StepHeight));
                return;
            }

            double TopStepPositionDraw = Height > 0 ? TopStepPosition : TopStepPosition + StepHeight;
            double BottomStepPositionDraw = Height > 0 ? BottomStepPosition : BottomStepPosition - StepHeight;

            var points = new Point[]
            {
                new Point(0, StepHeight),
                new Point(TopStepPositionDraw, StepHeight),
                new Point(TopStepPositionDraw, 0),
                new Point(Width, 0),
                new Point(Width, Height - StepHeight),
                new Point(BottomStepPositionDraw, Height - StepHeight),
                new Point(BottomStepPositionDraw, Height),
                new Point(0, Height)
            };

            for (int i = 0; i < points.Length - 1; i++)
                drawingContext.DrawLine(pen, points[i], points[i + 1]);
            drawingContext.DrawLine(pen, points[^1], points[0]);
        }
    }
}
