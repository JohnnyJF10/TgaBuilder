using System;
using System.ComponentModel;
using static TgaBuilderLib.Transitions.TransitionHelper;

namespace TgaBuilderLib.ViewModel;

public class EdgeViewModel : ViewModelBase
{
    private readonly TransitionViewModel _transitionViewModel;

    public EdgeViewModel(TransitionViewModel transitionViewModel)
    {
        _transitionViewModel = transitionViewModel;
        _transitionViewModel.PropertyChanged += TransitionViewModelOnPropertyChanged;
    }

    public TransitionsPresentersViewModel TransitionsPresentersVM => _transitionViewModel.TransitionsPresentersVM;

    public EdgeBlendMode BlendMode
    {
        get => _transitionViewModel.BlendMode;
        set => _transitionViewModel.BlendMode = value;
    }

    public Array EdgeBlendModes => _transitionViewModel.EdgeBlendModes;

    public int EdgeWidth
    {
        get => _transitionViewModel.EdgeWidth;
        set => _transitionViewModel.EdgeWidth = value;
    }

    private void TransitionViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TransitionViewModel.BlendMode)
            || e.PropertyName == nameof(TransitionViewModel.EdgeWidth))
            OnPropertyChanged(e.PropertyName);
    }
}
