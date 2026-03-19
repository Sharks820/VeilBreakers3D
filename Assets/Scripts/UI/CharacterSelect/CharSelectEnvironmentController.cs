using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Manages the cinematic background layers and adds interactive parallax movement.
    /// Responds to hero theme changes by tinting the atmospheric fog and assigning pre-baked nebula textures.
    /// All nebula textures are generated once at initialization using a shared pixel buffer -- zero per-switch
    /// Color[] allocation.
    /// </summary>
    public class CharSelectEnvironmentController : MonoBehaviour
    {
        // =============================================================================
        // SERIALIZED FIELDS
        // =============================================================================

        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Parallax Settings")]
        [SerializeField] private float _deepIntensity = 20f;
        [SerializeField] private float _fogIntensity = 50f;
        [SerializeField] private float _vignetteIntensity = 80f;
        [SerializeField] private float _lerpSpeed = 5f;

        [Header("Procedural Generation")]
        [SerializeField] private int _textureSize = 256;
        [SerializeField] private float _noiseScale = 5f;

        // =============================================================================
        // CACHED UI REFERENCES
        // =============================================================================

        private VisualElement _parallaxRoot;
        private VisualElement _parallaxDeep;
        private VisualElement _parallaxFog;
        private VisualElement _vignette;
        private VisualElement _fogParticles;

        // =============================================================================
        // PARALLAX STATE
        // =============================================================================

        private Vector2 _targetParallax;
        private Vector2 _currentParallax;

        // =============================================================================
        // NEBULA CACHE -- PRE-BAKED AT INIT
        // =============================================================================

        private readonly Dictionary<string, Texture2D> _nebulaCache =
            new Dictionary<string, Texture2D>(4);
        private Color[] _sharedPixelBuffer;
        private bool _nebulasPreBaked;

        // =============================================================================
        // LIFECYCLE
        // =============================================================================

        private void OnEnable()
        {
            CacheReferences();
            CharSelectEvents.OnHeroChanged += HandleHeroChanged;
            CharSelectEvents.OnScreenReady += HandleScreenReady;
        }

        private void OnDisable()
        {
            CharSelectEvents.OnHeroChanged -= HandleHeroChanged;
            CharSelectEvents.OnScreenReady -= HandleScreenReady;

            // Destroy all cached nebula textures to prevent memory leaks
            foreach (var kvp in _nebulaCache)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            _nebulaCache.Clear();

            _sharedPixelBuffer = null;
            _nebulasPreBaked = false;
        }

        private void Update()
        {
            ApplyParallax();
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void CacheReferences()
        {
            if (_uiDocument == null) return;
            var root = _uiDocument.rootVisualElement;

            _parallaxRoot = root.Q<VisualElement>("parallax-bg");
            _parallaxDeep = root.Q<VisualElement>("parallax-deep");
            _parallaxFog = root.Q<VisualElement>("parallax-fog");
            _vignette = root.Q<VisualElement>("background-vignette");
            _fogParticles = root.Q<VisualElement>("fog-particles");

            // Mark for dynamic transform optimization in UI Toolkit
            if (_parallaxDeep != null) _parallaxDeep.usageHints |= UsageHints.DynamicTransform;
            if (_parallaxFog != null) _parallaxFog.usageHints |= UsageHints.DynamicTransform;
            if (_vignette != null) _vignette.usageHints |= UsageHints.DynamicTransform;
            if (_fogParticles != null) _fogParticles.usageHints |= UsageHints.DynamicColor;
        }

        private void HandleScreenReady()
        {
            _currentParallax = Vector2.zero;
            PreBakeAllNebulas();
        }

        // =============================================================================
        // PARALLAX
        // =============================================================================

        private void ApplyParallax()
        {
            if (_parallaxRoot == null) return;

            // Normalize mouse position (-1 to 1) using New Input System
            Vector2 mousePos = InputManager.HasInstance ? InputManager.Instance.MousePosition : Vector2.zero;
            float nx = (mousePos.x / Mathf.Max(Screen.width, 1)) * 2f - 1f;
            float ny = (mousePos.y / Mathf.Max(Screen.height, 1)) * 2f - 1f;

            _targetParallax = new Vector2(nx, ny);
            Vector2 newParallax = Vector2.Lerp(_currentParallax, _targetParallax, Time.deltaTime * _lerpSpeed);

            // Skip style writes when parallax has converged (< 0.01px movement)
            if ((newParallax - _currentParallax).sqrMagnitude < 0.0001f) return;
            _currentParallax = newParallax;

            // Apply offsets with different intensities for depth effect
            // translate is GPU-accelerated with UsageHints.DynamicTransform -- no layout recalc
            if (_parallaxDeep != null)
                SetTranslate(_parallaxDeep, _currentParallax * _deepIntensity);

            if (_parallaxFog != null)
                SetTranslate(_parallaxFog, _currentParallax * _fogIntensity);

            if (_vignette != null)
                SetTranslate(_vignette, _currentParallax * _vignetteIntensity);
        }

        private static void SetTranslate(VisualElement element, Vector2 offset)
        {
            element.style.translate = new Translate(new Length(offset.x, LengthUnit.Pixel), new Length(-offset.y, LengthUnit.Pixel), 0f);
        }

        // =============================================================================
        // NEBULA PRE-BAKING
        // =============================================================================

        /// <summary>
        /// Pre-generates nebula textures for all heroes during screen initialization.
        /// Uses a shared pixel buffer to avoid per-hero Color[] allocations.
        /// </summary>
        private void PreBakeAllNebulas()
        {
            if (!GameDatabase.HasInstance) return;

            var heroes = GameDatabase.Instance.GetAllHeroes();
            if (heroes == null || heroes.Count == 0) return;

            var configs = Resources.LoadAll<HeroDisplayConfig>("CharacterSelect/HeroDisplayConfigs");
            if (configs == null || configs.Length == 0) return;

            int pixelCount = _textureSize * _textureSize;
            _sharedPixelBuffer = new Color[pixelCount]; // One allocation for all heroes

            for (int i = 0; i < heroes.Count; i++)
            {
                string heroId = heroes[i].hero_id;
                HeroDisplayConfig config = FindConfig(configs, heroId);
                if (config == null) continue;

                var tex = new Texture2D(_textureSize, _textureSize, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;

                GenerateNebulaIntoBuffer(config.secondaryColor, config.primaryColor, heroId);
                tex.SetPixels(_sharedPixelBuffer);
                tex.Apply();

                _nebulaCache[heroId] = tex;
            }

            _nebulasPreBaked = true;
        }

        /// <summary>
        /// Finds a HeroDisplayConfig by heroId without LINQ.
        /// </summary>
        private static HeroDisplayConfig FindConfig(HeroDisplayConfig[] configs, string heroId)
        {
            for (int i = 0; i < configs.Length; i++)
            {
                if (configs[i] != null && configs[i].heroId == heroId)
                    return configs[i];
            }
            return null;
        }

        /// <summary>
        /// Generates a procedural nebula texture into the shared pixel buffer.
        /// Uses the hero ID hash code for deterministic noise offset instead of Random.Range
        /// to ensure consistent visuals across sessions.
        /// </summary>
        private void GenerateNebulaIntoBuffer(Color baseColor, Color accentColor, string heroId)
        {
            // Use heroId hashcode for deterministic offset instead of Random.Range
            int hash = heroId.GetHashCode();
            float offsetX = Mathf.Abs(hash % 1000) * 0.1f;
            float offsetY = Mathf.Abs((hash / 1000) % 1000) * 0.1f;

            float scale = _noiseScale;
            float center = _textureSize * 0.5f;
            float radiusInv = 1f / center;

            for (int y = 0; y < _textureSize; y++)
            {
                for (int x = 0; x < _textureSize; x++)
                {
                    float xCoord = (float)x / _textureSize * scale + offsetX;
                    float yCoord = (float)y / _textureSize * scale + offsetY;

                    // Layered Perlin noise for "cloudy" look
                    float sample = Mathf.PerlinNoise(xCoord, yCoord);
                    float sample2 = Mathf.PerlinNoise(xCoord * 2f + 10f, yCoord * 2f + 10f) * 0.5f;
                    float combined = Mathf.Clamp01((sample + sample2) / 1.5f);

                    // Gradient: Dark Void -> Base Color -> Accent Highlights
                    Color finalColor = Color.Lerp(Color.black, baseColor, combined);
                    finalColor = Color.Lerp(finalColor, accentColor, Mathf.Pow(combined, 3f));

                    // Vignette edges
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) * radiusInv;
                    finalColor *= Mathf.SmoothStep(1.2f, 0.4f, dist);

                    _sharedPixelBuffer[y * _textureSize + x] = finalColor;
                }
            }
        }

        // =============================================================================
        // HERO CHANGED -- ZERO-ALLOC TEXTURE ASSIGNMENT
        // =============================================================================

        private void HandleHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            if (config == null) return;

            // Tint fog particles
            if (_fogParticles != null)
            {
                Color tint = config.primaryColor;
                tint.a = 0.15f;
                _fogParticles.style.backgroundColor = tint;
            }

            // Assign pre-baked nebula -- ZERO allocation
            if (_parallaxDeep != null && data != null && _nebulaCache.TryGetValue(data.hero_id, out var cached))
            {
                _parallaxDeep.style.backgroundImage = new StyleBackground(cached);
            }
        }
    }
}
