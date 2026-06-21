using Avalonia.Controls;
using System;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderAvaloniaUi.Services
{
    internal class VisualInvalidator : IVisualInvalidator
    {
        private readonly Image _image;

        public VisualInvalidator(Image image)
        {
            _image = image ?? throw new ArgumentNullException(nameof(image));
        }

        public void InvalidateVisual()
        {
            _image.InvalidateVisual();
        }
    }
}
