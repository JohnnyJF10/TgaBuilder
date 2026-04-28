using System;
using System.ComponentModel;
using Avalonia.Controls;
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
            _collapsedWindowHeight = Height;
            _collapsedWindowMinHeight = MinHeight;
            SubscribeToExpanderState();
        }

        private void InitializeVisualInvalidator(INotifyPropertyChanged viewModel)
        {
            if (viewModel is BrickTransitionViewModel vm)
                vm.VisualInvalidator = new VisualInvalidator(ResultImage);
        }

        private void SubscribeToExpanderState()
        {
            // Use the generated field (x:Name="OptionsExpander") directly – always valid after InitializeComponent
            OptionsExpander.PropertyChanged += (_, e) =>
            {
                if (e.Property == Expander.IsExpandedProperty)
                    OnExpanderStateChanged(OptionsExpander.IsExpanded);
            };
        }

        private void OnExpanderStateChanged(bool isExpanded)
        {
            if (isExpanded)
            {
                _collapsedWindowHeight = Height;
                _collapsedWindowMinHeight = MinHeight;

                // Use LayoutUpdated to measure the content only after it has been laid out
                if (OptionsExpander.Content is Control content)
                {
                    void OnLayoutUpdated(object? sender, EventArgs args)
                    {
                        content.LayoutUpdated -= OnLayoutUpdated;
                        double extra = content.Bounds.Height;
                        if (extra > 0)
                        {
                            // Set MinHeight before Height so the resize is not clamped
                            MinHeight = _collapsedWindowMinHeight + extra;
                            Height = _collapsedWindowHeight + extra;
                        }
                    }
                    content.LayoutUpdated += OnLayoutUpdated;
                }
            }
            else
            {
                // Set MinHeight before Height so the window can actually shrink
                MinHeight = _collapsedWindowMinHeight;
                Height = _collapsedWindowHeight;
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
