using System.Runtime.InteropServices;
using TrLynxLib.Icc;

namespace TrLynxLib.Psd;

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
