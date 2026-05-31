using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Transitions;
using static TgaBuilderLib.Transitions.TransitionHelper;

namespace TgaBuilderLib.ViewModel.Transitions;

/// <summary>
/// Child VM for the Brick transition edge coloring section.
/// Controls edge color, blend mode, edge width, and eyedropper mode.
/// </summary>
public class TransitionEdgeViewModel : ThrottledViewModelBase
{
    public TransitionEdgeViewModel(
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

    private Color _edgeColor = new Color(255, 255, 255, 128);
    private EdgeBlendMode _blendMode = EdgeBlendMode.Multiply;
    private int _edgeWidth = 1;
    private bool _isEyedropperMode;

    // =====================================================================
    // Properties
    // =====================================================================

    public Color EdgeColor
    {
        get => _edgeColor;
        set => SetPropertyTriggerRecalculation(ref _edgeColor, value);
    }

    public EdgeBlendMode BlendMode
    {
        get => _blendMode;
        set => SetPropertyTriggerRecalculation(ref _blendMode, value);
    }

    public System.Array EdgeBlendModes => System.Enum.GetValues(typeof(EdgeBlendMode));

    public int EdgeWidth
    {
        get => _edgeWidth;
        set => SetPropertyTriggerRecalculation(ref _edgeWidth, value);
    }

    public bool IsEyedropperMode
    {
        get => _isEyedropperMode;
        set => SetCallerProperty(ref _isEyedropperMode, value);
    }

    // =====================================================================
    // ThrottledViewModelBase overrides
    // =====================================================================

    protected override bool DoPreProcessing()
    {
        _transitionHelper.CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresDrawing;
        _transitionHelper.EdgeColor = _edgeColor;
        _transitionHelper.BlendMode = _blendMode;
        _transitionHelper.EdgeWidth = _edgeWidth;
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
