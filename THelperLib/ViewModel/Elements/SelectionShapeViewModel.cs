using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using THelperLib.Commands;
using System.Diagnostics;

namespace THelperLib.ViewModel
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

        private DateTime _testTime = DateTime.Now;

        public int X
        {
            get => _x;
            set
            {
                if (value == _x) return;

                _x = Math.Clamp(value, MinX, MaxX);
                OnPropertyChanged(nameof(X));
            }
        }

        public int Y
        {
            get => _y;
            set
            {
                if (value == _y) return;

                _y = Math.Clamp(value, MinY, MaxY);
                OnPropertyChanged(nameof(Y));
            }
        }

        public int Width
        {
            get => _width;
            set
            {
                if (value == _width) return;
                _width = Math.Clamp(value, 0, MaxX);
                OnPropertyChanged(nameof(Width));
            }
        }

        public int Height
        {
            get => _height;
            set
            {
                if (value == _height) return;
                _height = Math.Clamp(value, 0, MaxY);
                OnPropertyChanged(nameof(Height));
            }
        }

        public double StrokeThickness
        {
            get => _strokeThickness;
            set => SetProperty(ref _strokeThickness, value, nameof(StrokeThickness));
        }
    }
}
