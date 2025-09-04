using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Messaging;
using TgaBuilderLib.Utils;

namespace TgaBuilderLib.ViewModel
{
    public class FormatTabViewModel : ViewModelBase
    {
        public FormatTabViewModel(
            SelectionViewModel selection,
            TexturePanelViewModelBase panel,

            IEyeDropper eyeDropper,

            IMessageBoxService? messageBoxService = null)
        {
            _messageBoxService = messageBoxService;

            _selection = selection;
            _panel = panel;

            _eyeDropper = eyeDropper;
            _colorSource = new Color(0, 0, 0, 0); // Default black
            _colorTarget = new Color(0, 0, 0, 0); // Default black

            _panel.PresenterChanged += (_, _) => OnFormatBooleanPropertiesChanged();
        }

        // Dependencies
        private readonly IMessageBoxService? _messageBoxService;
        private readonly SelectionViewModel _selection;
        private readonly TexturePanelViewModelBase _panel;
        private readonly IEyeDropper _eyeDropper;

        // Commands
        private RelayCommand? _eyedropperCommand;
        private RelayCommand<Color>? _selectionMonoColorFillCommand;
        private RelayCommand? _replaceSourceColorCommand;

        // Brushes
        private Color _colorSource;
        public Color ColorSource
        {
            get => _colorSource;
            set => SetProperty(ref _colorSource, value, nameof(ColorSource));
        }

        private Color _colorTarget;
        public Color ColorTarget
        {
            get => _colorTarget;
            set => SetProperty(ref _colorTarget, value, nameof(ColorTarget));
        }

        // Eyedropper mode
        private bool _isEyedropperMode;
        public bool IsEyedropperMode
        {
            get => _isEyedropperMode;
            set => SetProperty(ref _isEyedropperMode, value, nameof(IsEyedropperMode));
        }

        public ICommand EyeDropperCommand => _eyedropperCommand
            ??= new RelayCommand(StartColorPicking);

        public ICommand SelectionMonoColorFillCommand => _selectionMonoColorFillCommand
            ??= new RelayCommand<Color>(SelectionMonoColorFill);

        public ICommand ReplaceSourceColorCommand => _replaceSourceColorCommand
            ??= new RelayCommand(ReplaceColor);

        private void ReplaceColor()
        {
            var result = _messageBoxService?.ShowOkCancelMessageBox(
                "Replace Color",
                "Replacing a color will might result in loss of image info. " +
                "Are you sure you want to proceed?")
                .Result;

            if (result != false)
                _panel.ReplaceColor();
        }

        public event EventHandler? EyedroppingRequested;


        public bool IsReplaceSelectionColor
        {
            get => _panel.ReplaceColorEnabled;
            set
            {
                _panel.ReplaceColorEnabled = value;
                OnCallerPropertyChanged(nameof(IsReplaceSelectionColor));
            }
        }

        // Format management
        public bool IsBgra32
        {
            get => _panel.Presenter.HasAlpha;
            set => SetIsBgra32(value);
        }

        public bool IsRgb24 => !IsBgra32;

        private void SetIsBgra32(bool value)
        {
            if ((_panel.Presenter.HasAlpha) == value)
                return;

            if (value)
            {
                _panel.ConvertToBgra32();
            }
            else
            {
                var result = _messageBoxService?.ShowOkCancelMessageBox(
                    "Convert to Rgb24",
                    "Converting to Rgb24 will discard alpha channel information. " +
                    "Are you sure you want to proceed?")
                    .Result;

                if (result != false)
                    _panel.ConvertToRgb24();
            }

            OnFormatBooleanPropertiesChanged();
        }

        private void OnFormatBooleanPropertiesChanged()
        {
            OnPropertyChanged(nameof(IsBgra32));
            OnPropertyChanged(nameof(IsRgb24));
        }

        // Color picking operations
        internal void StartColorPicking()
        {
            _eyeDropper.IsActive = true;
            IsEyedropperMode = true;

            EyedroppingRequested?.Invoke(this, EventArgs.Empty);
        }

        internal void DoColorPicking()
        {
            ColorSource = _eyeDropper.Color;
        }

        internal void EndColorPicking()
        {
            _eyeDropper.IsActive = false;
            _eyeDropper.Color = ColorSource;
            IsEyedropperMode = false;

            _panel.EyedropperEnd();
        }

        internal void SelectionMonoColorFill(Color color)
            => _selection.FillSelection(_panel.Presenter, color);

    }
}
