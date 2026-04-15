using System;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.ViewModel;

public class SmoothTransitionViewModel : TransitionViewModelBase
{
    public SmoothTransitionViewModel(
        IMediaFactory mediaFactory,
        ITransitionHelper transitionHelper,
        MainViewModel mainViewModel)
        : base(mediaFactory, transitionHelper, mainViewModel)
    {
    }

    private float _blendHardnessValue = 0.5f;

    public float BlendHardnessValue
    {
        get => _blendHardnessValue;
        set => SetPropertyTriggerRecalculation(ref _blendHardnessValue, value);
    }

    protected override byte[] CreateMixedPixels(bool requiresAnalysis)
        => TransitionHelper.MixPixels(Pixels1, Pixels2);

    protected override void ConfigureTransitionHelperCore()
    {
        TransitionHelper.Hardness = _blendHardnessValue;
    }
}
