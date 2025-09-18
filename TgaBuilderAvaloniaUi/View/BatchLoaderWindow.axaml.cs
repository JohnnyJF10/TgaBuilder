using System.ComponentModel;
using TgaBuilderLib.Abstraction;
using Avalonia.Controls.PanAndZoom;
using TgaBuilderAvaloniaUi.Elements;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class BatchLoaderWindow : AsyncWindow
    {
        public BatchLoaderWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
        }
    }
}
