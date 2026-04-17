using Avalonia.Controls;
using System;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderAvaloniaUi.Services
{
    internal class VisualInvalidatorFactory : IVisualInvalidatorFactory
    {
        public IVisualInvalidator Create(object target)
        {
            if (target is Image image)
                return new ImageVisualInvalidator(image);

            throw new ArgumentException(
                $"Expected an Avalonia Image control, but got {target.GetType().Name}.",
                nameof(target));
        }
    }
}
