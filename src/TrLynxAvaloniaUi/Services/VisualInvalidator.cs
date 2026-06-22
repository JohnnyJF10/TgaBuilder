using Avalonia.Controls;
using System;
using TrLynxLib.Abstraction;

namespace TrLynxAvaloniaUi.Services
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
