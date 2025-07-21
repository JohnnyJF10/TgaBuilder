using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TgaBuilderLib.ViewModel
{
    public class PanelVisualSizeViewModel : ViewModelBase
    {
        private double _viewportWidth;
        private double _viewportHeight;

        public double ViewportWidth
        {
            get => _viewportWidth;
            set => SetVisualSize(ref _viewportWidth, value);
        }
        public double ViewportHeight
        {
            get => _viewportHeight;
            set => SetVisualSize(ref _viewportHeight, value);
        }

        protected void SetVisualSize(ref double field, double value, [CallerMemberName] string? propertyName = null)
        {
            if (value == 0 || string.IsNullOrEmpty(propertyName)) 
                return;

            field = value;
            OnPropertyChanged(propertyName);

            Debug.WriteLine($"{propertyName}: {value}");
        }
    }
}
