using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using System.Collections.Specialized;
using System.ComponentModel;
using TgaBuilderLib.Commands;

namespace TgaBuilderAvaloniaUi.Elements
{
    /// <summary>
    /// A horizontal notification bar styled like a BreadcrumbBar.
    /// Inspired by Lepoco's WPF UI BreadcrumbBar.
    /// Only the newest (single) notification is shown at a time.
    /// </summary>
    public class NotificationBar : ItemsControl
    {
        public static readonly StyledProperty<Services.NotificationManager?> ManagerProperty =
            AvaloniaProperty.Register<NotificationBar, Services.NotificationManager?>(nameof(Manager));

        public Services.NotificationManager? Manager
        {
            get => GetValue(ManagerProperty);
            set => SetValue(ManagerProperty, value);
        }

        private NotificationEntry? _currentEntry;

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ManagerProperty)
            {
                if (change.OldValue is Services.NotificationManager oldMgr)
                    oldMgr.Notifications.CollectionChanged -= OnNotificationsChanged;

                if (change.NewValue is Services.NotificationManager newMgr)
                {
                    ItemsSource = newMgr.Notifications;
                    newMgr.Notifications.CollectionChanged += OnNotificationsChanged;
                    RefreshCurrentEntry(newMgr);
                }
                else
                {
                    DetachCurrentEntry();
                    ItemsSource = null;
                    IsVisible = false;
                }
            }
        }

        private void OnNotificationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (Manager is null) return;
            RefreshCurrentEntry(Manager);
        }

        private void RefreshCurrentEntry(Services.NotificationManager manager)
        {
            DetachCurrentEntry();
            UpdateIsLast(manager);

            var list = manager.Notifications;
            if (list.Count > 0)
            {
                _currentEntry = list[list.Count - 1];
                _currentEntry.PropertyChanged += OnCurrentEntryPropertyChanged;

                // Reset opacity instantly (without the fade transition) before revealing
                var saved = Transitions;
                Transitions = null;
                Opacity = 1.0;
                Transitions = saved;

                IsVisible = true;
            }
            else
            {
                // Item removed after fade — reset opacity (bar is invisible so no visual effect)
                var saved = Transitions;
                Transitions = null;
                Opacity = 1.0;
                Transitions = saved;

                IsVisible = false;
            }
        }

        private void DetachCurrentEntry()
        {
            if (_currentEntry != null)
            {
                _currentEntry.PropertyChanged -= OnCurrentEntryPropertyChanged;
                _currentEntry = null;
            }
        }

        private void OnCurrentEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NotificationEntry.IsDismissing) &&
                sender is NotificationEntry entry && entry.IsDismissing)
            {
                // Already on the UI thread (fired from BeginDismiss via InvokeAsync)
                Opacity = 0.0;
            }
        }

        private static void UpdateIsLast(Services.NotificationManager manager)
        {
            var list = manager.Notifications;
            for (int i = 0; i < list.Count; i++)
                list[i].IsLast = (i == list.Count - 1);
        }

        protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        {
            recycleKey = null;
            return item is not NotificationBarItem;
        }

        protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
            => new NotificationBarItem();

        protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
        {
            base.PrepareContainerForItemOverride(container, item, index);

            if (container is not NotificationBarItem nbItem || item is not NotificationEntry entry)
                return;

            nbItem.Title = entry.Title;
            nbItem.Message = entry.Message;
            nbItem.AccentBrush = entry.AccentBrush;
            nbItem.DismissCommand = new RelayCommand(() => Manager?.Dismiss(entry));

            // Keep IsLast in sync with the live NotificationEntry property
            nbItem.Bind(NotificationBarItem.IsLastProperty,
                new Binding(nameof(NotificationEntry.IsLast)) { Source = entry });
        }
    }
}
