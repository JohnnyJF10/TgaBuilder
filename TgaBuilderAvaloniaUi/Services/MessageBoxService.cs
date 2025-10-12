using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DialogHostAvalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgaBuilderAvaloniaUi.View;
using TgaBuilderLib.Messaging;

namespace TgaBuilderAvaloniaUi.Services
{
    internal class MessageBoxService : IMessageBoxService
    {
        public async Task ShowErrorMessageBox(string header, string message, Exception? ex = null)
        {
            if (ex != null)
                message += "\n\n" + ex.Message;

            var dlg = new MessageBoxWindow(header, message, "Error");
            await dlg.ShowDialogAsync();
        }

        public async Task ShowInfoMessageBox(string header, string message)
        {
            var dlg = new MessageBoxWindow(header, message, "Info");
            await dlg.ShowDialogAsync();
        }

        public async Task<bool> ShowOkCancelMessageBox(string header, string message)
        {
            var dlg = new MessageBoxWindow(header, message, "OkCancel");
            await dlg.ShowDialogAsync();
            var result = dlg.Result;
            return result == MessageBoxResult.Ok;
        }

        public async Task<YesNoCancel> ShowYesNoCancelMessageBox(string header, string message)
        {
            var dlg = new MessageBoxWindow(header, message, "YesNoCancel");
            await dlg.ShowDialogAsync();
            var result = dlg.Result;

            return result switch
            {
                MessageBoxResult.Yes => YesNoCancel.Yes,
                MessageBoxResult.No => YesNoCancel.No,
                _ => YesNoCancel.Cancel
            };
        }
    }
}
