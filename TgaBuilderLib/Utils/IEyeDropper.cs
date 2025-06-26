using System.Windows.Media;

namespace TgaBuilderLib.Utils
{
    public interface IEyeDropper
    {
        Color Color { get; set; }
        bool IsActive { get; set; }
    }
}