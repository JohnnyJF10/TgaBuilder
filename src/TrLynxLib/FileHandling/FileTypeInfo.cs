namespace TrLynxLib.FileHandling
{
    /// <summary>
    /// Platform-neutral metadata for a single file type, used by the WPF and Avalonia
    /// file services to build open/save dialog filters from one shared source.
    /// </summary>
    /// <param name="Extension">File extension without the leading dot (e.g. "psd").</param>
    /// <param name="DisplayName">Human-readable name (e.g. "Photoshop Files").</param>
    public sealed record FileTypeInfo(string Extension, string DisplayName);
}
