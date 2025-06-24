using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFZoomPanel.Commands;
using WPFZoomPanel.Helpers;

namespace WPFZoomPanel
{
    public partial class ZoomPanel
    {
        #region Private Fields

        private readonly Stack<UndoRedoStackItem> _redoStack = new Stack<UndoRedoStackItem>();
        private readonly Stack<UndoRedoStackItem> _undoStack = new Stack<UndoRedoStackItem>();
        private RelayCommand? _redoZoomCommand;
        private KeepAliveTimer? _timer1500Miliseconds;
        private KeepAliveTimer? _timer750Miliseconds;
        private RelayCommand? _undoZoomCommand;
        private UndoRedoStackItem _viewportZoomCache = new(new Rect(), 1);

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Command to implement Redo
        /// </summary>
        public ICommand RedoZoomCommand => _redoZoomCommand ?? (_redoZoomCommand =
             new RelayCommand(RedoZoom, () => CanRedoZoom));

        /// <summary>
        /// Command to implement Undo
        /// </summary>
        public ICommand UndoZoomCommand => _undoZoomCommand ?? (_undoZoomCommand =
            new RelayCommand(UndoZoom, () => CanUndoZoom));

        #endregion Public Properties

        #region Private Properties

        private bool CanRedoZoom => _redoStack.Any();

        private bool CanUndoZoom => _undoStack.Any();

        #endregion Private Properties

        #region Public Methods

        /// <summary>
        /// Record the last saved zoom level, so that we can return to it if no
        /// activity for 1550 milliseconds
        /// </summary>
        public void DelayedSaveZoom1500Miliseconds()
        {
            if (!_timer1500Miliseconds?.Running != true)
            {
                _viewportZoomCache = CreateUndoRedoStackItem();
            } (_timer1500Miliseconds ?? (_timer1500Miliseconds = new KeepAliveTimer(TimeSpan.FromMilliseconds(1500), () =>
            {
                if (_undoStack.Any() && _viewportZoomCache.Equals(_undoStack.Peek()))
                {
                    return;
                }

                _undoStack.Push(_viewportZoomCache);
                _redoStack.Clear();
                _undoZoomCommand?.RaiseCanExecuteChanged();
                _redoZoomCommand?.RaiseCanExecuteChanged();
            }))).Nudge();
        }

        /// <summary>
        /// Record the last saved zoom level, so that we can return to it if no
        /// activity for 750 milliseconds
        /// </summary>
        public void DelayedSaveZoom750Miliseconds()
        {
            if (_timer750Miliseconds?.Running != true)
            {
                _viewportZoomCache = CreateUndoRedoStackItem();
            } (_timer750Miliseconds ?? (_timer750Miliseconds = new KeepAliveTimer(TimeSpan.FromMilliseconds(740), () =>
            {
                if (_undoStack.Any() && _viewportZoomCache.Equals(_undoStack.Peek()))
                {
                    return;
                }

                _undoStack.Push(_viewportZoomCache);
                _redoStack.Clear();
                _undoZoomCommand?.RaiseCanExecuteChanged();
                _redoZoomCommand?.RaiseCanExecuteChanged();
            }))).Nudge();
        }

        /// <summary>
        /// Record the previous zoom level, so that we can return to it.
        /// </summary>
        public void SaveZoom()
        {
            _viewportZoomCache = CreateUndoRedoStackItem();
            if (_undoStack.Any() && _viewportZoomCache.Equals(_undoStack.Peek()))
            {
                return;
            }

            _undoStack.Push(_viewportZoomCache);
            _redoStack.Clear();
            _undoZoomCommand?.RaiseCanExecuteChanged();
            _redoZoomCommand?.RaiseCanExecuteChanged();
        }

        #endregion Public Methods

        #region Private Methods

        private UndoRedoStackItem CreateUndoRedoStackItem()
        {
            return new UndoRedoStackItem(ContentOffsetX, ContentOffsetY,
                ContentViewportWidth, ContentViewportHeight, InternalViewportZoom);
        }

        /// <summary>
        /// Jump back to the most recent zoom level saved on redo stack.
        /// </summary>
        private void RedoZoom()
        {
            _viewportZoomCache = CreateUndoRedoStackItem();
            if (!_redoStack.Any() || !_viewportZoomCache.Equals(_redoStack.Peek()))
            {
                _undoStack.Push(_viewportZoomCache);
            }

            _viewportZoomCache = _redoStack.Pop();
            AnimatedZoomTo(_viewportZoomCache.Zoom, _viewportZoomCache.Rect);
            SetScrollViewerFocus();
            _undoZoomCommand?.RaiseCanExecuteChanged();
            _redoZoomCommand?.RaiseCanExecuteChanged();
        }

        private void SetScrollViewerFocus()
        {
            if (_content.FindParentControl<ScrollViewer>() is ScrollViewer scrollViewer)
            {
                Keyboard.Focus(scrollViewer);
                scrollViewer.Focus();
            }
        }

        /// <summary>
        /// Jump back to the previous zoom level, saving current zoom to Redo Stack.
        /// </summary>
        private void UndoZoom()
        {
            _viewportZoomCache = CreateUndoRedoStackItem();
            if (!_undoStack.Any() || !_viewportZoomCache.Equals(_undoStack.Peek()))
            {
                _redoStack.Push(_viewportZoomCache);
            }

            _viewportZoomCache = _undoStack.Pop();
            AnimatedZoomTo(_viewportZoomCache.Zoom, _viewportZoomCache.Rect);
            SetScrollViewerFocus();
            _undoZoomCommand?.RaiseCanExecuteChanged();
            _redoZoomCommand?.RaiseCanExecuteChanged();
        }

        #endregion Private Methods

        #region Private Classes

        private class UndoRedoStackItem
        {
            #region Public Constructors + Destructors

            public UndoRedoStackItem(Rect rect, double zoom)
            {
                Rect = rect;
                Zoom = zoom;
            }

            public UndoRedoStackItem(double offsetX, double offsetY, double width, double height, double zoom)
            {
                Rect = new Rect(offsetX, offsetY, width, height);
                Zoom = zoom;
            }

            #endregion Public Constructors + Destructors

            #region Public Properties

            public Rect Rect { get; }
            public double Zoom { get; }

            #endregion Public Properties

            #region Public Methods

            public bool Equals(UndoRedoStackItem obj) => Zoom.IsWithinOnePercent(obj.Zoom) && Rect.Equals(obj.Rect);

            public override string ToString() => $"Rectangle {{{Rect.X},{Rect.X}}}, Zoom {Zoom}";

            #endregion Public Methods
        }

        #endregion Private Classes
    }
}
