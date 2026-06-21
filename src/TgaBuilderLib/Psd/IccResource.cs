using System.Runtime.InteropServices;
using TgaBuilderLib.Icc;

namespace TgaBuilderLib.Psd;

public sealed class IccResource : ImageResource
{
    public IccResource()
    {
        ID = (short)ResourceIDs.ICCProfile;
    }

    protected override void StoreData()
    {
        var sRgbBuildInIccProfile = new IccProfile();

        sRgbBuildInIccProfile.PrimaryPlatformVal = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? PrimaryPlatform.Microsoft
            : PrimaryPlatform.Apple;

        Data = sRgbBuildInIccProfile.Build();
    }
}
