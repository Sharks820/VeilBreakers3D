using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Manages the cinematic background layers and adds interactive parallax movement.
    /// Responds to hero theme changes by tinting the atmospheric fog and generating dynamic nebula textures.
    /// </summary>
    public class CharSelectEnvironmentController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;
        
        [Header("Parallax Settings")]
        [SerializeField] private float _deepIntensity = 20f;    // Minimal movement
        [SerializeField] private float _fogIntensity = 50f;     // Mid movement
        [SerializeField] private float _vignetteIntensity = 80f; // Overlay movement
        [SerializeField] private float _lerpSpeed = 5f;

        [Header("Procedural Generation")]
        [SerializeField] private int _textureSize = 256;
        [SerializeField] private float _noiseScale = 5f;

        private VisualElement _parallaxRoot;
        private VisualElement _parallaxDeep;
        private VisualElement _parallaxFog;
        private VisualElement _vignette;
        private VisualElement _fogParticles;

        private Vector2 _targetParallax;
        private Vector2 _currentParallax;
        private Texture2D _nebulaTexture;
        private Color[] _nebulaPixels;

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
            
            if (_nebulaTexture != null)
            {
                Destroy(_nebulaTexture);
                _nebulaTexture = null;
            }
            _nebulaPixels = null;
        }

        private void Update()
        {
            ApplyParallax();
        }

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
            if (_parallaxDeep != null) _parallaxDeep.usageHints = UsageHints.DynamicTransform;
            if (_parallaxFog != null) _parallaxFog.usageHints = UsageHints.DynamicTransform;
            if (_vignette != null) _vignette.usageHints = UsageHints.DynamicTransform;
            if (_fogParticles != null) _fogParticles.usageHints = UsageHints.DynamicColor;
        }

        private void HandleScreenReady()
        {
            // Initial positioning
            _currentParallax = Vector2.zero;
        }

        private void ApplyParallax()
        {
            if (_parallaxRoot == null) return;

            // Normalize mouse position (-1 to 1) using New Input System
            Vector2 mousePos = InputManager.HasInstance ? InputManager.Instance.MousePosition : Vector2.zero;
            float nx = (mousePos.x / Mathf.Max(Screen.width, 1)) * 2f - 1f;
            float ny = (mousePos.y / Mathf.Max(Screen.height, 1)) * 2f - 1f;

            _targetParallax = new Vector2(nx, ny);
            _currentParallax = Vector2.Lerp(_currentParallax, _targetParallax, Time.deltaTime * _lerpSpeed);

            // Apply offsets with different intensities for depth effect
            if (_parallaxDeep != null) 
                SetTranslate(_parallaxDeep, _currentParallax * _deepIntensity);
            
            if (_parallaxFog != null) 
                SetTranslate(_parallaxFog, _currentParallax * _fogIntensity);
            
            if (_vignette != null) 
                SetTranslate(_vignette, _currentParallax * _vignetteIntensity);
        }

        private void SetTranslate(VisualElement element, Vector2 offset)
        {
            // UI Toolkit uses translate for hardware-accelerated movement
            // Correct usage: Translate(Length x, Length y, float z = 0)
            element.style.translate = new Translate(new Length(offset.x, LengthUnit.Pixel), new Length(-offset.y, LengthUnit.Pixel), 0f);
        }

        private void HandleHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            if (config == null) return;

            // 1. Tint Fog Particles
            if (_fogParticles != null)
            {
                Color tint = config.primaryColor;
                tint.a = 0.15f; // Subtle tint
                _fogParticles.style.backgroundColor = tint;
            }

            // 2. Generate Dynamic Nebula Background
            // If the Nano Banana AI generation failed, we generate a procedural texture here
            if (_parallaxDeep != null)
            {
                GenerateNebula(config.secondaryColor, config.primaryColor);
            }
        }

        private void GenerateNebula(Color baseColor, Color accentColor)
        {
            // Recreate texture if size changed (SerializeField edited at runtime)
            if (_nebulaTexture != null && (_nebulaTexture.width != _textureSize || _nebulaTexture.height != _textureSize))
            {
                Destroy(_nebulaTexture);
                _nebulaTexture = null;
            }

            if (_nebulaTexture == null)
            {
                _nebulaTexture = new Texture2D(_textureSize, _textureSize, TextureFormat.RGBA32, false);
                _nebulaTexture.wrapMode = TextureWrapMode.Clamp;
                _nebulaTexture.filterMode = FilterMode.Bilinear;
            }

            int pixelCount = _textureSize * _textureSize;
            if (_nebulaPixels == null || _nebulaPixels.Length != pixelCount)
            {
                _nebulaPixels = new Color[pixelCount];
            }

            float scale = _noiseScale;

            // Random offset for unique patterns per hero
            float offsetX = Random.Range(0f, 100f);
            float offsetY = Random.Range(0f, 100f);

            // Cache center and radius outside loops
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
                    finalColor = Color.Lerp(finalColor, accentColor, Mathf.Pow(combined, 3f)); // Highlights on peaks

                    // Vignette edges
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) * radiusInv;
                    finalColor *= Mathf.SmoothStep(1.2f, 0.4f, dist);

                    _nebulaPixels[y * _textureSize + x] = finalColor;
                }
            }

            _nebulaTexture.SetPixels(_nebulaPixels);
            _nebulaTexture.Apply();

            _parallaxDeep.style.backgroundImage = new StyleBackground(_nebulaTexture);
        }
    }
}
