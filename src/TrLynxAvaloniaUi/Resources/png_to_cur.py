import os
import io
import struct
from PIL import Image

# ==========================================
# CONFIGURATION
# ==========================================
# Cursors cannot exceed 256x256. Standard sizes are 32, 48, 64. 
TARGET_CURSOR_SIZE = 24  

# Options: "top-left", "bottom-left", "center", "top-right", "bottom-right"
HOTSPOT_POSITION = "bottom-left" 

INPUT_PNG = "hand.png"  # Default input PNG file name (if you want to specify a single file)
# ==========================================

def get_hotspot_coordinates(position, width, height):
    """Calculates X, Y coordinates based on the selected position and image dimensions."""
    max_x = width - 1
    max_y = height - 1
    
    mapping = {
        "top-left": (0, 0),
        "bottom-left": (0, max_y),
        "center": (width // 2, height // 2),
        "top-right": (max_x, 0),
        "bottom-right": (max_x, max_y)
    }
    return mapping.get(position.lower(), (0, 0))

def patch_ico_to_cur(ico_bytes, hotspot_x, hotspot_y):
    """Patches ICO binary structure into a valid CUR format."""
    data = bytearray(ico_bytes)
    
    # 1. Patch header type to 2 (CUR)
    data[2:4] = struct.pack('<H', 2)
    
    # 2. Extract image count
    num_images = struct.unpack('<H', data[4:6])[0]
    
    # 3. Inject hotspot data into directory entries
    for i in range(num_images):
        entry_offset = 6 + (i * 16)
        data[entry_offset + 4 : entry_offset + 6] = struct.pack('<H', hotspot_x)
        data[entry_offset + 6 : entry_offset + 8] = struct.pack('<H', hotspot_y)
        
    return bytes(data)

def batch_convert_png_to_cur():
    current_dir = os.path.dirname(os.path.abspath(__file__))
    png_files = [f for f in os.listdir(current_dir) if f.lower().endswith('.png')]

    if not png_files:
        print("No PNG files found in the current directory.")
        return

    print(f"Target Output Size: {TARGET_CURSOR_SIZE}x{TARGET_CURSOR_SIZE}")
    print(f"Hotspot Position: {HOTSPOT_POSITION}")
    print(f"Found {len(png_files)} PNG file(s). Converting to CUR...\n")

    for png_file in png_files:
        convert_png_to_cur(png_file, current_dir)


    print("\nPNG to CUR conversion complete!")

def convert_png_to_cur(png_file, current_dir):
    base_name = os.path.splitext(png_file)[0]
    cur_filename = f"{base_name}.cur"
    png_path = os.path.join(current_dir, png_file)
    cur_path = os.path.join(current_dir, cur_filename)

    try:
        # Open the existing PNG file
        img = Image.open(png_path).convert("RGBA")
        
        # THE FIX: Automatically scale down if the image isn't the target size.
        # (Ensures it stays well below the 256px hardware limit of the CUR format)
        if img.size != (TARGET_CURSOR_SIZE, TARGET_CURSOR_SIZE):
            print(f"  -> Resizing {png_file} from {img.size[0]}x{img.size[1]} to {TARGET_CURSOR_SIZE}x{TARGET_CURSOR_SIZE}...")
            img = img.resize((TARGET_CURSOR_SIZE, TARGET_CURSOR_SIZE), Image.Resampling.LANCZOS)
            
        width, height = img.size

        # Dynamically calculate hotspot relative to this new image size
        hotspot_x, hotspot_y = get_hotspot_coordinates(HOTSPOT_POSITION, width, height)

        # Build ICO structure utilizing uncompressed BMP for C# framework decoding
        ico_buffer = io.BytesIO()
        img.save(ico_buffer, format='ICO', sizes=[(width, height)], bitmap_format='bmp')
        ico_bytes = ico_buffer.getvalue()

        # Execute binary format modifications
        cur_bytes = patch_ico_to_cur(ico_bytes, hotspot_x, hotspot_y)

        # Write final file out
        with open(cur_path, 'wb') as f:
            f.write(cur_bytes)

        print(f"✅ Created CUR: {png_file} -> {cur_filename} (Hotspot: {hotspot_x},{hotspot_y})")

    except Exception as e:
        print(f"❌ Failed to process {png_file}: {e}")

if __name__ == "__main__":
    if INPUT_PNG:
        current_dir = os.path.dirname(os.path.abspath(__file__))
        if os.path.isfile(os.path.join(current_dir, INPUT_PNG)):
            convert_png_to_cur(INPUT_PNG, current_dir)
    else:
        batch_convert_png_to_cur()