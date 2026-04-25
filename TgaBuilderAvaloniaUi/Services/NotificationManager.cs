using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TgaBuilderAvaloniaUi.Elements;

namespace TgaBuilderAvaloniaUi.Services
{
    public class NotificationManager
    {
        private const int MaxNotifications = 5;

        public ObservableCollection<NotificationEntry> Notifications { get; } = new();

        public void QueueNotification(NotificationEntry entry)
        {
            Dispatcher.UIThread.Post(() =>
            {
                while (Notifications.Count >= MaxNotifications)
                    Notifications.RemoveAt(0);

                Notifications.Add(entry);
            });

            if (entry.TimeoutSeconds > 0)
                _ = AutoDismissAsync(entry, entry.TimeoutSeconds);
        }

        public void Dismiss(NotificationEntry entry)
        {
            Dispatcher.UIThread.Post(() => Notifications.Remove(entry));
        }

        private async Task AutoDismissAsync(NotificationEntry entry, int seconds)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(seconds));
                Dismiss(entry);
            }
            catch (Exception)
            {
                // Swallow exceptions from auto-dismiss to prevent unhandled task exceptions.
            }
        }
    }
}
