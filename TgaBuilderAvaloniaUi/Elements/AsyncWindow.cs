using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderAvaloniaUi.Elements
{
    public class AsyncWindow : Window, IView
    {
        public new object? DataContext
        {
            get => base.DataContext;
            set => base.DataContext = value;
        }

        

        private bool? _dialogResult;

        public bool? DialogResult
        {
            get => _dialogResult;
            set
            {
                _dialogResult = value;
                Close(value); 
            }
        }

        public new bool IsLoaded => base.IsLoaded;

        public Task ShowAsync()
        {
            base.Show();
            return Task.CompletedTask;
        }

        public Task HideAsync()
        {
            base.Hide();
            return Task.CompletedTask;
        }

        public Task CloseAsync()
        {
            base.Close();
            return Task.CompletedTask;
        }

        public async Task<bool?> ShowDialogAsync()
        {
            if (Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                Debug.WriteLine("The application is not running in desktop mode.");
                return null;
            }

            if (desktop.MainWindow is not Window mainWindow)
            {
                Debug.WriteLine("The main window is not available.");
                return null;
            }

            return await base.ShowDialog<bool?>(mainWindow);
        }
    }
}
