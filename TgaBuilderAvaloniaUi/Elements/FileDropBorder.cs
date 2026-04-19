using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace TgaBuilderAvaloniaUi.Elements
{
    public class FileDropBorder : Border
    {
        public static readonly StyledProperty<ICommand?> DropCommandProperty =
            AvaloniaProperty.Register<FileDropBorder, ICommand?>(nameof(DropCommand));

        public ICommand? DropCommand
        {
            get => GetValue(DropCommandProperty);
            set => SetValue(DropCommandProperty, value);
        }

        public FileDropBorder()
        {
            // Can Drop ?

            AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
            AddHandler(DragDrop.DragOverEvent, OnDragOver);
            AddHandler(DragDrop.DropEvent, OnDrop);

            DetachedFromVisualTree += (_, _) =>
            {
                RemoveHandler(DragDrop.DragEnterEvent, OnDragEnter);
                RemoveHandler(DragDrop.DragOverEvent, OnDragOver);
                RemoveHandler(DragDrop.DropEvent, OnDrop);
            };
        }

        private void OnDragEnter(object? sender, DragEventArgs e)
        {
            e.DragEffects = IsFileDrop(e) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = IsFileDrop(e) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnDrop(object? sender, DragEventArgs e)
        {
            var files = e.DataTransfer.TryGetFiles();

            if (files is not null)
            {

                if (files == null)
                {
                    e.Handled = true;
                    return;
                }

                var paths = files
                    .Select(f => f.TryGetLocalPath())
                    .Where(p => p != null)
                    .ToList(); 

                if (paths.Count > 0 && DropCommand?.CanExecute(paths) == true)
                {
                    DropCommand.Execute(paths);
                }
            }

            e.Handled = true;
        }

        private bool IsFileDrop(DragEventArgs e) =>
            e.DataTransfer.TryGetFiles() is not null;
    }
}
