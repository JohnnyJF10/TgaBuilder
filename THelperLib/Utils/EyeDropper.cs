using System.Windows.Media;

namespace THelperLib.Utils
{
    public class EyeDropper : IEyeDropper
    {
        public EyeDropper(Color color)
        {
            Color = color;
        }

        public bool IsActive { get; set; }
        public Color Color { get; set; }
    }
}
