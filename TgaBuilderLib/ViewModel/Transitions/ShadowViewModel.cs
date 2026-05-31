using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.ViewModel.Transitions;

/// <summary>
/// Child VM for the Brick transition shadow section.
/// Controls shadow color, size, hardness, and eyedropper mode.
/// </summary>
public class TransitionShadowViewModel : ThrottledViewModelBase
{
    public TransitionShadowViewModel(
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

    private Color _shadowColor = new Color(42, 42, 42, 42);
    private int _shadowSize = 3;
    private int _shadowHardness = 50;
    private bool _isShadowEyedropperMode;

    // =====================================================================
    // Properties
    // =====================================================================

    public Color ShadowColor
    {
        get => _shadowColor;
        set => SetPropertyTriggerRecalculation(ref _shadowColor, value);
    }

    public int ShadowSize
    {
        get => _shadowSize;
        set => SetPropertyTriggerRecalculation(ref _shadowSize, value);
    }

    public int ShadowHardness
    {
        get => _shadowHardness;
        set => SetPropertyTriggerRecalculation(ref _shadowHardness, value);
    }

    public bool IsShadowEyedropperMode
    {
        get => _isShadowEyedropperMode;
        set => SetCallerProperty(ref _isShadowEyedropperMode, value);
    }

    // =====================================================================
    // ThrottledViewModelBase overrides
    // =====================================================================

    protected override bool DoPreProcessing()
    {
        _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresDrawing;
        _transitionHelper.ShadowColor = _shadowColor;
        _transitionHelper.ShadowSize = _shadowSize;
        _transitionHelper.ShadowHardness = _shadowHardness;
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
