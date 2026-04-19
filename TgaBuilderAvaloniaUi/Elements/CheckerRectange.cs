using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Styling;

namespace TgaBuilderAvaloniaUi.Elements
{
    public class CheckerRectangle : Rectangle
    {

        public static readonly StyledProperty<double> CutOffXProperty =
            AvaloniaProperty.Register<CheckerRectangle, double>(
                nameof(CutOffX), 0);

        public static readonly StyledProperty<double> CutOffYProperty =
            AvaloniaProperty.Register<CheckerRectangle, double>(
                nameof(CutOffY), 0);

        public static readonly StyledProperty<double> TileDensityProperty =
            AvaloniaProperty.Register<CheckerRectangle, double>(
                nameof(TileDensity), 16);

        public double TileDensity
        {
            get => GetValue(TileDensityProperty);
            set => SetValue(TileDensityProperty, value);
        }

        public double CutOffX
        {
            get => GetValue(CutOffXProperty);
            set => SetValue(CutOffXProperty, value);
        }

        public double CutOffY
        {
            get => GetValue(CutOffYProperty);
            set => SetValue(CutOffYProperty, value);
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

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            ActualThemeVariantChanged += OnThemeChanged;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            ActualThemeVariantChanged -= OnThemeChanged;
            base.OnDetachedFromVisualTree(e);
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            UpdateBrush();
        }

        private void UpdateBrush()
        {
            double density = TileDensity <= 0 ? 16 : 1f / TileDensity * 20;

            var drawing = new DrawingGroup();


            var backgroundDrawing = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, density, density))
            };
            
            backgroundDrawing.Bind(GeometryDrawing.BrushProperty, this.GetResourceObservable("CheckerboardSecondaryBrush"));
            drawing.Children.Add(backgroundDrawing);

            var lightGroup = new GeometryGroup();
            lightGroup.Children.Add(new RectangleGeometry(new Rect(0, 0, density / 2, density / 2)));
            lightGroup.Children.Add(new RectangleGeometry(new Rect(density / 2, density / 2, density / 2, density / 2)));

            var foregroundDrawing = new GeometryDrawing
            {
                Geometry = lightGroup
            };

            foregroundDrawing.Bind(GeometryDrawing.BrushProperty, this.GetResourceObservable("CheckerboardPrimaryBrush"));
            drawing.Children.Add(foregroundDrawing);

            _checkerBrush = new DrawingBrush
            {
                TileMode = TileMode.Tile,
                Stretch = Stretch.None,
                SourceRect = new RelativeRect(0, 0, density, density, RelativeUnit.Absolute),
                DestinationRect = new RelativeRect(0, 0, density, density, RelativeUnit.Absolute),
                Drawing = drawing
            };

            Fill = _checkerBrush;
        }
    }
}
