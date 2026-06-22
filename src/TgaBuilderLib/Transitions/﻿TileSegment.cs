namespace TgaBuilderLib.Transitions;

public class TileSegment : ICloneable
{
    public float CentroidX { get; set; }
    public float CentroidY { get; set; }
    public List<int> PixelOffsets { get; set; } = new List<int>();

    public bool? ShouldDrawExplicitly { get; set; } = null;

    public int OffsetX { get; set; } = 0;
    public int OffsetY { get; set; } = 0;

    public float TwistAngle { get; set; } = 0.0f;

    public object Clone()
    {
        var pixelOffsets = new List<int>(this.PixelOffsets);

        return new TileSegment
        {
            CentroidX = this.CentroidX,
            CentroidY = this.CentroidY,
            PixelOffsets = pixelOffsets,
            ShouldDrawExplicitly = this.ShouldDrawExplicitly,
            OffsetX = this.OffsetX,
            OffsetY = this.OffsetY,
            TwistAngle = this.TwistAngle
        };
    }
}
