using System.Diagnostics;
using TgaBuilderLib.Enums;


namespace TgaBuilderLib.ViewModel
{
    public class PlacingTabViewModel : ViewModelBase
    {
        private readonly TargetTexturePanelViewModel _destination;

        public PlacingTabViewModel(TargetTexturePanelViewModel destination)
        {
            _destination = destination;
        }

        public bool OverlayTransparentModeSelected
        {
            get => _destination.placingMode.HasFlag(PlacingMode.OverlayTransparent);
            set => SetPlacingModeFlag(PlacingMode.OverlayTransparent, value, nameof(OverlayTransparentModeSelected));
        }

        public bool SwapAndPlaceModeSelected
        {
            get => _destination.placingMode.HasFlag(PlacingMode.PlaceAndSwap);
            set => SetPlacingModeFlag(PlacingMode.PlaceAndSwap, value, nameof(SwapAndPlaceModeSelected));
        }

        public bool ResizeToPickerModeSelected
        {
            get => _destination.placingMode.HasFlag(PlacingMode.ResizeToPicker);
            set => SetPlacingModeFlag(PlacingMode.ResizeToPicker, value, nameof(ResizeToPickerModeSelected));
        }

        public int PickerSize
        {
            get => _destination.Picker.Size;
            set => SetPickerSize(value);
        }



        private void SetPlacingModeFlag(PlacingMode modeFlag, bool enabled, string propertyName)
        {
            if (enabled)
                _destination.placingMode |= modeFlag;
            else
                _destination.placingMode &= ~modeFlag;

            OnPropertyChanged(propertyName);
            Debug.WriteLine($"Current Mode Flags: {_destination.placingMode}");
        }

        private void SetPickerSize(int value)
        {
            if (_destination.Picker.Size == value) 
                return;

            _destination.Picker.Size = value;
            OnPropertyChanged(nameof(PickerSize));

            _destination.RefreshPanelStatement();
        }
    }
}
