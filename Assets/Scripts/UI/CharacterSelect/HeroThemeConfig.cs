using UnityEngine;
using VeilBreakers.Core;
using UnityEngine.Rendering;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Unified per-hero visual identity data. All visual systems read from this single
    /// source for guaranteed cross-system consistency.
    /// </summary>
    [CreateAssetMenu(fileName = "NewHeroTheme", menuName = "VeilBreakers/Hero Theme Config")]
    public class HeroThemeConfig : ScriptableObject
    {
        // =============================================================================
        // IDENTITY
        // =============================================================================

        [Header("Identity")]
        [Tooltip("Must match heroId in HeroDisplayConfig and heroes.json")]
        public string heroId;

        // =============================================================================
        // COLORS
        // =============================================================================

        [Header("Colors")]
        [Tooltip("Main brand color (Vex: red-orange, Seraphina: purple, Orion: purplish blue, Nyx: blood red)")]
        public Color primaryColor;

        [Tooltip("Glow/accent color with alpha for intensity")]
        public Color glowColor;

        [Tooltip("Dark variant for shadows and backgrounds")]
        public Color darkColor;

        [Tooltip("HDR edge color for VeilDissolve shader dissolve boundary")]
        public Color dissolveEdgeColor;

        // =============================================================================
        // POST-PROCESSING
        // =============================================================================

        [Header("Post-Processing")]
        [Tooltip("Per-hero URP VolumeProfile asset for Bloom, DoF, Vignette, Color Adjustments")]
        public VolumeProfile volumeProfile;

        [Tooltip("Chromatic aberration intensity (only Nyx > 0, value ~0.15)")]
        [Range(0f, 5f)]
        public float chromaticAberrationIntensity = 0f;

        // =============================================================================
        // LIGHTING
        // =============================================================================

        [Header("Lighting")]
        public Color fillLightColor;

        [Range(0f, 2f)]
        public float fillLightIntensity = 0.8f;

        public Color rimLightColor;

        [Range(0f, 3f)]
        public float rimLightIntensity = 1.2f;

        public Color ambientColor;

        [Tooltip("Only true for Nyx (unstable reality rim flicker)")]
        public bool rimFlicker = false;

        // =============================================================================
        // MUSIC PARAMETERS
        // =============================================================================

        [Header("Music Parameters")]
        [Tooltip("Overall music intensity level")]
        [Range(0f, 1f)]
        public float musicIntensity;

        [Tooltip("Warmth/organic feel (Vex high, Nyx low)")]
        [Range(0f, 1f)]
        public float musicWarmth;

        [Tooltip("Tension/urgency level (Orion high)")]
        [Range(0f, 1f)]
        public float musicTension;

        [Tooltip("Synthesizer layer prominence (Nyx high)")]
        [Range(0f, 1f)]
        public float musicSynth;

        [Tooltip("Percussion layer prominence (Vex/Orion high)")]
        [Range(0f, 1f)]
        public float musicPerc;

        [Tooltip("Pad/ambient layer prominence (Seraphina high)")]
        [Range(0f, 1f)]
        public float musicPad;

        [Tooltip("Filter sweep intensity (Seraphina/Nyx high)")]
        [Range(0f, 1f)]
        public float musicFilter;

        // =============================================================================
        // PARTICLES
        // =============================================================================

        [Header("Particles")]
        [Tooltip("Per-hero particle system prefab for 3D stage background")]
        public GameObject particlePrefab;

        [Tooltip("Maximum particle count (GPU instanced, cap for performance)")]
        public int maxParticleCount = 200;

        // =============================================================================
        // OVERLAYS
        // =============================================================================

        [Header("Overlays")]
        [Tooltip("Scanline overlay opacity (Nyx highest at 0.12)")]
        [Range(0f, 0.2f)]
        public float scanlineOpacity = 0.05f;

        [Tooltip("Vignette darkness intensity (Orion tightest at 0.40)")]
        [Range(0f, 0.5f)]
        public float vignetteIntensity = 0.25f;

        [Tooltip("Vignette border width in pixels")]
        [Range(0f, 80f)]
        public float vignetteBorderWidth = 80f;

        [Tooltip("Veil glow overlay opacity (hero accent color wash)")]
        [Range(0f, 0.2f)]
        public float veilGlowOpacity = 0.08f;

        // =============================================================================
        // DISSOLVE
        // =============================================================================

        [Header("Dissolve")]
        [Tooltip("Noise texture scale for VeilDissolve shader (Nyx chaotic/high, Vex medium)")]
        [Range(0.1f, 2f)]
        public float dissolveNoiseScale = 1f;

        [Tooltip("Duration of dissolve in/out animation in seconds")]
        [Range(0.1f, 1f)]
        public float dissolveDuration = 0.4f;

        // =============================================================================
        // GLITCH TEXT
        // =============================================================================

        [Header("Glitch Text")]
        [Tooltip("Characters per second for glitch text resolve (Nyx slower, Vex faster)")]
        [Range(20f, 80f)]
        public float glitchResolveSpeed = 50f;

        // =============================================================================
        // MONSTER
        // =============================================================================

        [Header("Monster")]
        [Tooltip("Brand aura color for starter monster glow effect")]
        public Color monsterAuraColor;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(heroId))
            {
                ErrorLogger.Warn($"[HeroThemeConfig] {name} has no heroId assigned!");
            }
        }
#endif
    }
}
