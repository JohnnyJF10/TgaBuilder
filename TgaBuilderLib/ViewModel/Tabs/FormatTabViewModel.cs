using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TgaBuilderLib.BitmapOperations;
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
            _brushSource = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)); // Default black
            _brushTarget = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)); // Default black

            _panel.PresenterChanged += (_, _) => OnFormatBooleanPropertiesChanged();
        }

        // Dependencies
        private readonly IMessageBoxService? _messageBoxService;
        private readonly SelectionViewModel _selection;
        private readonly TexturePanelViewModelBase _panel;
        private readonly IEyeDropper _eyeDropper;

        // Commands
        private RelayCommand? _eyedropperCommand;
        private RelayCommand<SolidColorBrush>? _selectionMonoColorFillCommand;
        private RelayCommand? _replaceSourceColorCommand;

        // Brushes
        private SolidColorBrush _brushSource;
        public SolidColorBrush BrushSource
        {
            get => _brushSource;
            set => SetProperty(ref _brushSource, value, nameof(BrushSource));
        }

        private SolidColorBrush _brushTarget;
        public SolidColorBrush BrushTarget
        {
            get => _brushTarget;
            set => SetProperty(ref _brushTarget, value, nameof(BrushTarget));
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
            ??= new RelayCommand<SolidColorBrush>(SelectionMonoColorFill);

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
            get => _panel.Presenter.Format == PixelFormats.Bgra32;
            set => SetIsBgra32(value);
        }

        public bool IsRgb24 => !IsBgra32;

        private void SetIsBgra32(bool value)
        {
            if ((_panel.Presenter.Format == PixelFormats.Bgra32) == value)
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
            BrushSource = new SolidColorBrush(_eyeDropper.Color);
        }

        internal void EndColorPicking()
        {
            _eyeDropper.IsActive = false;
            _eyeDropper.Color = BrushSource.Color;
            IsEyedropperMode = false;

            _panel.EyedropperEnd();
        }

        internal void SelectionMonoColorFill(SolidColorBrush brush)
            => _selection.FillSelection(_panel.Presenter, brush.Color);

    }
}
