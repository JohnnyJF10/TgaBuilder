using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace TgaBuilderAvaloniaUi.Elements
{
    public class AnimRangeSelectionShape : Control
    {
        // --- Properties ---
        public static readonly StyledProperty<int> ShapeWidthProperty =
            AvaloniaProperty.Register<AnimRangeSelectionShape, int>(nameof(ShapeWidth), 0);

        public int ShapeWidth
        {
            get => GetValue(ShapeWidthProperty);
            set => SetValue(ShapeWidthProperty, value);
        }

        public static readonly StyledProperty<int> ShapeHeightProperty =
            AvaloniaProperty.Register<AnimRangeSelectionShape, int>(nameof(ShapeHeight), 0);

        public int ShapeHeight
        {
            get => GetValue(ShapeHeightProperty);
            set => SetValue(ShapeHeightProperty, value);
        }

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
                ShapeWidthProperty,
                ShapeHeightProperty,
                TopStepPositionProperty,
                BottomStepPositionProperty,
                StepHeightProperty,
                FillColorProperty,
                StrokeThicknessProperty,
                StrokeColorProperty);
        }

        // --- Rendering ---
        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var pen = new Pen(StrokeColor, StrokeThickness);

            double width = ShapeWidth;
            double height = ShapeHeight;

            // --- Special Cases ---
            if (height == 0 && BottomStepPosition > TopStepPosition + StepHeight)
            {
                // Top rectangle
                context.DrawLine(pen, new Point(0, 0), new Point(TopStepPosition + StepHeight, 0));
                context.DrawLine(pen, new Point(TopStepPosition + StepHeight, 0), new Point(TopStepPosition + StepHeight, StepHeight));
                context.DrawLine(pen, new Point(TopStepPosition + StepHeight, StepHeight), new Point(0, StepHeight));
                context.DrawLine(pen, new Point(0, StepHeight), new Point(0, 0));

                // Bottom rectangle
                context.DrawLine(pen, new Point(BottomStepPosition - StepHeight, height - StepHeight), new Point(width, height - StepHeight));
                context.DrawLine(pen, new Point(width, height - StepHeight), new Point(width, height));
                context.DrawLine(pen, new Point(width, height), new Point(BottomStepPosition - StepHeight, height));
                context.DrawLine(pen, new Point(BottomStepPosition - StepHeight, height), new Point(BottomStepPosition - StepHeight, height));
                return;
            }

            if (height == StepHeight)
            {
                if (BottomStepPosition > TopStepPosition)
                {
                    context.DrawLine(pen, new Point(TopStepPosition, 0), new Point(BottomStepPosition, 0));
                    context.DrawLine(pen, new Point(BottomStepPosition, 0), new Point(BottomStepPosition, StepHeight));
                    context.DrawLine(pen, new Point(BottomStepPosition, StepHeight), new Point(TopStepPosition, StepHeight));
                    context.DrawLine(pen, new Point(TopStepPosition, StepHeight), new Point(TopStepPosition, 0));
                }
                else
                {
                    context.DrawLine(pen, new Point(BottomStepPosition - StepHeight, 0), new Point(TopStepPosition + StepHeight, 0));
                    context.DrawLine(pen, new Point(TopStepPosition + StepHeight, 0), new Point(TopStepPosition + StepHeight, StepHeight));
                    context.DrawLine(pen, new Point(TopStepPosition + StepHeight, StepHeight), new Point(BottomStepPosition - StepHeight, StepHeight));
                    context.DrawLine(pen, new Point(BottomStepPosition - StepHeight, StepHeight), new Point(BottomStepPosition - StepHeight, 0));
                }
                return;
            }

            if (height - StepHeight == StepHeight && BottomStepPosition < TopStepPosition)
            {
                // Top rectangle
                context.DrawLine(pen, new Point(TopStepPosition, 0), new Point(width, 0));
                context.DrawLine(pen, new Point(width, 0), new Point(width, StepHeight));
                context.DrawLine(pen, new Point(width, StepHeight), new Point(TopStepPosition, StepHeight));
                context.DrawLine(pen, new Point(TopStepPosition, StepHeight), new Point(TopStepPosition, 0));

                // Bottom rectangle
                context.DrawLine(pen, new Point(0, height - StepHeight), new Point(BottomStepPosition, height - StepHeight));
                context.DrawLine(pen, new Point(BottomStepPosition, height - StepHeight), new Point(BottomStepPosition, height));
                context.DrawLine(pen, new Point(BottomStepPosition, height), new Point(0, height));
                context.DrawLine(pen, new Point(0, height), new Point(0, height - StepHeight));
                return;
            }

            // --- Default polygon ---
            double topDraw = height > 0 ? TopStepPosition : TopStepPosition + StepHeight;
            double bottomDraw = height > 0 ? BottomStepPosition : BottomStepPosition - StepHeight;

            var points = new Point[]
            {
                new Point(0, StepHeight),
                new Point(topDraw, StepHeight),
                new Point(topDraw, 0),
                new Point(width, 0),
                new Point(width, height - StepHeight),
                new Point(bottomDraw, height - StepHeight),
                new Point(bottomDraw, height),
                new Point(0, height)
            };

            for (int i = 0; i < points.Length - 1; i++)
                context.DrawLine(pen, points[i], points[i + 1]);
            context.DrawLine(pen, points[^1], points[0]);
        }
    }
}
