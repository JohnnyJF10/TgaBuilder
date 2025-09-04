namespace TgaBuilderLib.Abstraction
{
    public struct Color
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte? A { get; set; }
        public Color(byte r, byte g, byte b, byte? a = null)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
        public override string ToString()
        {
            return $"R:{R}, G:{G}, B:{B}, A:{A}";
        }
    }
}
