using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace TgaBuilderAvaloniaUi.Elements
{
    public class CheckerRectangle : Rectangle
    {

        public static readonly StyledProperty<double> TileDensityProperty =
            AvaloniaProperty.Register<CheckerRectangle, double>(
                nameof(TileDensity), 16);

        public double TileDensity
        {
            get => GetValue(TileDensityProperty);
            set => SetValue(TileDensityProperty, value);
        }

        private DrawingBrush? _checkerBrush;

        static CheckerRectangle()
        {

            TileDensityProperty.Changed.AddClassHandler<CheckerRectangle>((s, e) => s.UpdateBrush());
        }

        public CheckerRectangle()
        {
            UpdateBrush();
        }

        private void UpdateBrush()
        {
            double density = TileDensity <= 0 ? 16 : 1f / TileDensity * 20;

            var drawing = new DrawingGroup();


            drawing.Children.Add(new GeometryDrawing
            {
                Brush = Brushes.Gray,
                Geometry = new RectangleGeometry(new Rect(0, 0, density, density))
            });


            var light = new GeometryGroup();
            light.Children.Add(new RectangleGeometry(new Rect(0, 0, density / 2, density / 2)));
            light.Children.Add(new RectangleGeometry(new Rect(density / 2, density / 2, density / 2, density / 2)));

            drawing.Children.Add(new GeometryDrawing
            {
                Brush = Brushes.LightGray,
                Geometry = light
            });

            _checkerBrush = new DrawingBrush
            {
                TileMode = TileMode.Tile,
                Stretch = Stretch.None,
                SourceRect = new RelativeRect(0, 0, density, density, RelativeUnit.Absolute),
                DestinationRect = new RelativeRect(0, 0, density, density, RelativeUnit.Absolute),
                Drawing = drawing
            };

            //Debug.WriteLine($"Zoom: {TileDensity}");
            //Debug.WriteLine($"Claculated TileDensity: {density}");  

            Fill = _checkerBrush;
        }
    }
}
