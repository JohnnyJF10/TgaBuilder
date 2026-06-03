using System.ComponentModel;

namespace TgaBuilderLib.ViewModel;

public class ShadowViewModel : ViewModelBase
{
    private readonly TransitionViewModel _transitionViewModel;

    public ShadowViewModel(TransitionViewModel transitionViewModel)
    {
        _transitionViewModel = transitionViewModel;
        _transitionViewModel.PropertyChanged += TransitionViewModelOnPropertyChanged;
    }

    public TransitionsPresentersViewModel TransitionsPresentersVM => _transitionViewModel.TransitionsPresentersVM;

    public int ShadowSize
    {
        get => _transitionViewModel.ShadowSize;
        set => _transitionViewModel.ShadowSize = value;
    }

    public int ShadowHardness
    {
        get => _transitionViewModel.ShadowHardness;
        set => _transitionViewModel.ShadowHardness = value;
    }

    private void TransitionViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TransitionViewModel.ShadowSize)
            || e.PropertyName == nameof(TransitionViewModel.ShadowHardness))
            OnPropertyChanged(e.PropertyName);
    }
}
