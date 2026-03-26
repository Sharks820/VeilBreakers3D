using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Environmental VFX: Volcanic ash drifting slowly down
/// AAA ParticleSystem with noise turbulence, world-space simulation, LOD-friendly.
/// </summary>
public static class VeilBreakers_EnvVFX_Ash
{
    [MenuItem("VeilBreakers/VFX/Environment/Ash")]
    public static void Execute()
    {
        try
        {
            // Create environmental VFX -- Volcanic ash drifting slowly down
            var go = new GameObject("Ash_EnvironmentVFX");
            var ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();

            // Main module
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(9.600000000000001f, 12.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.041999999999999996f, 0.078f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.05f;
            main.playOnAwake = true;
            main.loop = true;

            // Emission
            var emission = ps.emission;
            emission.rateOverTime = 25f;

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
                    new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 0f),
                    new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.5f, 0.1f),
                    new GradientAlphaKey(0.5f, 0.85f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = gradient;

            // Noise turbulence for organic motion
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.5f;
            noise.scrollSpeed = 0.2f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.High;

            // Renderer
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var mat = new Material(Shader.Find("Particles/Standard Unlit"));
            mat.SetColor("_Color", new Color(0.3f, 0.3f, 0.3f, 0.5f));
            renderer.material = mat;

            // Save as prefab
            string prefabDir = "Assets/Prefabs/VFX/Environment";
            if (!AssetDatabase.IsValidFolder(prefabDir))
            {
                Directory.CreateDirectory(prefabDir);
                AssetDatabase.Refresh();
            }
            string prefabPath = prefabDir + "/Ash_EnvVFX.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);

            string json = "{\"status\": \"success\", \"action\": \"create_environmental_vfx\", \"type\": \"ash\", \"prefab_path\": \"" + prefabPath + "\"}";
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
