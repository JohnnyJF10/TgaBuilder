using System.Windows.Media;

namespace THelperLib.Utils
{
    public interface IEyeDropper
    {
        Color Color { get; set; }
        bool IsActive { get; set; }
    }
}