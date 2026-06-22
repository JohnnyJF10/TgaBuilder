namespace TrLynxLib.Messaging
{
    public enum MessageType
    {
        SourceOpenSuccess,
        SourceOpenSuccessButResized,
        SourceOpenSuccessButIncomplete,
        SourceOpenFirstFileReached,
        SourceOpenLastFileReached,
        SourceOpenCancelledByUser,
        SourceOpenError,

        DestinationOpenSuccess,
        DestinationOpenSuccessButResized,
        DestinationOpenSuccessButIncomplete,
        DestinationOpenCancelledByUser,
        DestinationOpenError,

        DestinationSaveSuccess,
        DestinationSaveCancelledByUser,
        DestinationSaveError,

        UnsupportedDimensions,
        SortedResizingNotPossible,

        BatchLoaderPanelExceedsMaxDimensions,
        BatchLoaderFolderSetSuccess,
        BatchLoaderFolderSetNoImageFiles,
        BatchLoaderFolderSetFail,
        BatchLoaderPanelLoadIssues,
        BatchLoaderDropNoImageFiles,
        BatchLoaderDropSuccess,

        UsageDataLoadError,

        ClipboardNotContainingImageData,
        ClipboardPasteError,

        UnknownError,
    }
}
