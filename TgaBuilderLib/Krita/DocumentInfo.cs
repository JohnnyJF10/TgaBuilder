namespace TgaBuilderLib.Krita;

public class DocumentInfo
{
    public string ImageName { get; set; } = "Unnamed";
    public string Description { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Abstract { get; set; } = string.Empty;
    public string Keyword { get; set; } = string.Empty;
    public string InitialCreator { get; set; } = "TgaBuilder";
    public int EditingCycles { get; set; } = 1;
    public int EditingTime { get; set; } = 0;
    public string CreatorFirstName { get; set; } = string.Empty;
    public string CreatorLastName { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public string AuthorFullName { get; set; } = string.Empty;


    public string Build()
        =>  "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            "<!DOCTYPE document-info PUBLIC '-//KDE//DTD document-info 1.1//EN' 'http://www.calligra.org/DTD/document-info-1.1.dtd'>\n" +
            "<document-info xmlns=\"http://www.calligra.org/DTD/document-info\">\n" +
            " <about>\n" +
            $"  <title>{Xml.Escape(ImageName)}</title>\n" +
            $"  <description>{Xml.Escape(Description)}</description>\n" +
            $"  <subject>{Xml.Escape(Subject)}</subject>\n" +
            $"  <abstract>{Xml.Escape(Abstract)}</abstract>\n" +
            $"  <keyword>{Xml.Escape(Keyword)}</keyword>\n" +
            $"  <initial-creator>{Xml.Escape(InitialCreator)}</initial-creator>\n" +
            $"  <editing-cycles>{EditingCycles}</editing-cycles>\n" +
            $"  <editing-time>{EditingTime}</editing-time>\n" +
            $"  <creator-first-name>{Xml.Escape(CreatorFirstName)}</creator-first-name>\n" +
            $"  <creator-last-name>{Xml.Escape(CreatorLastName)}</creator-last-name>\n" +
            $"  <creator>{Xml.Escape(Creator)}</creator>\n" +
            " </about>\n" +
            " <author>\n" +
            $"  <full-name>{Xml.Escape(AuthorFullName)}</full-name>\n" +
            " </author>\n" +
            "</document-info>\n";
}