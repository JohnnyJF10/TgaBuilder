using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using Avalonia.Controls;
using Avalonia.Input;
using TgaBuilderAvaloniaUi.Elements;
using TgaBuilderAvaloniaUi.Services;
using TgaBuilderLib.ViewModel;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class ModificationsWindow : AsyncWindow
    {
        public ModificationsWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
            InitializeVisualInvalidator(viewModel);
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
