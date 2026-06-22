using Avalonia.Controls;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using TrLynxAvaloniaUi.Elements;
using TrLynxAvaloniaUi.Services;
using TrLynxLib.ViewModel;

namespace TrLynxAvaloniaUi.View
{
    public partial class ModificationsWindow : AsyncWindow
    {
        private ColumnDefinition? _secondaryColumn;
        private ColumnDefinition? _secondaryArrowColumn;

        public ModificationsWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
            InitializeVisualInvalidator(viewModel);
            SubscribeToColorOverrideEnabled(viewModel);
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

        private void InitializeVisualInvalidator(INotifyPropertyChanged viewModel)
        {
            if (viewModel is not ModificationsViewModel vm)
                return;

            vm.ModificationOutVM.VisualInvalidator = new VisualInvalidator(ResultImage);
        }

        private void SubscribeToColorOverrideEnabled(INotifyPropertyChanged viewModel)
        {
            if (viewModel is not ModificationsViewModel vm)
                return;

            vm.ColorOverrideVM.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(vm.ColorOverrideVM.IsColorOverrideEnabled))
                    UpdateSecondaryColumnWidths();
            };

            UpdateSecondaryColumnWidths();
        }

        private void UpdateSecondaryColumnWidths()
        {
            if (_secondaryColumn is null || _secondaryArrowColumn is null)
            {
                var grid = this.FindControl<Grid>("ImageAreaGrid");
                if (grid is not null && grid.ColumnDefinitions.Count > 4)
                {
                    _secondaryColumn = grid.ColumnDefinitions[2];
                    _secondaryArrowColumn = grid.ColumnDefinitions[3];
                }
            }

            if (_secondaryColumn is null || _secondaryArrowColumn is null
                || DataContext is not ModificationsViewModel vm)
                return;

            bool enabled = vm.ColorOverrideVM.IsColorOverrideEnabled;

            _secondaryColumn.Width = enabled ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
            _secondaryArrowColumn.Width = enabled ? new GridLength(35) : new GridLength(0);
        }

        protected override void OnClosing(Avalonia.Controls.WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is ModificationsViewModel vm)
                vm.MarkFinished();
        }

        private void InputImage_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (DataContext is not ModificationsViewModel mvm || sender is not Image image)
                return;

            var mivm = mvm.ModificationInVM;

            if (!mivm.IsEyedropperMode)
                return;

            var position = e.GetPosition(image);
            mivm.MouseOverCommand.Execute((X: (int)position.X, Y: (int)position.Y));
        }

        private void InputImage_PointerEntered(object? sender, PointerEventArgs e)
        {
            if (DataContext is not ModificationsViewModel mvm || sender is not Image image)
                return;

            var mivm = mvm.ModificationInVM;

            if (!mivm.IsEyedropperMode)
                return;

            this.Cursor = CursorProvider.EyedropperCursor;
        }

        private void InputImage_PointerExited(object? sender, PointerEventArgs e)
        {
            if (DataContext is not ModificationsViewModel mvm || sender is not Image image)
                return;

            var mivm = mvm.ModificationInVM;

            if (!mivm.IsEyedropperMode)
                return;

            this.Cursor = CursorProvider.DefaultCursor;
        }

        private void InputImage_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is not ModificationsViewModel mvm || sender is not Image image)
                return;

            var mivm = mvm.ModificationInVM;

            if (!mivm.IsEyedropperMode)
                return;

            mivm.IsEyedropperMode = false;
            this.Cursor = CursorProvider.DefaultCursor;

        }
    }
}
