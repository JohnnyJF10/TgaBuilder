namespace TgaBuilderLib.Krita;

internal static class Xml
{
    public static string Escape(string s) => s
        .Replace("&", "&amp;")        
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;")
        .Replace("'", "&apos;");
}
