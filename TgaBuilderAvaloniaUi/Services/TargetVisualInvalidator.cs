using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderAvaloniaUi.Services
{
    internal class ImageVisualInvalidator : IVisualInvalidator
    {
        private readonly Image _image;

        public ImageVisualInvalidator(Image image)
        {
            _image = image ?? throw new ArgumentNullException(nameof(image));
        }

        public void InvalidateVisual()
        {
            _image.InvalidateVisual();
        }
    }
}
