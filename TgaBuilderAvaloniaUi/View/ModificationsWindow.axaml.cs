using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using TgaBuilderAvaloniaUi.Elements;
using TgaBuilderLib.ViewModel;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class ModificationsWindow : AsyncWindow
    {
        public ModificationsWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
        }

        [Obsolete("For designer use only")]
        public ModificationsWindow()
        {
            var serviceProvider = GlobalServiceProvider.Instance;

            var vm = serviceProvider.GetRequiredService<ModificationsViewModel>()
                ?? throw new InvalidOperationException("ModificationsViewModel not found in DI container");
            InitializeComponent();
            base.DataContext = vm;
        }

        protected override void OnClosing(Avalonia.Controls.WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is ModificationsViewModel vm)
                vm.MarkFinished();
        }
    }
}
