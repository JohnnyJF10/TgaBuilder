using System;
using System.ComponentModel;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using TgaBuilderAvaloniaUi.Elements;
using TgaBuilderLib.ViewModel;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class SmoothTransitionWindow : AsyncWindow
    {
        public SmoothTransitionWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
        }

        [Obsolete("For designer use only")]
        public SmoothTransitionWindow()
        {
            var serviceProvider = GlobalServiceProvider.Instance;

            var vm = serviceProvider.GetRequiredService<SmoothTransitionViewModel>()
                ?? throw new InvalidOperationException("SmoothTransitionViewModel not found in DI container");
            InitializeComponent();
            base.DataContext = vm;
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is SmoothTransitionViewModel vm)
                vm.MarkFinishedCommand.Execute(null);
        }
    }
}
