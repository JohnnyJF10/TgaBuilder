import os
from pathlib import Path

def format_axaml_file(file_path):
    # Read the file safely handling UTF-8 with or without BOM
    try:
        with open(file_path, 'r', encoding='utf-8-sig') as f:
            content = f.read()
    except Exception as e:
        print(f"Error reading {file_path}: {e}")
        return

    lines = content.split('\n')
    formatted_lines = []
    
    indent_level = 0
    indent_size = 4
    in_tag = False
    in_comment = False

    for line in lines:
        stripped = line.strip()
        
        # 1. Preserve empty lines as they are
        if not stripped:
            formatted_lines.append("")
            continue

        # 2. Handle ongoing multi-line XML comments
        if in_comment:
            formatted_lines.append(" " * (indent_level * indent_size) + stripped)
            if "-->" in stripped:
                in_comment = False
            continue

        # 3. Handle the start of an XML comment (can be single-line or multi-line)
        if stripped.startswith("<!--"):
            if "-->" not in stripped:
                in_comment = True
            continue

        # 4. Handle XML declaration lines (e.g., <?xml version="1.0" ?>)
        if stripped.startswith("<?"):
            formatted_lines.append(" " * (indent_level * indent_size) + stripped)
            continue

        # 5. Handle ongoing multi-line tag attributes (properties on new lines)
        if in_tag:
            # Attributes are indented +4 spaces relative to the tag's base indentation
            formatted_lines.append(" " * (indent_level * indent_size + indent_size) + stripped)
            
            if stripped.endswith("/>"):
                in_tag = False
            elif stripped.endswith(">"):
                in_tag = False
                indent_level += 1  # Tag is fully open now, children get indented
            continue

        # 6. Handle XML closing tags (e.g., </Border>)
        if stripped.startswith("</"):
            indent_level = max(0, indent_level - 1)
            formatted_lines.append(" " * (indent_level * indent_size) + stripped)
            continue

        # 7. Handle XML opening tags (e.g., <Border> or <Border ... )
        if stripped.startswith("<"):
            formatted_lines.append(" " * (indent_level * indent_size) + stripped)
            
            if stripped.endswith("/>"):
                # Self-closing single-line tag, no indent change needed
                pass
            elif stripped.endswith(">"):
                # If it contains its own closing tag (like <TextBlock>Text</TextBlock>), do not increase indent
                if "</" not in stripped:
                    indent_level += 1
            else:
                # Tag doesn't close on this line -> attributes will follow on next lines
                in_tag = True
            continue

        # 8. Regular text content between tags
        formatted_lines.append(" " * (indent_level * indent_size) + stripped)

    # Write the beautifully formatted content back to the file
    try:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write('\n'.join(formatted_lines))
    except Exception as e:
        print(f"Error writing to {file_path}: {e}")

def main():
    current_dir = Path.cwd()
    print(f"Scanning for .axaml files recursively in {current_dir} ...")
    
    axaml_files = list(current_dir.rglob('*.axaml'))
    
    if not axaml_files:
        print("No .axaml files found in the current directory or subdirectories.")
        return

    for file_path in axaml_files:
        format_axaml_file(file_path)
        
    print(f"Success: Formatted {len(axaml_files)} .axaml files correctly.")

if __name__ == "__main__":
    main()