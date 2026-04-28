using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using TgaBuilderAvaloniaUi.Elements;
using TgaBuilderAvaloniaUi.Services;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.ViewModel;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class BrickTransitionWindow : AsyncWindow
    {
        private double _collapsedWindowHeight;
        private double _collapsedWindowMinHeight;

        public BrickTransitionWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
            _collapsedWindowHeight = Height;
            _collapsedWindowMinHeight = MinHeight;
            InitializeVisualInvalidator(viewModel);
            SubscribeToExpanderState();
        }

        [Obsolete("For designer use only")]
        public BrickTransitionWindow()
        {
            var serviceProvider = GlobalServiceProvider.Instance;

            var vm = serviceProvider.GetRequiredService<BrickTransitionViewModel>()
                ?? throw new InvalidOperationException("BrickTransitionViewModel not found in DI container");
            InitializeComponent();
            base.DataContext = vm;
            SubscribeToExpanderState();
        }

        private void InitializeVisualInvalidator(INotifyPropertyChanged viewModel)
        {
            if (viewModel is BrickTransitionViewModel vm)
                vm.VisualInvalidator = new VisualInvalidator(ResultImage);
        }

        private void SubscribeToExpanderState()
        {
            var expander = this.FindControl<Expander>("OptionsExpander");
            if (expander is null) return;

            expander.PropertyChanged += (_, e) =>
            {
                if (e.Property == Expander.IsExpandedProperty)
                    OnExpanderStateChanged(expander.IsExpanded, expander);
            };
        }

        private void OnExpanderStateChanged(bool isExpanded, Expander expander)
        {
            if (isExpanded)
            {
                _collapsedWindowHeight = Height;
                _collapsedWindowMinHeight = MinHeight;

                Dispatcher.UIThread.Post(() =>
                {
                    if (expander.Content is Control content && content.Bounds.Height > 0)
                    {
                        double extra = content.Bounds.Height;
                        MinHeight = _collapsedWindowMinHeight + extra;
                        Height = _collapsedWindowHeight + extra;
                    }
                }, DispatcherPriority.Loaded);
            }
            else
            {
                Height = _collapsedWindowHeight;
                MinHeight = _collapsedWindowMinHeight;
            }
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is BrickTransitionViewModel vm)
                vm.MarkFinishedCommand.Execute(null);
        }
    }
}
