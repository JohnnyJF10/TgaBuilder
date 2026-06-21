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
            CutOffXProperty.Changed.AddClassHandler<CheckerRectangle>((s, e) => s.UpdateOffset());
            CutOffYProperty.Changed.AddClassHandler<CheckerRectangle>((s, e) => s.UpdateOffset());
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

        /// <summary>
        /// Calculates the tile offset for a given axis to counteract panning.
        /// The CutOff value (from ZoomBorder OffsetX/Y) is in screen pixels;
        /// dividing by the zoom level converts to image-pixel space.
        /// The result is wrapped modulo tile size so the first row/column
        /// of squares appears cut off, creating the illusion of a stationary
        /// checker pattern even though the rectangle moves with the canvas.
        /// </summary>
        private double CalculateTileOffset(double cutOff, double density)
        {
            if (density <= 0) return 0;
            double zoom = TileDensity > 0 ? TileDensity : 1;
            double shift = -cutOff / zoom;
            return ((shift % density) + density) % density;
        }

        /// <summary>
        /// Lightweight update: only adjusts the DestinationRect offset of the
        /// existing brush so the first row/column tiles are cut off to match
        /// the current pan position. Called when CutOffX or CutOffY change.
        /// </summary>
        private void UpdateOffset()
        {
            if (_checkerBrush == null) return;

            double density = TileDensity <= 0 ? 16 : 1f / TileDensity * 20;
            double shiftX = CalculateTileOffset(CutOffX, density);
            double shiftY = CalculateTileOffset(CutOffY, density);

            _checkerBrush.DestinationRect =
                new RelativeRect(shiftX, shiftY, density, density, RelativeUnit.Absolute);
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

            double shiftX = CalculateTileOffset(CutOffX, density);
            double shiftY = CalculateTileOffset(CutOffY, density);

            _checkerBrush = new DrawingBrush
            {
                TileMode = TileMode.Tile,
                Stretch = Stretch.None,
                SourceRect = new RelativeRect(0, 0, density, density, RelativeUnit.Absolute),
                DestinationRect = new RelativeRect(shiftX, shiftY, density, density, RelativeUnit.Absolute),
                Drawing = drawing
            };

            Fill = _checkerBrush;
        }
    }
}
