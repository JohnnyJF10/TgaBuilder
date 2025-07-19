using System.Windows.Media;
using TgaBuilderLib.Commands;

namespace TgaBuilderLib.ViewModel
{
    public class VisualGridViewModel : ViewModelBase
    {
        public VisualGridViewModel( int cellSize = 64)
        {
            _cellSize = cellSize;
        }

        private int _offsetX;
        private int _offsetY;
        private int _cellSize;

        private double _strokeThickness;

        private bool _isVisible;
        private bool _gridVisibleSelected;
        private bool _gridDashedSelected;

        private DashStyle? mainDashStyle;

        private RelayCommand? _resetCommand;

        public int SourceWidth { get; set; } = 0;
        public int SourceHeight { get; set; } = 0;


        public int OffsetX
        {
            get => _offsetX;
            set => SetOffsetX(value);
        }

        public int OffsetY
        {
            get => _offsetY;
            set => SetOffsetY(value);
        }

        public int CellSize
        {
            get => _cellSize;
            set => SetCellSize(value);
        }

        public double StrokeThickness
        {
            get => _strokeThickness;
            set => SetProperty(ref _strokeThickness, value, nameof(StrokeThickness));
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetIsVisible(value);
        }

        public bool GridVisibleSelected
        {
            get => _gridVisibleSelected;
            set => SetGridVisibleSelected(value);
        }



        private void SetOffsetX(int value)
        {
            if (value == _offsetX) 
                return;

            int maxOffset = Math.Min(CellSize - 1, Math.Abs(SourceWidth - CellSize));

            _offsetX = Math.Clamp(value, 0, maxOffset);

            OnPropertyChanged(nameof(OffsetX));
        }

        private void SetOffsetY(int value)
        {
            if (value == _offsetY) return;

            int maxOffset = Math.Min(CellSize - 1, Math.Abs(SourceHeight - CellSize));

            _offsetY = Math.Clamp(value, 0, maxOffset);

            OnPropertyChanged(nameof(OffsetY));
        }

        private void SetCellSize(int value)
        {
            if (value == _cellSize) 
                return;

            _cellSize = value;

            if (_offsetX > _cellSize)
                OffsetX = _cellSize - 1;

            if (_offsetY > _cellSize)
                OffsetY = _cellSize - 1;

            OnPropertyChanged(nameof(CellSize));
        }


        private void SetIsVisible(bool value)
        {
            if (value == _isVisible)
                return;

            if (!value && _gridVisibleSelected)
                return;

            _isVisible = value;
            OnPropertyChanged(nameof(IsVisible));
        }

        private void SetGridVisibleSelected(bool value)
        {
            if (value == _gridVisibleSelected)
                return;

            _gridVisibleSelected = value;

            OnPropertyChanged(nameof(GridVisibleSelected));

            if (value)
                IsVisible = true;
            else
                IsVisible = false;
        }

        public DashStyle MainDashStyle 
        { 
            get => mainDashStyle ?? DashStyles.Solid; 
            set => SetProperty(ref mainDashStyle, value, nameof(MainDashStyle));
        }


        public bool GridDashedSelected
        {
            get => _gridDashedSelected;
            set
            {
                if (value == _gridDashedSelected)
                    return;

                _gridDashedSelected = value;

                OnPropertyChanged(nameof(GridDashedSelected));

                if (value)
                    MainDashStyle = DashStyles.Dot;
                else
                    MainDashStyle = DashStyles.Solid;
            }
        }

        public RelayCommand ResetCommand
            => _resetCommand ??= new RelayCommand(Reset);


        public void Reset()
        {
            OffsetX = 0;
            OffsetY = 0;
        }
    }
}
