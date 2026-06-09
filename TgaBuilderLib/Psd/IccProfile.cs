using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.Psd;

public sealed class IccProfile : ImageResource
{
    /* Krita's information text regarding the sRGB build-in ICC profile
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
    /// Krita's sRGB build-in ICC profile base64 encoded
    /// </summary>
    //private readonly string sRgbBuildInIccStr =
    //"AAACTGxjbXMEQAAAbW50clJHQiBYWVogB+oABgAFAA8AOAAfYWNzcEFQUEwAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
    //"AAAAAAAPbWAAEAAAAA0y1sY21zAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
    //"AAALZGVzYwAAAQgAAAA2Y3BydAAAAUAAAABMd3RwdAAAAYwAAAAUY2hhZAAAAaAAAAAsclhZWgAAAcwAAAAUYl" +
    //"hZWgAAAeAAAAAUZ1hZWgAAAfQAAAAUclRSQwAAAggAAAAgZ1RSQwAAAggAAAAgYlRSQwAAAggAAAAgY2hybQAA" +
    //"AigAAAAkbWx1YwAAAAAAAAABAAAADGVuVVMAAAAaAAAAHABzAFIARwBCACAAYgB1AGkAbAB0AC0AaQBuAABtbH" +
    //"VjAAAAAAAAAAEAAAAMZW5VUwAAADAAAAAcAE4AbwAgAGMAbwBwAHkAcgBpAGcAaAB0ACwAIAB1AHMAZQAgAGYA" +
    //"cgBlAGUAbAB5WFlaIAAAAAAAAPbWAAEAAAAA0y1zZjMyAAAAAAABDEIAAAXe///zJQAAB5MAAP2Q///7of///a" +
    //"IAAAPcAADAblhZWiAAAAAAAABvoAAAOPUAAAOQWFlaIAAAAAAAACSfAAAPhAAAtsNYWVogAAAAAAAAYpcAALeH" +
    //"AAAY2XBhcmEAAAAAAAMAAAACZmYAAPKnAAANWQAAE9AAAApbY2hybQAAAAAAAwAAAACj1wAAVHsAAEzNAACZmg" +
    //"AAJmYAAA9c";

    private readonly string sRgbBuildInIccStr =
    "AAACTGxjbXMEQAAAbW50clJHQiBYWVogB+oABgAJAAkANgAcYWNzcE1TRlQAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAPbWAAEAAAAA0y1sY21zAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAALZGVzYwAAAQgAAAA2Y3BydAAAAUAAAABMd3RwdAAAAYwAAAAUY2hhZAAAAaAAAAAsclhZWgAAAc" +
        "wAAAAUYlhZWgAAAeAAAAAUZ1hZWgAAAfQAAAAUclRSQwAAAggAAAAgZ1RSQwAAAggAAAAgYlRSQwAAAggA" +
        "AAAgY2hybQAAAigAAAAkbWx1YwAAAAAAAAABAAAADGVuVVMAAAAaAAAAHABzAFIARwBCACAAYgB1AGkAbA" +
        "B0AC0AaQBuAABtbHVjAAAAAAAAAAEAAAAMZW5VUwAAADAAAAAcAE4AbwAgAGMAbwBwAHkAcgBpAGcAaAB0" +
        "ACwAIAB1AHMAZQAgAGYAcgBlAGUAbAB5WFlaIAAAAAAAAPbWAAEAAAAA0y1zZjMyAAAAAAABDEIAAAXe//" +
        "/zJQAAB5MAAP2Q///7of///aIAAAPcAADAblhZWiAAAAAAAABvoAAAOPUAAAOQWFlaIAAAAAAAACSfAAAP" +
        "hAAAtsNYWVogAAAAAAAAYpcAALeHAAAY2XBhcmEAAAAAAAMAAAACZmYAAPKnAAANWQAAE9AAAApbY2hybQ" +
        "AAAAAAAwAAAACj1wAAVHsAAEzNAACZmgAAJmYAAA9c";


    public IccProfile()
    {
        ID = (short)ResourceIDs.ICCProfile;
    }

    protected override void StoreData()
    {
        using (var memoryStream = new MemoryStream())
        using (var reverseWriter = new BinaryWriter(memoryStream))
        {
            reverseWriter.Write(Convert.FromBase64String(sRgbBuildInIccStr));

            Data = memoryStream.ToArray();
        }
    }

}
