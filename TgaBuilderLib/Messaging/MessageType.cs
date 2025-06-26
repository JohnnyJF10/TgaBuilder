namespace TgaBuilderLib.Messaging
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
        BatchLoaderDropNoImageFiles,
        BatchLoaderDropSuccess,

        UsageDataLoadError,

        ClipboardNotContainingImageData,
        ClipboardPasteError,

        UnknownError,
    }
}
