using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data;
using System;
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
        // Instance-level fade-out transition (style-level Transitions setter is not readable
        // from code, so we own the Transitions object here to enable the save/restore pattern).
        private static readonly Transitions _fadeTransitions = new Transitions
        {
            new DoubleTransition
            {
                Property = OpacityProperty,
                Duration = TimeSpan.FromSeconds(1),
                Easing   = new SineEaseIn()
            }
        };

        public static readonly StyledProperty<Services.NotificationManager?> ManagerProperty =
            AvaloniaProperty.Register<NotificationBar, Services.NotificationManager?>(nameof(Manager));

        public Services.NotificationManager? Manager
        {
            get => GetValue(ManagerProperty);
            set => SetValue(ManagerProperty, value);
        }

        private NotificationEntry? _currentEntry;

        public NotificationBar()
        {
            // Set transitions at the instance level so they are accessible via this.Transitions.
            // A ControlTheme <Setter Property="Transitions"> only applies at the style level and
            // is never reflected in the instance Transitions property, breaking the save/null/restore pattern.
            Transitions = _fadeTransitions;
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

                // Reset opacity instantly (suppress transition so bar appears at full opacity immediately)
                Transitions = null;
                Opacity = 1.0;
                Transitions = _fadeTransitions;

                IsVisible = true;
            }
            else
            {
                // Item removed after fade — reset opacity instantly (bar is invisible, no visual effect)
                Transitions = null;
                Opacity = 1.0;
                Transitions = _fadeTransitions;

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
                // Trigger the 1-second fade-out via the instance-level DoubleTransition
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
