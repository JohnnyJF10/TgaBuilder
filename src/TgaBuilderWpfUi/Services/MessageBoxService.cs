using TgaBuilderLib.Messaging;
using Wpf.Ui.Controls;

namespace TgaBuilderWpfUi.Services
{
    internal class MessageBoxService : IMessageBoxService
    {
        public async Task ShowErrorMessageBox(string Header, string Message, Exception? ex = null)
        {
            var uiMessageBox = new MessageBox
            {
                Title = Header,
                Content = Message,
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel"
            };
            if (ex != null)
            {
                uiMessageBox.Content += $"\n\nException: {ex.Message}";
            }
            _ = await uiMessageBox.ShowDialogAsync();
            return;
        }

        public async Task ShowInfoMessageBox(string Header, string Message)
        {
            var uiMessageBox = new MessageBox
            {
                Title = Header,
                Content = Message,
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel"
            };
            _ = await uiMessageBox.ShowDialogAsync();
            return;
        }


        public async Task<YesNoCancel> ShowYesNoCancelMessageBox(string title, string message)
        {
            var uiMessageBox = new MessageBox
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                CloseButtonText = "Cancel",
            };

            var messageBoxResult = await uiMessageBox.ShowDialogAsync();

            return messageBoxResult switch
            {
                MessageBoxResult.Primary => YesNoCancel.Yes,
                MessageBoxResult.Secondary => YesNoCancel.No,
                MessageBoxResult.None => YesNoCancel.Cancel,
                _ => YesNoCancel.Cancel
            };
        }

        public async Task<bool> ShowOkCancelMessageBox(string Header, string Message)
        {
            var uiMessageBox = new MessageBox
            {
                Title = Header,
                Content = Message,
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel"
            };
            var messageBoxResult = await uiMessageBox.ShowDialogAsync();
            return messageBoxResult == MessageBoxResult.Primary;
        }
    }
}
