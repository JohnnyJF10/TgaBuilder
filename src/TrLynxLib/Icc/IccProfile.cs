namespace TrLynxLib.Icc;

/* The following implementation has Krita's sRGB built-in ICC profile 
 * as the default layout and tag content source.
 * 
 * Krita's information text regarding the sRGB built-in ICC profile:
 * 
 * About RGB/Alpha (8-bit Ganzzahl pro Kanal)/sRGB built-in
 * ========================================================
 * ICC Version: 4.4
 * ----------------
 * Copyright: No copyright, use freely
 * ----------------
 * RGB (Red, Green, Blue) https://en.wikipedia.org/wiki/RGB_color_spaces, 
 * is the color model used by screens and other light-based media.
 * RGB is an additive color model: adding colors together makes them brighter. This color 
 * model is the most extensive of all color models, and is recommended as a model for 
 * painting,that you can later convert to other spaces. RGB is also the recommended 
 * colorspace for HDR editing.
 * ----------------
 * 8 bit integer: 
 * The default number of colors per channel. Each channel will have 256 
 * values available, leading to a total amount of colors of 256 to the power of the 
 * number of channels. Recommended to use for images intended for the web, or otherwise 
 * simple images.
 * The following conversion intents are possible: 
 *  Relatively colorimetric
 */

/// <summary>
/// Configurable ICC v4 color profile builder.
/// Every property is pre-initialized with Krita's sRGB built-in ICC defaults.
/// Callers may override any subset of properties before calling <see cref="Build"/>
/// or <see cref="Save"/>.
/// </summary>
public partial class IccProfile
{
    // ===================================================================
    //                     CONFIGURABLE PROPERTIES
    // ===================================================================

    // --------- Header ---------
    /// <summary>Preferred CMM signature (4 chars, "" for none).</summary>
    public string PreferredCmm { get; set; } = "lcms";

    /// <summary>Profile version as (Major, Minor, Bugfix).</summary>
    public (int Major, int Minor, int Bugfix) ProfileVersion { get; set; } = (4, 4, 0);

    /// <summary>Profile/device class: scnr, mntr, prtr, link, spac, abst, nmcl.</summary>
    public string ProfileClass { get; set; } = "mntr";

    /// <summary>Data color space (4 chars, e.g. "RGB ").</summary>
    public string DataColorSpace { get; set; } = "RGB ";

    /// <summary>Profile connection space ("XYZ " or "Lab ").</summary>
    public string Pcs { get; set; } = "XYZ ";

    /// <summary>Creation date/time. If null, <see cref="DateTime.UtcNow"/> is used at build time.</summary>
    public DateTime? CreationDateTime { get; set; }

    /// <summary>Primary platform: APPL, MSFT, SGI , SUNW, TGNT, or "" for none.</summary>
    public PrimaryPlatform PrimaryPlatformVal { get; set; } = PrimaryPlatform.Apple;

    /// <summary>Profile flags. Bit 0=embedded, bit 1=cannot be used independently.</summary>
    public uint ProfileFlags { get; set; } = 0x00000000u;

    /// <summary>Device manufacturer signature (4 chars or "").</summary>
    public string DeviceManufacturer { get; set; } = "";

    /// <summary>Device model signature (4 chars or "").</summary>
    public string DeviceModel { get; set; } = "";

    /// <summary>
    /// Device attributes (uint64).
    /// Bit 0: 1=transparency, 0=reflective
    /// Bit 1: 1=matte,        0=glossy
    /// Bit 2: 1=negative,     0=positive
    /// Bit 3: 1=B&amp;W,      0=colour
    /// </summary>
    public ulong DeviceAttributes { get; set; } = 0x0000000000000000UL;

    /// <summary>Rendering intent: 0=Perceptual, 1=RelCol, 2=Sat, 3=AbsCol.</summary>
    public uint RenderingIntent { get; set; } = 0;

    /// <summary>PCS illuminant (must be D50 per spec).</summary>
    public (double X, double Y, double Z) PcsIlluminantXyz { get; set; } =
        (0.964203, 1.000000, 0.824905);

    /// <summary>Profile creator signature (4 chars).</summary>
    public string ProfileCreator { get; set; } = "lcms";

    // --------- Text tags ---------
    /// <summary>Localized profile description records ('desc' tag).</summary>
    public List<MlucRecord> DescriptionRecords { get; set; } = new()
    {
        new MlucRecord("en", "US", "sRGB built-in"),
    };

    /// <summary>Localized copyright records ('cprt' tag).</summary>
    public List<MlucRecord> CopyrightRecords { get; set; } = new()
    {
        new MlucRecord("en", "US", "No copyright, use freely"),
    };

    // --------- Colorimetric tags ---------

    /// <summary>Media white point ('wtpt'); typically equals PCS illuminant for displays.</summary>
    public (double X, double Y, double Z) MediaWhitePointXyz { get; set; } =
        (0.964203, 1.000000, 0.824905);

    /// <summary>3x3 row-major chromatic adaptation matrix ('chad'), Bradford-style D50 adaptation.</summary>
    public double[] ChromaticAdaptationMatrix { get; set; } = new double[]
    {
        +1.047882, +0.022919, -0.050217,
        +0.029587, +0.990479, -0.017075,
        -0.009247, +0.015076, +0.751678,
    };

    public (double X, double Y, double Z) RedMatrixColumnXyz { get; set; } = (0.436035, 0.222488, 0.013916);
    public (double X, double Y, double Z) GreenMatrixColumnXyz { get; set; } = (0.385117, 0.716904, 0.097061);
    public (double X, double Y, double Z) BlueMatrixColumnXyz { get; set; } = (0.143051, 0.060608, 0.713913);

    // --------- Tone Reproduction Curves ---------

    /// <summary>Red TRC. Defaults to sRGB-style parametric curve (function type 3).</summary>
    public ToneReproductionCurveSpec RedTrc { get; set; } = ToneReproductionCurveSpec.Para(3,
        new double[] { 2.399994, 0.947861, 0.052139, 0.077393, 0.040451 });

    /// <summary>Green TRC.</summary>
    public ToneReproductionCurveSpec GreenTrc { get; set; } = ToneReproductionCurveSpec.Para(3,
        new double[] { 2.399994, 0.947861, 0.052139, 0.077393, 0.040451 });

    /// <summary>Blue TRC.</summary>
    public ToneReproductionCurveSpec BlueTrc { get; set; } = ToneReproductionCurveSpec.Para(3,
        new double[] { 2.399994, 0.947861, 0.052139, 0.077393, 0.040451 });

    /// <summary>
    /// If true and all three TRC byte blocks are identical, rTRC/gTRC/bTRC will share
    /// one offset (matches the analyzed sRGB built-in layout).
    /// </summary>
    public bool ShareIdenticalTrcBlocks { get; set; } = true;

    // --------- Chromaticity tag ('chrm') ---------

    /// <summary>Phosphor type: 0=Unknown, 1=BT.709, 2=SMPTE RP145, 3=EBU 3213, 4=P22.</summary>
    public int PhosphorType { get; set; } = 0;

    /// <summary>Chromaticity coordinates per channel (R, G, B).</summary>
    public List<(double X, double Y)> ChromaticityXy { get; set; } = new()
    {
        (0.639999, 0.330002),   // Red
        (0.300003, 0.600006),   // Green
        (0.149994, 0.059998),   // Blue
    };

    // --------- Profile ID ---------

    /// <summary>If true, compute the spec-defined MD5 profile ID per ICC.1:2010 §7.2.18.</summary>
    public bool ComputeProfileId { get; set; } = false;

    // ===================================================================
    //                  INTERNAL: layout helper records
    // ===================================================================

    private sealed record TagBlob(string Signature, byte[] Bytes, string? ShareKey);
    private sealed record TagEntry(string Signature, int Offset, int Length, byte[] Blob, bool IsFirstOccurrence);
}

