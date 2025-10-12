using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using TgaBuilderAvaloniaUi.Elements;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.ViewModel.Views;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class AboutWindow : AsyncWindow
    {
        public AboutWindow(INotifyPropertyChanged viewModel)
        {
            InitializeComponent();
            base.DataContext = viewModel;
        }

        [Obsolete("For designer use only")]
        public AboutWindow()
        {
            var serviceProvider = GlobalServiceProvider.Instance;

            var vm = serviceProvider.GetRequiredService<AboutViewModel>()
                ?? throw new InvalidOperationException("AboutViewModel not found in DI container");
            InitializeComponent();
            base.DataContext = vm;
        }
    }
}
