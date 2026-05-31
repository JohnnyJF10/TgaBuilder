using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.ViewModel.Transitions;

/// <summary>
/// Child VM for the Smooth transition pivot/blend section.
/// Controls blend hardness, pivot, widening, shift.
/// </summary>
public class TransitionPivotSmoothViewModel : ThrottledViewModelBase
{
    public TransitionPivotSmoothViewModel(
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

    private TransitionMode _selectedTransitionMode = TransitionMode.Top;
    private float _pivotValue = 0.5f;
    private float _wideningValue = 0f;
    private float _shiftValue = 0f;
    private float _blendHardnessValue = 0.5f;

    // =====================================================================
    // Properties
    // =====================================================================

    public TransitionMode SelectedTransitionMode
    {
        get => _selectedTransitionMode;
        set => SetPropertyTriggerRecalculation(ref _selectedTransitionMode, value);
    }

    public float PivotValue
    {
        get => _pivotValue;
        set => SetPropertyTriggerRecalculation(ref _pivotValue, value);
    }

    public float WideningValue
    {
        get => _wideningValue;
        set => SetPropertyTriggerRecalculation(ref _wideningValue, value);
    }

    public float ShiftValue
    {
        get => _shiftValue;
        set => SetPropertyTriggerRecalculation(ref _shiftValue, value);
    }

    public float BlendHardnessValue
    {
        get => _blendHardnessValue;
        set => SetPropertyTriggerRecalculation(ref _blendHardnessValue, value);
    }

    // =====================================================================
    // ThrottledViewModelBase overrides
    // =====================================================================

    protected override bool DoPreProcessing()
    {
        _transitionHelper.Mode = _selectedTransitionMode;
        _transitionHelper.Pivot = _pivotValue;
        _transitionHelper.Widening = _wideningValue;
        _transitionHelper.Shift = _shiftValue;
        _transitionHelper.Hardness = _blendHardnessValue;
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
