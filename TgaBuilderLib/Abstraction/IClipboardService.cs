using System.Collections.Generic;

namespace TgaBuilderLib.Abstraction
{
    public interface IClipboardService
    {
        /// <summary>
        /// Sets the given bitmap into the clipboard.
        /// </summary>
        /// <param name="bitmap">The bitmap to set.</param>
        void SetImage(IReadableBitmap bitmap);

        /// <summary>
        /// Checks if the clipboard contains an image.
        /// </summary>
        /// <returns>True, if an image is present.</returns>
        bool ContainsImage();

        /// <summary>
        /// Returns the image from the clipboard.
        /// </summary>
        /// <returns>The bitmap or null if no image is present.</returns>
        IReadableBitmap? GetImage();
    }
}

