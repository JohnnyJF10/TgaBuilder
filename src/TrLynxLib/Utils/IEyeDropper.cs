using TrLynxLib.Abstraction;

namespace TrLynxLib.Utils
{
    public interface IEyeDropper
    {
        Color Color { get; set; }
        bool IsActive { get; set; }
    }
}