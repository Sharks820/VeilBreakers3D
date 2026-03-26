using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Environmental VFX: Bioluminescent fireflies drifting at night
/// AAA ParticleSystem with noise turbulence, world-space simulation, LOD-friendly.
/// </summary>
public static class VeilBreakers_EnvVFX_Fireflies
{
    [MenuItem("VeilBreakers/VFX/Environment/Fireflies")]
    public static void Execute()
    {
        try
        {
            // Create environmental VFX -- Bioluminescent fireflies drifting at night
            var go = new GameObject("Fireflies_EnvironmentVFX");
            var ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();

            // Main module
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4.800000000000001f, 6.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.020999999999999998f, 0.039f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startColor = new Color(0.9f, 1.0f, 0.3f, 0.9f);
            main.maxParticles = 96;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.0f;
            main.playOnAwake = true;
            main.loop = true;

            // Emission
            var emission = ps.emission;
            emission.rateOverTime = 8f;

            // Shape -- large area emitter for environment coverage
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20f, 0.5f, 20f);

            // Color over lifetime
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.9f, 1.0f, 0.3f), 0f),
                    new GradientColorKey(new Color(0.9f, 1.0f, 0.3f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.9f, 0.1f),
                    new GradientAlphaKey(0.9f, 0.85f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = gradient;

            // Noise turbulence for organic motion
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 1.2f;
            noise.frequency = 0.5f;
            noise.scrollSpeed = 0.2f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.High;

            // Renderer
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var mat = new Material(Shader.Find("Particles/Standard Unlit"));
            mat.SetColor("_Color", new Color(0.9f, 1.0f, 0.3f, 0.9f));
            renderer.material = mat;

            // Save as prefab
            string prefabDir = "Assets/Prefabs/VFX/Environment";
            if (!AssetDatabase.IsValidFolder(prefabDir))
            {
                Directory.CreateDirectory(prefabDir);
                AssetDatabase.Refresh();
            }
            string prefabPath = prefabDir + "/Fireflies_EnvVFX.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);

            string json = "{\"status\": \"success\", \"action\": \"create_environmental_vfx\", \"type\": \"fireflies\", \"prefab_path\": \"" + prefabPath + "\"}";
            File.WriteAllText("Temp/vb_result.json", json);
            Debug.Log("[VeilBreakers] Environmental VFX created: " + prefabPath);

            Object.DestroyImmediate(go);
        }
        catch (System.Exception ex)
        {
            string json = "{\"status\": \"error\", \"action\": \"create_environmental_vfx\", \"message\": \"" + ex.Message.Replace("\"", "\\\"") + "\"}";
            File.WriteAllText("Temp/vb_result.json", json);
            Debug.LogError("[VeilBreakers] Environmental VFX creation failed: " + ex.Message);
        }
    }
}
