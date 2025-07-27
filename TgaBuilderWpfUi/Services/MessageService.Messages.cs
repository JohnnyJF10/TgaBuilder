using TgaBuilderLib.Messaging;
using Wpf.Ui.Controls;

namespace TgaBuilderWpfUi.Services
{
    public partial class MessageService
    {
        private static readonly Dictionary<MessageType, WpfUiMessage> _messageDict = new()
        {
            {
                MessageType.SourceOpenSuccess,
                new WpfUiMessage(
                    "File Opened",
                    "The file was opened successfully.",
                    ControlAppearance.Success,
                    new SymbolIcon(SymbolRegular.CheckboxChecked24),
                    TimeSpan.FromSeconds(2))
            },
            {
                MessageType.SourceOpenSuccessButResized,
                new WpfUiMessage(
                    "File Opened",
                    "The file was opened successfully after resizing.",
                    ControlAppearance.Info,
                    new SymbolIcon(SymbolRegular.CheckboxWarning24),
                    TimeSpan.FromSeconds(5))
            },
            {
                MessageType.SourceOpenSuccessButIncomplete,
                new WpfUiMessage(
                    "File Opened partially",
                    "The file was opened successfully, but the Bitmap space ist not sufficient. Please try again with more horizantal pages.",
                    ControlAppearance.Info,
                    new SymbolIcon(SymbolRegular.Warning24),
                    TimeSpan.FromSeconds(10))
            },
            {
                MessageType.SourceOpenCancelledByUser,
                new WpfUiMessage(
                    "File Open Cancelled",
                    "The file open operation was cancelled by the user.",
                    ControlAppearance.Info,
                    new SymbolIcon(SymbolRegular.Info24),
                    TimeSpan.FromSeconds(5))
            },
            {
                MessageType.SourceOpenError,
                new WpfUiMessage(
                    "File Open Error",
                    "An error occurred while opening the file.",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    TimeSpan.FromSeconds(10))
            },
            {
                MessageType.DestinationOpenSuccess,
                new WpfUiMessage(
                    "Destination Opened",
                    "The destination was opened successfully.",
                    ControlAppearance.Success,
                    new SymbolIcon(SymbolRegular.CheckboxChecked24),
                    TimeSpan.FromSeconds(2))
            },
            {
                MessageType.DestinationOpenSuccessButResized,
                new WpfUiMessage(
                    "Destination Opened",
                    "The destination was opened successfully after resizing.",
                    ControlAppearance.Info,
                    new SymbolIcon(SymbolRegular.CheckboxWarning24),
                    TimeSpan.FromSeconds(5))
            },
            {
                MessageType.DestinationOpenSuccessButIncomplete,
                new WpfUiMessage(
                    "Destination Opened partially",
                    "The destination was opened successfully, but the Bitmap space is not sufficient.",
                    ControlAppearance.Info,
                    new SymbolIcon(SymbolRegular.Warning24),
                    TimeSpan.FromSeconds(10))
            },
            {
                MessageType.DestinationOpenCancelledByUser,
                new WpfUiMessage(
                    "Destination Open Cancelled",
                    "The destination open operation was cancelled by the user.",
                    ControlAppearance.Info,
                    new SymbolIcon(SymbolRegular.Info24),
                    TimeSpan.FromSeconds(5))
            },
            {
                MessageType.DestinationOpenError,
                new WpfUiMessage(
                    "Destination Open Error",
                    "An error occurred while opening the destination.",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    TimeSpan.FromSeconds(10))
            },
            {
                MessageType.DestinationSaveSuccess,
                new WpfUiMessage(
                    "Destination Saved",
                    "The destination was saved successfully.",
                    ControlAppearance.Success,
                    new SymbolIcon(SymbolRegular.CheckboxChecked24),
                    TimeSpan.FromSeconds(2))
            },
            {
                MessageType.DestinationSaveCancelledByUser,
                new WpfUiMessage(
                    "Destination Save Cancelled",
                    "The destination save operation was cancelled by the user.",
                    ControlAppearance.Info,
                    new SymbolIcon(SymbolRegular.Info24),
                    TimeSpan.FromSeconds(5))
            },
            {
                MessageType.DestinationSaveError,
                new WpfUiMessage(
                    "Destination Save Error",
                    "An error occurred while saving the destination.",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    TimeSpan.FromSeconds(10))
            },
            {
                MessageType.UnsupportedDimensions,
                new WpfUiMessage(
                    "Unsupported Dimensions",
                    "The dimensions of the image are not supported. Please use an image with dimensions that are a multiple of 8.",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.Warning24),
                    TimeSpan.FromSeconds(10))
            },
            {
                MessageType.BatchLoaderPanelExceedsMaxDimensions,
                new WpfUiMessage(
                    "Batch Loader Import Warning",
                    "The BatchLoader panel exceeds the maximum allowed dimensions. Please reduce the amount of tectures or their size.",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    TimeSpan.FromSeconds(10))
                },
            {
                MessageType.BatchLoaderFolderSetSuccess,
                new WpfUiMessage(
                    "Folder Set Succesfully",
                    "The folder was set successfully.",
                    ControlAppearance.Success,
                    new SymbolIcon(SymbolRegular.CheckboxChecked24),
                    TimeSpan.FromSeconds(2))
            },
            {
                MessageType.BatchLoaderFolderSetNoImageFiles,
                new WpfUiMessage(
                    "No Image Files Found",
                    "The specified folder does not contain any supported image files the Batch File Loader." +
                    "Supported formats are: PNG, JPG, JPEG, TGA, BMP, DDS.",
                    ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24),
                    TimeSpan.FromSeconds(5))
            },
            {
                MessageType.BatchLoaderFolderSetFail,
                new WpfUiMessage(
                    "Set Folder Failed",
                    "The specified folder does not exist or is not accessible.",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    TimeSpan.FromSeconds(10))
            },
            {
                MessageType.BatchLoaderPanelLoadIssues,
                new WpfUiMessage(
                    "Batch Loader Panel Load Issues",
                    "Some file could not been loaded into the Batch Loader panel. Please find more information in the log file.",
                    ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24),
                    TimeSpan.FromSeconds(10))
            },
            {
                MessageType.BatchLoaderDropNoImageFiles,
                new WpfUiMessage(
                    "No Image Files Dropped",
                    "The dropped files do not contain any supported image files. Supported formats are: PNG, JPG, JPEG, TGA, BMP, DDS.",
                    ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24),
                    TimeSpan.FromSeconds(5))
            },
            {
                MessageType.BatchLoaderDropSuccess,
                new WpfUiMessage(
                    "Files Dropped Successfully",
                    "The files were dropped successfully into the Batch Loader panel.",
                    ControlAppearance.Success,
                    new SymbolIcon(SymbolRegular.CheckboxChecked24),
                    TimeSpan.FromSeconds(2))
            },
            {
                MessageType.UnknownError,
                new WpfUiMessage(
                    "Unknown Error",
                    "An unknown error occurred. Please try again or contact support.",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    TimeSpan.FromSeconds(10))
            },
            {
                MessageType.UsageDataLoadError,
                new WpfUiMessage(
                    "Usage Data Load Error",
                    "An error occurred while loading usage data. Please check the log file for more information.",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    TimeSpan.FromSeconds(10))
            },
            {
                MessageType.SortedRezisingNoPossible,
                new WpfUiMessage(
                    "Sorted Resizing Not Possible",
                    $"Sorted resizing is not possible as the resulting new number of pages in Y direction would be larger than {MAX_NUM_PAGES}. " +
                    "Please reduce the height at first or do width resizing without the sorting option selected.",
                    ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24),
                    TimeSpan.FromSeconds(10))
            },
            {
                MessageType.ClipboardNotContainingImageData,
                new WpfUiMessage(
                    "Clipboard Not Containing Image Data",
                    "The clipboard does not contain image data.",
                    ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24),
                    TimeSpan.FromSeconds(5))
            },
            {
                MessageType.ClipboardPasteError,
                new WpfUiMessage(
                    "Clipboard Paste Error",
                    "An error occurred while pasting the image from the clipboard. Please check the log file for more information.",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    TimeSpan.FromSeconds(10))
            }
        };

    }
}
