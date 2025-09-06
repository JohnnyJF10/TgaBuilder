using System.Diagnostics;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Utils;

namespace TgaBuilderLib.ViewModel
{
    public class SourceTexturePanelViewModel : TexturePanelViewModelBase
    {
        public SourceTexturePanelViewModel(
            ICursorSetter cursorSetter,
            IBitmapOperations bitmapOperations,
            IEyeDropper eyeDropper,

            IWriteableBitmap presenter,

            SelectionViewModel SelectionVM,
            AnimationViewModel AnimationVM,

            PickerViewModel pickerVM,
            AnimSelectShapeViewModel animSelectShapeVM,
            SelectionShapeViewModel selectionShapeVM,

            VisualGridViewModel visualGridVM)
            : base(
                  cursorSetter:         cursorSetter,
                  bitmapOperations:     bitmapOperations,
                  eyeDropper:           eyeDropper,

                  presenter:            presenter,

                  SelectionVM:          SelectionVM,
                  AnimationVM:          AnimationVM,

                  pickerVM:             pickerVM,
                  animSelectShapeVM:    animSelectShapeVM,
                  selectionShapeVM:     selectionShapeVM)
        {
            VisualGrid = visualGridVM;
        }
        
        private bool _isGridDragging;
        private double _zoom = 1.0;

        internal bool IsGridlessMode = false;

        public VisualGridViewModel VisualGrid { get; set; }

        public override string PanelInfo
            => $"{Presenter.PixelWidth} x {Presenter.PixelHeight}px, {(Presenter.HasAlpha ? 32 : 24)}bpp";

        public override string PanelHelp
            => $"Source Panel: Left: Select, Right: Animate, Alt: Free selecting, Double Left: Move Grid";

        public override double Zoom
        {
            get => _zoom;
            set => SetZoom(value);
        }

        internal override bool CanScroll 
            => IsDragging || IsRightDragging || _eyeDropper.IsActive;



        public override void SetPresenter(IWriteableBitmap bitmap)
        {
            int expectedWidth = (int)Math.Ceiling(bitmap.PixelWidth / 256.0) * 256;
            int expectedHeight = (int)Math.Ceiling(bitmap.PixelHeight / 256.0) * 256;

            if (bitmap.PixelHeight != expectedHeight || bitmap.PixelWidth != expectedWidth)
                throw new ArgumentException(
                    $"Presenter must be a multiple of 256 in both dimensions. " +
                    $"Current size: {bitmap.PixelWidth}x{bitmap.PixelHeight}, " +
                    $"Expected size: {expectedWidth}x{expectedHeight}.");

            SetProperty(ref _presenter, bitmap, nameof(Presenter));

            Picker.MaxSize = expectedWidth > expectedHeight ? expectedHeight : expectedWidth;
            SelectionShape.MaxX = expectedWidth;
            SelectionShape.MaxY = expectedHeight;
            AnimSelectShape.PanelWidth = expectedWidth;

            VisualGrid.OffsetX = 0;
            VisualGrid.OffsetY = 0;
            VisualGrid.SourceWidth = expectedWidth;
            VisualGrid.SourceHeight = expectedHeight;

            RefreshPresenter();
            OnPresenterChanged();
            Debug.WriteLine($"Presenter set to {bitmap.PixelWidth}x{bitmap.PixelHeight} pixels.");
        }

        public override void SetZoom(double zoom)
        {
            if (zoom == _zoom)
                return;
            if (zoom <= 0)
                zoom = 1;

            _zoom = zoom;

            ThicknessUpdate(zoom);
        }

        public override void MouseEnter() 
        {
            if (_eyeDropper.IsActive)
                _cursorSetter.SetEyedropperCursor();
        }

        public override void MouseLeave() => TerminateAllUserActions();

        public override void MouseMove()
        {
            int x = VisualGrid.OffsetX > 0
                ? Math.Clamp(XPointer, VisualGrid.OffsetX, Presenter.PixelWidth - 2 * VisualGrid.CellSize + VisualGrid.OffsetX) 
                : XPointer;

            int y = VisualGrid.OffsetY > 0
                ? Math.Clamp(YPointer, VisualGrid.OffsetY, Presenter.PixelHeight - 2 * VisualGrid.CellSize + VisualGrid.OffsetY)
                : YPointer;

            Picker.IsVisible = true;
            Picker.X = (x - VisualGrid.OffsetX & ~(Picker.Size - 1)) + VisualGrid.OffsetX;
            Picker.Y = (y - VisualGrid.OffsetY & ~(Picker.Size - 1)) + VisualGrid.OffsetY;
        }

        public override void AltMove()
        {
            IsGridlessMode = true;
            Picker.IsVisible = true;
            Picker.X = XPointer;
            Picker.Y = YPointer;
        }

        public override void Drag()
        {
            _xGrid = ((XPointer - VisualGrid.OffsetX) & ~(Picker.Size - 1)) + VisualGrid.OffsetX;
            _yGrid = ((YPointer - VisualGrid.OffsetY) & ~(Picker.Size - 1)) + VisualGrid.OffsetY;

            IsDragging = true;
            Picker.IsVisible = false;
            SelectionShape.IsVisible = true;

            if (VisualGrid.OffsetX == 0)
                SetSelectionHorizontal();
            else
                SetSelectionSizeWithOffsetHor();

            if (VisualGrid.OffsetY == 0)
                SetSelectionVertical();
            else
                SetSelectionSizeWithOffsetVer();
        }

        public override void AltDrag()
        {
            IsDragging = true;

            SelectionShape.IsVisible = true;
            SetSelectionSizeGridless();
        }

        public override void DoubleDrag()
        {
            if (Picker.Size >= Presenter.PixelHeight || Picker.Size >= Presenter.PixelWidth)
                return;

            if (!_isGridDragging)
            {
                if (IsGridlessMode) return;

                _isGridDragging = true;
                IsDragging = false;

                Picker.IsVisible = false;
                SelectionShape.IsVisible = false;

                VisualGrid.CellSize = Picker.Size;
            }

            IsDragging = true;
            Picker.IsVisible = false;

            VisualGrid.IsVisible = true;
            VisualGrid.OffsetX = XPointer % VisualGrid.CellSize;
            VisualGrid.OffsetY = YPointer % VisualGrid.CellSize;
        }

        public override void DragEnd()
        {
            if (!IsDragging)
            {
                SelectionShape.X = Picker.X;
                SelectionShape.Y = Picker.Y;
                SelectionShape.Width = Picker.Size;
                SelectionShape.Height = Picker.Size;
            }
            IsDragging = false;
            IsGridlessMode = false;
            
            SelectionShape.IsVisible = false;
            if (SelectionShape.Width == 0 || SelectionShape.Height == 0) return;
            SetSelection();
        }

        public override void DragEndShift() => DragEnd();

        public override void DragEndAlt() => DragEnd();

        public override void DoubleDragEnd()
        {
            _isGridDragging = false;
            IsDragging = false;
            VisualGrid.IsVisible = false;
        }

        public override void RightDrag() => ManageAnimSelectShape();

        public override void RightDragEnd() => SetupAnimation();

        public override void ReplaceColor() => ReplaceColorBase();

        public override void ConvertToBgra32() => ConvertToBgra32Base();

        public override void ConvertToRgb24() => ConvertToRgb24Base();

        internal override void SetSelection() => SetSelectionBase();

        internal override void SetSelectedPickerSize(int size)
        {
            if (size == Picker.Size)
                return;

            if ((VisualGrid.OffsetX > 0 || VisualGrid.OffsetY > 0)
                && (NextHigherPowerOfTwo(size) >= Presenter.PixelWidth || NextHigherPowerOfTwo(size) >= Presenter.PixelHeight))
                return;

            Picker.Size = size;
            Picker.X = (XPointer - VisualGrid.OffsetX & ~(Picker.Size - 1)) + VisualGrid.OffsetX;
            Picker.Y = (YPointer - VisualGrid.OffsetY & ~(Picker.Size - 1)) + VisualGrid.OffsetY;


            VisualGrid.CellSize = Picker.Size;

            OnPropertyChanged(nameof(SelectedPickerSize));
        }



        private void ThicknessUpdate(double value)
        {
            SelectionShape.StrokeThickness = 2 / value;
            AnimSelectShape.StrokeThickness = 2 / value;
            Picker.StrokeThickness = 2 / value;
            VisualGrid.StrokeThickness = 2 / value;
        }

        private void SetSelectionSizeGridless()
        {
            SelectionShape.Width = XPointer - Picker.X > 0
                ? XPointer - Picker.X
                : Picker.X - XPointer;
            SelectionShape.X = (XPointer - Picker.X > 0)
                ? Picker.X
                : Picker.X - SelectionShape.Width;

            SelectionShape.Height = YPointer - Picker.Y > 0
                ? YPointer - Picker.Y
                : Picker.Y - YPointer;
            SelectionShape.Y = (YPointer - Picker.Y > 0)
                ? Picker.Y
                : Picker.Y - SelectionShape.Height;
        }

        private void SetSelectionSizeWithOffsetHor()
        {
            SelectionShape.Width = _xGrid > Picker.X
                ? Math.Clamp(Picker.Size + _xGrid - Picker.X, 
                0, Presenter.PixelWidth - Picker.X - Picker.Size + VisualGrid.OffsetX)
                : Math.Clamp(Picker.Size + Picker.X - _xGrid, 
                0, Picker.X - VisualGrid.OffsetX + Picker.Size);

            SelectionShape.X = _xGrid > Picker.X
                ? Math.Clamp(Picker.X, 
                VisualGrid.OffsetX, Presenter.PixelWidth - 2 * Picker.Size + VisualGrid.OffsetX)
                : Math.Clamp(Picker.Size + Picker.X - SelectionShape.Width, 
                VisualGrid.OffsetX, Presenter.PixelWidth - 2 * Picker.Size + VisualGrid.OffsetX);
        }

        private void SetSelectionSizeWithOffsetVer()
        {
            SelectionShape.Height = _yGrid > Picker.Y
                ? Math.Clamp(Picker.Size + _yGrid - Picker.Y,
                0, Presenter.PixelHeight - Picker.Y - Picker.Size + VisualGrid.OffsetY)
                : Math.Clamp(Picker.Size + Picker.Y - _yGrid,
                0, Picker.Y - VisualGrid.OffsetY + Picker.Size);

            SelectionShape.Y = _yGrid > Picker.Y
                ? Math.Clamp(Picker.Y,
                VisualGrid.OffsetY, Presenter.PixelHeight - 2 * Picker.Size + VisualGrid.OffsetY)
                : Math.Clamp(Picker.Size + Picker.Y - SelectionShape.Height,
                VisualGrid.OffsetY, Presenter.PixelHeight - 2 * Picker.Size + VisualGrid.OffsetY);

        }
    }
}
