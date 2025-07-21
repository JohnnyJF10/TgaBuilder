using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.FileHandling;
using TgaBuilderLib.UndoRedo;
using TgaBuilderLib.Utils;
using TgaBuilderLib.ViewModel.Elements;

namespace TgaBuilderLib.ViewModel
{
    public class TargetTexturePanelViewModel : TexturePanelViewModelBase
    {
        public TargetTexturePanelViewModel(
            ICursorSetter cursorSetter,
            IBitmapOperations bitmapOperations,
            IUndoRedoManager undoRedoManager,
            IEyeDropper eyeDropper,

            WriteableBitmap presenter,

            SelectionViewModel SelectionVM,
            AnimationViewModel AnimationVM,
            SingleSelectionShapeViewModel originalPosShapeVM,
            SingleSelectionShapeViewModel targetPosShapeVM,

            PickerViewModel pickerVM,
            AnimSelectShapeViewModel animSelectShapeVM,
            SelectionShapeViewModel selectionShapeVM) 
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
            _undoRedoManager = undoRedoManager;

            OriginalPosShape = originalPosShapeVM;
            TargetPosShape =   targetPosShapeVM;
        }

        internal TargetMode mode = TargetMode.Default;
        internal PlacingMode placingMode = PlacingMode.Default;

        private int _xGrid;
        private int _yGrid;

        private (int x, int y) _tileToShiftPos;
        private bool _isPreviewVisible;
        private bool _resizeSelectionToPicker;
        private double _zoom = 1.0;

        private IUndoRedoManager _undoRedoManager;


        public SingleSelectionShapeViewModel OriginalPosShape { get; set; }
        public SingleSelectionShapeViewModel TargetPosShape { get; set; }

        public override string PanelStatement 
            => $"{Presenter.PixelWidth} x {Presenter.PixelHeight} pixels " +
            $"({Presenter.PixelWidth / Picker.Size} x {Presenter.PixelHeight / Picker.Size} = " +
            $"{Presenter.PixelWidth / Picker.Size * Presenter.PixelHeight / Picker.Size} textures)";

        public override double Zoom
        {
            get => _zoom;
            set => SetZoom(value);
        }

        public bool IsPreviewVisible
        {
            get => _isPreviewVisible;
            set => SetPropertyPrimitive(ref _isPreviewVisible, value, nameof(IsPreviewVisible));
        }

        public bool ResizeSelectionToPicker
        {
            get => _resizeSelectionToPicker;
            set => SetPropertyPrimitive(ref _resizeSelectionToPicker, value, nameof(ResizeSelectionToPicker));
        }

        internal override bool CanScroll
            => Selection.IsPlacing || IsDragging || IsRightDragging || _eyeDropper.IsActive;




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

            RefreshPresenter();
            OnPresenterChanged();
            OnPropertyChanged(nameof(PanelStatement));
            Debug.WriteLine($"Presenter set to {bitmap.PixelWidth}x{bitmap.PixelHeight} pixels.");
        }

        public override void SetZoom(double zoom)
        {
            if (zoom == _zoom) 
                return;
            if (zoom <= 0) 
                zoom = 1;

            _zoom = zoom;

            SelectionShape.StrokeThickness = 2 / zoom;
            AnimSelectShape.StrokeThickness = 2 / zoom;
            Picker.StrokeThickness = 2 / zoom;
            OriginalPosShape.StrokeThickness = 4 / zoom;
            TargetPosShape.StrokeThickness = 4 / zoom;

            OnPropertyChanged(nameof(Zoom));
        }

        public override void MouseEnter()
        {
            if (_eyeDropper.IsActive)
            {
                _cursorSetter.SetEyedropperCursor();
                return;
            }

            if (WetherSelectionRequiresResize())
                Selection.Presenter = _bitmapOperations.ResizeScaled(Selection.Presenter, Picker.Size);

            if (mode is TargetMode.ClockwiseRotating or TargetMode.MirrorHorizontal or TargetMode.MirrorVertical)
                Selection.IsPlacing = false;

            if (Selection.IsPlacing)
                if (mode is TargetMode.Default)
                {
                    IsPreviewVisible = true;
                }
                else 
                {
                    OriginalPosShape.IsVisible = true;
                    TargetPosShape.IsVisible = true;
                }
            else
                Picker.IsVisible = true;

            OriginalPosShape.Size = Picker.Size;
            TargetPosShape.Size = Picker.Size;
        }

        public override void MouseLeave()
        {
            TerminateAllUserActions();
            IsPreviewVisible = false;
            OriginalPosShape.IsVisible = false;
            TargetPosShape.IsVisible = false;
        }

        public override void MouseMove(int x, int y)
        {
            Picker.X = x & ~(Picker.Size - 1);
            Picker.Y = y & ~(Picker.Size - 1);

            if (mode >= TargetMode.TileSwapping && Selection.IsPlacing)
            {
                TargetPosShape.X = Picker.X;
                TargetPosShape.Y = Picker.Y;
            }
        }

        public override void Drag(int x, int y)
        {
            if (mode != TargetMode.Default) return;

            if (!Selection.IsPlacing)
                SelectionShape.IsVisible = true;

            IsDragging = true;
            Picker.IsVisible = false;

            _xGrid = x & ~(Picker.Size - 1);
            _yGrid = y & ~(Picker.Size - 1);

            SetSelectionHorizontal(_xGrid);
            SetSelectionVertical(_yGrid);
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
            SelectionShape.IsVisible = false;
            switch (mode)
            {
                case TargetMode.Default:
                    if (Selection.IsPlacing)
                        PlaceTileAndUpdateView();
                    else SetSelection();
                    return;

                case TargetMode.ClockwiseRotating:
                    RotateTile();
                    return;

                case TargetMode.MirrorHorizontal:
                    FlipTileHorizontal();
                    return;

                case TargetMode.MirrorVertical:
                    FlipTileVertical();
                    return;

                case TargetMode.TileSwapping:
                    if (Selection.IsPlacing)
                        TileRally();
                    else SetTileToShiftPos();
                    return;

                case TargetMode.TileMoving:
                    if (Selection.IsPlacing)
                        TileSwitch();
                    else SetTileToShiftPos();
                    return;

                default: goto case TargetMode.Default;
            }
        }

        public override void RightDrag(int x, int y)
        {
            if (Selection.IsPlacing) 
            {
                Selection.IsPlacing = false;
                IsPreviewVisible = false; 
            }
            else
                ManageAnimSelectShape(x, y);

            Picker.X = x & ~(Picker.Size - 1);
            Picker.Y = y & ~(Picker.Size - 1);
        }

        public override void RightDragEnd()
        {
            if (!Selection.IsPlacing)
                SetupAnimation(); 

            EndPlacingStartPicking();
        }

        public override void DoubleDrag(int x, int y) => Drag(x, y);

        public override void DoubleDragEnd() => DragEnd();

        public override void AltMove(int x, int y) => MouseMove(x, y);

        public override void AltDrag(int x, int y) => Drag(x, y);

        internal void Undo()
        {
            if (_undoRedoManager.CanUndo)
                _undoRedoManager.Undo();
        }

        internal void Redo()
        {
            if (_undoRedoManager.CanRedo)
                _undoRedoManager.Redo();
        }

        internal override void SetSelection()
        {
            Selection.Presenter = _bitmapOperations.CropBitmap(
                source:     Presenter, 
                rectangle:  new Int32Rect(
                    x:      SelectionShape.X,
                    y:      SelectionShape.Y,
                    width:  SelectionShape.Width,
                    height: SelectionShape.Height));

            Selection.IsPlacing = true;
            EndPickingStartPlacing();
        }

        internal void RefreshPanelStatement() => OnPropertyChanged(nameof(PanelStatement));

        private void EndPickingStartPlacing()
        {
            if (mode == TargetMode.Default)
            {
                IsPreviewVisible = true;

                if (SelectionShape.Width != Picker.Size)
                    Picker.X = _xGrid;

                if (SelectionShape.Height != Picker.Size)
                    Picker.Y = _yGrid;
            }
            else
            {
                OriginalPosShape.IsVisible = true;
                TargetPosShape.IsVisible = true;

                OriginalPosShape.X = Picker.X;
                OriginalPosShape.Y = Picker.Y;

                TargetPosShape.X = Picker.X;
                TargetPosShape.Y = Picker.Y;
            }
            Selection.IsPlacing = true;
            Picker.IsVisible = false;
        }

        private void EndPlacingStartPicking()
        {
            IsPreviewVisible = false;
            Selection.IsPlacing = false;
            OriginalPosShape.IsVisible = false;
            TargetPosShape.IsVisible = false;
            Picker.IsVisible = true;
        }

        private void PlaceTileAndUpdateView()
        {
            int SelectionWidth = Selection.Presenter.PixelWidth;
            int SelectionHeight = Selection.Presenter.PixelHeight;

            if (Picker.X + SelectionWidth > Presenter.PixelWidth)
                SelectionWidth = Presenter.PixelWidth - Picker.X;

            if (Picker.Y + SelectionHeight > Presenter.PixelHeight)
                SelectionHeight = Presenter.PixelHeight - Picker.Y;

            int finalByteSize = SelectionWidth * SelectionHeight * 3;

            if ((placingMode & PlacingMode.PlaceAndSwap) == PlacingMode.PlaceAndSwap)
                _bitmapOperations.SwapBitmap = new WriteableBitmap(
                    pixelWidth:     SelectionWidth,
                    pixelHeight:    SelectionHeight,
                    dpiX:           96,
                    dpiY:           96,
                    pixelFormat:    PixelFormats.Rgb24,
                    palette:        null);
            else
                _bitmapOperations.SwapBitmap = null;

            if (_undoRedoManager.TryBeginRenting(2 * finalByteSize))
            {
                var undoPixels = _undoRedoManager.RentUndoRedoArray();
                var redoPixels = _undoRedoManager.RentUndoRedoArray();

                _bitmapOperations.FillRectBitmapMonitored(
                    source:         Selection.Presenter,
                    target:         Presenter,
                    pos:            (Picker.X, Picker.Y),
                    undoPixels:     undoPixels,
                    redoPixels:     redoPixels,
                    placingMode:    placingMode);

                _undoRedoManager.PushBitmapEditAction(
                    region: new Int32Rect(Picker.X, Picker.Y, 
                        SelectionWidth, SelectionHeight),
                    oldPixels:          undoPixels,
                    newPixels:          redoPixels,
                    placingCallback:    UndoRedoPlacing);
            }
            else
            {
                _bitmapOperations.FillRectBitmap(
                    source:         Selection.Presenter,
                    target:         Presenter,
                    pos:            (Picker.X, Picker.Y),
                    placingMode:    placingMode);

                _undoRedoManager.ClearAllOutOfMemory();
            }


            if (_bitmapOperations.SwapBitmap is not null)
                Selection.Presenter = _bitmapOperations.SwapBitmap;
            else
                EndPlacingStartPicking();
        }

        private void UndoRedoPlacing(Int32Rect rect, byte[] pixels)
        {
            _bitmapOperations.FillRectArray(
                bitmap: Presenter,
                rect:   rect,
                pixels: pixels);
        }



        public void ResizePresenter(int width, int height)
        {
            int oldWidth = Presenter.PixelWidth;
            int oldHeight = Presenter.PixelHeight;

            if (height < oldHeight || width < oldWidth)
            {
                int requiredBytes = height < oldHeight
                    ? width * (oldHeight - height) * 3
                    : (oldWidth - width) * height * 3;

                if (_undoRedoManager.TryBeginRenting(
                    totalSizeInBytes:   requiredBytes,
                    arraysNeeded:       1))
                {
                    var undoPixels = _undoRedoManager.RentUndoRedoArray();

                    Presenter = height < oldHeight 
                        ? _bitmapOperations.ResizeHeightMonitored(
                            sourceBitmap:   Presenter,
                            newHeight:      height,
                            undoData:       undoPixels)
                        : _bitmapOperations.ResizeWidthMonitored(
                            sourceBitmap:   Presenter,
                            newWidth:       width,
                            undoData:       undoPixels);

                    _undoRedoManager.PushResizeSmallerAction(
                        croppedPixels:          undoPixels,
                        oldWidth:               oldWidth,
                        newWidth:               width,
                        oldHeight:              oldHeight,
                        newHeight:              height,
                        resizeLargerCallback:   UndoRedoEnlargeandFillPresenter,
                        resizeSmallerCallback:  UndoRedoResizePresenter);
                }
                else
                {
                    Presenter = _bitmapOperations.Resize(
                        sourceBitmap: Presenter,
                        newWidth: width,
                        newHeight: height);

                    _undoRedoManager.ClearAllOutOfMemory();
                }
            }
            else
            {
                Presenter = _bitmapOperations.Resize(
                    sourceBitmap:   Presenter,
                    newWidth:       width,
                    newHeight:      height);

                _undoRedoManager.PushResizeLargerAction(
                    oldWidth:               oldWidth,
                    newWidth:               width,
                    oldHeight:              oldHeight,
                    newHeight:              height,
                    resizeLargerCallback:   UndoRedoEnlargeandFillPresenter,
                    resizeSmallerCallback:  UndoRedoResizePresenter);
            }
            UpdatePanelAfterResize();
        }

        private void UndoRedoEnlargeandFillPresenter(int width, int height, byte[] pixels)
        {
            Int32Rect fillRect;

            if (height > Presenter.PixelHeight)
            {
                if (pixels.Length < width * (height - Presenter.PixelHeight) * 3)
                    throw new ArgumentException(
                        "Pixels array length is too short for the expected size for the given bitmap dimensions.");

                fillRect = new Int32Rect(0, Presenter.PixelHeight, width, height - Presenter.PixelHeight);
            }
            else if (width > Presenter.PixelWidth)
            {
                if (pixels.Length < (width - Presenter.PixelWidth) * height * 3)
                    throw new ArgumentException(
                        "Pixels array length is too short for the expected size for the given bitmap dimensions.");

                fillRect = new Int32Rect(Presenter.PixelWidth, 0, width - Presenter.PixelWidth, height);
            }
            else
                throw new ArgumentException(
                    "Either one of the arguments must be greater than the current dimensions of the bitmap.");

            Presenter = _bitmapOperations.Resize(
                sourceBitmap:   Presenter,
                newWidth:       width,
                newHeight:      height);


            _bitmapOperations.FillRectArray(
                bitmap:     Presenter,
                rect:       fillRect,
                pixels:     pixels);

            UpdatePanelAfterResize();
        }

        private void UndoRedoEnlargeandFillPresenter(int width, int height, Color color)
        {
            Int32Rect fillRect;

            if (height > Presenter.PixelHeight)
                fillRect = new Int32Rect(0, Presenter.PixelHeight, width, height - Presenter.PixelHeight);
            else if (width > Presenter.PixelWidth)
                fillRect = new Int32Rect(Presenter.PixelWidth, 0, width - Presenter.PixelWidth, height);
            else
                throw new ArgumentException(
                    "Either one of the arguments must be greater than the current dimensions of the bitmap.");

            Presenter = _bitmapOperations.Resize(
                sourceBitmap:   Presenter,
                newWidth:       width,
                newHeight:      height);


            _bitmapOperations.FillRectColor(
                bitmap:     Presenter,
                rect:       fillRect,
                fillColor:  color);

            UpdatePanelAfterResize();
        }

        private void UndoRedoResizePresenter(int width, int height)
        {
            Presenter = _bitmapOperations.Resize(Presenter, width, height);

            UpdatePanelAfterResize();
        }

        public void ResizePresenterSorted(int width)
        {
            int oldWidth = Presenter.PixelWidth;
            int oldHeight = Presenter.PixelHeight;

            Presenter = _bitmapOperations.ResizeSorted(
                oldBitmap:  Presenter,
                newWidth:   width,
                tileSize:   Picker.Size);

            int newWidth = Presenter.PixelWidth;
            int newHeight = Presenter.PixelHeight;

            _undoRedoManager.PushResizeSortedAction(
                oldWidth:               oldWidth,
                newWidth:               newWidth,
                oldHeight:              oldHeight,
                newHeight:              newHeight,
                pickerSize:             Picker.Size,
                resizeSortedCallback:   UndoRedoResizeSorted);

            UpdatePanelAfterResize();
        }

        private void UndoRedoResizeSorted(int width, int height, int pickerSize)
        {
            Presenter = _bitmapOperations.ResizeSorted(Presenter, width, pickerSize, height);

            UpdatePanelAfterResize();
        }

        private void UpdatePanelAfterResize()
        {
            SelectionShape.MaxX = Presenter.PixelWidth;
            SelectionShape.MaxY = Presenter.PixelHeight;

            RefreshPresenter();
            OnPropertyChanged(nameof(PanelStatement));
        }

        private void RotateTile()
        {
            Int32Rect rect = new Int32Rect(Picker.X, Picker.Y, Picker.Size, Picker.Size);
            _bitmapOperations.RotateRec(Presenter, rect);

            _undoRedoManager.PushRegionRotateAction(
                rectangle:        rect,
                rotatingCallback: RotateUndoRedo);
        }

        private void RotateUndoRedo(Int32Rect rect, bool counterclockwise)
        {
            _bitmapOperations.RotateRec(Presenter, rect, counterclockwise);
        }

        private void FlipTileHorizontal()
        {
            var rect = new Int32Rect(Picker.X, Picker.Y, Picker.Size, Picker.Size);
            _bitmapOperations.FlipRectHor(Presenter, rect);

            _undoRedoManager.PushRegionFlipAction(
                rectangle:          rect,
                flippingCallback:   FlipHorizontalUndoRedo);
        }

        private void FlipHorizontalUndoRedo(Int32Rect rect)
        {
            _bitmapOperations.FlipRectHor(Presenter, rect);
        }

        private void FlipTileVertical()
        {
            Int32Rect rect = new Int32Rect(Picker.X, Picker.Y, Picker.Size, Picker.Size);
            _bitmapOperations.FlipRectVert(Presenter, rect);

            _undoRedoManager.PushRegionFlipAction(
                rectangle:          rect,
                flippingCallback:   FlipVerticalUndoRedo);
        }

        private void FlipVerticalUndoRedo(Int32Rect rect)
        {
            _bitmapOperations.FlipRectVert(Presenter, rect);
        }

        private void SetTileToShiftPos()
        {
            _tileToShiftPos = (Picker.X, Picker.Y);
            EndPickingStartPlacing();
        }

        private void TileRally()
        {
            _bitmapOperations.TileRally(
                Presenter,
                _tileToShiftPos, (Picker.X, Picker.Y), Picker.Size);

            _undoRedoManager.PushRegionMoveAction(
                origPos:        (OriginalPosShape.X, OriginalPosShape.Y),
                targetPos:      (TargetPosShape.X, TargetPosShape.Y),
                tileSize:       Picker.Size,
                movingCallback: TileRallyUndoRedo);

            EndPlacingStartPicking();
        }

        private void TileRallyUndoRedo((int X, int Y) origPos, (int X, int Y) targetPos, int tileSize)
        {
            _bitmapOperations.TileRally(
                Presenter,
                origPos, targetPos, tileSize);
        }

        private void TileSwitch()
        {
            _bitmapOperations.TileSwitch(
                Presenter,
                _tileToShiftPos, (Picker.X, Picker.Y), Picker.Size);

            _undoRedoManager.PushRegionMoveAction(
                origPos:        (OriginalPosShape.X, OriginalPosShape.Y),
                targetPos:      (TargetPosShape.X, TargetPosShape.Y),
                tileSize:       Picker.Size,
                movingCallback: TileSwitchUndoRedo);

            EndPlacingStartPicking();
        }

        private void TileSwitchUndoRedo((int X, int Y) origPos, (int X, int Y) targetPos, int tileSize)
        {
            _bitmapOperations.TileSwitch(
                Presenter,
                origPos, targetPos, tileSize);
        }

        private bool WetherSelectionRequiresResize() =>
            (placingMode & PlacingMode.ResizeToPicker) == PlacingMode.ResizeToPicker &&
                (Picker.Size != Selection.Presenter.PixelWidth ||
                Picker.Size != Selection.Presenter.PixelHeight);

        [Obsolete]
        private byte[] GetByteArrayFromWriteableBitmap(WriteableBitmap writeableBitmap)
        {
            int width = writeableBitmap.PixelWidth;
            int height = writeableBitmap.PixelHeight;
            int stride = width * (writeableBitmap.Format.BitsPerPixel / 8);
            byte[] pixelData = new byte[height * stride];

            writeableBitmap.CopyPixels(pixelData, stride, 0);

            return pixelData;
        }
    }
}
