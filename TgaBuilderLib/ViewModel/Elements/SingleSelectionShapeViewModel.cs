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
            set => SetX(value);
        }

        public int Y
        {
            get => _y;
            set => SetY(value);
        }

        public int Size
        {
            get => _size;
            set => SetSize(value);
        }

        public double StrokeThickness
        {
            get => _strokeThicknes;
            set => SetProperty(ref _strokeThicknes, value, nameof(StrokeThickness));
        }



        private void SetX(int value)
        {
            if (value == _x)
                return;

            _x = value;

            OnPropertyChanged(nameof(CenterX));
            OnPropertyChanged(nameof(X));
        }

        private void SetY(int value)
        {
            if (value == _y)
                return;

            _y = value;

            OnPropertyChanged(nameof(CenterY));
            OnPropertyChanged(nameof(Y));
        }

        private void SetSize(int value)
        {
            if (value == _size)
                return;
            _size = value;

            OnPropertyChanged(nameof(CenterX));
            OnPropertyChanged(nameof(CenterY));
            OnPropertyChanged(nameof(Size));
        }
    }
}
