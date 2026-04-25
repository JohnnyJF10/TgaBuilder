using Avalonia.Media;
using System;
using System.Diagnostics;
using TgaBuilderAvaloniaUi.Elements;
using TgaBuilderLib.Messaging;

namespace TgaBuilderAvaloniaUi.Services
{
    internal partial class MessageService : IMessageService
    {
        private readonly NotificationManager _manager;

        public MessageService(NotificationManager manager)
        {
            _manager = manager;
        }

        public void SendMessage(MessageType message, string additionalInfo = "", Exception? ex = null)
        {
            if (!_messageDict.TryGetValue(message, out var uiMessage))
            {
                Debug.WriteLine($"Unknown MessageType: {message}");
                return;
            }

            var text = string.IsNullOrEmpty(additionalInfo) ? uiMessage.Message : additionalInfo;

            if (ex is not null)
                text += $" Error: {ex.Message} - Please find more information in the log file.";

            var accent = new SolidColorBrush(Color.Parse(uiMessage.Accent));

            _manager.QueueNotification(new NotificationEntry
            {
                Title = uiMessage.Title,
                Message = text,
                AccentBrush = accent,
                TimeoutSeconds = uiMessage.timeout,
            });
        }
    }
}
