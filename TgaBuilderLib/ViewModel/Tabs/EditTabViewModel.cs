using System.Runtime.CompilerServices;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.ViewModel
{
    public class EditTabViewModel : ViewModelBase
    {
        public EditTabViewModel(TargetTexturePanelViewModel destination)
        {
            _destination = destination;
        }

        private readonly Dictionary<string, TargetMode> _keyValuePairs = new()
        {
            {
                nameof(RegularModeSelected),
                TargetMode.Default
            },            
            {
                nameof(RotateModeSelected),
                TargetMode.ClockwiseRotating
            },
            {
                nameof(MirrorHorizontalModeSelected),
                TargetMode.MirrorHorizontal 
            },
            {
                nameof(MirrorVerticalModeSelected),
                TargetMode.MirrorVertical 
            },
            {
                nameof(TileSwapModeSelected),
                TargetMode.TileSwapping 
            },
            {
                nameof(MoveModeSelected),
                TargetMode.TileMoving 
            }
        };

        private TargetTexturePanelViewModel _destination;

        public bool RegularModeSelected
        {
            get => CheckIfSelected();
            set => ApplySelection(value);
        }

        public bool RotateModeSelected
        {
            get => CheckIfSelected();
            set => ApplySelection(value);
        }
        
        public bool MirrorHorizontalModeSelected
        {
            get => CheckIfSelected();
            set => ApplySelection(value);
        }
        
        public bool MirrorVerticalModeSelected
        {
            get => CheckIfSelected();
            set => ApplySelection(value);
        }

        public bool TileSwapModeSelected
        {
            get => CheckIfSelected();
            set => ApplySelection(value);
        }

        public bool MoveModeSelected
        {
            get => CheckIfSelected();
            set => ApplySelection(value);
        }


        private void SetDestinationMode([CallerMemberName] string? propertyName = null)
        {
            if (_keyValuePairs.TryGetValue(propertyName ?? "", out TargetMode lMode))
                _destination.mode = lMode;
        }
        
        private string? GetSelectedMode() 
            => _keyValuePairs.FirstOrDefault(x => x.Value == _destination.mode).Key;
        
        private bool CheckIfSelected([CallerMemberName] string? propertyName = null)
        {
            if (_keyValuePairs.TryGetValue(propertyName ?? "", out TargetMode lMode))
                return _destination.mode == lMode;
            return false;
        }
        
        private void ApplySelection(bool value, [CallerMemberName] string? propertyName = null)
        {
            if (!value)
                return;

            string? oldSelected = GetSelectedMode();

            SetDestinationMode(propertyName);
            if (!String.IsNullOrEmpty(oldSelected))
                OnCallerPropertyChanged(oldSelected);

            _destination.IsPreviewVisible = false;
            _destination.Selection.IsPlacing = false;
            OnCallerPropertyChanged(propertyName);
        }
    }
}
