using System.Text;

namespace TgaBuilderLib.Krita;

public class MainDoc
{
    public string ImageName { get; set; } = "Unnamed";
    public int Width { get; set; }
    public int Height { get; set; }
    public IReadOnlyList<LayerSource> Layers { get; set; } = new List<LayerSource>();

    public string Build()
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        sb.Append("<!DOCTYPE DOC PUBLIC '-//KDE//DTD krita 2.0//EN' 'http://www.calligra.org/DTD/krita-2.0.dtd'>\n");
        sb.Append("<DOC xmlns=\"http://www.calligra.org/DTD/krita\" editor=\"Krita\" syntaxVersion=\"2.0\" kritaVersion=\"5.2.2\">\n");
        sb.Append($" <IMAGE name=\"{Xml.Escape(ImageName)}\" mime=\"application/x-kra\" width=\"{Width}\" height=\"{Height}\"")
          .Append(" x-res=\"100\" y-res=\"100\" colorspacename=\"RGBA\" profile=\"sRGB built-in\" description=\"\">\n");
        sb.Append("  <layers>\n");

        // Krita lists layers top-first; layers[0] is the bottom layer, so emit in reverse.
        for (int i = Layers.Count - 1; i >= 0; i--)
        {
            var l = Layers[i];
            sb.Append("   <layer ")
              .Append($"name=\"{Xml.Escape(l.Name)}\" ")
              .Append($"filename=\"{Xml.Escape(l.Name)}\" ")
              .Append($"uuid=\"{l.Uuid}\" ")
              .Append("nodetype=\"paintlayer\" colorspacename=\"RGBA\" compositeop=\"normal\" ")
              .Append($"opacity=\"255\" visible=\"{(l.Visible ? "1" : "0")}\" locked=\"0\" collapsed=\"0\" ")
              .Append("x=\"0\" y=\"0\" intimeline=\"1\" colorlabel=\"0\" ")
              .Append("channelflags=\"\" channellockflags=\"1111\" ")
              .Append($"selected=\"{(i == Layers.Count - 1 ? "true" : "false")}\"/>\n");
        }

        sb.Append("  </layers>\n");
        sb.Append(" </IMAGE>\n");
        sb.Append("</DOC>\n");
        return sb.ToString();
    }
}