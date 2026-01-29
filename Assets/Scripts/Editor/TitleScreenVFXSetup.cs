using UnityEngine;
using UnityEditor;

namespace VeilBreakers.Editor
{
    /// <summary>
    /// Editor utility to set up Title Screen VFX system with one click.
    /// Creates all necessary GameObjects, materials, and components.
    /// </summary>
    public static class TitleScreenVFXSetup
    {
        private const string VFX_TEXTURES_PATH = "Assets/Art/VFX/TitleScreen/";
        private const string VFX_SHADERS_PATH = "Assets/Art/VFX/Shaders/";
        private const string VFX_MATERIALS_PATH = "Assets/Art/VFX/Materials/";

        [MenuItem("VeilBreakers/Setup Title Screen VFX")]
        public static void SetupTitleScreenVFX()
        {
            // Ensure materials folder exists
            if (!AssetDatabase.IsValidFolder(VFX_MATERIALS_PATH.TrimEnd('/')))
            {
                AssetDatabase.CreateFolder("Assets/Art/VFX", "Materials");
            }

            // Create materials
            Material vignetteMat = CreateMaterial("VFX_Vignette", "M_TitleVignette", "Vignette_Soft.png");
            Material smokeMat = CreateMaterial("VFX_ScrollingSmoke", "M_TitleSmoke", "Noise_Smoke_01.png");
            Material particlesMat = CreateMaterial("VFX_FloatingParticles", "M_TitleParticles", "Noise_Ash_01.png");
            Material heatMat = CreateMaterial("VFX_HeatDistortion", "M_TitleHeatDistortion", "Distort_Noise_01.png");

            // Configure materials with default values
            ConfigureVignetteMaterial(vignetteMat);
            ConfigureSmokeMaterial(smokeMat);
            ConfigureParticlesMaterial(particlesMat);
            ConfigureHeatDistortionMaterial(heatMat);

            // Create VFX root GameObject
            GameObject vfxRoot = new GameObject("TitleScreenVFX");
            Undo.RegisterCreatedObjectUndo(vfxRoot, "Create Title Screen VFX");

            // Create VFX layers as child quads
            GameObject vignetteQuad = CreateVFXQuad("VFX_Vignette", vignetteMat, vfxRoot.transform, -5f);
            GameObject smokeQuad = CreateVFXQuad("VFX_Smoke", smokeMat, vfxRoot.transform, -3f);
            GameObject particlesQuad = CreateVFXQuad("VFX_Particles", particlesMat, vfxRoot.transform, -2f);
            GameObject heatQuad = CreateVFXQuad("VFX_HeatDistortion", heatMat, vfxRoot.transform, -1f);

            // Position smoke quad to cover bottom portion of screen
            smokeQuad.transform.localPosition = new Vector3(0, -2.5f, -3f);
            smokeQuad.transform.localScale = new Vector3(20f, 10f, 1f);

            // Position particles to cover full screen
            particlesQuad.transform.localScale = new Vector3(20f, 12f, 1f);

            // Position heat distortion - smaller, centered on creature's chest
            heatQuad.transform.localPosition = new Vector3(0, 0f, -1f);
            heatQuad.transform.localScale = new Vector3(8f, 8f, 1f);

            // Add VFX Controller component
            var controller = vfxRoot.AddComponent<VeilBreakers.UI.Effects.TitleScreenVFXController>();

            // Use serialized object to set private fields
            SerializedObject so = new SerializedObject(controller);

            so.FindProperty("_vignetteMaterial").objectReferenceValue = vignetteMat;
            so.FindProperty("_smokeMaterial").objectReferenceValue = smokeMat;
            so.FindProperty("_particlesMaterial").objectReferenceValue = particlesMat;
            so.FindProperty("_heatDistortionMaterial").objectReferenceValue = heatMat;

            so.FindProperty("_vignetteRenderer").objectReferenceValue = vignetteQuad.GetComponent<MeshRenderer>();
            so.FindProperty("_smokeRenderer").objectReferenceValue = smokeQuad.GetComponent<MeshRenderer>();
            so.FindProperty("_particlesRenderer").objectReferenceValue = particlesQuad.GetComponent<MeshRenderer>();
            so.FindProperty("_heatDistortionRenderer").objectReferenceValue = heatQuad.GetComponent<MeshRenderer>();

            so.ApplyModifiedProperties();

            // Select the created object
            Selection.activeGameObject = vfxRoot;

            Debug.Log("Title Screen VFX setup complete! Position the 'TitleScreenVFX' GameObject in your MainMenu scene.");
            Debug.Log("Tip: Place it as a child of Main Camera or adjust quad positions to match your camera setup.");
        }

        private static Material CreateMaterial(string shaderName, string materialName, string textureName)
        {
            string materialPath = VFX_MATERIALS_PATH + materialName + ".mat";

            // Check if material already exists
            Material existingMat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existingMat != null)
            {
                Debug.Log($"Material already exists: {materialPath}");
                return existingMat;
            }

            // Find shader
            Shader shader = Shader.Find("VeilBreakers/VFX/" + shaderName.Replace("VFX_", ""));
            if (shader == null)
            {
                Debug.LogError($"Shader not found: VeilBreakers/VFX/{shaderName.Replace("VFX_", "")}");
                return null;
            }

            // Create material
            Material mat = new Material(shader);
            mat.name = materialName;

            // Load and assign texture
            string texturePath = VFX_TEXTURES_PATH + textureName;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture != null)
            {
                mat.SetTexture("_MainTex", texture);

                // For distortion shader, also set distort tex
                if (shaderName.Contains("Distort"))
                {
                    mat.SetTexture("_DistortTex", texture);
                }
            }
            else
            {
                Debug.LogWarning($"Texture not found: {texturePath}");
            }

            // Save material as asset
            AssetDatabase.CreateAsset(mat, materialPath);
            Debug.Log($"Created material: {materialPath}");

            return mat;
        }

        private static void ConfigureVignetteMaterial(Material mat)
        {
            if (mat == null) return;

            mat.SetColor("_Color", new Color(0.02f, 0.01f, 0.03f, 1f));
            mat.SetFloat("_Intensity", 0.6f);
            mat.SetFloat("_PulseSpeed", 0.3f);
            mat.SetFloat("_PulseAmount", 0.05f);
        }

        private static void ConfigureSmokeMaterial(Material mat)
        {
            if (mat == null) return;

            mat.SetColor("_Color", new Color(1f, 0.4f, 0.15f, 0.2f));
            mat.SetVector("_ScrollSpeed", new Vector4(0.04f, 0.015f, 0, 0));
            mat.SetFloat("_Intensity", 0.35f);
            mat.SetFloat("_FadeTop", 0.5f);
            mat.SetFloat("_FadeBottom", 0.0f);
            mat.SetFloat("_TileScale", 2f);
            mat.SetVector("_Layer2Speed", new Vector4(0.025f, 0.01f, 0, 0));
            mat.SetFloat("_Layer2Scale", 1.5f);
            mat.SetFloat("_Layer2Intensity", 0.25f);
        }

        private static void ConfigureParticlesMaterial(Material mat)
        {
            if (mat == null) return;

            mat.SetColor("_Color", new Color(1f, 0.5f, 0.2f, 0.4f));
            mat.SetVector("_ScrollSpeed", new Vector4(0.012f, -0.02f, 0.008f, -0.015f));
            mat.SetFloat("_Intensity", 0.7f);
            mat.SetFloat("_TileScale", 4f);
            mat.SetFloat("_Layer2Scale", 6f);
            mat.SetFloat("_GlowStrength", 0.6f);
            mat.SetFloat("_FadeEdges", 0.1f);
        }

        private static void ConfigureHeatDistortionMaterial(Material mat)
        {
            if (mat == null) return;

            mat.SetFloat("_DistortStrength", 0.012f);
            mat.SetVector("_ScrollSpeed", new Vector4(0.08f, 0.1f, 0, 0));
            mat.SetFloat("_TileScale", 1.5f);
        }

        private static GameObject CreateVFXQuad(string name, Material material, Transform parent, float zOffset)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent);
            quad.transform.localPosition = new Vector3(0, 0, zOffset);
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(20f, 12f, 1f); // Roughly 16:9 aspect

            // Remove collider (not needed for VFX)
            Object.DestroyImmediate(quad.GetComponent<Collider>());

            // Set material
            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            renderer.material = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return quad;
        }

        [MenuItem("VeilBreakers/Create Heat Distortion Mask")]
        public static void CreateHeatDistortionMask()
        {
            // Create a radial gradient mask centered on screen
            // This will focus distortion on the creature's glowing chest area

            int size = 1024;
            Texture2D mask = new Texture2D(size, size, TextureFormat.R8, false);

            Vector2 center = new Vector2(0.5f, 0.45f); // Slightly below center (chest area)
            float radius = 0.35f;
            float falloff = 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    float dist = Vector2.Distance(new Vector2(u, v), center);
                    float value = 1f - Mathf.Clamp01((dist - radius) / falloff);
                    value = Mathf.SmoothStep(0, 1, value);

                    // Add secondary glow for eyes (upper area)
                    Vector2 eyeCenter = new Vector2(0.5f, 0.65f);
                    float eyeDist = Vector2.Distance(new Vector2(u, v), eyeCenter);
                    float eyeValue = 1f - Mathf.Clamp01((eyeDist - 0.15f) / 0.2f);
                    eyeValue = Mathf.SmoothStep(0, 1, eyeValue) * 0.7f;

                    value = Mathf.Max(value, eyeValue);

                    mask.SetPixel(x, y, new Color(value, value, value, 1f));
                }
            }

            mask.Apply();

            // Save texture
            byte[] bytes = mask.EncodeToPNG();
            string path = VFX_TEXTURES_PATH + "HeatDistortion_Mask.png";
            System.IO.File.WriteAllBytes(path, bytes);

            AssetDatabase.Refresh();

            // Configure import settings
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                importer.sRGBTexture = false;
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }

            Debug.Log($"Created heat distortion mask at: {path}");
        }
    }
}
