using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System.Windows.Input;

namespace TgaBuilderAvaloniaUi.Elements
{
    /// <summary>
    /// Represents a single item inside a <see cref="NotificationBar"/>.
    /// Inspired by Lepoco's WPF UI BreadcrumbBarItem.
    /// </summary>
    public class NotificationBarItem : ContentControl
    {
        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<NotificationBarItem, string>(nameof(Title), "");

        public static readonly StyledProperty<string> MessageProperty =
            AvaloniaProperty.Register<NotificationBarItem, string>(nameof(Message), "");

        public static readonly StyledProperty<IBrush> AccentBrushProperty =
            AvaloniaProperty.Register<NotificationBarItem, IBrush>(nameof(AccentBrush), Brushes.Gray);

        public static readonly StyledProperty<bool> IsLastProperty =
            AvaloniaProperty.Register<NotificationBarItem, bool>(nameof(IsLast), false);

        public static readonly StyledProperty<ICommand?> DismissCommandProperty =
            AvaloniaProperty.Register<NotificationBarItem, ICommand?>(nameof(DismissCommand));

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Message
        {
            get => GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public IBrush AccentBrush
        {
            get => GetValue(AccentBrushProperty);
            set => SetValue(AccentBrushProperty, value);
        }

        public bool IsLast
        {
            get => GetValue(IsLastProperty);
            set => SetValue(IsLastProperty, value);
        }

        public ICommand? DismissCommand
        {
            get => GetValue(DismissCommandProperty);
            set => SetValue(DismissCommandProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsLastProperty)
                PseudoClasses.Set(":last", (bool)(change.NewValue ?? false));
        }
    }
}
