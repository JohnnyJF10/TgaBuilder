using System.Diagnostics;

namespace TgaBuilderLib.ViewModel
{
    public class PanelVisualSizeViewModel : ViewModelBase
    {
        private double _contentWidth;
        private double _contentHeight;
        private double _viewportWidth;
        private double _viewportHeight;

        public double ContentWidth
        {
            get => _contentWidth;
            set
            {
                if (value == 0) return;
                SetProperty(ref _contentWidth, value, nameof(ContentWidth));
                Debug.WriteLine($"ContentWidth: {value}");
            }
        }
        public double ContentHeight
        {
            get => _contentHeight;
            set
            {
                if (value == 0) return;
                SetProperty(ref _contentHeight, value, nameof(ContentHeight));
                Debug.WriteLine($"ContentHeight: {value}");
            }
        }
        public double ViewportWidth
        {
            get => _viewportWidth;
            set
            {
                if (value == 0) return;
                SetProperty(ref _viewportWidth, value, nameof(ViewportWidth));
                Debug.WriteLine($"ViewportWidth: {value}");
            }
        }
        public double ViewportHeight
        {
            get => _viewportHeight;
            set
            {
                if (value == 0) return;
                SetProperty(ref _viewportHeight, value, nameof(ViewportHeight));
                Debug.WriteLine($"ViewportHeight: {value}");
            }
        }
    }
}
