using System.Windows;
using WPFZoomPanel.Helpers;

namespace WPFZoomPanel
{
    public partial class ZoomPanel
    {
        #region Public Methods

        /// <summary>
        /// Do animation that scales the content so that it fits completely in
        /// the control.
        /// </summary>
        public void AnimatedScaleToFit()
        {
            if (_content == null)
            {
                throw new ApplicationException("PART_Content was not found in the ZoomAndPanControl visual template!");
            }

            ZoomTo(FillZoomValue);
            //AnimatedZoomTo(new Rect(0, 0, _content.ActualWidth, _content.ActualHeight));
        }

        /// <summary>
        /// Use animation to center the view on the specified point (in content coordinates).
        /// </summary>
        public void AnimatedSnapTo(Point contentPoint)
        {
            double newX = contentPoint.X - (ContentViewportWidth / 2);
            double newY = contentPoint.Y - (ContentViewportHeight / 2);

            AnimationHelper.StartAnimation(this, ContentOffsetXProperty, newX, AnimationDuration, UseAnimations);
            AnimationHelper.StartAnimation(this, ContentOffsetYProperty, newY, AnimationDuration, UseAnimations);
        }

        /// <summary>
        /// Zoom in/out centered on the specified point (in content
        /// coordinates). The focus point is kept locked to it's on screen
        /// position (ala google maps).
        /// </summary>
        public void AnimatedZoomAboutPoint(double newContentZoom, Point contentZoomFocus)
        {
            newContentZoom = Math.Min(Math.Max(newContentZoom, MinimumZoomClamped), MaximumZoom);

            AnimationHelper.CancelAnimation(this, ContentZoomFocusXProperty);
            AnimationHelper.CancelAnimation(this, ContentZoomFocusYProperty);
            AnimationHelper.CancelAnimation(this, ViewportZoomFocusXProperty);
            AnimationHelper.CancelAnimation(this, ViewportZoomFocusYProperty);

            ContentZoomFocusX = contentZoomFocus.X;
            ContentZoomFocusY = contentZoomFocus.Y;
            ViewportZoomFocusX = (ContentZoomFocusX - ContentOffsetX) * InternalViewportZoom;
            ViewportZoomFocusY = (ContentZoomFocusY - ContentOffsetY) * InternalViewportZoom;

            // When zooming about a point make updates to ViewportZoom also
            // update content offset.
            _enableContentOffsetUpdateFromScale = true;

            AnimationHelper.StartAnimation(this, _internalViewportZoomProperty, newContentZoom, AnimationDuration,
                (sender, e) =>
                {
                    _enableContentOffsetUpdateFromScale = false;
                    ResetViewportZoomFocus();
                }, UseAnimations);
        }

        /// <summary>
        /// Do an animated zoom to view a specific scale and rectangle (in
        /// content coordinates).
        /// </summary>
        public void AnimatedZoomTo(double newScale, Rect contentRect)
        {
            AnimatedZoomPointToViewportCenter(newScale, new Point(contentRect.X + (contentRect.Width / 2), contentRect.Y + (contentRect.Height / 2)),
                delegate
                {
                    // At the end of the animation, ensure that we are snapped
                    // to the specified content offset. Due to zooming in on the
                    // content focus point and rounding errors, the content
                    // offset may be slightly off what we want at the end of the
                    // animation and this bit of code corrects it.
                    ContentOffsetX = contentRect.X;
                    ContentOffsetY = contentRect.Y;
                });
        }

        /// <summary>
        /// Do an animated zoom to the specified rectangle (in content coordinates).
        /// </summary>
        public void AnimatedZoomTo(Rect contentRect)
        {
            double scaleX = ContentViewportWidth / contentRect.Width;
            double scaleY = ContentViewportHeight / contentRect.Height;
            double contentFitZoom = InternalViewportZoom * Math.Min(scaleX, scaleY);
            AnimatedZoomPointToViewportCenter(contentFitZoom, new Point(contentRect.X + (contentRect.Width / 2), contentRect.Y + (contentRect.Height / 2)), (_, _) => { });
        }

        /// <summary>
        /// Zoom in/out centered on the viewport center.
        /// </summary>
        public void AnimatedZoomTo(double viewportZoom)
        {
            double xadjust = (ContentViewportWidth - _content.ActualWidth) * InternalViewportZoom / 2;
            double yadjust = (ContentViewportHeight - _content.ActualHeight) * InternalViewportZoom / 2;
            Point zoomCenter = (InternalViewportZoom >= FillZoomValue)
                ? new Point(ContentOffsetX + (ContentViewportWidth / 2), ContentOffsetY + (ContentViewportHeight / 2))
                : new Point(_content.ActualWidth / 2 - xadjust, _content.ActualHeight / 2 + yadjust);
            AnimatedZoomAboutPoint(viewportZoom, zoomCenter);
        }

        /// <summary>
        /// Zoom in/out centered on the viewport center.
        /// </summary>
        public void AnimatedZoomToCentered(double viewportZoom)
        {
            Point zoomCenter = new Point(_content.ActualWidth / 2, _content.ActualHeight / 2); ;
            AnimatedZoomAboutPoint(viewportZoom, zoomCenter);
        }

        /// <summary>
        /// Instantly scale the content so that it fits completely in the control.
        /// </summary>
        public void ScaleToFit()
        {
            if (_content == null)
            {
                throw new ApplicationException("PART_Content was not found in the ZoomAndPanControl visual template!");
            }

            ZoomTo(FitZoomValue);
            //ZoomTo(new Rect(0, 0, _content.ActualWidth, _content.ActualHeight));
        }

        /// <summary>
        /// Instantly center the view on the specified point (in content coordinates).
        /// </summary>
        public void SnapContentOffsetTo(Point contentOffset)
        {
            AnimationHelper.CancelAnimation(this, ContentOffsetXProperty);
            AnimationHelper.CancelAnimation(this, ContentOffsetYProperty);
            ContentOffsetX = contentOffset.X;
            ContentOffsetY = contentOffset.Y;
        }

        /// <summary>
        /// Instantly center the view on the specified point (in content coordinates).
        /// </summary>
        public void SnapTo(Point contentPoint)
        {
            //AnimationHelper.CancelAnimation(this, ContentOffsetXProperty);
            //AnimationHelper.CancelAnimation(this, ContentOffsetYProperty);
            ContentOffsetX = contentPoint.X - (ContentViewportWidth / 2);
            ContentOffsetY = contentPoint.Y - (ContentViewportHeight / 2);
        }

        /// <summary>
        /// Zoom in/out centered on the specified point (in content
        /// coordinates). The focus point is kept locked to it's on screen
        /// position (ala google maps).
        /// </summary>
        public void ZoomAboutPoint(double newContentZoom, Point contentZoomFocus)
        {
            newContentZoom = Math.Min(Math.Max(newContentZoom, MinimumZoomClamped), MaximumZoom);

            double screenSpaceZoomOffsetX = (contentZoomFocus.X - ContentOffsetX) * InternalViewportZoom;
            double screenSpaceZoomOffsetY = (contentZoomFocus.Y - ContentOffsetY) * InternalViewportZoom;
            double contentSpaceZoomOffsetX = screenSpaceZoomOffsetX / newContentZoom;
            double contentSpaceZoomOffsetY = screenSpaceZoomOffsetY / newContentZoom;
            double newContentOffsetX = contentZoomFocus.X - contentSpaceZoomOffsetX;
            double newContentOffsetY = contentZoomFocus.Y - contentSpaceZoomOffsetY;

            AnimationHelper.CancelAnimation(this, _internalViewportZoomProperty);
            AnimationHelper.CancelAnimation(this, ContentOffsetXProperty);
            AnimationHelper.CancelAnimation(this, ContentOffsetYProperty);

            InternalViewportZoom = newContentZoom;
            ContentOffsetX = newContentOffsetX;
            ContentOffsetY = newContentOffsetY;
            RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Instantly zoom to the specified rectangle (in content coordinates).
        /// </summary>
        public void ZoomTo(Rect contentRect)
        {
            double scaleX = ContentViewportWidth / contentRect.Width;
            double scaleY = ContentViewportHeight / contentRect.Height;
            double newScale = InternalViewportZoom * Math.Min(scaleX, scaleY);

            ZoomPointToViewportCenter(newScale, new Point(contentRect.X + (contentRect.Width / 2), contentRect.Y + (contentRect.Height / 2)));
        }

        /// <summary>
        /// Zoom in/out centered on the viewport center.
        /// </summary>
        public void ZoomTo(double viewportZoom)
        {
            Point zoomCenter = new Point(ContentOffsetX + (ContentViewportWidth / 2), ContentOffsetY + (ContentViewportHeight / 2));
            ZoomAboutPoint(viewportZoom, zoomCenter);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Zoom to the specified scale and move the specified focus point to
        /// the center of the viewport.
        /// </summary>
        private void AnimatedZoomPointToViewportCenter(double newContentZoom, Point contentZoomFocus, EventHandler callback)
        {
            newContentZoom = Math.Min(Math.Max(newContentZoom, MinimumZoomClamped), MaximumZoom);

            AnimationHelper.CancelAnimation(this, ContentZoomFocusXProperty);
            AnimationHelper.CancelAnimation(this, ContentZoomFocusYProperty);
            AnimationHelper.CancelAnimation(this, ViewportZoomFocusXProperty);
            AnimationHelper.CancelAnimation(this, ViewportZoomFocusYProperty);

            ContentZoomFocusX = contentZoomFocus.X;
            ContentZoomFocusY = contentZoomFocus.Y;
            ViewportZoomFocusX = (ContentZoomFocusX - ContentOffsetX) * InternalViewportZoom;
            ViewportZoomFocusY = (ContentZoomFocusY - ContentOffsetY) * InternalViewportZoom;

            // When zooming about a point make updates to ViewportZoom also
            // update content offset.
            _enableContentOffsetUpdateFromScale = true;

            AnimationHelper.StartAnimation(this, _internalViewportZoomProperty, newContentZoom, AnimationDuration,
                delegate (object? sender, EventArgs e)
                {
                    _enableContentOffsetUpdateFromScale = false;
                    callback?.Invoke(this, EventArgs.Empty);
                }, UseAnimations);

            AnimationHelper.StartAnimation(this, ViewportZoomFocusXProperty, ViewportWidth / 2, AnimationDuration, UseAnimations);
            AnimationHelper.StartAnimation(this, ViewportZoomFocusYProperty, ViewportHeight / 2, AnimationDuration, UseAnimations);
        }

        /// <summary>
        /// Zoom to the specified scale and move the specified focus point to
        /// the center of the viewport.
        /// </summary>
        private void ZoomPointToViewportCenter(double newContentZoom, Point contentZoomFocus)
        {
            newContentZoom = Math.Min(Math.Max(newContentZoom, MinimumZoomClamped), MaximumZoom);

            AnimationHelper.CancelAnimation(this, _internalViewportZoomProperty);
            AnimationHelper.CancelAnimation(this, ContentOffsetXProperty);
            AnimationHelper.CancelAnimation(this, ContentOffsetYProperty);

            InternalViewportZoom = newContentZoom;
            ContentOffsetX = contentZoomFocus.X - (ContentViewportWidth / 2);
            ContentOffsetY = contentZoomFocus.Y - (ContentViewportHeight / 2);
        }

        #endregion Private Methods
    }
}