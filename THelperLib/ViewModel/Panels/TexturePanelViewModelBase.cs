using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using THelperLib.Abstraction;
using THelperLib.BitmapOperations;
using THelperLib.FileHandling;
using THelperLib.Utils;

namespace THelperLib.ViewModel
{
    public abstract class TexturePanelViewModelBase : ViewModelBase
    {
        public TexturePanelViewModelBase(
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
            SelectionShapeViewModel selectionShapeVM
            )
        {
            _cursorSetter =     cursorSetter;
            _imageManager =     imageManager;
            _bitmapOperations = bitmapOperations;
            _eyeDropper =       eyeDropper;
            AlphaColor =        initTransparentColor;
            _presenter =        presenter;
            VisualPanelSize =   visualPanelSize;
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

        protected readonly IImageFileManager _imageManager;
        protected readonly IBitmapOperations _bitmapOperations;

        protected IEyeDropper _eyeDropper;

        protected WriteableBitmap _presenter;

        private double _observedWidth;
        private double _observedHeight;

        public bool IsDragging { get; set; }
        public bool IsRightDragging { get; set; }

        public PickerViewModel Picker { get; set; }
        public SelectionViewModel Selection { get; set; }
        public SelectionShapeViewModel SelectionShape { get; set; }
        public AnimSelectShapeViewModel AnimSelectShape { get; set; }
        public AnimationViewModel Animation { get; set; }
        public PanelVisualSizeViewModel VisualPanelSize { get; set; } 

        public WriteableBitmap Presenter
        {
            get => _presenter;
            set => SetPresenter(value);
        }
        public Color AlphaColor { get; set; }

        public abstract string PanelStatement { get; }
        public abstract double Zoom { get; set; }

        internal abstract bool CanScroll { get; }

        public abstract void SetPresenter(WriteableBitmap bitmap);

        public abstract void MouseEnter();
        public abstract void MouseLeave();

        public abstract void MouseMove(int x, int y);
        public abstract void Drag(int x, int y);
        public abstract void DragEnd();

        public abstract void RightDrag(int x, int y);
        public abstract void RightDragEnd();

        public abstract void DoubleDrag(int x, int y);
        public abstract void DoubleDragEnd();

        public abstract void AltMove(int x, int y);
        public abstract void AltDrag(int x, int y);

        internal abstract void SetSelection();

        public void EyedropperMove(int x, int y)
        {
            _eyeDropper.Color = _bitmapOperations.GetPixelBrush(_presenter, x, y);
        }

        public void EyedropperEnd()
        {
            _cursorSetter.SetDefaultCursor();
            AlphaColor = _eyeDropper.Color;
        }

        public void ManageAnimSelectShape(int x, int y)
        {
            int xGrid = x & ~(Picker.Size - 1);
            int yGrid = y & ~(Picker.Size - 1);

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
            _ = Animation.SetupAnimation(
                spriteSheet: Presenter,
                anchor1: (AnimSelectShape.InitialTexX, AnimSelectShape.InitialTexY),
                anchor2: (AnimSelectShape.CurrentTexX, AnimSelectShape.CurrentTexY),
                tileSize: Picker.Size
                );
        }

        public void RefreshPresenter()
        {
            Presenter.Lock();
            Presenter.AddDirtyRect(new Int32Rect(0, 0, Presenter.PixelWidth, Presenter.PixelHeight));
            Presenter.Unlock();
        }

        protected void SetSelectionHorizontal(int x)
        {
            SelectionShape.Width = x - Picker.X > 0
                ? Picker.Size + x - Picker.X
                : Picker.Size + Picker.X - x;
            SelectionShape.X = (x - Picker.X > 0)
                ? Picker.X
                : Picker.Size + Picker.X - SelectionShape.Width;
        }

        protected void SetSelectionVertical(int y)
        {
            SelectionShape.Height = y - Picker.Y > 0
                ? Picker.Size + y - Picker.Y
                : Picker.Size + Picker.Y - y;
            SelectionShape.Y = (y - Picker.Y > 0)
                ? Picker.Y
                : Picker.Size + Picker.Y - SelectionShape.Height;
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
