namespace TgaBuilderLib.Messaging
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
        SortedRezisingNoPossible,

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
