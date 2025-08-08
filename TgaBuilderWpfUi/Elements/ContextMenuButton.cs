using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Button = Wpf.Ui.Controls.Button;
using MenuItem = Wpf.Ui.Controls.MenuItem;

namespace TgaBuilderWpfUi.Elements
{
    public class ContextMenuButton : Button
    {

        public ContextMenuButton()
        {
            DefaultStyleKey = typeof(Button);
            Click += OnClick;
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource), 
                typeof(IEnumerable), 
                typeof(ContextMenuButton), 
                new PropertyMetadata(null));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ItemCommandProperty =
            DependencyProperty.Register(
                nameof(ItemCommand), 
                typeof(ICommand), 
                typeof(ContextMenuButton), 
                new PropertyMetadata(null));

        public ICommand ItemCommand
        {
            get => (ICommand)GetValue(ItemCommandProperty);
            set => SetValue(ItemCommandProperty, value);
        }

        public static readonly DependencyProperty CommandParameterSelectorProperty =
            DependencyProperty.Register(
                nameof(CommandParameterSelector), 
                typeof(Func<object, object>), 
                typeof(ContextMenuButton), 
                new PropertyMetadata(null));

        public Func<object, object> CommandParameterSelector
        {
            get => (Func<object, object>)GetValue(CommandParameterSelectorProperty);
            set => SetValue(CommandParameterSelectorProperty, value);
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            if (ItemCommand == null) return;
            if (ItemsSource is not IEnumerable<string> ItemsList) return;
            if (!ItemsList.Any()) return;

            var contextMenu = new ContextMenu();

            foreach (var item in ItemsList)
            {
                var menuItem = new MenuItem
                {
                    Header = item.ToString(),
                    Command = ItemCommand,
                    CommandParameter = CommandParameterSelector?.Invoke(item) ?? item
                };
                contextMenu.Items.Add(menuItem);
            }

            ContextMenu = contextMenu;
            contextMenu.PlacementTarget = this;
            contextMenu.Placement = PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }
    }
}
