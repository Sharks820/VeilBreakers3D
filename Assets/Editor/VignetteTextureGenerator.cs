using UnityEngine;
using UnityEditor;
using System.IO;

namespace VeilBreakers.Editor
{
    public class VignetteTextureGenerator : EditorWindow
    {
        [MenuItem("VeilBreakers/Generate Vignette Texture")]
        public static void GenerateVignette()
        {
            int width = 1024;
            int height = 1024;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            Color vignetteColor = new Color(0.02f, 0.01f, 0.03f, 1f); // Dark purple-black

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Normalize coordinates to -1 to 1
                    float nx = (x / (float)width) * 2f - 1f;
                    float ny = (y / (float)height) * 2f - 1f;

                    // Calculate distance from center (elliptical)
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);

                    // Smooth vignette falloff - start later, only at edges
                    float vignette = Mathf.SmoothStep(0.6f, 1.4f, dist);

                    // Fade out the TOP of the vignette so logo stays bright
                    // ny goes from -1 (bottom) to +1 (top)
                    // We want full vignette at bottom (ny=-1), fading to zero at top (ny=0.5+)
                    float topFade = Mathf.SmoothStep(0.3f, -0.2f, ny); // Fade out in top 35% of screen

                    // Apply color with vignette alpha - MORE VISIBLE (max 70%)
                    Color pixel = vignetteColor;
                    pixel.a = vignette * 0.70f * topFade; // Apply top fade

                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();

            // Save to file
            string path = "Assets/Art/UI/MainMenu/vignette_overlay.png";
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            DestroyImmediate(texture);

            AssetDatabase.Refresh();

            // Set import settings
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            Debug.Log($"[VeilBreakers] Vignette texture generated at: {path}");
        }
    }
}
