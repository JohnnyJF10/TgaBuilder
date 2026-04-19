using System.ComponentModel;
using System.Windows.Input;
using TgaBuilderLib.ViewModel;

namespace TgaBuilderLib.Abstraction
{
    /// <summary>
    /// Interface for view tab view models that control zoom and pan behavior
    /// for texture panels. Provides the common contract used by both
    /// writable (WPF) and read-only (Avalonia) implementations.
    /// </summary>
    public interface IViewTabViewModel : INotifyPropertyChanged
    {
        bool IsScrolling { get; set; }

        PanelVisualSizeViewModel VisualPanelSize { get; }

        double ContentActualWidth { get; }
        double ContentActualHeight { get; }

        double OffsetX { get; set; }
        double OffsetY { get; set; }
        double Zoom { get; set; }

        double MultipliedOffsetX { get; set; }
        double MultipliedOffsetY { get; set; }

        double HorizonatlMargin { get; set; }

        ICommand FillCommand { get; }
        ICommand FitCommand { get; }
        ICommand Zoom100Command { get; }
        ICommand ScrollCommand { get; }
        ICommand EndScrollCommand { get; }
        ICommand ZoomInCommand { get; }
        ICommand ZoomOutCommand { get; }

        Task DefferedFill();
        void Fill();
        void Fit();
        void Zoom100();
    }
}
