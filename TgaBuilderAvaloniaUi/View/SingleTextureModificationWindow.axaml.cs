using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using TgaBuilderAvaloniaUi.Elements;
using TgaBuilderLib.ViewModel;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class SingleTextureModificationWindow : AsyncWindow
    {
        public SingleTextureModificationWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
        }

        [Obsolete("For designer use only")]
        public SingleTextureModificationWindow()
        {
            var serviceProvider = GlobalServiceProvider.Instance;

            var vm = serviceProvider.GetRequiredService<SingleTextureModificationViewModel>()
                ?? throw new InvalidOperationException("SingleTextureModificationViewModel not found in DI container");
            InitializeComponent();
            base.DataContext = vm;
        }

        protected override void OnClosing(Avalonia.Controls.WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is SingleTextureModificationViewModel vm)
                vm.MarkFinished();
        }
    }
}
