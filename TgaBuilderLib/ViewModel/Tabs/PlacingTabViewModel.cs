using System.Diagnostics;
using TgaBuilderLib.Abstraction;


namespace TgaBuilderLib.ViewModel
{
    public class PlacingTabViewModel : ViewModelBase
    {
        public PlacingTabViewModel(TargetTexturePanelViewModel destination)
        {
            _destination = destination;
        }

        private TargetTexturePanelViewModel _destination;

        public bool OverlayTransparentModeSelected
        {
            get => _destination.placingMode.HasFlag(PlacingMode.OverlayTransparent);
            set
            {
                if (value)
                    _destination.placingMode |= PlacingMode.OverlayTransparent;
                else
                    _destination.placingMode &= ~PlacingMode.OverlayTransparent;
                OnPropertyChanged(nameof(OverlayTransparentModeSelected));
                Debug.WriteLine($"Current Mode Flags: {_destination.placingMode}");
            }
        }

        public bool SwapAndPlaceModeSelected
        {
            get => _destination.placingMode.HasFlag(PlacingMode.PlaceAndSwap);
            set
            {
                if (value)
                    _destination.placingMode |= PlacingMode.PlaceAndSwap;
                else
                    _destination.placingMode &= ~PlacingMode.PlaceAndSwap;
                OnPropertyChanged(nameof(SwapAndPlaceModeSelected));
                Debug.WriteLine($"Current Mode Flags: {_destination.placingMode}");
            }
        }

        public bool ResizeToPickerModeSelected
        {
            get => _destination.placingMode.HasFlag(PlacingMode.ResizeToPicker);
            set
            {
                if (value)
                    _destination.placingMode |= PlacingMode.ResizeToPicker;
                else
                    _destination.placingMode &= ~PlacingMode.ResizeToPicker;
                OnPropertyChanged(nameof(ResizeToPickerModeSelected));
                Debug.WriteLine($"Current Mode Flags: {_destination.placingMode}");
            }
        }


        public int PickerSize
        {
            get => _destination.Picker.Size;
            set
            {
                if (_destination.Picker.Size == value) return;
                _destination.Picker.Size = value;

                OnPropertyChanged(nameof(PickerSize));
                _destination.RefreshPanelStatement();
            }
        }
    }
}
