namespace TgaBuilderAvaloniaUi.Services
{
    internal class AvaloniaUIMessage
    {
        internal string Title { get; set; }
        internal string Message { get; set; }
        internal string Accent { get; set; }
        internal string Badge { get; set; }
        internal int timeout { get; set; }

        internal AvaloniaUIMessage(
            string title,
            string message,
            string accent,
            string badge,
            int timeout)
        {
            Title = title;
            Message = message;
            Accent = accent;
            Badge = badge;
            this.timeout = timeout;
        }
    }
}
