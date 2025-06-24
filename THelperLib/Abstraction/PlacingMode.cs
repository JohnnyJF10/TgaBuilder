using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THelperLib.Abstraction
{
    [Flags]
    public enum PlacingMode
    {
        Default = 0,
        OverlayTransparent = 1,
        PlaceAndSwap = 2,
        ResizeToPicker = 4
    }
}
