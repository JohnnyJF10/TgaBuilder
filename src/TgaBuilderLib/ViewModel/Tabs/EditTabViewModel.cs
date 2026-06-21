using TgaBuilderLib.Enums;

namespace TgaBuilderLib.ViewModel
{
    public class EditTabViewModel : ViewModelBase
    {
        public EditTabViewModel(TargetTexturePanelViewModel destination)
        {
            _destination = destination;
        }

        private TargetTexturePanelViewModel _destination;

        public TargetMode SelectedMode
        {
            get => _destination.mode;
            set
            {
                if (_destination.mode != value)
                {
                    _destination.mode = value;
                    _destination.IsPreviewVisible = false;
                    _destination.Selection.IsPlacing = false;
                    OnCallerPropertyChanged();
                }
            }
        }
    }
}
