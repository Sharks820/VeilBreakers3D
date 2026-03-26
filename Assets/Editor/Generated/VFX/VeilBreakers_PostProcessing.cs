using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

public static class VeilBreakers_PostProcessing
{
    [MenuItem("VeilBreakers/VFX/Setup Post Processing")]
    public static void Execute()
    {
        try
        {
            // Create Volume GameObject
            var go = new GameObject("VeilBreakers_PostProcessVolume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;

            // Create VolumeProfile asset
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            // === TONEMAPPING (AAA standard: ACES filmic curve) ===
            var tonemapping = profile.Add<Tonemapping>();
            tonemapping.mode.Override(TonemappingMode.ACES);

            // === BLOOM (brand-glow emphasizer) ===
            var bloom = profile.Add<Bloom>();
            bloom.intensity.Override(1.2f);
            bloom.threshold.Override(0.85f);
            bloom.scatter.Override(0.7f);
            bloom.tint.Override(new Color(1.0f, 0.95f, 0.9f, 1.0f));
            bloom.highQualityFiltering.Override(true);

            // === COLOR ADJUSTMENTS (dark fantasy base grade) ===
            var colorAdj = profile.Add<ColorAdjustments>();
            colorAdj.colorFilter.Override(new Color(0.95f, 0.93f, 0.98f, 1.0f));
            colorAdj.saturation.Override(0.0f - 12f);
            colorAdj.contrast.Override(18f);
            colorAdj.postExposure.Override(0.15f);
            colorAdj.hueShift.Override(0f);

            // === WHITE BALANCE (cool moonlit tint for dark fantasy) ===
            var whiteBalance = profile.Add<WhiteBalance>();
            whiteBalance.temperature.Override(0.0f - 8f);
            whiteBalance.tint.Override(3f);

            // === SHADOWS, MIDTONES, HIGHLIGHTS (cinematic color grading) ===
            var smh = profile.Add<ShadowsMidtonesHighlights>();
            // Warm shadows (amber/rust tones in dark areas)
            smh.shadows.Override(new Vector4(1.05f, 0.95f, 0.85f, 0f));
            // Cool midtones (slight blue-grey)
            smh.midtones.Override(new Vector4(0.95f, 0.97f, 1.05f, 0f));
            // Desaturated highlights (pale, washed-out peaks)
            smh.highlights.Override(new Vector4(1.0f, 0.98f, 1.02f, -0.05f));
            smh.shadowsStart.Override(0f);
            smh.shadowsEnd.Override(0.25f);
            smh.highlightsStart.Override(0.55f);
            smh.highlightsEnd.Override(1f);

            // === VIGNETTE (dramatic framing) ===
            var vignette = profile.Add<Vignette>();
            vignette.intensity.Override(0.35f);
            vignette.smoothness.Override(0.35f);
            // roundness removed — not available in URP Vignette
            vignette.color.Override(new Color(0.05f, 0.02f, 0.08f, 1f));

            // === DEPTH OF FIELD (cinematic focus) ===
            var dof = profile.Add<DepthOfField>();
            dof.mode.Override(DepthOfFieldMode.Bokeh);
            dof.focusDistance.Override(100.0f);
            dof.focalLength.Override(50f);
            dof.aperture.Override(5.6f);
            dof.bladeCount.Override(6);

            // === CHROMATIC ABERRATION (subtle edge distortion) ===
            var chromAb = profile.Add<ChromaticAberration>();
            chromAb.intensity.Override(0.08f);

            // === FILM GRAIN (dark fantasy grit) ===
            var filmGrain = profile.Add<FilmGrain>();
            filmGrain.type.Override(FilmGrainLookup.Medium3);
            filmGrain.intensity.Override(0.15f);
            filmGrain.response.Override(0.8f);

            // === MOTION BLUR (combat impact) ===
            var motionBlur = profile.Add<MotionBlur>();
            motionBlur.intensity.Override(0.15f);
            motionBlur.quality.Override(MotionBlurQuality.High);

            // === LENS DISTORTION (subtle dark fantasy warping) ===
            var lensDist = profile.Add<LensDistortion>();
            lensDist.intensity.Override(-0.08f);
            lensDist.scale.Override(1.01f);

            // NOTE: SSAO in URP is a Renderer Feature, not a Volume Override.
            // Configure SSAO on the Universal Renderer Data asset:
            //   UniversalRendererData > Add Renderer Feature > Screen Space Ambient Occlusion
            //   Recommended: Intensity=0.0, Radius=0.3, Sample Count=Medium
            //   Enable "After Opaque" for best quality.

            // Assign profile to volume
            volume.profile = profile;

            // Save profile asset
            string profileDir = "Assets/Settings/PostProcessing";
            if (!AssetDatabase.IsValidFolder(profileDir))
            {
                Directory.CreateDirectory(profileDir);
                AssetDatabase.Refresh();
            }
            string profilePath = profileDir + "/VeilBreakers_PostProcess.asset";
            AssetDatabase.CreateAsset(profile, profilePath);
            AssetDatabase.SaveAssets();

            string json = "{\"status\": \"success\", \"action\": \"setup_post_processing\", \"profile_path\": \"" + profilePath + "\", \"bloom\": 1.2, \"vignette\": 0.35, \"ao\": 0.0, \"dof_focus\": 100.0, \"tonemapping\": \"ACES\", \"film_grain\": 0.15, \"chromatic_aberration\": 0.08, \"motion_blur\": 0.15}";
            File.WriteAllText("Temp/vb_result.json", json);
            Debug.Log("[VeilBreakers] AAA post-processing chain created: " + profilePath);
        }
        catch (System.Exception ex)
        {
            string json = "{\"status\": \"error\", \"action\": \"setup_post_processing\", \"message\": \"" + ex.Message.Replace("\"", "\\\"") + "\"}";
            File.WriteAllText("Temp/vb_result.json", json);
            Debug.LogError("[VeilBreakers] Post-processing setup failed: " + ex.Message);
        }
    }
}
