using System.ComponentModel;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.FileHandling;
using TgaBuilderLib.Utils;

namespace TgaBuilderLib.ViewModel
{
    public abstract class TexturePanelViewModelBase : ViewModelBase
    {
        public TexturePanelViewModelBase(
            ICursorSetter cursorSetter,
            IBitmapOperations bitmapOperations,
            IEyeDropper eyeDropper,

            IWriteableBitmap presenter,

            SelectionViewModel SelectionVM,
            AnimationViewModel AnimationVM,

            PickerViewModel pickerVM,
            AnimSelectShapeViewModel animSelectShapeVM,
            SelectionShapeViewModel selectionShapeVM
            )
        {
            _cursorSetter =     cursorSetter;
            _bitmapOperations = bitmapOperations;
            _eyeDropper =       eyeDropper;
            _presenter =        presenter;
            Selection =         SelectionVM;
            Animation =         AnimationVM;
            Picker =            pickerVM;
            AnimSelectShape =   animSelectShapeVM;
            SelectionShape =    selectionShapeVM;
        }

#if DEBUG
#pragma warning disable CS8618
        [EditorBrowsable(EditorBrowsableState.Never)]
        public TexturePanelViewModelBase() {} // For Designer Tooltip only.
#pragma warning restore CS8618 
#endif

        protected readonly ICursorSetter _cursorSetter;

        protected readonly IBitmapOperations _bitmapOperations;

        protected IEyeDropper _eyeDropper;

        protected IWriteableBitmap _presenter;

        protected int _xGrid;
        protected int _yGrid;

        public bool IsDragging { get; set; }
        public bool IsRightDragging { get; set; }

        public PickerViewModel Picker { get; set; }
        public SelectionViewModel Selection { get; set; }
        public SelectionShapeViewModel SelectionShape { get; set; }
        public AnimSelectShapeViewModel AnimSelectShape { get; set; }
        public AnimationViewModel Animation { get; set; }

        public IWriteableBitmap Presenter
        {
            get => _presenter;
            set => SetPresenter(value);
        }
        public Color AlphaColor { get; set; } = new Color(255, 0, 255);

        internal bool ReplaceColorEnabled { get; set; }


        public event EventHandler? PresenterChanged;

        public string PixelInfo => $"{XPointer}, {YPointer}px";

        public string TileInfo => SelectionShape.IsVisible 
            ? $"{SelectionShape.Width / Picker.Size}, {SelectionShape.Height / Picker.Size}"
            : $"{Picker.X / Picker.Size + 1}, {Picker.Y / Picker.Size + 1}";


        public abstract string PanelInfo { get; }
        public abstract string PanelHelp { get; }



        public abstract double Zoom { get; set; }

        internal abstract bool CanScroll { get; }

        public abstract void SetPresenter(IWriteableBitmap bitmap);
        public abstract void SetZoom(double zoom);

        public abstract void MouseEnter();
        public abstract void MouseLeave();

        public abstract void MouseMove();
        public abstract void Drag();
        public abstract void DragEnd();
        public abstract void DragEndShift();
        public abstract void DragEndAlt();

        public abstract void RightDrag();
        public abstract void RightDragEnd();

        public abstract void DoubleDrag();
        public abstract void DoubleDragEnd();

        public abstract void AltMove();
        public abstract void AltDrag();

        internal abstract void SetSelection();

        public abstract void ConvertToBgra32();
        public abstract void ConvertToRgb24();
        public abstract void ReplaceColor();

        internal abstract void SetSelectedPickerSize(int size);


        public int XPointer { get; set; }
        public int YPointer { get; set; }

        public void EyedropperMove()
        {
            _eyeDropper.Color = _bitmapOperations.GetPixelBrush(_presenter, XPointer, YPointer);
        }

        public void EyedropperEnd()
        {
            _cursorSetter.SetDefaultCursor();
            AlphaColor = _eyeDropper.Color;
        }

        public int SelectedPickerSize
        {
            get => Picker.Size;
            set => SetSelectedPickerSize(value);
        }


        public void ManageAnimSelectShape()
        {
            int xGrid = XPointer & ~(Picker.Size - 1);
            int yGrid = YPointer & ~(Picker.Size - 1);

            if (!IsRightDragging)
                AnimSelectShape.SetInitialsCoordinates(xGrid, yGrid);

            IsRightDragging = true;
            Picker.IsVisible = false;

            AnimSelectShape.SetShapeProperties(xGrid, yGrid, Picker.Size);
        }

        public void SetupAnimation()
        {
            if (!IsRightDragging)
                AnimSelectShape.SetShapePropertiesSingle(Picker.X, Picker.Y, Picker.Size);

            IsRightDragging = false;
            AnimSelectShape.IsVisible = false;
            Animation.SetupAnimation(
                spriteSheet: Presenter,
                anchor1: (AnimSelectShape.InitialTexX, AnimSelectShape.InitialTexY),
                anchor2: (AnimSelectShape.CurrentTexX, AnimSelectShape.CurrentTexY),
                tileSize: Picker.Size);
        }

        protected void OnPresenterChanged()
            => PresenterChanged?.Invoke(this, EventArgs.Empty);

        protected void SetSelectionBase()
        {
            if (ReplaceColorEnabled)
                Selection.Presenter = _bitmapOperations.CropBitmap(
                    source:         Presenter,
                    rectangle:      new PixelRect(SelectionShape.X, SelectionShape.Y, SelectionShape.Width, SelectionShape.Height),
                    replacedColor:  _eyeDropper.Color,
                    newColor:       Presenter.HasAlpha 
                                        ? new(0, 0, 0, 0) 
                                        : new(255, 0, 255));
            else
                Selection.Presenter = _bitmapOperations.CropBitmap(
                    source: Presenter,
                    rectangle: new PixelRect(SelectionShape.X, SelectionShape.Y, SelectionShape.Width, SelectionShape.Height));
            Selection.IsPlacing = true;
        }

        public void RefreshPresenter()
        {
            Presenter.Lock();
            Presenter.AddDirtyRect(new PixelRect(0, 0, Presenter.PixelWidth, Presenter.PixelHeight));
            Presenter.Unlock();
        }

        protected void SetSelectionHorizontal()
        {
            SelectionShape.Width = _xGrid > Picker.X
                ? Picker.Size + _xGrid - Picker.X
                : Picker.Size + Picker.X - _xGrid;
            SelectionShape.X = _xGrid > Picker.X
                ? Picker.X
                : Picker.Size + Picker.X - SelectionShape.Width;
        }

        protected void SetSelectionVertical()
        {
            SelectionShape.Height = _yGrid > Picker.Y
                ? Picker.Size + _yGrid - Picker.Y
                : Picker.Size + Picker.Y - _yGrid;
            SelectionShape.Y = _yGrid > Picker.Y
                ? Picker.Y
                : Picker.Size + Picker.Y - SelectionShape.Height;
        }

        protected void ReplaceColorBase()
        {
            if (Presenter == null) return;
            var res = _bitmapOperations.ReplaceColor(
                source:         Presenter,
                replacedColor:  _eyeDropper.Color,
                newColor:       Presenter.HasAlpha 
                                    ? new Color(0, 0, 0, 0) 
                                    : new Color(255, 0, 255));
            SetPresenter(res);
        }

        protected void ConvertToBgra32Base()
        {
            if (Presenter.HasAlpha)
                return;

            Presenter = _bitmapOperations.ConvertRGB24ToBGRA32(Presenter);

            OnPropertyChanged(nameof(Presenter));
            OnPropertyChanged(nameof(PanelHelp));
        }

        protected void ConvertToRgb24Base()
        {
            if (!Presenter.HasAlpha)
                return;

            Presenter = _bitmapOperations.ConvertBGRA32ToRGB24(Presenter);

            OnPropertyChanged(nameof(Presenter));
            OnPropertyChanged(nameof(PanelHelp));
        }

        protected void TerminateAllUserActions()
        {
            IsDragging = false;
            IsRightDragging = false;
            SelectionShape.IsVisible = false;
            Picker.IsVisible = false;
            AnimSelectShape.IsVisible = false;
            _cursorSetter.SetDefaultCursor();
        }

        protected int NextLowerPowerOfTwo(int n)
        {
            n |= (n >> 1);
            n |= (n >> 2);
            n |= (n >> 4);
            n |= (n >> 8);
            n |= (n >> 16);
            return n - (n >> 1);
        }

        protected int NextHigherPowerOfTwo(int n)
        {
            if (n < 1) return 1;
            n--;
            n |= (n >> 1);
            n |= (n >> 2);
            n |= (n >> 4);
            n |= (n >> 8);
            n |= (n >> 16);
            return n + 1;
        }

        protected long NextHigherPowerOfTwo(long l)
        {
            if (l < 1) return 1;
            l--;
            l |= (l >> 1);
            l |= (l >> 2);
            l |= (l >> 4);
            l |= (l >> 8);
            l |= (l >> 16);
            l |= (l >> 32);
            return l + 1;
        }
    }
}
