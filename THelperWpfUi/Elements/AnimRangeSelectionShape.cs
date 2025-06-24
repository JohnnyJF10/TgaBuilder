using System.Windows.Media;
using System.Windows;

namespace THelperWpfUi.Elements
{
    public class AnimRangeSelectionShape : FrameworkElement
    {
        // Dependency Properties
        public static readonly new DependencyProperty WidthProperty =
            DependencyProperty.Register(
                "Width", 
                typeof(int), 
                typeof(AnimRangeSelectionShape), 
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly new DependencyProperty HeightProperty =
            DependencyProperty.Register(
                "Height", 
                typeof(int), 
                typeof(AnimRangeSelectionShape), 
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TopStepPositionProperty =
            DependencyProperty.Register(
                "TopStepPosition", 
                typeof(int), 
                typeof(AnimRangeSelectionShape), 
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BottomStepPositionProperty =
            DependencyProperty.Register(
                "BottomStepPosition", 
                typeof(int), 
                typeof(AnimRangeSelectionShape), 
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StepHeightProperty =
            DependencyProperty.Register(
                "StepHeight", 
                typeof(int), 
                typeof(AnimRangeSelectionShape), 
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FillColorProperty =
            DependencyProperty.Register(
                "FillColor", 
                typeof(Brush), 
                typeof(AnimRangeSelectionShape), 
                new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                "StrokeThickness", 
                typeof(double), 
                typeof(AnimRangeSelectionShape), 
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeColorProperty =
            DependencyProperty.Register(
                "StrokeColor", 
                typeof(Brush), 
                typeof(AnimRangeSelectionShape), 
                new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        // CLR Properties
        public new int Width
        {
            get => (int)GetValue(WidthProperty);
            set => SetValue(WidthProperty, value);
        }

        public new int Height
        {
            get => (int)GetValue(HeightProperty);
            set => SetValue(HeightProperty, value);
        }

        public int TopStepPosition
        {
            get => (int)GetValue(TopStepPositionProperty);
            set => SetValue(TopStepPositionProperty, value);
        }

        public int BottomStepPosition
        {
            get => (int)GetValue(BottomStepPositionProperty);
            set => SetValue(BottomStepPositionProperty, value);
        }

        public int StepHeight
        {
            get => (int)GetValue(StepHeightProperty);
            set => SetValue(StepHeightProperty, value);
        }

        public Brush FillColor
        {
            get => (Brush)GetValue(FillColorProperty);
            set => SetValue(FillColorProperty, value);
        }

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public Brush StrokeColor
        {
            get => (Brush)GetValue(StrokeColorProperty);
            set => SetValue(StrokeColorProperty, value);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            Pen pen = new Pen(StrokeColor, StrokeThickness);

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
            {
                drawingContext.DrawLine(pen, points[i], points[i + 1]);
            }
            drawingContext.DrawLine(pen, points[points.Length - 1], points[0]);
        }
    }
}
