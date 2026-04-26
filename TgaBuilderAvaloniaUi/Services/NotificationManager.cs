using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using TgaBuilderAvaloniaUi.Elements;

namespace TgaBuilderAvaloniaUi.Services
{
    public class NotificationManager
    {
        private CancellationTokenSource? _dismissCts;

        public ObservableCollection<NotificationEntry> Notifications { get; } = new();

        public void QueueNotification(NotificationEntry entry)
        {
            // Cancel any pending dismiss (instant replace — no stacking)
            CancelCurrentDismiss();

            Dispatcher.UIThread.Post(() =>
            {
                Notifications.Clear();     // Replace older notifications immediately
                Notifications.Add(entry);
            });

            if (entry.TimeoutSeconds > 0)
            {
                var cts = new CancellationTokenSource();
                var old = Interlocked.Exchange(ref _dismissCts, cts);
                old?.Cancel();
                old?.Dispose();
                _ = AutoDismissAsync(entry, entry.TimeoutSeconds, cts.Token);
            }
        }

        public void Dismiss(NotificationEntry entry)
        {
            CancelCurrentDismiss();
            _ = DismissWithFadeAsync(entry);
        }

        private void CancelCurrentDismiss()
        {
            var cts = Interlocked.Exchange(ref _dismissCts, null);
            cts?.Cancel();
            cts?.Dispose();
        }

        private async Task AutoDismissAsync(NotificationEntry entry, int seconds, CancellationToken ct)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(seconds), ct);
                await DismissWithFadeAsync(entry, ct);
            }
            catch (OperationCanceledException) { }
            catch (Exception) { }
        }

        private async Task DismissWithFadeAsync(NotificationEntry entry, CancellationToken ct = default)
        {
            try
            {
                // Signal entry to begin fade-out (~1 second transition in the UI)
                await Dispatcher.UIThread.InvokeAsync(() => entry.BeginDismiss());

                // Wait for the fade animation to complete
                await Task.Delay(TimeSpan.FromSeconds(1), ct);

                Dispatcher.UIThread.Post(() => Notifications.Remove(entry));
            }
            catch (OperationCanceledException) { }
            catch (Exception) { }
        }
    }
}
