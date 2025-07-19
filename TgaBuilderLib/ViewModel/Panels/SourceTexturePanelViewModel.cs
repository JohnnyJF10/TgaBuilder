using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.FileHandling;
using TgaBuilderLib.Utils;

namespace TgaBuilderLib.ViewModel
{
    public class SourceTexturePanelViewModel : TexturePanelViewModelBase
    {
        public SourceTexturePanelViewModel(
            ICursorSetter cursorSetter,
            IImageFileManager imageManager,
            IBitmapOperations bitmapOperations,
            IEyeDropper eyeDropper,

            Color initTransparentColor,
            WriteableBitmap presenter,

            PanelVisualSizeViewModel visualPanelSize,
            SelectionViewModel SelectionVM,
            AnimationViewModel AnimationVM,

            PickerViewModel pickerVM,
            AnimSelectShapeViewModel animSelectShapeVM,
            SelectionShapeViewModel selectionShapeVM,

            VisualGridViewModel visualGridVM)
            : base(
                  cursorSetter:         cursorSetter,
                  imageManager:         imageManager,
                  bitmapOperations:     bitmapOperations,
                  eyeDropper:           eyeDropper,

                  initTransparentColor: initTransparentColor,
                  presenter:            presenter,

                  visualPanelSize:      visualPanelSize,

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

        public override string PanelStatement
            => $"{Presenter.PixelWidth} x {Presenter.PixelHeight} pixels x {Presenter.Format.BitsPerPixel}";

        public override double Zoom
        {
            get => _zoom;
            set => SetZoom(value);
        }

        public int SelectedPickerSize
        {
            get => Picker.Size;
            set => SetSelectedPickerSize(value);
        }

        internal bool ReplaceColorEnabled { get; set; }

        internal override bool CanScroll 
            => IsDragging || IsRightDragging || _eyeDropper.IsActive;



        public override void SetPresenter(WriteableBitmap bitmap)
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
        }

        public override void SetZoom(double zoom)
        {
            if (zoom == _zoom)
                return;
            if (zoom <= 0)
                zoom = 1;

            _zoom = zoom;

            ThicknessUpdate(zoom);
            OnPropertyChanged(nameof(Zoom));
        }

        public void OpenFile(string filePath)
        {
            Presenter = _imageManager.OpenImageFile(filePath);

            SelectionShape.MaxX = Presenter.PixelWidth;
            SelectionShape.MaxY = Presenter.PixelHeight;

            RefreshPresenter();
            OnPropertyChanged(nameof(PanelStatement));
        }

        public override void MouseEnter() 
        {
            if (_eyeDropper.IsActive)
                _cursorSetter.SetEyedropperCursor();
        }

        public override void MouseLeave() => TerminateAllUserActions();

        public override void MouseMove(int x, int y)
        {
            if (VisualGrid.OffsetX > 0)
                x = Math.Clamp(x, VisualGrid.OffsetX, Presenter.PixelWidth - 2 * VisualGrid.CellSize + VisualGrid.OffsetX);
            if (VisualGrid.OffsetY > 0)
                y = Math.Clamp(y, VisualGrid.OffsetY, Presenter.PixelHeight - 2 * VisualGrid.CellSize + VisualGrid.OffsetY);

            Picker.IsVisible = true;
            Picker.X = (x - VisualGrid.OffsetX & ~(Picker.Size - 1)) + VisualGrid.OffsetX;
            Picker.Y = (y - VisualGrid.OffsetY & ~(Picker.Size - 1)) + VisualGrid.OffsetY;
        }

        public override void AltMove(int x, int y)
        {
            IsGridlessMode = true;
            Picker.IsVisible = true;
            Picker.X = x;
            Picker.Y = y;
        }

        public override void Drag(int x, int y)
        {
            int xGrid = ((x - VisualGrid.OffsetX) & ~(Picker.Size - 1)) + VisualGrid.OffsetX;
            int yGrid = ((y - VisualGrid.OffsetY) & ~(Picker.Size - 1)) + VisualGrid.OffsetY;

            IsDragging = true;
            Picker.IsVisible = false;
            SelectionShape.IsVisible = true;

            if (VisualGrid.OffsetX == 0)
                SetSelectionHorizontal(xGrid);
            else
                SetSelectionSizeWithOffsetHor(xGrid);

            if (VisualGrid.OffsetY == 0)
                SetSelectionVertical(yGrid);
            else
                SetSelectionSizeWithOffsetVer(yGrid);
        }

        public override void AltDrag(int x, int y)
        {
            IsDragging = true;

            SelectionShape.IsVisible = true;
            SetSelectionSizeGridless(x, y);
        }

        public override void DoubleDrag(int x, int y)
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
            VisualGrid.OffsetX = x % VisualGrid.CellSize;
            VisualGrid.OffsetY = y % VisualGrid.CellSize;
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

        public override void DoubleDragEnd()
        {
            _isGridDragging = false;
            VisualGrid.IsVisible = false;
        }

        public override void RightDrag(int x, int y) => ManageAnimSelectShape(x, y);

        public override void RightDragEnd() => SetupAnimation();

        internal override void SetSelection()
        {
            if (ReplaceColorEnabled)
                Selection.Presenter = _bitmapOperations.CropBitmap(
                    source:         Presenter,
                    rectangle:      new Int32Rect(SelectionShape.X, SelectionShape.Y, SelectionShape.Width, SelectionShape.Height),
                    replacedColor:  _eyeDropper.Color,
                    newColor:       Color.FromRgb(255, 0, 255));
            else
                Selection.Presenter = _bitmapOperations.CropBitmap(
                    source:     Presenter, 
                    rectangle:  new Int32Rect(SelectionShape.X, SelectionShape.Y, SelectionShape.Width, SelectionShape.Height));
            Selection.IsPlacing = true;
        }

        public void SetSelectedPickerSize(int value)
        {
            if (value == Picker.Size)
                return;

            if ((VisualGrid.OffsetX > 0 || VisualGrid.OffsetY > 0)
                && (NextHigherPowerOfTwo(value) >= Presenter.PixelWidth || NextHigherPowerOfTwo(value) >= Presenter.PixelHeight))
                return;

            Picker.Size = value;

            VisualGrid.CellSize = Picker.Size;

            OnCallerPropertyChanged();
        }


        public void PresenterColorReplace()
        {
            if (Presenter == null) return;
            var res = _bitmapOperations.ReplaceColor(Presenter, _eyeDropper.Color, Color.FromRgb(255, 0, 255));
            SetPresenter(res);
        }

        private void ThicknessUpdate(double value)
        {
            SelectionShape.StrokeThickness = 2 / value;
            AnimSelectShape.StrokeThickness = 2 / value;
            Picker.StrokeThickness = 2 / value;
            VisualGrid.StrokeThickness = 2 / value;
        }

        private bool SelectionOutOfBounds(int x, int y)
            => VisualGrid.OffsetX > 0 && 
            (x < VisualGrid.OffsetX || x >= Presenter.PixelWidth - VisualGrid.CellSize + VisualGrid.OffsetX) ||
            VisualGrid.OffsetY > 0 && 
            (y < VisualGrid.OffsetY || y >= Presenter.PixelHeight - VisualGrid.CellSize + VisualGrid.OffsetY);

        private bool SelectionOutOfBoundsX(int x)
            => VisualGrid.OffsetX > 0 &&
            (x < VisualGrid.OffsetX || x >= Presenter.PixelWidth - VisualGrid.CellSize + VisualGrid.OffsetX);

        private bool SelectionOutOfBoundsY(int y)
            => VisualGrid.OffsetY > 0 &&
            (y < VisualGrid.OffsetY || y >= Presenter.PixelHeight - VisualGrid.CellSize + VisualGrid.OffsetY);

        private void SetSelectionSizeGridless(int x, int y)
        {
            SelectionShape.Width = x - Picker.X > 0
                ? x - Picker.X
                : Picker.X - x;
            SelectionShape.X = (x - Picker.X > 0)
                ? Picker.X
                : Picker.X - SelectionShape.Width;

            SelectionShape.Height = y - Picker.Y > 0
                ? y - Picker.Y
                : Picker.Y - y;
            SelectionShape.Y = (y - Picker.Y > 0)
                ? Picker.Y
                : Picker.Y - SelectionShape.Height;
        }

        private void SetSelectionSizeWithOffsetHor(int x)
        {
            SelectionShape.Width = x - Picker.X > 0
                ? Math.Clamp(Picker.Size + x - Picker.X, 
                0, Presenter.PixelWidth - Picker.X - Picker.Size + VisualGrid.OffsetX)
                : Math.Clamp(Picker.Size + Picker.X - x, 
                0, Picker.X - VisualGrid.OffsetX + Picker.Size);

            SelectionShape.X = (x - Picker.X > 0)
                ? Math.Clamp(Picker.X, 
                VisualGrid.OffsetX, Presenter.PixelWidth - 2 * Picker.Size + VisualGrid.OffsetX)
                : Math.Clamp(Picker.Size + Picker.X - SelectionShape.Width, 
                VisualGrid.OffsetX, Presenter.PixelWidth - 2 * Picker.Size + VisualGrid.OffsetX);
        }

        private void SetSelectionSizeWithOffsetVer(int y)
        {
            SelectionShape.Height = y - Picker.Y > 0
                ? Math.Clamp(Picker.Size + y - Picker.Y,
                0, Presenter.PixelHeight - Picker.Y - Picker.Size + VisualGrid.OffsetY)
                : Math.Clamp(Picker.Size + Picker.Y - y,
                0, Picker.Y - VisualGrid.OffsetY + Picker.Size);

            SelectionShape.Y = (y - Picker.Y > 0)
                ? Math.Clamp(Picker.Y,
                VisualGrid.OffsetY, Presenter.PixelHeight - 2 * Picker.Size + VisualGrid.OffsetY)
                : Math.Clamp(Picker.Size + Picker.Y - SelectionShape.Height,
                VisualGrid.OffsetY, Presenter.PixelHeight - 2 * Picker.Size + VisualGrid.OffsetY);

        }
    }
}
