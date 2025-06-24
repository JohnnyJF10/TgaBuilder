using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THelperLib.Messaging
{
    public enum MessageType
    {
        SourceOpenSuccess,
        SourceOpenSuccessButResized,
        SourceOpenSuccessButIncomplete,
        SourceOpenError,

        DestinationOpenSuccess,
        DestinationOpenSuccessButResized,
        DestinationOpenSuccessButIncomplete,
        DestinationOpenError,

        DestinationSaveSuccess,
        DestinationSaveError,

        UnsupportedDimensions,

        BatchLoaderPanelExceedsMaxDimensions,

        BatchLoaderFolderSetSuccess,
        BatchLoaderFolderSetFail,

        UnknownError,
        UsageDataLoadError,
        SortedRezisingNoPossible,
    }
}
