namespace TgaBuilderLib.ViewModel
{
    public class PickerViewModel : ViewModelBase
    {
        public PickerViewModel(int initSize, int initMaxSize)
        {
            _size = initSize;
            _maxSize = initMaxSize;
        }

        private const int PICKER_MIN_SIZE = 8;
        private const int PICKER_MAX_SIZE = 512;

        private int _size;
        private int _maxSize;
        private bool _isVisible;
        private int _x;
        private int _y;
        private double _strokeThicknes = 1;

        internal int MaxSize
        {
            get => _maxSize;
            set => SetMaxSize(value);
        }

        public int Size
        {
            get => _size;
            set => SetSize(value);
        }

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

        public double StrokeThickness
        {
            get => _strokeThicknes;
            set => SetProperty(ref _strokeThicknes, value, nameof(StrokeThickness));
        }



        private void SetMaxSize(int value)
        {
            if (value == _maxSize) 
                return;

            if (value > PICKER_MAX_SIZE) 
                value = PICKER_MAX_SIZE;

            _maxSize = value;

            if (_size > _maxSize)
                Size = _maxSize;
        }

        private void SetSize(int value)
        {
            if (value == _size) return;

            if (value < _size)
                value = NextLowerPowerOfTwo(value);
            else
                value = NextHigherPowerOfTwo(value);
            _size = Math.Clamp(value, PICKER_MIN_SIZE, MaxSize);

            OnPropertyChanged(nameof(Size));
        }

        private int NextLowerPowerOfTwo(int n)
        {
            n |= (n >> 1);
            n |= (n >> 2);
            n |= (n >> 4);
            n |= (n >> 8);
            n |= (n >> 16);
            return n - (n >> 1);
        }

        private int NextHigherPowerOfTwo(int n)
        {
            if (n < 1) return 1;
            n--;
            n |= (n >> 1);
            n |= (n >> 2);
            n |= (n >> 4);
            n |= (n >> 8);
            n |= (n >> 16);
            return n + 1;
        }
    }
}
