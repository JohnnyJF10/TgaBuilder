using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.ViewModel.Transitions;

/// <summary>
/// Child VM for the Brick transition manual draw/erase section.
/// Controls explicit tile visibility draw/erase modes.
/// </summary>
public class TransitionManualViewModel : ThrottledViewModelBase
{
    public TransitionManualViewModel(
        IMediaFactory mediaFactory,
        ITransitionHelper transitionHelper,
        TransitionPresentersViewModel presenters)
    {
        _mediaFactory = mediaFactory;
        _transitionHelper = transitionHelper;
        Presenters = presenters;
    }

    private readonly IMediaFactory _mediaFactory;
    private readonly ITransitionHelper _transitionHelper;
    public TransitionPresentersViewModel Presenters { get; }

    // =====================================================================
    // Fields
    // =====================================================================

    private bool _isExplicitTileVisibilityDrawMode;
    private bool _isExplicitTileVisibilityEraseMode;
    private bool _isLabelMapExpanded;

    // =====================================================================
    // Properties
    // =====================================================================

    public bool IsExplicitTileVisibilityDrawMode
    {
        get => _isExplicitTileVisibilityDrawMode;
        set
        {
            SetProperty(ref _isExplicitTileVisibilityDrawMode, value, nameof(IsExplicitTileVisibilityDrawMode));

            if (!value)
                return;

            _isExplicitTileVisibilityEraseMode = false;
            OnPropertyChanged(nameof(IsExplicitTileVisibilityEraseMode));
        }
    }

    public bool IsExplicitTileVisibilityEraseMode
    {
        get => _isExplicitTileVisibilityEraseMode;
        set
        {
            SetProperty(ref _isExplicitTileVisibilityEraseMode, value, nameof(IsExplicitTileVisibilityEraseMode));
            if (!value)
                return;

            _isExplicitTileVisibilityDrawMode = false;
            OnPropertyChanged(nameof(IsExplicitTileVisibilityDrawMode));
        }
    }

    public bool IsLabelMapExpanded
    {
        get => _isLabelMapExpanded;
        set => SetCallerProperty(ref _isLabelMapExpanded, value);
    }

    // =====================================================================
    // ThrottledViewModelBase overrides
    // =====================================================================

    protected override bool DoPreProcessing()
    {
        _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresSelectionBuilding;
        return true;
    }

    protected override void Recalculate()
    {
        var p1 = Presenters.Pixels1;
        var p2 = Presenters.Pixels2;
        if (p1 == null || p2 == null || p1.Length == 0 || p2.Length == 0)
            return;

        int w = Presenters.Image1.PixelWidth;
        int h = Presenters.Image1.PixelHeight;
        if (w == 0 || h == 0)
            return;

        if (w != Presenters.Image2.PixelWidth || h != Presenters.Image2.PixelHeight)
            return;

        _transitionHelper.Width = w;
        _transitionHelper.Height = h;

        byte[] result = Presenters.IsSmoothMode
            ? _transitionHelper.MixSmooth(p1, p2)
            : _transitionHelper.MixBricks(p1, p2);

        var bmp = _mediaFactory.CreateBitmapFromRaw(w, h, true, result, w * 4);
        Presenters.ResultImage = _mediaFactory.CloneBitmap(bmp);

        if (!Presenters.IsSmoothMode)
        {
            byte[] mapData = _transitionHelper.GetLabelMap();
            if (mapData.Length > 0)
            {
                var labelBmp = _mediaFactory.CreateEmptyBitmap(w, h, true);
                labelBmp.WritePixels(new PixelRect(0, 0, w, h), mapData, w * 4);
                Presenters.LabelMapImage = labelBmp;
            }
        }
    }
}
