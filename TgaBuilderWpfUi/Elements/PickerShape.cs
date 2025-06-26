using System.Windows;
using System.Windows.Media;

namespace TgaBuilderWpfUi.Elements
{
    public class PickerShape : FrameworkElement
    {
        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
            "Size", 
            typeof(int), 
            typeof(PickerShape), 
            new PropertyMetadata(64, OnSizeChanged));

        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
            "Stroke", 
            typeof(Brush), 
            typeof(PickerShape), 
            new PropertyMetadata(Brushes.Red, OnStrokeChanged));

        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
            "StrokeThickness", 
            typeof(double), 
            typeof(PickerShape), 
            new PropertyMetadata(1.0, OnStrokeThicknessChanged));

        public int Size
        {
            get { return (int)GetValue(SizeProperty); }
            set { SetValue(SizeProperty, value); }
        }

        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PickerShape)d).InvalidateVisual();
        }

        private static void OnStrokeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PickerShape)d).InvalidateVisual();
        }

        private static void OnStrokeThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PickerShape)d).InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            Pen pen = new Pen(Stroke, StrokeThickness);
            StreamGeometry geometry = new StreamGeometry();

            using (StreamGeometryContext ctx = geometry.Open())
            {
                // Draw a shape that looks like a picker with four lines extending from the corners.
                // +-----------+---------+---------------+------------------------------+
                // |           |         | LineTo 1      | LineTo 2                     |
                // +-----------+---------+---------------+------------------------------+
                ctx.BeginFigure(new Point(0,              Size * 0.3),     false, false);
                ctx.LineTo     (new Point(0,              0),              true,  false);
                ctx.LineTo     (new Point(Size * 0.3,     0),              true,  false);
                
                ctx.BeginFigure(new Point(Size * 0.7,     0),              false, false);
                ctx.LineTo     (new Point(Size,           0),              true,  false);
                ctx.LineTo     (new Point(Size,           Size * 0.3),     true,  false);
                
                ctx.BeginFigure(new Point(Size,           Size * 0.7),     false, false);
                ctx.LineTo     (new Point(Size,           Size),           true,  false);
                ctx.LineTo     (new Point(Size * 0.7,     Size),           true,  false);
                
                ctx.BeginFigure(new Point(Size * 0.3,     Size),           false, false);
                ctx.LineTo     (new Point(0,              Size),           true,  false);
                ctx.LineTo     (new Point(0,              Size * 0.7),     true,  false);
                // +-----------+---------+---------------+------------------------------+
            }

            drawingContext.DrawGeometry(null, pen, geometry);
        }
    }
}
