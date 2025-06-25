using System.Windows;
using System.Windows.Media;

namespace THelperWpfUi.Elements
{
    public class VisualGrid : FrameworkElement
    {
        public static readonly DependencyProperty CellSizeProperty =
            DependencyProperty.Register("CellSize", typeof(int), typeof(VisualGrid),
                new FrameworkPropertyMetadata(20, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GridOffsetXProperty =
            DependencyProperty.Register("GridOffsetX", typeof(int), typeof(VisualGrid),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GridOffsetYProperty =
            DependencyProperty.Register("GridOffsetY", typeof(int), typeof(VisualGrid),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeThicknessProperty = 
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(VisualGrid),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke", typeof(Brush), typeof(VisualGrid),
                new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MainDashStyleProperty =
            DependencyProperty.Register("MainDashStyle", typeof(DashStyle), typeof(VisualGrid),
                new FrameworkPropertyMetadata(DashStyles.Solid, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BorderAreasDashStyleProperty =
            DependencyProperty.Register("BorderAreasDashStyle", typeof(DashStyle), typeof(VisualGrid),
                new FrameworkPropertyMetadata(DashStyles.Dash, FrameworkPropertyMetadataOptions.AffectsRender));

        public DashStyle BorderAreasDashStyle
        {
            get => (DashStyle)GetValue(BorderAreasDashStyleProperty);
            set => SetValue(BorderAreasDashStyleProperty, value);
        }

        public DashStyle MainDashStyle
        {
            get => (DashStyle)GetValue(MainDashStyleProperty);
            set => SetValue(MainDashStyleProperty, value);
        }

        public Brush Stroke
        {
            get => (Brush)GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public int CellSize
        {
            get => (int)GetValue(CellSizeProperty); 
            set => SetValue(CellSizeProperty, value); 
        }

        public int GridOffsetX
        {
            get => (int)GetValue(GridOffsetXProperty); 
            set => SetValue(GridOffsetXProperty, value); 
        }

        public int GridOffsetY
        {
            get => (int)GetValue(GridOffsetYProperty); 
            set => SetValue(GridOffsetYProperty, value); 
        }

        public VisualGrid()
        {
            this.SizeChanged += OnSizeChanged;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            DrawGrid(drawingContext);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            InvalidateVisual();
        }

        private void DrawGrid(DrawingContext drawingContext)
        {
            int cellWidth = CellSize;
            int offsetX = GridOffsetX;
            int offsetY = GridOffsetY;

            Pen mainPen = new Pen(Stroke, StrokeThickness) { DashStyle = MainDashStyle };
            Pen borderAreaPen = new Pen(Stroke, StrokeThickness / 2) { DashStyle = BorderAreasDashStyle };

            // Calculate the number of cells based on the actual size and offsets
            int numCellsX = offsetX == 0 ? (int)ActualWidth / cellWidth : (int)ActualWidth / cellWidth - 1;
            int numCellsY = offsetY == 0 ? (int)ActualHeight / cellWidth : (int)ActualHeight / cellWidth - 1;

            // Draw the vertical lines
            for (int i = 0; i <= numCellsX; i++)
            {
                double x = i * cellWidth + offsetX;
                drawingContext.DrawLine(borderAreaPen, new Point(x, 0), new Point(x, ActualHeight));
                drawingContext.DrawLine(mainPen, new Point(x, offsetY), new Point(x, ActualHeight - CellSize + offsetY));
            }

            // Draw the horizontal lines
            for (int i = 0; i <= numCellsY; i++)
            {
                double y = i * cellWidth + offsetY;
                drawingContext.DrawLine(borderAreaPen, new Point(0, y), new Point(ActualWidth, y));
                drawingContext.DrawLine(mainPen, new Point(offsetX, y), new Point(ActualWidth - CellSize + offsetX, y));
            }
        }
    }
}
