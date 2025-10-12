using Avalonia.Controls.PanAndZoom;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Linq;
using TgaBuilderAvaloniaUi.Elements;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.ViewModel;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class BatchLoaderWindow : AsyncWindow
    {
        public BatchLoaderWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
        }

        [Obsolete("For designer use only")]
        public BatchLoaderWindow()
        {
            var serviceProvider = GlobalServiceProvider.Instance;

            var vm = serviceProvider.GetRequiredService<BatchLoaderViewModel>()
                ?? throw new InvalidOperationException("BatchLoaderViewModel not found in DI container");
            InitializeComponent();
            base.DataContext = vm;
        }
    }
}
