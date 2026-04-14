using System;
using System.Collections.Generic;
using System.Text;

namespace TgaBuilderLib.Transitions
{
    public class TileSegment
    {
        public float CentroidX { get; set; }
        public float CentroidY { get; set; }
        public List<int> PixelOffsets { get; set; } = new List<int>();

        // Hilfsvariablen für die Berechnung
        internal long SumX;
        internal long SumY;
    }
}
