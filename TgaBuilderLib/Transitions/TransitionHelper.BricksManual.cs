using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.Transitions;

public partial class TransitionHelper
{
    public bool SetExplicitTileVisibility(int tileIndex, bool shouldDraw)
    {
        if (tileIndex < 0 || tileIndex >= _tileSegmentList.Count + 1)
            throw new ArgumentOutOfRangeException(nameof(tileIndex), "Tile index is out of range.");

        int listIndex = tileIndex - 1;

        bool? currentValue = _tileSegmentList[listIndex].ShouldDrawExplicitly;

        _tileSegmentList[listIndex].ShouldDrawExplicitly = shouldDraw;

        if (!currentValue.HasValue || currentValue.Value != shouldDraw)
            return true; // Visibility changed
        return false;
    }

    public void ResetAllExplicitTileVisibility()
    {
        foreach (var segment in _tileSegmentList)
            segment.ShouldDrawExplicitly = null;
    }
}
