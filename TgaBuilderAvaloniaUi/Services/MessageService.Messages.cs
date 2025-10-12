using System.Collections.Generic;
using TgaBuilderLib.Messaging;

namespace TgaBuilderAvaloniaUi.Services
{
    internal partial class MessageService
    {
        private static readonly Dictionary<MessageType, AvaloniaUIMessage> _messageDict = new()
        {
            {
                MessageType.SourceOpenSuccess,
                new AvaloniaUIMessage(
                    title: "Information",
                    message: "The source image was loaded successfully.",
                    accent: "#4CAF50",
                    badge: "Info",
                    timeout: 5)
            },
            {
                MessageType.SourceOpenSuccessButResized,
                new AvaloniaUIMessage(
                    title: "Warning",
                    message: "The source image was resized to fit the panel dimensions.",
                    accent: "#FFC107",
                    badge: "Warning",
                    timeout: 10)
            },
            {
                MessageType.SourceOpenSuccessButIncomplete,
                new AvaloniaUIMessage(
                    title: "Warning",
                    message: "The source image was only partially loaded. Some frames may be missing.",
                    accent: "#FFC107",
                    badge: "Warning",
                    timeout: 10)
            },
            {
                MessageType.SourceOpenFirstFileReached,
                new AvaloniaUIMessage(
                    title: "Information",
                    message: "The first file in the sequence has been reached.",
                    accent: "#4CAF50",
                    badge: "Info",
                    timeout: 5)
            },
            {
                MessageType.SourceOpenLastFileReached,
                new AvaloniaUIMessage(
                    title: "Information",
                    message: "The last file in the sequence has been reached.",
                    accent: "#4CAF50",
                    badge: "Info",
                    timeout: 5)
            },
            {
                MessageType.SourceOpenCancelledByUser,
                new AvaloniaUIMessage(
                    title: "Information",
                    message: "Source image loading was cancelled by the user.",
                    accent: "#4CAF50",
                    badge: "Info",
                    timeout: 5)
            },
            {
                MessageType.SourceOpenError,
                new AvaloniaUIMessage(
                    title: "Error",
                    message: "An error occurred while loading the source image.",
                    accent: "#F44336",
                    badge: "Error",
                    timeout: 10)
            },
            {
                MessageType.DestinationOpenSuccess,
                new AvaloniaUIMessage(
                    title: "Information",
                    message: "The destination image was loaded successfully.",
                    accent: "#4CAF50",
                    badge: "Info",
                    timeout: 5)
            },
            {
                MessageType.DestinationOpenSuccessButResized,
                new AvaloniaUIMessage(
                    title: "Warning",
                    message: "The destination image was resized to fit the panel dimensions.",
                    accent: "#FFC107",
                    badge: "Warning",
                    timeout: 10)
            },
            {
                MessageType.DestinationOpenSuccessButIncomplete,
                new AvaloniaUIMessage(
                    title: "Warning",
                    message: "The destination image was only partially loaded. Some frames may be missing.",
                    accent: "#FFC107",
                    badge: "Warning",
                    timeout: 10)
            },
            {
                MessageType.DestinationOpenCancelledByUser,
                new AvaloniaUIMessage(
                    title: "Information",
                    message: "Destination image loading was cancelled by the user.",
                    accent: "#4CAF50",
                    badge: "Info",
                    timeout: 5)
            },
            {
                MessageType.DestinationOpenError,
                new AvaloniaUIMessage(
                    title: "Error",
                    message: "An error occurred while loading the destination image.",
                    accent: "#F44336",
                    badge: "Error",
                    timeout: 10)
            },
            {
                MessageType.DestinationSaveSuccess,
                new AvaloniaUIMessage(
                    title: "Information",
                    message: "The destination image was saved successfully.",
                    accent: "#4CAF50",
                    badge: "Info",
                    timeout: 5)
            },
            {
                MessageType.DestinationSaveCancelledByUser,
                new AvaloniaUIMessage(
                    title: "Information",
                    message: "Destination image saving was cancelled by the user.",
                    accent: "#4CAF50",
                    badge: "Info",
                    timeout: 5)
            },
            {
                MessageType.DestinationSaveError,
                new AvaloniaUIMessage(
                    title: "Error",
                    message: "An error occurred while saving the destination image.",
                    accent: "#F44336",
                    badge: "Error",
                    timeout: 10)
            },
            {
                MessageType.UnsupportedDimensions,
                new AvaloniaUIMessage(
                    title: "Error",
                    message: "The image dimensions are not supported. Please use images with dimensions that are powers of two (e.g., 64x64, 128x128, 256x256).",
                    accent: "#F44336",
                    badge: "Error",
                    timeout: 15)
            },
            {
                MessageType.SortedRezisingNoPossible,
                new AvaloniaUIMessage(
                    title: "Error",
                    message: "Sorted resizing is not possible with the current image dimensions. Please use images with dimensions that are powers of two (e.g., 64x64, 128x128, 256x256).",
                    accent: "#F44336",
                    badge: "Error",
                    timeout: 15)
            },
            {
                MessageType.BatchLoaderPanelExceedsMaxDimensions,
                new AvaloniaUIMessage(
                    title: "Error",
                    message: "The panel dimensions exceed the maximum allowed size of 2048x2048 pixels.",
                    accent: "#F44336",
                    badge: "Error",
                    timeout: 15)
            },
            {
                MessageType.BatchLoaderFolderSetSuccess,
                new AvaloniaUIMessage(
                    title: "Information",
                    message: "The folder was set successfully and image files were found.",
                    accent: "#4CAF50",
                    badge: "Info",
                    timeout: 5)
            },
            {
                MessageType.BatchLoaderFolderSetNoImageFiles,
                new AvaloniaUIMessage(
                    title: "Warning",
                    message: "No image files were found in the selected folder.",
                    accent: "#FFC107",
                    badge: "Warning",
                    timeout: 10)
            },
            {
                MessageType.BatchLoaderFolderSetFail,
                new AvaloniaUIMessage(
                    title: "Error",
                    message: "Failed to set the folder. Please check if the folder exists and is accessible.",
                    accent: "#F44336",
                    badge: "Error",
                    timeout: 10)
            },
            {
                MessageType.BatchLoaderPanelLoadIssues,
                new AvaloniaUIMessage(
                    title: "Warning",
                    message: "Some panels could not be loaded due to issues with the image files.",
                    accent: "#FFC107",
                    badge: "Warning",
                    timeout: 10)
            },
            {
                MessageType.BatchLoaderDropNoImageFiles,
                new AvaloniaUIMessage(
                    title: "Warning",
                    message: "No valid image files were found in the dropped items.",
                    accent: "#FFC107",
                    badge: "Warning",
                    timeout: 10)
            },
            {
                MessageType.BatchLoaderDropSuccess,
                new AvaloniaUIMessage(
                    title: "Information",
                    message: "Image files were successfully added from the dropped items.",
                    accent: "#4CAF50",
                    badge: "Info",
                    timeout: 5)
            },
            {
                MessageType.UsageDataLoadError,
                new AvaloniaUIMessage(
                    title: "Error",
                    message: "An error occurred while loading usage data.",
                    accent: "#F44336",
                    badge: "Error",
                    timeout: 10)
            },
            {
                MessageType.ClipboardNotContainingImageData,
                new AvaloniaUIMessage(
                    title: "Warning",
                    message: "The clipboard does not contain any image data to paste.",
                    accent: "#FFC107",
                    badge: "Warning",
                    timeout: 10)
            },
            {
                MessageType.ClipboardPasteError,
                new AvaloniaUIMessage(
                    title: "Error",
                    message: "An error occurred while pasting image data from the clipboard.",
                    accent: "#F44336",
                    badge: "Error",
                    timeout: 10)
            },
            {
                MessageType.UnknownError,
                new AvaloniaUIMessage(
                    title: "Error",
                    message: "An unknown error has occurred. Please try again.",
                    accent: "#F44336",
                    badge: "Error",
                    timeout: 10)
            },
        };
    }
}
