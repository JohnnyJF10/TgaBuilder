using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgaBuilderLib.Abstraction;
using Wpf.Ui.Controls;

namespace TgaBuilderWpfUi.Elements
{
    public class AsyncWindow : FluentWindow, IView
    {
        public new object? DataContext
        {
            get => base.DataContext;
            set => base.DataContext = value;
        }

        public new bool? DialogResult
        {
            get => base.DialogResult;
            set => base.DialogResult = value;
        }

        

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

        public Task<bool?> ShowDialogAsync()
        {
            return Task.FromResult<bool?>(base.ShowDialog());
        }
    }
}
