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
    private bool _isTileRotateMode;

    // Rotation applied per mouse-wheel notch, in degrees. The wheel gives discrete steps; the tile
    // can still reach any angle by accumulating notches.
    private const float RotationStepDegrees = 5f;

    // Degrees of rotation applied per pixel of horizontal drag in the dedicated rotate-only mode.
    private const float RotateDragDegreesPerPixel = 1f;

    // Drag/rotate target state. _activeManipulationLabel is the tile the user grabbed or last set
    // visible (the wheel/rotate target); the rest anchor a drag to the tile's offset/angle at grab
    // time.
    private int _activeManipulationLabel;
    private int _dragStartX;
    private int _dragStartY;
    private int _dragBaseOffsetX;
    private int _dragBaseOffsetY;
    private float _dragBaseTwist;


    public bool IsExplicitTileVisibilityDrawMode
    {
        get => _isExplicitTileVisibilityDrawMode;
        set
        {
            SetProperty(ref _isExplicitTileVisibilityDrawMode, value, nameof(IsExplicitTileVisibilityDrawMode));
            OnPropertyChanged(nameof(IsAnyManualMode));
            OnPropertyChanged(nameof(IsWheelRotationMode));

            if (value)
                DisableManualModesExcept(nameof(IsExplicitTileVisibilityDrawMode));
        }
    }

    public bool IsExplicitTileVisibilityEraseMode
    {
        get => _isExplicitTileVisibilityEraseMode;
        set
        {
            SetProperty(ref _isExplicitTileVisibilityEraseMode, value, nameof(IsExplicitTileVisibilityEraseMode));
            OnPropertyChanged(nameof(IsAnyManualMode));
            OnPropertyChanged(nameof(IsWheelRotationMode));

            if (value)
                DisableManualModesExcept(nameof(IsExplicitTileVisibilityEraseMode));
        }
    }

    public bool IsTileMoveRotateMode
    {
        get => _isTileMoveRotateMode;
        set
        {
            SetProperty(ref _isTileMoveRotateMode, value, nameof(IsTileMoveRotateMode));
            OnPropertyChanged(nameof(IsAnyManualMode));
            OnPropertyChanged(nameof(IsWheelRotationMode));

            if (value)
                DisableManualModesExcept(nameof(IsTileMoveRotateMode));
            else
                EndActiveManipulation();
        }
    }

    public bool IsTileRotateMode
    {
        get => _isTileRotateMode;
        set
        {
            SetProperty(ref _isTileRotateMode, value, nameof(IsTileRotateMode));
            OnPropertyChanged(nameof(IsAnyManualMode));
            OnPropertyChanged(nameof(IsWheelRotationMode));

            if (value)
                DisableManualModesExcept(nameof(IsTileRotateMode));
            else
                EndActiveManipulation();
        }
    }

    // Turns off every manual mode except the named one and releases any active manipulation, so the
    // toggles stay mutually exclusive. Sets backing fields directly to avoid re-entering setters.
    private void DisableManualModesExcept(string keep)
    {
        if (keep != nameof(IsExplicitTileVisibilityDrawMode) && _isExplicitTileVisibilityDrawMode)
        {
            _isExplicitTileVisibilityDrawMode = false;
            OnPropertyChanged(nameof(IsExplicitTileVisibilityDrawMode));
        }
        if (keep != nameof(IsExplicitTileVisibilityEraseMode) && _isExplicitTileVisibilityEraseMode)
        {
            _isExplicitTileVisibilityEraseMode = false;
            OnPropertyChanged(nameof(IsExplicitTileVisibilityEraseMode));
        }
        if (keep != nameof(IsTileMoveRotateMode) && _isTileMoveRotateMode)
        {
            _isTileMoveRotateMode = false;
            OnPropertyChanged(nameof(IsTileMoveRotateMode));
        }
        if (keep != nameof(IsTileRotateMode) && _isTileRotateMode)
        {
            _isTileRotateMode = false;
            OnPropertyChanged(nameof(IsTileRotateMode));
        }

        EndActiveManipulation();
    }

    // True while any of the manual single-tile modes (draw / erase / move-rotate / rotate) is
    // active. The views use it to decide whether to show the hover indicator and route pointer
    // interactions.
    public bool IsAnyManualMode =>
        _isExplicitTileVisibilityDrawMode || _isExplicitTileVisibilityEraseMode
        || _isTileMoveRotateMode || _isTileRotateMode;

    // True in the modes where the mouse wheel rotates the active tile: the visibility pen (rotates
    // the latest tile set visible), move-rotate, and the dedicated rotate-only mode.
    public bool IsWheelRotationMode =>
        _isExplicitTileVisibilityDrawMode || _isTileMoveRotateMode || _isTileRotateMode;


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

        // In move/rotate modes only highlight tiles that can actually be grabbed (protected edge
        // tiles read as none), giving the user clear feedback about what is manipulable.
        int label = (_isTileMoveRotateMode || _isTileRotateMode)
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

        if (_isExplicitTileVisibilityDrawMode)
        {
            // The latest tile set visible with the pen becomes the wheel-rotation target and is
            // drawn on top of other manipulated tiles.
            _activeManipulationLabel = label;
            _transitionHelper.ActiveManipulatedTileLabel = label;
        }
        else if (_activeManipulationLabel == label)
        {
            // Erasing the active tile clears the rotation target (it is also reset to its original
            // position/rotation by the helper).
            _activeManipulationLabel = 0;
        }

        bool changed = _transitionHelper.SetExplicitTileVisibility(label, _isExplicitTileVisibilityDrawMode);

        _transitionHelper.CurrentBricksPipelineRequirements
        = BricksPipelineRequirements.RequiresSelectionBuilding;

        if (!changed)
            return;

        _ = TriggerRecalculation();
    }

    // Pointer pressed in the result image. Draw/erase modes toggle tile visibility; move and
    // rotate modes grab the tile under the cursor as the active manipulation/rotation target.
    private void ManualPointerDown(int x, int y)
    {
        if (_isTileMoveRotateMode)
        {
            BeginManipulation(x, y);
            return;
        }

        if (_isTileRotateMode)
        {
            BeginRotation(x, y);
            return;
        }

        if (_isExplicitTileVisibilityDrawMode || _isExplicitTileVisibilityEraseMode)
            SetExplicitTileVisibility(x, y);
    }

    // Pointer dragged (left button held). Draw/erase keep toggling along the path; move drags the
    // grabbed tile by the cursor delta; rotate spins it by the horizontal drag distance.
    private void ManualPointerDrag(int x, int y)
    {
        if (_isTileMoveRotateMode)
        {
            DragActiveTile(x, y);
            return;
        }

        if (_isTileRotateMode)
        {
            DragRotateActiveTile(x, y);
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

    private void BeginRotation(int x, int y)
    {
        int label = _transitionHelper.PickManipulableTileAt(x, y);

        _activeManipulationLabel = label;
        _transitionHelper.ActiveManipulatedTileLabel = label;

        if (label == 0)
            return;

        _dragBaseTwist = _transitionHelper.GetTileTwist(label);
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

    private void DragRotateActiveTile(int x, int y)
    {
        if (_activeManipulationLabel == 0)
            return;

        // Distance-based rotation: horizontal drag from the grab point maps to an absolute angle
        // (anchored to the tile's angle at grab time), so dragging back unwinds the rotation.
        float angle = _dragBaseTwist + (x - _dragStartX) * RotateDragDegreesPerPixel;

        bool changed = _transitionHelper.SetTileTwist(_activeManipulationLabel, angle);

        _transitionHelper.CurrentBricksPipelineRequirements
            = BricksPipelineRequirements.RequiresSelectionBuilding;

        if (changed)
            _ = TriggerRecalculation();
    }

    private void RotateActiveTile(int x, int y, int notches)
    {
        if (!IsWheelRotationMode || notches == 0)
            return;

        if (_activeManipulationLabel == 0)
        {
            // In move/rotate modes, adopt the tile under the cursor (scrolling without grabbing
            // first). In pen mode the wheel only rotates the latest tile set visible — no fallback.
            if (!_isTileMoveRotateMode && !_isTileRotateMode)
                return;

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
        IsTileRotateMode = false;
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
