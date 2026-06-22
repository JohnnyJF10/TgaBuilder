import os
from PIL import Image

def convert_png_to_ico(source_path, dest_path=None, icon_sizes=None):
    """
    Converts a PNG image to an ICO file.
    
    :param source_path: Path to the source PNG file.
    :param dest_path: Path where the ICO file should be saved (optional).
    :param icon_sizes: List of sizes to include in the ICO (optional).
    """
    # Default standard icon sizes if none are provided
    if icon_sizes is None:
        icon_sizes = [(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
        
    # If no destination is provided, save it in the same directory with .ico extension
    if dest_path is None:
        dest_path = os.path.splitext(source_path)[0] + '.ico'
        
    try:
        # Open the PNG image
        with Image.open(source_path) as img:
            # Ensure the image is in RGBA mode to preserve transparency
            if img.mode != 'RGBA':
                img = img.convert('RGBA')
                
            # Save as ICO with the specified sizes
            img.save(dest_path, format='ICO', sizes=icon_sizes)
            print(f"Success! Converted '{source_path}' to '{dest_path}'")
            
    except FileNotFoundError:
        print(f"Error: The file '{source_path}' could not be found.")
    except Exception as e:
        print(f"An error occurred: {e}")

# Example usage:
if __name__ == "__main__":
    # Replace with the path to your PNG file
    png_file = "TrLynx_picture.png" 
    
    convert_png_to_ico(png_file)