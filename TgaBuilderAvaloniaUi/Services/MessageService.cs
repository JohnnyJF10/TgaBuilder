using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Notification;
using System;
using System.Diagnostics;
using TgaBuilderAvaloniaUi.View;
using TgaBuilderLib.Messaging;

namespace TgaBuilderAvaloniaUi.Services
{
    internal partial class MessageService : IMessageService
    {
        public void SendMessage(MessageType message, string additionalInfo = "", Exception? ex = null)
        {
            Window ActiveWindow;

            if (Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                Debug.WriteLine("The application is not running in desktop mode.");
                return;
            }

            if (desktop.MainWindow is not MainWindow mainWindow)
            {
                Debug.WriteLine("Could not get the top-level window.");
                return;
            }

            ActiveWindow = mainWindow;

            var uiMessage = _messageDict[message];

            if (!string.IsNullOrEmpty(additionalInfo))
                uiMessage.Message = additionalInfo;

            if (ex is not null)
                uiMessage.Message += $" Error: {ex.Message} - Please find more information in the log file.";

            /*
            mainWindow.Manager
                .CreateMessage()
                .Accent(uiMessage.Accent)
                .Animates(true)
                .Background("#333")
                .HasBadge(uiMessage.Title)
                .HasMessage(uiMessage.Message)
                .Dismiss().WithButton("x", button => { })
                .Dismiss().WithDelay(TimeSpan.FromSeconds(uiMessage.timeout))
                .Queue();
            */
        }
    }
}
