using System.Windows.Media;
using Wpf.Ui.Controls;
using WPFZoomPanel;
using Image = System.Windows.Controls.Image;


namespace TgaBuilderWpfUi.View
{
    public partial class MainWindow
    {
        public ZoomPanel GetPanelFromImage(Image image)
        {
            if ((_imagePanelDict ??= new()).TryGetValue(image, out var panel))
                return panel;
            else
            {
                var FE = VisualTreeHelper.GetParent(image);
                while (FE.GetType() != typeof(ZoomPanel))
                {
                    FE = VisualTreeHelper.GetParent(FE);
                    if (FE == null) throw new Exception("ZoomPanel not found");
                }
                _imagePanelDict[image] = (ZoomPanel)FE;
                return (ZoomPanel)FE;
            }
        }

        public SnackbarPresenter SnackbarPresenter => MessageSnackbarPresenter;

        public void SetPanelFromImage(Image image) 
            => CurrentPanel = GetPanelFromImage(image);
    }
}
