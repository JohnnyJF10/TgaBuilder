using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.ViewModel;

public class SmoothTransitionViewModel : TransitionViewModelBase
{
    public SmoothTransitionViewModel(
        IMediaFactory mediaFactory,
        ITransitionHelper transitionHelper,
        IBitmapOperations bitmapOperations,
        MainViewModel mainViewModel)
        : base(mediaFactory, transitionHelper, bitmapOperations, mainViewModel)
    {
    }

    private float _blendHardnessValue = 0.5f;


    public float BlendHardnessValue
    {
        get => _blendHardnessValue;
        set => SetPropertyTriggerRecalculation(ref _blendHardnessValue, value);
    }

    private float _wideningValue = 0f;

    public float WideningValue
    {
        get => _wideningValue;
        set => SetPropertyTriggerRecalculation(ref _wideningValue, value);
    }

    private float _tiltValue = 0f;

    public float TiltValue
    {
        get => _tiltValue;
        set => SetPropertyTriggerRecalculation(ref _tiltValue, value);
    }

    protected override byte[] CreateMixedPixels()
        => _transitionHelper.MixSmooth(Pixels1, Pixels2);

    protected override void ConfigureTransitionHelperCore()
    {
        _transitionHelper.Hardness = _blendHardnessValue;
        _transitionHelper.Widening = _wideningValue;
        _transitionHelper.Shift = _tiltValue;
    }
}
