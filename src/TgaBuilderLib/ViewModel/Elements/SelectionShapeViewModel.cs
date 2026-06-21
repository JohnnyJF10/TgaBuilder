namespace TgaBuilderLib.ViewModel
{
    public class SelectionShapeViewModel : ViewModelBase
    {
        public SelectionShapeViewModel(int maxX, int maxY)
        {
            MaxX = maxX;
            MaxY = maxY;
        }

        private bool _isVisible;
        private int _x;
        private int _y;
        private int _width;
        private int _height;
        private double _strokeThickness = 2;

        public int MinX { get; set; } = 0;
        public int MinY { get; set; } = 0;

        public int MaxX { get; set; }
        public int MaxY { get; set; }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetPropertyPrimitive(ref _isVisible, value, nameof(IsVisible));
        }

        public int X
        {
            get => _x;
            set => SetX(value);
        }

        public int Y
        {
            get => _y;
            set => SetY(value);
        }

        public int Width
        {
            get => _width;
            set => SetWidth(value);
        }

        public int Height
        {
            get => _height;
            set => SetHeight(value);
        }

        public double StrokeThickness
        {
            get => _strokeThickness;
            set => SetProperty(ref _strokeThickness, value, nameof(StrokeThickness));
        }



        private void SetX(int value)
        {
            if (value == _x)
                return;

            _x = Math.Clamp(value, MinX, MaxX);

            OnPropertyChanged(nameof(X));
        }

        private void SetY(int value)
        {
            if (value == _y)
                return;

            _y = Math.Clamp(value, MinY, MaxY);

            OnPropertyChanged(nameof(Y));
        }

        private void SetWidth(int value)
        {
            if (value == _width)
                return;

            _width = Math.Clamp(value, 0, MaxX);

            OnPropertyChanged(nameof(Width));
        }

        private void SetHeight(int value)
        {
            if (value == _height)
                return;

            _height = Math.Clamp(value, 0, MaxY);

            OnPropertyChanged(nameof(Height));
        }
    }
}
