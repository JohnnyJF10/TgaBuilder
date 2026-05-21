using Avalonia.Controls;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Windows.Input;
using TgaBuilderAvaloniaUi.Elements;
using TgaBuilderAvaloniaUi.Services;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.ViewModel;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class TransitionWindow : AsyncWindow
    {
        private ColumnDefinition? _labelMapColumn;

        public TransitionWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
            SubscribeToLabelMapExpanded(viewModel);
            InitializeVisualInvalidator(viewModel);
        }

        [Obsolete("For designer use only")]
        public TransitionWindow()
        {
            var serviceProvider = GlobalServiceProvider.Instance;

            var vm = serviceProvider.GetRequiredService<TransitionViewModel>()
                ?? throw new InvalidOperationException("TransitionViewModel not found in DI container");
            InitializeComponent();
            base.DataContext = vm;
            SubscribeToLabelMapExpanded(vm);
        }

        private void InitializeVisualInvalidator(INotifyPropertyChanged viewModel)
        {
            if (viewModel is TransitionViewModel vm)
                vm.VisualInvalidator = new VisualInvalidator(ResultImage);
        }

        private void SubscribeToLabelMapExpanded(INotifyPropertyChanged viewModel)
        {
            viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(TransitionViewModel.IsLabelMapExpanded))
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

            if (_labelMapColumn is not null && DataContext is TransitionViewModel vm)
                _labelMapColumn.Width = vm.IsLabelMapExpanded ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is TransitionViewModel vm)
                vm.MarkFinishedCommand.Execute(null);
        }

        private void Image1_PointerMoved(object? sender, PointerEventArgs e)
        {
            DoEyedropperMouseMove(Image1, e, 1);
        }

        private void Image1_PointerEntered(object? sender, PointerEventArgs e)
        {
            if (DataContext is TransitionViewModel vm && (vm.IsEyedropperMode || vm.IsShadowEyedropperMode))
                this.Cursor = CursorProvider.EyedropperCursor;
        }

        private void Image1_PointerExited(object? sender, PointerEventArgs e)
        {
            if (DataContext is TransitionViewModel vm && (vm.IsEyedropperMode || vm.IsShadowEyedropperMode))
                this.Cursor = CursorProvider.DefaultCursor;
        }

        private void Image1_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is TransitionViewModel vm && (vm.IsEyedropperMode || vm.IsShadowEyedropperMode))
            {
                vm.IsEyedropperMode = false;
                vm.IsShadowEyedropperMode = false;
                this.Cursor = CursorProvider.DefaultCursor;
            }
        }

        private void Image2_PointerMoved(object? sender, PointerEventArgs e)
        {
            DoEyedropperMouseMove(Image2, e, 2);
        }

        private void Image2_PointerEntered(object? sender, PointerEventArgs e)
        {
            if (DataContext is TransitionViewModel vm && (vm.IsEyedropperMode || vm.IsShadowEyedropperMode))
                this.Cursor = CursorProvider.EyedropperCursor;
        }

        private void Image2_PointerExited(object? sender, PointerEventArgs e)
        {
            if (DataContext is TransitionViewModel vm && (vm.IsEyedropperMode || vm.IsShadowEyedropperMode))
                this.Cursor = CursorProvider.DefaultCursor;
        }

        private void Image2_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is TransitionViewModel vm && (vm.IsEyedropperMode || vm.IsShadowEyedropperMode))
            {
                vm.IsEyedropperMode = false;
                vm.IsShadowEyedropperMode = false;
                this.Cursor = CursorProvider.DefaultCursor;
            }
        }

        private void DoEyedropperMouseMove(Image image, PointerEventArgs e, int imageNum)
        {
            if (DataContext is TransitionViewModel vm && (vm.IsEyedropperMode || vm.IsShadowEyedropperMode))
            {
                var position = e.GetPosition(image);
                vm.MouseOverCommand.Execute((X: (int)position.X, Y: (int)position.Y, imageNum));
            }
        }

        private void ResultImage_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (DataContext is not TransitionViewModel vm)
                return;

            if (!vm.IsExplicitTileVisibilityDrawMode && !vm.IsExplicitTileVisibilityEraseMode)
                return;

            if (vm.RequestLabelIndicatorCommand is ICommand requestLabelIndicatorCommand)
            {
                var currentPosition = e.GetPosition(ResultImage);
                requestLabelIndicatorCommand.Execute((X: (int)currentPosition.X, Y: (int)currentPosition.Y));
            }

            if (e.GetCurrentPoint(ResultImage).Properties.IsLeftButtonPressed
                && vm.SetExplicitTileVisibilityCommand is ICommand setExplicitTileVisibilityCommand)
            {
                var currentPosition = e.GetPosition(ResultImage);
                setExplicitTileVisibilityCommand.Execute((X: (int)currentPosition.X, Y: (int)currentPosition.Y));
            }
        }

        private void ResultImage_PointerEntered(object? sender, PointerEventArgs e)
        {
            if (DataContext is not TransitionViewModel vm)
                return;

            if (!vm.IsExplicitTileVisibilityDrawMode && !vm.IsExplicitTileVisibilityEraseMode)
                return;

            vm.IsIndicatorMapVisible = true;
        }

        private void ResultImage_PointerExited(object? sender, PointerEventArgs e)
        {
            if (DataContext is not TransitionViewModel vm)
                return;

            if (!vm.IsExplicitTileVisibilityDrawMode && !vm.IsExplicitTileVisibilityEraseMode)
                return;

            vm.IsIndicatorMapVisible = false;
        }

        private void ResultImage_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is not TransitionViewModel vm)
                return;

            if (!vm.IsExplicitTileVisibilityDrawMode && !vm.IsExplicitTileVisibilityEraseMode)
                return;

            if (e.GetCurrentPoint(ResultImage).Properties.IsLeftButtonPressed
                && vm.SetExplicitTileVisibilityCommand is ICommand setExplicitTileVisibilityCommand)
            {
                var currentPosition = e.GetPosition(ResultImage);
                setExplicitTileVisibilityCommand.Execute((X: (int)currentPosition.X, Y: (int)currentPosition.Y));
            }
        }
    }
}
