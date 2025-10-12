
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.VisualTree;
using System;

namespace TgaBuilderAvaloniaUi.View
{
    public partial class MainWindow
    {
        public ZoomBorder GetPanelFromImage(Image image)
        {
            if ((_imagePanelDict ??= new()).TryGetValue(image, out var panel))
                return panel;
            else
            {
                var FE = image.GetVisualParent();
                while (FE.GetType() != typeof(ZoomBorder))
                {
                    FE = FE.GetVisualParent();
                    if (FE == null) throw new Exception("ZoomBorder not found");
                }
                _imagePanelDict[image] = (ZoomBorder)FE;
                return (ZoomBorder)FE;
            }
        }

        public void SetPanelFromImage(Image image) 
            => CurrentPanel = GetPanelFromImage(image);
    }
}
