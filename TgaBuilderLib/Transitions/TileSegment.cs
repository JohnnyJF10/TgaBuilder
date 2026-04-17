using System;
using System.Collections.Generic;
using System.Text;

namespace TgaBuilderLib.Transitions
{
    public class TileSegment : ICloneable
    {
        public float CentroidX { get; set; }
        public float CentroidY { get; set; }
        public List<int> PixelOffsets { get; set; } = new List<int>();

        // Hilfsvariablen für die Berechnung
        internal long SumX;
        internal long SumY;

        public object Clone()
        {
            var pixelOffsets = new List<int>(this.PixelOffsets);
            foreach (var offset in this.PixelOffsets)
            {
                pixelOffsets.Add(offset);
            }

            return new TileSegment
            {
                CentroidX = this.CentroidX,
                CentroidY = this.CentroidY,
                PixelOffsets = pixelOffsets,
                SumX = this.SumX,
                SumY = this.SumY
            };
        }
    }
}
