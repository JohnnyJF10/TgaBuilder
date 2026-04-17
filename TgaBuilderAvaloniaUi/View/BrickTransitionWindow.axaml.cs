using System;
using System.ComponentModel;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using TgaBuilderAvaloniaUi.Elements;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.ViewModel;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class BrickTransitionWindow : AsyncWindow
    {
        private ColumnDefinition? _labelMapColumn;

        public BrickTransitionWindow(INotifyPropertyChanged viewModel, IVisualInvalidatorFactory? visualInvalidatorFactory = null)
        {
            InitializeComponent();
            base.DataContext = viewModel;
            SubscribeToLabelMapExpanded(viewModel);
            InitializeVisualInvalidator(viewModel, visualInvalidatorFactory);
        }

        [Obsolete("For designer use only")]
        public BrickTransitionWindow()
        {
            var serviceProvider = GlobalServiceProvider.Instance;

            var vm = serviceProvider.GetRequiredService<BrickTransitionViewModel>()
                ?? throw new InvalidOperationException("BrickTransitionViewModel not found in DI container");
            InitializeComponent();
            base.DataContext = vm;
            SubscribeToLabelMapExpanded(vm);
        }

        private void InitializeVisualInvalidator(INotifyPropertyChanged viewModel, IVisualInvalidatorFactory? factory)
        {
            if (factory is not null && viewModel is BrickTransitionViewModel vm)
                vm.VisualInvalidator = factory.Create(ResultImage);
        }

        private void SubscribeToLabelMapExpanded(INotifyPropertyChanged viewModel)
        {
            viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(BrickTransitionViewModel.IsLabelMapExpanded))
                    UpdateLabelMapColumnWidth();
            };
        }

        private void UpdateLabelMapColumnWidth()
        {
            if (_labelMapColumn is null)
            {
                var grid = this.FindControl<Grid>("ImageAreaGrid");
                if (grid is not null && grid.ColumnDefinitions.Count > 6)
                    _labelMapColumn = grid.ColumnDefinitions[6];
            }

            if (_labelMapColumn is not null && DataContext is BrickTransitionViewModel vm)
                _labelMapColumn.Width = vm.IsLabelMapExpanded ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is BrickTransitionViewModel vm)
                vm.MarkFinishedCommand.Execute(null);
        }
    }
}
