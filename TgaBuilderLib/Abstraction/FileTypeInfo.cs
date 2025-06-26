namespace TgaBuilderLib.Abstraction
{
    public record FileTypeInfo
    {
        public string Extension { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }
}
