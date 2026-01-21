"""
Generate 3D model from Hollow 2D image using Hunyuan3D via HuggingFace Gradio API
"""
import subprocess
import sys

# Install gradio_client if not present
try:
    from gradio_client import Client, handle_file
except ImportError:
    subprocess.check_call([sys.executable, "-m", "pip", "install", "gradio_client", "-q"])
    from gradio_client import Client, handle_file

import os

# Paths
IMAGE_PATH = r"C:\Users\Conner\OneDrive\Documents\VeilBreakers3DCurrent\Assets\Art\Sprites\monsters\hollow.png"
OUTPUT_DIR = r"C:\Users\Conner\OneDrive\Documents\VeilBreakers3DCurrent\Assets\Art\3D_Models\Monsters"

# Ensure output dir exists
os.makedirs(OUTPUT_DIR, exist_ok=True)

print("Connecting to Hunyuan3D-2.1 on HuggingFace...")
client = Client("tencent/Hunyuan3D-2.1")

print(f"Uploading image: {IMAGE_PATH}")
print("Generating 3D model (this may take 2-5 minutes)...")

try:
    result = client.predict(
        image=handle_file(IMAGE_PATH),
        api_name="/image_to_3d"
    )
    print(f"Result: {result}")

    # Copy result to output directory if it's a file path
    if isinstance(result, str) and os.path.exists(result):
        import shutil
        output_path = os.path.join(OUTPUT_DIR, "hollow_3d.glb")
        shutil.copy(result, output_path)
        print(f"Model saved to: {output_path}")
    elif isinstance(result, tuple):
        for i, r in enumerate(result):
            if isinstance(r, str) and os.path.exists(r):
                ext = os.path.splitext(r)[1]
                output_path = os.path.join(OUTPUT_DIR, f"hollow_3d{ext}")
                import shutil
                shutil.copy(r, output_path)
                print(f"Model saved to: {output_path}")

except Exception as e:
    print(f"Error: {e}")
    print("\nTrying alternative API endpoint...")
    try:
        # Try the generation endpoint
        result = client.predict(
            caption="dark fantasy shadow monster with glowing red eyes",
            image=handle_file(IMAGE_PATH),
            steps=30,
            guidance_scale=5.0,
            seed=42,
            api_name="/generation"
        )
        print(f"Result: {result}")
    except Exception as e2:
        print(f"Alternative also failed: {e2}")
        print("\nListing available API endpoints...")
        print(client.view_api())
