using System;
using System.ComponentModel;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using TgaBuilderAvaloniaUi.Elements;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.ViewModel;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class SmoothTransitionWindow : AsyncWindow
    {
        public SmoothTransitionWindow(INotifyPropertyChanged viewModel, IVisualInvalidatorFactory? visualInvalidatorFactory = null)
        {
            InitializeComponent();
            base.DataContext = viewModel;
            InitializeVisualInvalidator(viewModel, visualInvalidatorFactory);
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

        private void InitializeVisualInvalidator(INotifyPropertyChanged viewModel, IVisualInvalidatorFactory? factory)
        {
            if (factory is not null && viewModel is SmoothTransitionViewModel vm)
                vm.VisualInvalidator = factory.Create(ResultImage);
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is SmoothTransitionViewModel vm)
                vm.MarkFinishedCommand.Execute(null);
        }
    }
}
