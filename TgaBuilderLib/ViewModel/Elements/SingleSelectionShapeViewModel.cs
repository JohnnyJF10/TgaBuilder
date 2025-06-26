namespace TgaBuilderLib.ViewModel.Elements
{
    public class SingleSelectionShapeViewModel : ViewModelBase
    {
        public SingleSelectionShapeViewModel(int initSize)
        {
            _size = initSize;
        }

        private bool _isVisible;

        private int _x;
        private int _y;
        private int _size;
        private double _strokeThicknes = 1;


        public int CenterX => X + Size / 2;
        public int CenterY => Y + Size / 2;


        public bool IsVisible
        {
            get => _isVisible;
            set => SetPropertyPrimitive(ref _isVisible, value, nameof(IsVisible));
        }

        public int X
        {
            get => _x;
            set
            {
                if (value == _x) return;
                _x = value;
                OnPropertyChanged(nameof(CenterX));
                OnPropertyChanged(nameof(X));
            }
        }

        public int Y
        {
            get => _y;
            set
            {
                if (value == _y) return;
                _y = value;
                OnPropertyChanged(nameof(CenterY));
                OnPropertyChanged(nameof(Y));
            }
        }
        public int Size
        {
            get => _size;
            set
            {
                if (value == _size) return;
                _size = value;
                OnPropertyChanged(nameof(CenterX));
                OnPropertyChanged(nameof(CenterY));
                OnPropertyChanged(nameof(Size));
            }
        }
        public double StrokeThickness
        {
            get => _strokeThicknes;
            set => SetProperty(ref _strokeThicknes, value, nameof(StrokeThickness));
        }
    }
}
