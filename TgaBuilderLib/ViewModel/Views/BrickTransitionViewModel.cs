using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.ViewModel;

public class BrickTransitionViewModel : TransitionViewModelBase
{
    public BrickTransitionViewModel(
        IMediaFactory mediaFactory,
        ITransitionHelper transitionHelper,
        MainViewModel mainViewModel)
        : base(mediaFactory, transitionHelper, mainViewModel)
    {
    }

    private IWriteableBitmap? _labelMapImage;
    private int _markerRadius = 3;
    private int _expectedRegionCount = -1;
    private bool _useExpectedRegionCount;
    private bool _reversePivot;
    private bool _isLabelMapExpanded;

    public IWriteableBitmap? LabelMapImage
    {
        get => _labelMapImage;
        set => SetCallerProperty(ref _labelMapImage, value);
    }

    public int MarkerRadius
    {
        get => _markerRadius;
        set => SetPropertyTriggerRecalculation(ref _markerRadius, value);
    }

    public int ExpectedRegionCount
    {
        get => _expectedRegionCount;
        set => SetPropertyTriggerRecalculation(ref _expectedRegionCount, value);
    }

    public bool UseExpectedRegionCount
    {
        get => _useExpectedRegionCount;
        set
        {
            if (SetCallerPropertyReturn(ref _useExpectedRegionCount, value))
            {
                if (!value)
                    ExpectedRegionCount = -1;
                else if (_expectedRegionCount < 0)
                    ExpectedRegionCount = 4;
            }
        }
    }

    public bool ReversePivot
    {
        get => _reversePivot;
        set => SetPropertyTriggerRecalculation(ref _reversePivot, value);
    }

    public bool IsLabelMapExpanded
    {
        get => _isLabelMapExpanded;
        set => SetCallerProperty(ref _isLabelMapExpanded, value);
    }

    protected override bool RequiresFullAnalysisOnPivotChange => false;

    protected override byte[] CreateMixedPixels(bool requiresAnalysis)
    {
        if (requiresAnalysis || TransitionHelper.LastAnalysisMap.Length == 0)
            TransitionHelper.AnalyzeTilesWatershed(Pixels1);

        return TransitionHelper.MixSmartTilesPixels(Pixels1, Pixels2);
    }

    protected override void ConfigureTransitionHelperCore()
    {
        TransitionHelper.ReversePivot = ReversePivot;
        TransitionHelper.MarkerRadius = MarkerRadius;
        TransitionHelper.ExpectedRegionCount = ExpectedRegionCount;
    }

    protected override void OnResultUpdated()
    {
        UpdateLabelMapImage();
    }

    private void UpdateLabelMapImage()
    {
        byte[] mapData = TransitionHelper.LastAnalysisMap;
        int mapW = TransitionHelper.LastAnalysisWidth;
        int mapH = TransitionHelper.LastAnalysisHeight;

        if (mapData.Length == 0 || mapW == 0 || mapH == 0)
            return;

        var labelBmp = MediaFactory.CreateEmptyBitmap(mapW, mapH, true);
        labelBmp.WritePixels(
            new PixelRect(0, 0, mapW, mapH),
            mapData,
            mapW * 4);

        LabelMapImage = labelBmp;
    }
}

