using System.Runtime.InteropServices;
using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.ViewModel;

public class TransitionOutViewModel : ThrottledViewModelBase
{
    public TransitionOutViewModel(
    IMediaFactory mediaFactory,
    ITransitionHelper transitionHelper,
    IBitmapOperations bitmapOperations)
    {
        _mediaFactory = mediaFactory;
        _transitionHelper = transitionHelper;
        _bitmapOperations = bitmapOperations;

        _resultImage = _mediaFactory.CreateEmptyBitmap(64, 64, true);

        _transitionHelper.RecalculationCompleted += OnRecalculationCompleted;
    }

    private const int TRANSITIONS_BPP = 4;

    private readonly IMediaFactory _mediaFactory;
    private readonly ITransitionHelper _transitionHelper;
    private readonly IBitmapOperations _bitmapOperations;

    // =====================================================================
    // Images and pixel buffers
    // =====================================================================

    private IWriteableBitmap _resultImage;

    private IWriteableBitmap? _labelMapImage;
    private IWriteableBitmap? _indicatorMapImage;

    private bool _isLabelMapExpanded;
    private bool _isIndicatorMapVisible;

    private bool _initTextVisible = true;

    public IVisualInvalidator? ResultInvalidator { get; set; }

    public IVisualInvalidator? LabelInvalidator { get; set; }



    public IWriteableBitmap ResultImage
    {
        get => _resultImage;
        set => SetCallerProperty(ref _resultImage, value);
    }

    public bool InitTextVisible
    {
        get => _initTextVisible;
        set => SetCallerProperty(ref _initTextVisible, value);
    }

    public bool IsLabelMapExpanded
    {
        get => _isLabelMapExpanded;
        set => SetCallerProperty(ref _isLabelMapExpanded, value);
    }

    public IWriteableBitmap? LabelMapImage
    {
        get => _labelMapImage;
        set => SetCallerProperty(ref _labelMapImage, value);
    }

    public IWriteableBitmap? IndicatorMapImage
    {
        get => _indicatorMapImage;
        set => SetCallerProperty(ref _indicatorMapImage, value);
    }

    public bool IsIndicatorMapVisible
    {
        get => _isIndicatorMapVisible;
        set => SetCallerProperty(ref _isIndicatorMapVisible, value);
    }




    // =====================================================================
    // User Mouse Interaction
    // =====================================================================


    private bool _isExplicitTileVisibilityDrawMode;
    private bool _isExplicitTileVisibilityEraseMode;
    private bool _isTileMoveRotateMode;

    // Rotation applied per mouse-wheel notch, in degrees. The wheel gives discrete steps; the tile
    // can still reach any angle by accumulating notches.
    private const float RotationStepDegrees = 5f;

    // Drag/rotate target state. _activeManipulationLabel is the tile the user grabbed (and the
    // wheel rotation target); the rest anchor a drag to the tile's offset at grab time.
    private int _activeManipulationLabel;
    private int _dragStartX;
    private int _dragStartY;
    private int _dragBaseOffsetX;
    private int _dragBaseOffsetY;


    public bool IsExplicitTileVisibilityDrawMode
    {
        get => _isExplicitTileVisibilityDrawMode;
        set
        {
            SetProperty(ref _isExplicitTileVisibilityDrawMode, value, nameof(IsExplicitTileVisibilityDrawMode));
            OnPropertyChanged(nameof(IsAnyManualMode));

            if (!value)
                return;

            _isExplicitTileVisibilityEraseMode = false;
            OnPropertyChanged(nameof(IsExplicitTileVisibilityEraseMode));

            _isTileMoveRotateMode = false;
            OnPropertyChanged(nameof(IsTileMoveRotateMode));

            EndActiveManipulation();
        }
    }

    public bool IsExplicitTileVisibilityEraseMode
    {
        get => _isExplicitTileVisibilityEraseMode;
        set
        {
            SetProperty(ref _isExplicitTileVisibilityEraseMode, value, nameof(IsExplicitTileVisibilityEraseMode));
            OnPropertyChanged(nameof(IsAnyManualMode));

            if (!value)
                return;

            _isExplicitTileVisibilityDrawMode = false;
            OnPropertyChanged(nameof(IsExplicitTileVisibilityDrawMode));

            _isTileMoveRotateMode = false;
            OnPropertyChanged(nameof(IsTileMoveRotateMode));

            EndActiveManipulation();
        }
    }

    public bool IsTileMoveRotateMode
    {
        get => _isTileMoveRotateMode;
        set
        {
            SetProperty(ref _isTileMoveRotateMode, value, nameof(IsTileMoveRotateMode));
            OnPropertyChanged(nameof(IsAnyManualMode));

            if (!value)
            {
                EndActiveManipulation();
                return;
            }

            _isExplicitTileVisibilityDrawMode = false;
            OnPropertyChanged(nameof(IsExplicitTileVisibilityDrawMode));

            _isExplicitTileVisibilityEraseMode = false;
            OnPropertyChanged(nameof(IsExplicitTileVisibilityEraseMode));
        }
    }

    // True while any of the manual single-tile modes (draw / erase / move-rotate) is active. The
    // views use it to decide whether to show the hover indicator and route pointer interactions.
    public bool IsAnyManualMode =>
        _isExplicitTileVisibilityDrawMode || _isExplicitTileVisibilityEraseMode || _isTileMoveRotateMode;


    // =====================================================================
    // Commands
    // =====================================================================

    private RelayCommand<(int, int)>? _setExplicitTileVisibilityCommand;
    private RelayCommand? _resetExplicitVisibilityCommand;
    private RelayCommand<(int, int)>? _requestLabelIndicatorCommand;
    private RelayCommand<(int, int)>? _manualPointerDownCommand;
    private RelayCommand<(int, int)>? _manualPointerDragCommand;
    private RelayCommand<(int, int, int)>? _rotateActiveTileCommand;
    private RelayCommand? _endManipulationCommand;


    public ICommand SetExplicitTileVisibilityCommand => _setExplicitTileVisibilityCommand
        ??= new RelayCommand<(int, int)>(args =>
        SetExplicitTileVisibility(args.Item1, args.Item2));
    public ICommand ResetExplicitVisibilityCommand => _resetExplicitVisibilityCommand
        ??= new RelayCommand(ResetAllExplicitTileVisibility);
    public ICommand RequestLabelIndicatorCommand => _requestLabelIndicatorCommand
        ??= new RelayCommand<(int, int)>(args => RequestNewIndicatorMapImage(args.Item1, args.Item2));

    // Routes a pointer press / drag in the result image to the active manual mode: visibility
    // draw/erase, or begin/continue a tile move.
    public ICommand ManualPointerDownCommand => _manualPointerDownCommand
        ??= new RelayCommand<(int, int)>(args => ManualPointerDown(args.Item1, args.Item2));
    public ICommand ManualPointerDragCommand => _manualPointerDragCommand
        ??= new RelayCommand<(int, int)>(args => ManualPointerDrag(args.Item1, args.Item2));
    // (x, y, notches) — rotates the active (or hovered) tile in move-rotate mode.
    public ICommand RotateActiveTileCommand => _rotateActiveTileCommand
        ??= new RelayCommand<(int, int, int)>(args => RotateActiveTile(args.Item1, args.Item2, args.Item3));
    public ICommand EndManipulationCommand => _endManipulationCommand
        ??= new RelayCommand(EndActiveManipulation);




    private void UpdateLabelMapImage()
    {
        if (ResultImage.PixelWidth <= 0 || ResultImage.PixelHeight <= 0)
            return;

        // mapData is the helper's reused label-map buffer (no per-call allocation).
        byte[] mapData = _transitionHelper.GetLabelMap();

        var labelBmp = LabelMapImage;
        if (labelBmp is null 
            || labelBmp.PixelWidth != ResultImage.PixelWidth 
            || labelBmp.PixelHeight != ResultImage.PixelHeight)
        {
            labelBmp = _mediaFactory.CreateEmptyBitmap(ResultImage.PixelWidth, ResultImage.PixelHeight, true);
            LabelMapImage = labelBmp;
        }

        using var frameBuffer = labelBmp.GetLocker(requiresRefresh: true);
        Marshal.Copy(mapData, 0, frameBuffer.BackBuffer, ResultImage.PixelWidth * ResultImage.PixelHeight * TRANSITIONS_BPP);

        LabelInvalidator?.InvalidateVisual();
    }

    private void RequestNewIndicatorMapImage(int x, int y)
    {
        if (ResultImage.PixelWidth <= 0 || ResultImage.PixelHeight <= 0)
            return;

        // In move-rotate mode only highlight tiles that can actually be grabbed (protected edge
        // tiles read as none), giving the user clear feedback about what is movable.
        int label = _isTileMoveRotateMode
            ? _transitionHelper.PickManipulableTileAt(x, y)
            : _transitionHelper.GetLabelAtPixel(x, y);

        if (label == 0)
            return;

        // mapData is the helper's reused label-map buffer (no per-call allocation).
        byte[] mapData = _transitionHelper.GetTileIndicator(label);

        if (mapData.Length == 0 || ResultImage.PixelWidth <= 0 || ResultImage.PixelHeight <= 0)
            return;

        var indicatorBmp = IndicatorMapImage;
        if (indicatorBmp is null 
            || indicatorBmp.PixelWidth != ResultImage.PixelWidth 
            || indicatorBmp.PixelHeight != ResultImage.PixelHeight)
        {
            indicatorBmp = _mediaFactory.CreateEmptyBitmap(ResultImage.PixelWidth, ResultImage.PixelHeight, true);
            IndicatorMapImage = indicatorBmp;
        }

        using var frameBuffer = indicatorBmp.GetLocker(requiresRefresh: true);
        Marshal.Copy(mapData, 0, frameBuffer.BackBuffer, ResultImage.PixelWidth * ResultImage.PixelHeight * TRANSITIONS_BPP);

        ResultInvalidator?.InvalidateVisual();
    }

    private void SetExplicitTileVisibility(int x, int y)
    {
        if (!IsExplicitTileVisibilityEraseMode && !IsExplicitTileVisibilityDrawMode)
            return;

        int label = _transitionHelper.GetLabelAtPixel(x, y);

        if (label == 0)
            return;

        bool visibilityChanged = _transitionHelper.SetExplicitTileVisibility(label, IsExplicitTileVisibilityDrawMode);

        _transitionHelper.CurrentBricksPipelineRequirements 
        = BricksPipelineRequirements.RequiresSelectionBuilding;

        if (!visibilityChanged)
            return;

        _ = TriggerRecalculation();
    }

    // Pointer pressed in the result image. Draw/erase modes toggle tile visibility; move-rotate
    // mode grabs the tile under the cursor as the active manipulation/rotation target.
    private void ManualPointerDown(int x, int y)
    {
        if (_isTileMoveRotateMode)
        {
            BeginManipulation(x, y);
            return;
        }

        if (_isExplicitTileVisibilityDrawMode || _isExplicitTileVisibilityEraseMode)
            SetExplicitTileVisibility(x, y);
    }

    // Pointer dragged (left button held). Draw/erase keep toggling along the path; move-rotate
    // drags the grabbed tile by the cursor delta.
    private void ManualPointerDrag(int x, int y)
    {
        if (_isTileMoveRotateMode)
        {
            DragActiveTile(x, y);
            return;
        }

        if (_isExplicitTileVisibilityDrawMode || _isExplicitTileVisibilityEraseMode)
            SetExplicitTileVisibility(x, y);
    }

    private void BeginManipulation(int x, int y)
    {
        int label = _transitionHelper.PickManipulableTileAt(x, y);

        _activeManipulationLabel = label;
        _transitionHelper.ActiveManipulatedTileLabel = label;

        if (label == 0)
            return;

        (_dragBaseOffsetX, _dragBaseOffsetY) = _transitionHelper.GetTileOffset(label);
        _dragStartX = x;
        _dragStartY = y;
    }

    private void DragActiveTile(int x, int y)
    {
        if (_activeManipulationLabel == 0)
            return;

        int offsetX = _dragBaseOffsetX + (x - _dragStartX);
        int offsetY = _dragBaseOffsetY + (y - _dragStartY);

        bool changed = _transitionHelper.MoveTile(_activeManipulationLabel, offsetX, offsetY);

        _transitionHelper.CurrentBricksPipelineRequirements
            = BricksPipelineRequirements.RequiresSelectionBuilding;

        if (changed)
            _ = TriggerRecalculation();
    }

    private void RotateActiveTile(int x, int y, int notches)
    {
        if (!_isTileMoveRotateMode || notches == 0)
            return;

        // Without a grabbed tile (scrolling over a tile without clicking first), adopt the tile
        // under the cursor as the active rotation target.
        if (_activeManipulationLabel == 0)
        {
            int label = _transitionHelper.PickManipulableTileAt(x, y);
            if (label == 0)
                return;

            _activeManipulationLabel = label;
            _transitionHelper.ActiveManipulatedTileLabel = label;
        }

        bool changed = _transitionHelper.RotateTileBy(_activeManipulationLabel, notches * RotationStepDegrees);

        _transitionHelper.CurrentBricksPipelineRequirements
            = BricksPipelineRequirements.RequiresSelectionBuilding;

        if (changed)
            _ = TriggerRecalculation();
    }

    private void EndActiveManipulation()
    {
        // Releases the wheel-rotation target. The helper keeps its ActiveManipulatedTileLabel so
        // the last-moved tile stays on top until another tile is grabbed or a reset occurs.
        _activeManipulationLabel = 0;
    }

    private void ResetAllExplicitTileVisibility()
    {
        _transitionHelper.ResetAllExplicitTileVisibility();
        EndActiveManipulation();

        _transitionHelper.CurrentBricksPipelineRequirements
            = BricksPipelineRequirements.RequiresSelectionBuilding;

        _ = TriggerRecalculation();
    }

    public void ResetImages()
    {
        InitTextVisible = true;
        _resultImage = _mediaFactory.CreateEmptyBitmap(64, 64, true);
        _labelMapImage = null;

        if (ResultInvalidator is not null)
            ResultInvalidator = null;

        if (LabelInvalidator is not null) 
            LabelInvalidator = null;
    }

    public void EndMouseInteraction()
    {
        IsLabelMapExpanded = false;
        IsExplicitTileVisibilityDrawMode = false;
        IsExplicitTileVisibilityEraseMode = false;
        IsTileMoveRotateMode = false;
        EndActiveManipulation();
    }



    private void OnRecalculationCompleted(object? sender, EventArgs e)
    {
        using var ResLockedFrameBuffer = ResultImage.GetLocker(requiresRefresh: true);

        Marshal.Copy(
            source: _transitionHelper.PixelsResult, 
            startIndex: 0, 
            destination: ResLockedFrameBuffer.BackBuffer, 
            length: _transitionHelper.PixelsResult.Length);

        ResultInvalidator?.InvalidateVisual();

        if (_transitionHelper.TypeOfTransition == TransitionType.Bricks)
            UpdateLabelMapImage();
    }

    protected override async Task TriggerRecalculation()
    {
        await _transitionHelper.QueueRecalc();
    }
}
