using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using System.Collections.Specialized;
using TgaBuilderLib.Commands;

namespace TgaBuilderAvaloniaUi.Elements
{
    /// <summary>
    /// A horizontal notification bar styled like a BreadcrumbBar.
    /// Inspired by Lepoco's WPF UI BreadcrumbBar.
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
                    UpdateIsLast(newMgr);
                    IsVisible = newMgr.Notifications.Count > 0;
                }
                else
                {
                    ItemsSource = null;
                    IsVisible = false;
                }
            }
        }

        private void OnNotificationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (Manager is null) return;
            UpdateIsLast(Manager);
            IsVisible = Manager.Notifications.Count > 0;
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
