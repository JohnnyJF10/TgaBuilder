namespace TgaBuilderLib.Abstraction
{
    public struct PixelRect
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public PixelRect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public static PixelRect Empty => new PixelRect(0, 0, 0, 0);

        public bool IsEmpty => X == 0 && Y == 0 && Width == 0 && Height == 0;

        public int Top => Y;
        public int Left => X;
        public int Bottom => Y + Height;
        public int Right => X + Width;
    }
}
