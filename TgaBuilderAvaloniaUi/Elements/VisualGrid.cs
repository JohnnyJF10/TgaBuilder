using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System.Collections.Generic;

namespace TgaBuilderAvaloniaUi.Elements
{
    public class VisualGrid : Control
    {
        // --- Properties ---
        public static readonly StyledProperty<int> CellSizeProperty =
            AvaloniaProperty.Register<VisualGrid, int>(nameof(CellSize), 20);

        public int CellSize
        {
            get => GetValue(CellSizeProperty);
            set => SetValue(CellSizeProperty, value);
        }

        public static readonly StyledProperty<int> GridOffsetXProperty =
            AvaloniaProperty.Register<VisualGrid, int>(nameof(GridOffsetX), 0);

        public int GridOffsetX
        {
            get => GetValue(GridOffsetXProperty);
            set => SetValue(GridOffsetXProperty, value);
        }

        public static readonly StyledProperty<int> GridOffsetYProperty =
            AvaloniaProperty.Register<VisualGrid, int>(nameof(GridOffsetY), 0);

        public int GridOffsetY
        {
            get => GetValue(GridOffsetYProperty);
            set => SetValue(GridOffsetYProperty, value);
        }

        public static readonly StyledProperty<double> StrokeThicknessProperty =
            AvaloniaProperty.Register<VisualGrid, double>(nameof(StrokeThickness), 1.0);

        public double StrokeThickness
        {
            get => GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public static readonly StyledProperty<IBrush> StrokeProperty =
            AvaloniaProperty.Register<VisualGrid, IBrush>(nameof(Stroke), Brushes.Black);

        public IBrush Stroke
        {
            get => GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        public static readonly StyledProperty<IDashStyle> MainDashStyleProperty =
            AvaloniaProperty.Register<VisualGrid, IDashStyle>(nameof(MainDashStyle), DashStyle.Dash);

        public IDashStyle MainDashStyle
        {
            get => GetValue(MainDashStyleProperty);
            set => SetValue(MainDashStyleProperty, value);
        }

        public static readonly StyledProperty<IDashStyle> BorderAreasDashStyleProperty =
            AvaloniaProperty.Register<VisualGrid, IDashStyle>(nameof(BorderAreasDashStyle), DashStyle.DashDot);

        public IDashStyle BorderAreasDashStyle
        {
            get => GetValue(BorderAreasDashStyleProperty);
            set => SetValue(BorderAreasDashStyleProperty, value);
        }

        // --- Constructor ---
        static VisualGrid()
        {
            AffectsRender<VisualGrid>(
                CellSizeProperty,
                GridOffsetXProperty,
                GridOffsetYProperty,
                StrokeProperty,
                StrokeThicknessProperty,
                MainDashStyleProperty,
                BorderAreasDashStyleProperty);
        }

        // --- Rendering ---
        public override void Render(DrawingContext context)
        {
            base.Render(context);
            DrawGrid(context);
        }

        private void DrawGrid(DrawingContext context)
        {
            int cellWidth = CellSize;
            int offsetX = GridOffsetX;
            int offsetY = GridOffsetY;

            var mainPen = new Pen(
                brush: Stroke,
                dashStyle: MainDashStyle);

            var borderPen = new Pen(
                brush: Stroke,
                dashStyle: BorderAreasDashStyle);

            int numCellsX = offsetX == 0 ? (int)Bounds.Width / cellWidth : (int)Bounds.Width / cellWidth - 1;
            int numCellsY = offsetY == 0 ? (int)Bounds.Height / cellWidth : (int)Bounds.Height / cellWidth - 1;

            for (int i = 0; i <= numCellsX; i++)
            {
                double x = i * cellWidth + offsetX;
                context.DrawLine(borderPen, new Point(x, 0), new Point(x, Bounds.Height));
                context.DrawLine(mainPen, new Point(x, offsetY), new Point(x, Bounds.Height - CellSize + offsetY));
            }

            for (int i = 0; i <= numCellsY; i++)
            {
                double y = i * cellWidth + offsetY;
                context.DrawLine(borderPen, new Point(0, y), new Point(Bounds.Width, y));
                context.DrawLine(mainPen, new Point(offsetX, y), new Point(Bounds.Width - CellSize + offsetX, y));
            }
        }
    }
}
