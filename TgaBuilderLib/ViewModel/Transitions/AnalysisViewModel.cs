using System.ComponentModel;
using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.ViewModel;

public class AnalysisViewModel : ViewModelBase
{
    private readonly TransitionViewModel _transitionViewModel;

    public AnalysisViewModel(TransitionViewModel transitionViewModel)
    {
        _transitionViewModel = transitionViewModel;
        _transitionViewModel.PropertyChanged += TransitionViewModelOnPropertyChanged;
    }

    public bool InvertGrayscale
    {
        get => _transitionViewModel.InvertGrayscale;
        set => _transitionViewModel.InvertGrayscale = value;
    }

    public int MarkerCount
    {
        get => _transitionViewModel.MarkerCount;
        set => _transitionViewModel.MarkerCount = value;
    }

    public int MarkerRadius
    {
        get => _transitionViewModel.MarkerRadius;
        set => _transitionViewModel.MarkerRadius = value;
    }

    public float GridFitAngle
    {
        get => _transitionViewModel.GridFitAngle;
        set => _transitionViewModel.GridFitAngle = value;
    }

    public FilterType SelectedFilter
    {
        get => _transitionViewModel.SelectedFilter;
        set => _transitionViewModel.SelectedFilter = value;
    }

    public int SelectedFilterIndex
    {
        get => _transitionViewModel.SelectedFilterIndex;
        set => _transitionViewModel.SelectedFilterIndex = value;
    }

    public SegmentationMethod SelectedSegmentationMethod
    {
        get => _transitionViewModel.SelectedSegmentationMethod;
        set => _transitionViewModel.SelectedSegmentationMethod = value;
    }

    public int SelectedSegmentationMethodIndex
    {
        get => _transitionViewModel.SelectedSegmentationMethodIndex;
        set => _transitionViewModel.SelectedSegmentationMethodIndex = value;
    }

    public bool ShowFelzenszwalbParameters => _transitionViewModel.ShowFelzenszwalbParameters;
    public bool ShowSlicParameters => _transitionViewModel.ShowSlicParameters;
    public bool ShowQuickshiftParameters => _transitionViewModel.ShowQuickshiftParameters;
    public bool ShowBilateralSigma => _transitionViewModel.ShowBilateralSigma;
    public bool ShowGaussianSigma => _transitionViewModel.ShowGaussianSigma;
    public bool ShowWatershedBasedSegmentationInputs => _transitionViewModel.ShowWatershedBasedSegmentationInputs;
    public bool ShowGridFittingSegmentationInputs => _transitionViewModel.ShowGridFittingSegmentationInputs;
    public bool ShowGrayBasedSegmentationInputs => _transitionViewModel.ShowGrayBasedSegmentationInputs;

    public int FelzenszwalbMinSize
    {
        get => _transitionViewModel.FelzenszwalbMinSize;
        set => _transitionViewModel.FelzenszwalbMinSize = value;
    }

    public float FelzenszwalbScale
    {
        get => _transitionViewModel.FelzenszwalbScale;
        set => _transitionViewModel.FelzenszwalbScale = value;
    }

    public int SlicSegmentCount
    {
        get => _transitionViewModel.SlicSegmentCount;
        set => _transitionViewModel.SlicSegmentCount = value;
    }

    public float SlicCompactness
    {
        get => _transitionViewModel.SlicCompactness;
        set => _transitionViewModel.SlicCompactness = value;
    }

    public int QuickshiftMaxDist
    {
        get => _transitionViewModel.QuickshiftMaxDist;
        set => _transitionViewModel.QuickshiftMaxDist = value;
    }

    public float QuickshiftRatio
    {
        get => _transitionViewModel.QuickshiftRatio;
        set => _transitionViewModel.QuickshiftRatio = value;
    }

    public float BilateralSigma
    {
        get => _transitionViewModel.BilateralSigma;
        set => _transitionViewModel.BilateralSigma = value;
    }

    public float GaussianSigma
    {
        get => _transitionViewModel.GaussianSigma;
        set => _transitionViewModel.GaussianSigma = value;
    }

    private void TransitionViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(TransitionViewModel.InvertGrayscale):
            case nameof(TransitionViewModel.MarkerCount):
            case nameof(TransitionViewModel.MarkerRadius):
            case nameof(TransitionViewModel.GridFitAngle):
            case nameof(TransitionViewModel.SelectedFilter):
            case nameof(TransitionViewModel.SelectedFilterIndex):
            case nameof(TransitionViewModel.SelectedSegmentationMethod):
            case nameof(TransitionViewModel.SelectedSegmentationMethodIndex):
            case nameof(TransitionViewModel.ShowFelzenszwalbParameters):
            case nameof(TransitionViewModel.ShowSlicParameters):
            case nameof(TransitionViewModel.ShowQuickshiftParameters):
            case nameof(TransitionViewModel.ShowBilateralSigma):
            case nameof(TransitionViewModel.ShowGaussianSigma):
            case nameof(TransitionViewModel.ShowWatershedBasedSegmentationInputs):
            case nameof(TransitionViewModel.ShowGridFittingSegmentationInputs):
            case nameof(TransitionViewModel.ShowGrayBasedSegmentationInputs):
            case nameof(TransitionViewModel.FelzenszwalbMinSize):
            case nameof(TransitionViewModel.FelzenszwalbScale):
            case nameof(TransitionViewModel.SlicSegmentCount):
            case nameof(TransitionViewModel.SlicCompactness):
            case nameof(TransitionViewModel.QuickshiftMaxDist):
            case nameof(TransitionViewModel.QuickshiftRatio):
            case nameof(TransitionViewModel.BilateralSigma):
            case nameof(TransitionViewModel.GaussianSigma):
                OnPropertyChanged(e.PropertyName);
                break;
        }
    }
}
