using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.BitmapOperations
{
    public partial class BitmapOperations : IBitmapOperations
    {
        public BitmapOperations(
            IMediaFactory mediaFactory)
        {
            _mediaFactory = mediaFactory;
        }

        private readonly IMediaFactory _mediaFactory;
    }
}
