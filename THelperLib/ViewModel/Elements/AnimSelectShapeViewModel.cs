namespace THelperLib.ViewModel
{
    public class AnimSelectShapeViewModel : ViewModelBase
    {
        public AnimSelectShapeViewModel() {}

        public AnimSelectShapeViewModel(int panelWidth, int stepSize)
        {
            PanelWidth = panelWidth;
            StepHeight = stepSize;
        }

        private int _width;
        private int _height;
        private int _stepPositionTop;
        private int _stepPositionBottom;
        private int _stepHeight;

        private bool _isVisible;
        private int _x;
        private int _y;
        private double _strokeThicknes = 1;

        public int PanelWidth { get; set; }

        public int InitialTexX { get; private set; }
        public int InitialTexY { get; private set; }

        public int CurrentTexX { get; private set; }
        public int CurrentTexY { get; private set; }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetPropertyPrimitive(ref _isVisible, value, nameof(IsVisible));
        }

        public int X
        {
            get => _x;
            set => SetPropertyPrimitive(ref _x, value, nameof(X));
        }

        public int Y
        {
            get => _y;
            set => SetPropertyPrimitive(ref _y, value, nameof(Y));
        }

        public int Width
        {
            get => _width;
            set => SetProperty(ref _width, value, nameof(Width));
        }

        public int Height
        {
            get => _height;
            set => SetProperty(ref _height, value, nameof(Height));
        }

        public int StepPositionTop
        {
            get => _stepPositionTop;
            set => SetProperty(ref _stepPositionTop, value, nameof(StepPositionTop));
        }

        public int StepPositionBottom
        {
            get => _stepPositionBottom;
            set => SetProperty(ref _stepPositionBottom, value, nameof(StepPositionBottom));
        }

        public int StepHeight
        {
            get => _stepHeight;
            set => SetProperty(ref _stepHeight, value, nameof(StepHeight));
        }

        public double StrokeThickness
        {
            get => _strokeThicknes;
            set => SetProperty(ref _strokeThicknes, value, nameof(StrokeThickness));
        }

        public void SetInitialsCoordinates(int x, int y)
        {
            InitialTexX = x;
            InitialTexY = y;
            Y = y;
        }

        public void SetShapeProperties(int x, int y, int stepHeight)
        {
            IsVisible = true;
            CurrentTexX = x;
            CurrentTexY = y;
            StepHeight = stepHeight;
            Width = PanelWidth;
            Height = y - InitialTexY + stepHeight;
            StepPositionTop = InitialTexX;
            StepPositionBottom = x + stepHeight;
        }

        internal void SetShapePropertiesSingle(int x, int y, int stepHeight)
        {
            IsVisible = true;
            InitialTexX = x;
            InitialTexY = y;
            CurrentTexX = x;
            CurrentTexY = y;
            StepHeight = stepHeight;
            Width = PanelWidth;
            Height = y - InitialTexY + stepHeight;
            StepPositionTop = InitialTexX;
            StepPositionBottom = x + stepHeight;
        }
    }
}
