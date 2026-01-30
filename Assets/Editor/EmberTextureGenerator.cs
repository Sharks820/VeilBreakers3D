using UnityEngine;
using UnityEditor;
using System.IO;

namespace VeilBreakers.Editor
{
    public class EmberTextureGenerator : EditorWindow
    {
        [MenuItem("VeilBreakers/Generate All VFX Textures")]
        public static void GenerateAllVFX()
        {
            GenerateEmbers();
            GenerateAsh();
            Debug.Log("[VeilBreakers] All VFX textures generated!");
        }

        [MenuItem("VeilBreakers/VFX/Generate Ember Texture")]
        public static void GenerateEmbers()
        {
            int width = 512;
            int height = 512;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // Clear to transparent
            Color[] clear = new Color[width * height];
            for (int i = 0; i < clear.Length; i++)
                clear[i] = Color.clear;
            texture.SetPixels(clear);

            // Generate random ember particles
            System.Random rand = new System.Random(42); // Fixed seed for consistency
            int emberCount = 150;

            for (int i = 0; i < emberCount; i++)
            {
                int x = rand.Next(width);
                int y = rand.Next(height);
                float size = 1f + (float)rand.NextDouble() * 3f; // 1-4 pixel radius
                float brightness = 0.5f + (float)rand.NextDouble() * 0.5f; // 0.5-1.0

                // Orange/red ember color
                Color emberColor = new Color(
                    1.0f * brightness,
                    0.3f * brightness + (float)rand.NextDouble() * 0.3f,
                    0.05f * brightness,
                    brightness
                );

                // Draw soft circle
                int radius = Mathf.CeilToInt(size);
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int px = x + dx;
                        int py = y + dy;
                        if (px >= 0 && px < width && py >= 0 && py < height)
                        {
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            float falloff = Mathf.Clamp01(1f - dist / size);
                            falloff = falloff * falloff; // Soft falloff

                            Color existing = texture.GetPixel(px, py);
                            Color blended = Color.Lerp(existing, emberColor, falloff * emberColor.a);
                            blended.a = Mathf.Max(existing.a, falloff * emberColor.a);
                            texture.SetPixel(px, py, blended);
                        }
                    }
                }
            }

            texture.Apply();

            // Save to file
            string path = "Assets/Art/UI/MainMenu/ember_particles.png";
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
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            Debug.Log($"[VeilBreakers] Ember texture generated at: {path}");
        }

        [MenuItem("VeilBreakers/VFX/Generate Ash Texture")]
        public static void GenerateAsh()
        {
            int width = 512;
            int height = 512;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // Clear to transparent
            Color[] clear = new Color[width * height];
            for (int i = 0; i < clear.Length; i++)
                clear[i] = Color.clear;
            texture.SetPixels(clear);

            // Generate random ash particles (smaller, grayer)
            System.Random rand = new System.Random(123);
            int ashCount = 200;

            for (int i = 0; i < ashCount; i++)
            {
                int x = rand.Next(width);
                int y = rand.Next(height);
                float size = 0.5f + (float)rand.NextDouble() * 2f; // 0.5-2.5 pixel radius
                float brightness = 0.2f + (float)rand.NextDouble() * 0.3f; // 0.2-0.5 (darker)

                // Gray ash color with slight warmth
                Color ashColor = new Color(
                    brightness + 0.05f,
                    brightness,
                    brightness - 0.02f,
                    0.3f + (float)rand.NextDouble() * 0.4f // Semi-transparent
                );

                // Draw soft circle
                int radius = Mathf.CeilToInt(size);
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int px = x + dx;
                        int py = y + dy;
                        if (px >= 0 && px < width && py >= 0 && py < height)
                        {
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            float falloff = Mathf.Clamp01(1f - dist / size);

                            Color existing = texture.GetPixel(px, py);
                            Color blended = Color.Lerp(existing, ashColor, falloff * ashColor.a);
                            blended.a = Mathf.Max(existing.a, falloff * ashColor.a);
                            texture.SetPixel(px, py, blended);
                        }
                    }
                }
            }

            texture.Apply();

            // Save to file
            string path = "Assets/Art/UI/MainMenu/ash_particles.png";
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
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            Debug.Log($"[VeilBreakers] Ash texture generated at: {path}");
        }
    }
}
