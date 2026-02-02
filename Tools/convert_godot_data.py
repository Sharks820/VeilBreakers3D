#!/usr/bin/env python3
"""
VeilBreakers Godot -> Unity Data Converter
Converts .tres (Godot Resource) files to JSON for Unity
"""

import os
import re
import json
from pathlib import Path

GODOT_PROJECT = Path(r"C:\Users\Conner\Downloads\VeilbreakersGame")
UNITY_PROJECT = Path(r"C:\Users\Conner\Downloads\VeilBreakers3D")

def parse_tres_file(filepath):
    """Parse a Godot .tres file and extract resource data."""
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # Find the [resource] section
    resource_match = re.search(r'\[resource\](.*)', content, re.DOTALL)
    if not resource_match:
        return None

    resource_content = resource_match.group(1)
    data = {}

    # Parse each line
    lines = resource_content.strip().split('\n')
    for line in lines:
        line = line.strip()
        if not line or line.startswith('#') or line.startswith('script'):
            continue

        # Match key = value
        match = re.match(r'^(\w+)\s*=\s*(.+)$', line)
        if match:
            key = match.group(1)
            value_str = match.group(2).strip()
            data[key] = parse_value(value_str)

    return data

def parse_value(value_str):
    """Parse a Godot value string to Python object."""
    value_str = value_str.strip()

    # Boolean
    if value_str == 'true':
        return True
    if value_str == 'false':
        return False

    # null/nil
    if value_str in ('null', 'nil'):
        return None

    # Color(r, g, b, a)
    color_match = re.match(r'Color\(\s*([\d.]+)\s*,\s*([\d.]+)\s*,\s*([\d.]+)\s*,\s*([\d.]+)\s*\)', value_str)
    if color_match:
        return {
            "r": float(color_match.group(1)),
            "g": float(color_match.group(2)),
            "b": float(color_match.group(3)),
            "a": float(color_match.group(4))
        }

    # Vector2
    vec2_match = re.match(r'Vector2\(\s*([\d.-]+)\s*,\s*([\d.-]+)\s*\)', value_str)
    if vec2_match:
        return {"x": float(vec2_match.group(1)), "y": float(vec2_match.group(2))}

    # String
    if value_str.startswith('"') and value_str.endswith('"'):
        return value_str[1:-1]

    # Array
    if value_str.startswith('['):
        return parse_array(value_str)

    # Dictionary
    if value_str.startswith('{'):
        return parse_dict(value_str)

    # Integer
    if re.match(r'^-?\d+$', value_str):
        return int(value_str)

    # Float
    if re.match(r'^-?\d+\.\d*$', value_str):
        return float(value_str)

    return value_str

def parse_array(value_str):
    """Parse Godot array notation."""
    try:
        # Handle nested structures by using a more careful approach
        content = value_str[1:-1].strip()
        if not content:
            return []

        items = []
        depth = 0
        current = ""
        in_string = False

        for char in content:
            if char == '"' and (not current or current[-1] != '\\'):
                in_string = not in_string

            if not in_string:
                if char in '[{':
                    depth += 1
                elif char in ']}':
                    depth -= 1
                elif char == ',' and depth == 0:
                    items.append(parse_value(current.strip()))
                    current = ""
                    continue

            current += char

        if current.strip():
            items.append(parse_value(current.strip()))

        return items
    except:
        return []

def parse_dict(value_str):
    """Parse Godot dictionary notation."""
    try:
        content = value_str[1:-1].strip()
        if not content:
            return {}

        result = {}
        depth = 0
        current_key = None
        current_val = ""
        in_string = False
        parsing_key = True

        i = 0
        while i < len(content):
            char = content[i]

            if char == '"' and (i == 0 or content[i-1] != '\\'):
                in_string = not in_string

            if not in_string:
                if char in '[{':
                    depth += 1
                elif char in ']}':
                    depth -= 1
                elif char == ':' and depth == 0 and parsing_key:
                    current_key = current_val.strip().strip('"')
                    current_val = ""
                    parsing_key = False
                    i += 1
                    continue
                elif char == ',' and depth == 0:
                    if current_key is not None:
                        result[current_key] = parse_value(current_val.strip())
                    current_key = None
                    current_val = ""
                    parsing_key = True
                    i += 1
                    continue

            current_val += char
            i += 1

        if current_key is not None and current_val.strip():
            result[current_key] = parse_value(current_val.strip())

        return result
    except:
        return {}

def convert_monsters():
    """Convert all monster .tres files to JSON."""
    monsters_dir = GODOT_PROJECT / "data" / "monsters"
    output = []

    for tres_file in monsters_dir.glob("*.tres"):
        data = parse_tres_file(tres_file)
        if data:
            # Convert sprite paths from Godot to Unity format
            if 'sprite_path' in data:
                data['sprite_path'] = convert_path(data['sprite_path'])
            if 'portrait_path' in data:
                data['portrait_path'] = convert_path(data['portrait_path'])
            output.append(data)
            print(f"  Converted monster: {data.get('display_name', tres_file.stem)}")

    return output

def convert_skills():
    """Convert all skill .tres files to JSON."""
    skills_dir = GODOT_PROJECT / "data" / "skills"
    output = []

    # Main skills
    for tres_file in skills_dir.glob("*.tres"):
        data = parse_tres_file(tres_file)
        if data:
            if 'icon_path' in data:
                data['icon_path'] = convert_path(data['icon_path'])
            output.append(data)
            print(f"  Converted skill: {data.get('display_name', tres_file.stem)}")

    # Monster skills
    monster_skills_dir = skills_dir / "monsters"
    if monster_skills_dir.exists():
        for tres_file in monster_skills_dir.glob("*.tres"):
            data = parse_tres_file(tres_file)
            if data:
                data['is_monster_skill'] = True
                if 'icon_path' in data:
                    data['icon_path'] = convert_path(data['icon_path'])
                output.append(data)
                print(f"  Converted monster skill: {data.get('display_name', tres_file.stem)}")

    return output

def convert_heroes():
    """Convert all hero .tres files to JSON."""
    heroes_dir = GODOT_PROJECT / "data" / "heroes"
    output = []

    for tres_file in heroes_dir.glob("*.tres"):
        data = parse_tres_file(tres_file)
        if data:
            for path_key in ['sprite_path', 'portrait_path', 'battle_sprite_path']:
                if path_key in data:
                    data[path_key] = convert_path(data[path_key])
            output.append(data)
            print(f"  Converted hero: {data.get('display_name', tres_file.stem)}")

    # Also check nested heroes folder in skills
    heroes_dir2 = GODOT_PROJECT / "data" / "skills" / "heroes"
    if heroes_dir2.exists():
        for tres_file in heroes_dir2.glob("*.tres"):
            data = parse_tres_file(tres_file)
            if data:
                output.append(data)
                print(f"  Converted hero skill: {data.get('display_name', tres_file.stem)}")

    return output

def convert_items():
    """Convert all item .tres files to JSON."""
    items_dir = GODOT_PROJECT / "data" / "items"
    output = []

    for subdir in ['consumables', 'equipment']:
        subpath = items_dir / subdir
        if subpath.exists():
            for tres_file in subpath.glob("*.tres"):
                data = parse_tres_file(tres_file)
                if data:
                    data['item_category'] = subdir
                    if 'icon_path' in data:
                        data['icon_path'] = convert_path(data['icon_path'])
                    output.append(data)
                    print(f"  Converted item: {data.get('display_name', tres_file.stem)}")

    return output

def convert_path(godot_path):
    """Convert Godot res:// path to Unity-friendly path."""
    if not godot_path:
        return ""
    # Remove res:// prefix and convert to relative Unity path
    path = godot_path.replace("res://", "")
    # Convert to forward slashes
    path = path.replace("\\", "/")
    return path

def main():
    """Main conversion routine."""
    print("=" * 60)
    print("VEILBREAKERS DATA CONVERTER")
    print("Godot .tres -> Unity JSON")
    print("=" * 60)

    # Create output directory
    output_dir = UNITY_PROJECT / "Assets" / "Data"
    output_dir.mkdir(parents=True, exist_ok=True)

    # Convert monsters
    print("\n[MONSTERS]")
    monsters = convert_monsters()
    with open(output_dir / "monsters.json", 'w', encoding='utf-8') as f:
        json.dump(monsters, f, indent=2)
    print(f"  Total: {len(monsters)} monsters")

    # Convert skills
    print("\n[SKILLS]")
    skills = convert_skills()
    with open(output_dir / "skills.json", 'w', encoding='utf-8') as f:
        json.dump(skills, f, indent=2)
    print(f"  Total: {len(skills)} skills")

    # Convert heroes
    print("\n[HEROES]")
    heroes = convert_heroes()
    with open(output_dir / "heroes.json", 'w', encoding='utf-8') as f:
        json.dump(heroes, f, indent=2)
    print(f"  Total: {len(heroes)} heroes")

    # Convert items
    print("\n[ITEMS]")
    items = convert_items()
    with open(output_dir / "items.json", 'w', encoding='utf-8') as f:
        json.dump(items, f, indent=2)
    print(f"  Total: {len(items)} items")

    print("\n" + "=" * 60)
    print("CONVERSION COMPLETE!")
    print(f"Output directory: {output_dir}")
    print("=" * 60)

if __name__ == "__main__":
    main()
