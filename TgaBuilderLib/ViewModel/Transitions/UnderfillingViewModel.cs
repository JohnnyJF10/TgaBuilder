using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.ViewModel.Transitions;

/// <summary>
/// Child VM for the Brick transition underfilling section.
/// Controls underfilling pivot, reverse, and threshold.
/// </summary>
public class TransitionUnderfillingViewModel : ThrottledViewModelBase
{
    public TransitionUnderfillingViewModel(
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

    private float _underfillingPivot = 0.5f;
    private bool _reverseUnderfilling;
    private int _underfillingThreshold;

    // =====================================================================
    // Properties
    // =====================================================================

    public float UnderfillingPivot
    {
        get => _underfillingPivot;
        set => SetPropertyTriggerRecalculation(ref _underfillingPivot, value);
    }

    public bool ReverseUnderfilling
    {
        get => _reverseUnderfilling;
        set => SetPropertyTriggerRecalculation(ref _reverseUnderfilling, value);
    }

    public int UnderfillingThreshold
    {
        get => _underfillingThreshold;
        set => SetPropertyTriggerRecalculation(ref _underfillingThreshold, value);
    }

    // =====================================================================
    // ThrottledViewModelBase overrides
    // =====================================================================

    protected override bool DoPreProcessing()
    {
        _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresSelectionBuilding;
        _transitionHelper.UnderfillingPivot = _underfillingPivot;
        _transitionHelper.ReverseUnderfilling = _reverseUnderfilling;
        _transitionHelper.UnderfillingThreshold = _underfillingThreshold;
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
