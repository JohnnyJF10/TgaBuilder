using TrLynxLib.Abstraction;

namespace TrLynxLib.Utils
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
