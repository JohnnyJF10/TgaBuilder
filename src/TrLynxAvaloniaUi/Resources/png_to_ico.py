import os
from PIL import Image

# ============================================================
# Configuration
# ============================================================

INPUT_PNG = "TrLynx_picture.png"
OUTPUT_ICO = "TrLynx_icon.ico"

ICON_SIZES = [
    256,
    128,
    64,
    48,
    32,
    24,
    16,
]

# Global fallback interpolation mode
DEFAULT_RESAMPLE = Image.Resampling.LANCZOS

# Optional per-size interpolation overrides
#
# Useful for pixel art or logos where tiny icons look better
# with nearest-neighbor scaling.
#
SIZE_RESAMPLE = {
    32: Image.Resampling.LANCZOS,
    24: Image.Resampling.LANCZOS,
    16: Image.Resampling.LANCZOS,
}


# ============================================================
# Conversion
# ============================================================

def convert_png_to_ico(
    source_path,
    dest_path,
    icon_sizes,
    default_resample,
    size_resample,
):
    """
    Convert a PNG file into a multi-resolution ICO file.

    Parameters
    ----------
    source_path : str
        Input PNG file.

    dest_path : str
        Output ICO file.

    icon_sizes : list[int]
        Icon sizes to embed.

    default_resample : PIL.Image.Resampling
        Default interpolation mode.

    size_resample : dict[int, PIL.Image.Resampling]
        Optional per-size interpolation overrides.
    """

    try:
        with Image.open(source_path) as img:

            if img.mode != "RGBA":
                img = img.convert("RGBA")

            icon_images = []

            for size in icon_sizes:
                resample = size_resample.get(size, default_resample)

                resized = img.resize(
                    (size, size),
                    resample=resample,
                )

                icon_images.append(resized)

                print(
                    f"Generated {size}x{size} "
                    f"using {resample.name}"
                )

            # Save ICO using the largest image as base
            icon_images[0].save(
                dest_path,
                format="ICO",
                append_images=icon_images[1:],
            )

            print(f"\nSuccess!")
            print(f"Input : {source_path}")
            print(f"Output: {dest_path}")

    except FileNotFoundError:
        print(f"Error: File not found: {source_path}")

    except Exception as e:
        print(f"Error: {e}")


# ============================================================
# Main
# ============================================================

if __name__ == "__main__":
    convert_png_to_ico(
        source_path=INPUT_PNG,
        dest_path=OUTPUT_ICO,
        icon_sizes=ICON_SIZES,
        default_resample=DEFAULT_RESAMPLE,
        size_resample=SIZE_RESAMPLE,
    )