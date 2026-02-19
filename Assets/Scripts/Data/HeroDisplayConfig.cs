using System;
using UnityEngine;

namespace VeilBreakers.Data
{
    [CreateAssetMenu(fileName = "NewHeroDisplayConfig", menuName = "VeilBreakers/Hero Display Config")]
    public class HeroDisplayConfig : ScriptableObject
    {
        // =============================================================================
        // IDENTITY
        // =============================================================================

        [Header("Identity")]
        [Tooltip("Must match hero_id in heroes.json")]
        public string heroId;

        // =============================================================================
        // CAMERA
        // =============================================================================

        [Header("Camera")]
        public Vector3 cameraOffset = new Vector3(0f, 1.2f, -3f);
        [Range(15f, 60f)]
        public float cameraFOV = 30f;
        [Range(0f, 0.5f)]
        public float cameraFramePadding = 0.15f;

        // =============================================================================
        // LIGHTING
        // =============================================================================

        [Header("Lighting - Key")]
        public Color keyLightColor = Color.white;
        [Range(0f, 3f)]
        public float keyLightIntensity = 1.2f;

        [Header("Lighting - Fill")]
        public Color fillLightColor = new Color(0.4f, 0.5f, 0.6f);
        [Range(0f, 2f)]
        public float fillLightIntensity = 0.6f;

        [Header("Lighting - Rim")]
        public Color rimLightColor = Color.cyan;
        [Range(0f, 3f)]
        public float rimLightIntensity = 1.5f;

        // =============================================================================
        // THEME COLORS
        // =============================================================================

        [Header("Theme Colors")]
        [Tooltip("Main brand color (panels, stat bars, particles)")]
        public Color primaryColor;
        [Tooltip("Supporting color (backgrounds, fills)")]
        public Color secondaryColor;
        [Tooltip("Highlights, glows, accents")]
        public Color accentColor;

        // =============================================================================
        // MODEL
        // =============================================================================

        [Header("Model")]
        [Tooltip("null = use brand-colored placeholder capsule")]
        public GameObject modelPrefab;

        // =============================================================================
        // ANIMATIONS
        // =============================================================================

        [Header("Animations")]
        public AnimationClip idleClip;
        [Tooltip("Random selection pool for idle variety")]
        public AnimationClip[] idleVariantClips;
        public AnimationClip selectedClip;
        public AnimationClip showcaseClip;
        public AnimationClip embarkClip;

        [Header("Animation Timing")]
        [Range(5f, 30f)]
        public float idleVariantMinDelay = 10f;
        [Range(5f, 30f)]
        public float idleVariantMaxDelay = 16f;
        [Range(0.5f, 5f)]
        public float selectedToShowcaseDelay = 2f;
        [Range(1f, 8f)]
        public float showcaseToIdleDelay = 4f;
        [Range(0.05f, 1f)]
        public float crossfadeDuration = 0.25f;

        // =============================================================================
        // AUDIO
        // =============================================================================

        [Header("Audio")]
        [Tooltip("Played when hero is selected in carousel")]
        public AudioClip selectionSFX;
        [Tooltip("Played on embark confirmation")]
        public AudioClip embarkSFX;
        [Tooltip("Background atmosphere loop per hero")]
        public AudioClip ambientLoop;

        // =============================================================================
        // CHAMPION MONSTER
        // =============================================================================

        [Header("Champion Monster")]
        [Tooltip("null = data-only display in left panel")]
        public GameObject championModelPrefab;
        public Vector3 championOffset = new Vector3(0.5f, 0f, 0.3f);
        [Range(0.1f, 1f)]
        public float championScale = 0.35f;
        public AnimationClip championIdleClip;

        // =============================================================================
        // VFX
        // =============================================================================

        [Header("VFX")]
        public Color particleColor;
        [Tooltip("null = skip selection VFX")]
        public GameObject selectionVFXPrefab;
    }
}
