namespace TrLynxLib.Psd;

public sealed class GridGuidesInfo : ImageResource
{
    /// <summary>
    /// Version of the Grid and Guides resource. Currently 1.
    /// </summary>
    public uint Version { get; private set; } = 1;

    /// <summary>
    /// Horizontal grid orientation. 0=normal, 1=90° CCW, 2=180°, 3=90° CW
    /// </summary>
    public uint Horizontal { get; private set; }

    /// <summary>
    /// Vertical grid orientation. 0=normal, 1=90° CCW, 2=180°, 3=90° CW
    /// </summary>
    public uint Vertical { get; private set; }

    /// <summary>
    /// Data of the Grid and Guides Resource Block
    /// </summary>
    public uint GGData { get; private set; } = 0;


    public GridGuidesInfo()
    {
        ID = (short)ResourceIDs.GridGuidesInfo;
    }

    /// <summary>
    /// Creates a GridGuidesInfo resource for writing into a PSD file.
    /// </summary>
    public GridGuidesInfo(
        uint horizontal, uint vertical)
    {
        ID = (short)ResourceIDs.GridGuidesInfo;
        Horizontal = horizontal;
        Vertical = vertical;
    }

    public GridGuidesInfo(ImageResource imgRes)
        : base(imgRes)
    {
        using (BinaryReverseReader reverseReader = imgRes.DataReader)
        {
            Version = reverseReader.ReadUInt32();
            Horizontal = reverseReader.ReadUInt32();
            Vertical = reverseReader.ReadUInt32();
            GGData = reverseReader.ReadUInt32();

        }
    }

    protected override void StoreData()
    {
        using (var memoryStream = new MemoryStream())
        using (var reverseWriter = new BinaryReverseWriter(memoryStream))
        {
            reverseWriter.Write(Version);
            reverseWriter.Write(Horizontal);
            reverseWriter.Write(Vertical);
            reverseWriter.Write(GGData);

            Data = memoryStream.ToArray();
        }
    }

    public override string ToString()
    {
        return $"GridGuided, Ver: {Version}, Vert: {Vertical}, Hor: {Horizontal}";
    }
}
