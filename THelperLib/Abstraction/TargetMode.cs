using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THelperLib.Abstraction
{
    public enum TargetMode
    {
        Default,

        ClockwiseRotating,
        CounterClockwiseRotating,
        MirrorHorizontal,
        MirrorVertical,

        TileSwapping,
        TileMoving,
        Animating
    }
}
