using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace TgaBuilderLib.Transitions;

public partial class TransitionHelper
{
    private int Felzenszwalb(byte[] pixels, int[] labels, int min_size = 50, float scale= 100f)
    {
        return 0;
    }

    private int Slic(byte[] pixels, int[] labels, int n_segments= 250, float compactness= 10f)
    {
        return 0;
    }

    private int Quickshift(byte[] pixels, int[] labels, int max_dist= 10, float ratio= 1)
    {
        return 0;
    }
}
