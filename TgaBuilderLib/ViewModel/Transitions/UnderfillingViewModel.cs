using System.ComponentModel;

namespace TgaBuilderLib.ViewModel;

public class UnderfillingViewModel : ViewModelBase
{
    private readonly TransitionViewModel _transitionViewModel;

    public UnderfillingViewModel(TransitionViewModel transitionViewModel)
    {
        _transitionViewModel = transitionViewModel;
        _transitionViewModel.PropertyChanged += TransitionViewModelOnPropertyChanged;
    }

    public bool ReverseUnderfilling
    {
        get => _transitionViewModel.ReverseUnderfilling;
        set => _transitionViewModel.ReverseUnderfilling = value;
    }

    public int UnderfillingThreshold
    {
        get => _transitionViewModel.UnderfillingThreshold;
        set => _transitionViewModel.UnderfillingThreshold = value;
    }

    public float UnderfillingPivot
    {
        get => _transitionViewModel.UnderfillingPivot;
        set => _transitionViewModel.UnderfillingPivot = value;
    }

    private void TransitionViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TransitionViewModel.ReverseUnderfilling)
            || e.PropertyName == nameof(TransitionViewModel.UnderfillingThreshold)
            || e.PropertyName == nameof(TransitionViewModel.UnderfillingPivot))
            OnPropertyChanged(e.PropertyName);
    }
}
