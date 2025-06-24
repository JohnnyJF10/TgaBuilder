using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using THelperLib.Messaging;
using THelperWpfUi.View;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace THelperWpfUi.Services
{
    public partial class MessageService : IMessageService
    {
        private const int MAX_NUM_PAGES = 128;

        public void SendMessage(MessageType message,
            string additionalInfo = "",
            Exception? ex = null)
        {
            var ActiveWindow = Application.Current.Windows
                .OfType<FluentWindow>()
                .FirstOrDefault(w => w.IsActive && w.IsKeyboardFocusWithin);

            var snackbarService = new SnackbarService();

            if (ActiveWindow is ISnackbarOwner snackbarOwner)
            {
                snackbarService.SetSnackbarPresenter(snackbarOwner.SnackbarPresenter);
            }
            else
            {
                if (Application.Current.MainWindow is MainWindow mainWindow)
                    snackbarService.SetSnackbarPresenter(mainWindow.MessageSnackbarPresenter);
            }

            var uiMessage = _messageDict[message];

            if (!string.IsNullOrEmpty(additionalInfo))
                uiMessage.Message = additionalInfo;

            if (ex is not null)
                uiMessage.Message += $" Error: {ex.Message} - Please find more information in the log file.";

            snackbarService.Show(
                title:      uiMessage.Title,
                message:    uiMessage.Message,
                appearance: uiMessage.Appearance,
                icon:       uiMessage.Icon,
                timeout:    uiMessage.timeout);
        }
    }
}
