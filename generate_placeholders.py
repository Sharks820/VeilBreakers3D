import os
from PIL import Image, ImageDraw

def create_placeholder_image(path, color, size=(512, 512), text=None):
    img = Image.new('RGB', size, color)
    if text:
        d = ImageDraw.Draw(img)
        # simplistic text centering (approximate)
        d.text((size[0]//2 - len(text)*3, size[1]//2), text, fill=(255, 255, 255))
    
    # Ensure directory exists
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    print(f"Created {path}")

def create_transparent_icon(path, color, shape='circle', size=(128, 128)):
    img = Image.new('RGBA', size, (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    
    if shape == 'circle':
        d.ellipse([10, 10, size[0]-10, size[1]-10], fill=color)
    elif shape == 'star':
        # Simple star polygon points
        cx, cy = size[0]//2, size[1]//2
        r = size[0]//2 - 10
        points = []
        import math
        for i in range(10):
            angle = i * math.pi / 5 - math.pi / 2
            curr_r = r if i % 2 == 0 else r * 0.4
            points.append((cx + curr_r * math.cos(angle), cy + curr_r * math.sin(angle)))
        d.polygon(points, fill=color)
    elif shape == 'arrow':
        d.polygon([(20, 40), (80, 40), (80, 20), (120, 64), (80, 108), (80, 88), (20, 88)], fill=color)
    
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    print(f"Created {path}")

# Backgrounds
bg_path = "Assets/Resources/Backgrounds"
create_placeholder_image(f"{bg_path}/prison_ruins.png", (50, 50, 60), text="PRISON RUINS")
create_placeholder_image(f"{bg_path}/poison_garden.png", (30, 60, 30), text="POISON GARDEN")
create_placeholder_image(f"{bg_path}/storm_summit.png", (40, 50, 80), text="STORM SUMMIT")
create_placeholder_image(f"{bg_path}/shadow_vault.png", (40, 20, 40), text="SHADOW VAULT")

# Hero Silhouettes (Characters/Heroes)
hero_path = "Assets/Resources/Characters/Heroes"
create_transparent_icon(f"{hero_path}/vex.png", (140, 150, 165, 255), size=(512, 1024))
create_transparent_icon(f"{hero_path}/seraphina.png", (80, 180, 60, 255), size=(512, 1024))
create_transparent_icon(f"{hero_path}/orion.png", (60, 140, 220, 255), size=(512, 1024))
create_transparent_icon(f"{hero_path}/nyx.png", (160, 40, 70, 255), size=(512, 1024))

# UI Textures
ui_path = "Assets/Resources/UI/Textures"
create_placeholder_image(f"{ui_path}/void_stars.png", (10, 10, 20), text="STARS")
create_placeholder_image(f"{ui_path}/fog_particles.png", (200, 200, 200), size=(256, 256), text="FOG")
create_placeholder_image(f"{ui_path}/hex_button.png", (50, 50, 50), size=(300, 60), text="BUTTON")

# UI Icons
icon_path = "Assets/Resources/UI/Icons"
create_transparent_icon(f"{icon_path}/star_filled.png", (255, 215, 0, 255), shape='star')
create_transparent_icon(f"{icon_path}/star_empty.png", (100, 100, 100, 150), shape='star')
create_transparent_icon(f"{icon_path}/arrow.png", (255, 255, 255, 255), shape='arrow')
