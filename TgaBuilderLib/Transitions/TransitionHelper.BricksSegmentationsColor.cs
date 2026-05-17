using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace TgaBuilderLib.Transitions;

public partial class TransitionHelper
{
    private int Felzenszwalb(float[] filtered, int[] labels, int min_size = 50, float scale= 100)
    {
        return 0;
    }

    private int Slic(float[] filtered, int[] labels, int n_segments= 250, float compactness= 10f)
    {
        return 0;
    }

    private int Quickshift(float[] filtered, int[] labels, int max_dist= 10, float ratio= 1)
    {
        return 0;
    }
}
