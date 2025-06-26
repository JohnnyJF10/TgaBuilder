using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Utils;
using Color = System.Windows.Media.Color;

namespace TgaBuilderLib.ViewModel
{
    public class AlphaTabViewModel : ViewModelBase
    {
        public AlphaTabViewModel(
            IBitmapOperations bitmapOperations,

            SelectionViewModel selection,
            SourceTexturePanelViewModel source,

            IEyeDropper eyeDropper, 
            Color initcolor)
        {
            _bitmapOperations = bitmapOperations;

            _selection = selection;
            _source = source;

            _eyeDropper = eyeDropper;
            _brushSource = new SolidColorBrush(initcolor);
            _brushTarget = new SolidColorBrush(initcolor);
        }

        private readonly IBitmapOperations _bitmapOperations;

        private SelectionViewModel _selection;
        private SourceTexturePanelViewModel _source;

        private bool _isEyedropperMode;
        private IEyeDropper _eyeDropper;

        private RelayCommand? _eyedropperCommand;
        private RelayCommand<SolidColorBrush>? selectionMonoColorFillCommand;
        private RelayCommand? _replaceSourceColorCommand;


        public bool IsEyedropperMode
        {
            get => _isEyedropperMode;
            set => SetProperty(ref _isEyedropperMode, value, nameof(IsEyedropperMode));
        }

        public bool IsReplaceSelectionColor
        {
            get => _source.ReplaceColorEnabled;
            set
            {
                _source.ReplaceColorEnabled = value;
                OnCallerPropertyChanged(nameof(IsReplaceSelectionColor));
            }
        }

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


        public ICommand EyeDropperCommand => _eyedropperCommand
            ??= new RelayCommand(StartColorPicking);

        public ICommand SelectionMonoColorFillCommand => selectionMonoColorFillCommand
            ??= new RelayCommand<SolidColorBrush>(SelectionMonoColorFill);

        public ICommand ReplaceSourceColorCommand => _replaceSourceColorCommand
            ??= new RelayCommand(_source.PresenterColorReplace);

        internal void StartColorPicking()
        {
            _eyeDropper.IsActive = true;
            IsEyedropperMode = true;
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
        }

        internal void SelectionMonoColorFill(SolidColorBrush brush)
        {
            Int32Rect rect = new(0, 0,
            _selection.Presenter.PixelWidth,
            _selection.Presenter.PixelHeight);

            _bitmapOperations.FillRectColor(
                _selection.Presenter, rect, brush.Color);

            _selection.IsPlacing = true;
        }
    }
}
