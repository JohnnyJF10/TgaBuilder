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
        SortedRezisingNoPossible,

        BatchLoaderPanelExceedsMaxDimensions,
        BatchLoaderFolderSetSuccess,
        BatchLoaderFolderSetNoImageFiles,
        BatchLoaderFolderSetFail,
        BatchLoaderPanelLoadIssues,

        UsageDataLoadError,

        ClipboardNotContainingImageData,
        ClipboardPasteError,

        UnknownError,
    }
}
