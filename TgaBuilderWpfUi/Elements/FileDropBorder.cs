using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace TgaBuilderWpfUi.Elements
{
    public class FileDropBorder : Border
    {
        public static readonly DependencyProperty DropCommandProperty =
            DependencyProperty.Register(
                nameof(DropCommand),
                typeof(ICommand),
                typeof(FileDropBorder),
                new PropertyMetadata(null)
            );

        public ICommand? DropCommand
        {
            get => (ICommand?)GetValue(DropCommandProperty);
            set => SetValue(DropCommandProperty, value);
        }

        public FileDropBorder()
        {
            AllowDrop = true;

            DragEnter += OnDragEnter;
            DragOver += OnDragOver;
            Drop += OnDrop;

            Unloaded += (_, _) =>
            {
                DragEnter -= OnDragEnter;
                DragOver -= OnDragOver;
                Drop -= OnDrop;
            };
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            e.Effects = IsFileDrop(e) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = IsFileDrop(e) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (IsFileDrop(e)
                && e.Data.GetData(DataFormats.FileDrop) is string[] files
                && DropCommand?.CanExecute(files) == true)
            {
                DropCommand.Execute(files.ToList());
            }

            e.Handled = true;
        }

        private bool IsFileDrop(DragEventArgs e) =>
            e.Data.GetDataPresent(DataFormats.FileDrop);
    }
}
