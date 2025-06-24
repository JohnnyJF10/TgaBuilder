using System.Windows;
using System.Windows.Media;

namespace WPFZoomPanel.Helpers
{
    public static class VisualTreeHelpers
    {
        #region Public Methods

        /// <summary>
        /// Recursively searches the visual tree for the first child of type T.
        /// </summary>
        public static T? FindChildControl<T>(this DependencyObject control) where T : DependencyObject
        {
            if (control == null)
                return null;

            int childCount = VisualTreeHelper.GetChildrenCount(control);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(control, i);

                if (child is T typedChild)
                    return typedChild;

                T? result = FindChildControl<T>(child);
                if (result != null)
                    return result;
            }

            return null; 
        }

        /// <summary>
        /// Find first parent of type T in VisualTree.
        /// </summary>
        public static T? FindParentControl<T>(this DependencyObject control) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(control);
            while (parent != null && parent is not T)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as T;
        }

        #endregion Public Methods
    }
}
