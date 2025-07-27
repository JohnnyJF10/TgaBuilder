using TgaBuilderLib.Enums;

namespace TgaBuilderWpfUi.Services
{
    public partial class FileService
    {
        private static readonly Dictionary<FileTypes, FileTypeInfo> FileTypeLookup = new()
        {
            {
                FileTypes.TGA,
                new FileTypeInfo
                {
                    Extension = ".tga",
                    Description = "Tagra Files (*.tga)|*.tga",
                    Title = "Open Targa File"
                }
            },
            {
                FileTypes.BMP,
                new FileTypeInfo
                {
                    Extension = ".bmp",
                    Description = "Bitmap Files (*.bmp)|*.bmp",
                    Title = "Open Bitmap File"
                }
            },
            {
                FileTypes.PNG,
                new FileTypeInfo
                {
                    Extension = ".png",
                    Description = "PNG Files (*.png)|*.png",
                    Title = "Open PNG File"
                }
            },
            {
                FileTypes.JPG,
                new FileTypeInfo
                {
                    Extension = ".jpg",
                    Description = "JPEG Files (*.jpg)|*.jpg",
                    Title = "Open JPEG File"
                }
            },
            {
                FileTypes.JPEG,
                new FileTypeInfo
                {
                    Extension = ".jpeg",
                    Description = "JPEG Files (*.jpeg)|*.jpeg",
                    Title = "Open JPEG File"
                }
            },
            {
                FileTypes.DDS,
                new FileTypeInfo
                {
                    Extension = ".dds",
                    Description = "DirectDraw Surface Files (*.dds)|*.dds",
                    Title = "Open DDS File"
                }
            },
            {
                FileTypes.PSD,
                new FileTypeInfo
                {
                    Extension = ".psd",
                    Description = "Photoshop Files (*.psd)|*.psd",
                    Title = "Open Photoshop File"
                }
            },
            {
                FileTypes.PHD,
                new FileTypeInfo
                {
                    Extension = ".phd",
                    Description = "PHD Files (*.phd)|*.phd",
                    Title = "Open PHD File"
                }
            },
            {
                FileTypes.TR2,
                new FileTypeInfo
                {
                    Extension = ".tr2",
                    Description = "TR2 Files (*.tr2)|*.tr2",
                    Title = "Open TR2 File"
                }
            },
            {
                FileTypes.TR4,
                new FileTypeInfo
                {
                    Extension = ".tr4",
                    Description = "TR4 Files (*.tr4)|*.tr4",
                    Title = "Open TR4 File"
                }
            },
            {
                FileTypes.TRC,
                new FileTypeInfo
                {
                    Extension = ".trc",
                    Description = "TR5 Files (*.tr5)|*.tr5",
                    Title = "Open TR5 File"
                }
            },
            {
                FileTypes.TEN,
                new FileTypeInfo
                {
                    Extension = ".ten",
                    Description = "TEN Files (*.ten)|*.ten",
                    Title = "Open TEN File"
                }
            }
        };
    }
}
