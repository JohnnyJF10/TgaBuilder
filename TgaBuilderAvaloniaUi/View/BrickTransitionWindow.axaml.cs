using Avalonia.Controls;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Reflection;
using TgaBuilderAvaloniaUi.Elements;
using TgaBuilderAvaloniaUi.Services;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.ViewModel;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class BrickTransitionWindow : AsyncWindow
    {
        private ColumnDefinition? _labelMapColumn;

        public BrickTransitionWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
            SubscribeToLabelMapExpanded(viewModel);
            InitializeVisualInvalidator(viewModel);
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

        private void InitializeVisualInvalidator(INotifyPropertyChanged viewModel)
        {
            if (viewModel is BrickTransitionViewModel vm)
                vm.VisualInvalidator = new VisualInvalidator(ResultImage);
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

        private void Image1_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            DoEyedropperMouseMove(Image1, e, 1);
        }

        private void Image1_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (DataContext is BrickTransitionViewModel vm && vm.IsEyedropperMode)
            {
                this.Cursor = CursorProvider.EyedropperCursor;
            }
        }

        private void DoEyedropperMouseMove(Image image, PointerEventArgs e, int imageNum)
        {
            if (DataContext is BrickTransitionViewModel vm && vm.IsEyedropperMode)
            {
                var position = e.GetPosition(image);
                vm.MouseOverCommand.Execute((X: (int)position.X, Y: (int)position.Y, imageNum));
            }
        }

        private void Image1_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (DataContext is BrickTransitionViewModel vm && vm.IsEyedropperMode)
            {
                this.Cursor = CursorProvider.DefaultCursor;
            }
        }

        private void Image1_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (DataContext is BrickTransitionViewModel vm && vm.IsEyedropperMode)
            {
                vm.IsEyedropperMode = false;
                this.Cursor = CursorProvider.DefaultCursor;
            }
        }

        private void Image2_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            DoEyedropperMouseMove(Image2, e, 2);
        }

        private void Image2_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (DataContext is BrickTransitionViewModel vm && vm.IsEyedropperMode)
            {
                this.Cursor = CursorProvider.EyedropperCursor;
            }
        }

        private void Image2_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (DataContext is BrickTransitionViewModel vm && vm.IsEyedropperMode)
            {
                this.Cursor = CursorProvider.DefaultCursor;
            }
        }

        private void Image2_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (DataContext is BrickTransitionViewModel vm && vm.IsEyedropperMode)
            {
                vm.IsEyedropperMode = false;
                this.Cursor = CursorProvider.DefaultCursor;
            }
        }
    }
}
