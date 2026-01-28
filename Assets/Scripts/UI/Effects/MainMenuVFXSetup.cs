using UnityEngine;
using UnityEngine.UI;

namespace VeilBreakers.UI.Effects
{
    /// <summary>
    /// Sets up and coordinates all main menu VFX effects.
    /// This bridges the UI Toolkit menu with Canvas/SpriteRenderer-based VFX.
    ///
    /// Hierarchy (created at runtime if needed):
    /// MainMenu_VFX_Root
    ///  ├─ Background_Layer (SpriteRenderer or Canvas)
    ///  │   └─ Demon_Figure
    ///  ├─ Effects_Layer
    ///  │   ├─ Veil_Rift_Distortion (behind logo)
    ///  │   ├─ Chest_Glow
    ///  │   └─ Ash_Embers (ParticleSystem)
    ///  ├─ Logo_Layer
    ///  │   ├─ Logo_Glow
    ///  │   └─ Logo_Base
    ///  └─ Overlay_Layer
    ///      └─ Crimson_Vignette
    /// </summary>
    public class MainMenuVFXSetup : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Art Assets")]
        [SerializeField] private Sprite _demonFigureSprite;
        [SerializeField] private Sprite _logoSprite;
        [SerializeField] private Sprite _logoGlowSprite;
        [SerializeField] private Sprite _circleGlowSprite; // For chest glow, rift, vignette

        [Header("Positioning")]
        [SerializeField] private Vector3 _demonPosition = new Vector3(0f, -1f, 0f);
        [SerializeField] private Vector3 _logoPosition = new Vector3(0f, 3f, 0f);
        [SerializeField] private Vector3 _chestGlowOffset = new Vector3(0f, 0.5f, 0f);

        [Header("Scales")]
        [SerializeField] private float _demonScale = 5f;
        [SerializeField] private float _logoScale = 2f;
        [SerializeField] private float _chestGlowScale = 1.5f;
        [SerializeField] private float _riftScale = 3f;
        [SerializeField] private float _vignetteScale = 20f;

        [Header("Effect Components (Auto-created if null)")]
        [SerializeField] private MenuPulseController _pulseController;
        [SerializeField] private LogoMoltenEffect _logoEffect;
        [SerializeField] private VeilRiftDistortion _riftDistortion;
        [SerializeField] private DemonChestBreathing _chestBreathing;
        [SerializeField] private AshEmberParticles _ashEmbers;
        [SerializeField] private CrimsonRuptureEvent _ruptureEvent;

        // =============================================================================
        // PRIVATE REFERENCES
        // =============================================================================

        private Transform _vfxRoot;
        private Transform _backgroundLayer;
        private Transform _effectsLayer;
        private Transform _logoLayer;
        private Transform _overlayLayer;

        private SpriteRenderer _demonRenderer;
        private SpriteRenderer _logoBaseRenderer;
        private SpriteRenderer _logoGlowRenderer;
        private SpriteRenderer _chestGlowRenderer;
        private SpriteRenderer _riftRenderer;
        private SpriteRenderer _vignetteRenderer;
        private ParticleSystem _particleSystem;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            CreateVFXHierarchy();
            SetupEffects();
        }

        // =============================================================================
        // HIERARCHY CREATION
        // =============================================================================

        private void CreateVFXHierarchy()
        {
            // Create root
            _vfxRoot = new GameObject("MainMenu_VFX_Root").transform;
            _vfxRoot.SetParent(transform);
            _vfxRoot.localPosition = Vector3.zero;

            // Background Layer (z = 10)
            _backgroundLayer = CreateLayer("Background_Layer", 10f);
            CreateDemonFigure();

            // Effects Layer (z = 5)
            _effectsLayer = CreateLayer("Effects_Layer", 5f);
            CreateRiftDistortion();
            CreateChestGlow();
            CreateAshEmbers();

            // Logo Layer (z = 2)
            _logoLayer = CreateLayer("Logo_Layer", 2f);
            CreateLogo();

            // Overlay Layer (z = -5, in front of everything)
            _overlayLayer = CreateLayer("Overlay_Layer", -5f);
            CreateVignette();
        }

        private Transform CreateLayer(string name, float zPosition)
        {
            var layer = new GameObject(name).transform;
            layer.SetParent(_vfxRoot);
            layer.localPosition = new Vector3(0f, 0f, zPosition);
            return layer;
        }

        private void CreateDemonFigure()
        {
            var demon = new GameObject("Demon_Figure");
            demon.transform.SetParent(_backgroundLayer);
            demon.transform.localPosition = _demonPosition;
            demon.transform.localScale = Vector3.one * _demonScale;

            _demonRenderer = demon.AddComponent<SpriteRenderer>();
            _demonRenderer.sprite = _demonFigureSprite;
            _demonRenderer.sortingOrder = 0;
        }

        private void CreateRiftDistortion()
        {
            var rift = new GameObject("Veil_Rift_Distortion");
            rift.transform.SetParent(_effectsLayer);
            rift.transform.localPosition = _logoPosition + new Vector3(0f, 0f, 1f); // Slightly behind logo
            rift.transform.localScale = Vector3.one * _riftScale;

            _riftRenderer = rift.AddComponent<SpriteRenderer>();
            _riftRenderer.sprite = _circleGlowSprite;
            _riftRenderer.color = new Color(0.8f, 0.2f, 0.3f, 0.05f);
            _riftRenderer.sortingOrder = 1;

            _riftDistortion = rift.AddComponent<VeilRiftDistortion>();
        }

        private void CreateChestGlow()
        {
            var chestGlow = new GameObject("Chest_Glow");
            chestGlow.transform.SetParent(_effectsLayer);
            chestGlow.transform.localPosition = _demonPosition + _chestGlowOffset;
            chestGlow.transform.localScale = Vector3.one * _chestGlowScale;

            _chestGlowRenderer = chestGlow.AddComponent<SpriteRenderer>();
            _chestGlowRenderer.sprite = _circleGlowSprite;
            _chestGlowRenderer.color = new Color(1f, 0.5f, 0.2f, 0.3f);
            _chestGlowRenderer.sortingOrder = 2;

            _chestBreathing = chestGlow.AddComponent<DemonChestBreathing>();
        }

        private void CreateAshEmbers()
        {
            var ashEmbers = new GameObject("Ash_Embers");
            ashEmbers.transform.SetParent(_effectsLayer);
            ashEmbers.transform.localPosition = _demonPosition;

            // Create particle system
            _particleSystem = ashEmbers.AddComponent<ParticleSystem>();
            var main = _particleSystem.main;
            main.startLifetime = 4f;
            main.startSpeed = 0.5f;
            main.startSize = 0.05f;
            main.startColor = new Color(0.2f, 0.18f, 0.15f, 0.6f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;
            main.playOnAwake = true;
            main.loop = true;

            // Shape (sphere around demon chest)
            var shape = _particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 2f;

            // Emission
            var emission = _particleSystem.emission;
            emission.rateOverTime = 5f;

            // Renderer
            var renderer = _particleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.sortingOrder = 5;

            _ashEmbers = ashEmbers.AddComponent<AshEmberParticles>();
        }

        private void CreateLogo()
        {
            // Logo Glow (behind base)
            var logoGlow = new GameObject("Logo_Glow");
            logoGlow.transform.SetParent(_logoLayer);
            logoGlow.transform.localPosition = _logoPosition;
            logoGlow.transform.localScale = Vector3.one * _logoScale * 1.05f;

            _logoGlowRenderer = logoGlow.AddComponent<SpriteRenderer>();
            _logoGlowRenderer.sprite = _logoGlowSprite ?? _logoSprite;
            _logoGlowRenderer.color = new Color(0.4f, 0.08f, 0.08f, 0.3f);
            _logoGlowRenderer.sortingOrder = 10;

            // Logo Base (main)
            var logoBase = new GameObject("Logo_Base");
            logoBase.transform.SetParent(_logoLayer);
            logoBase.transform.localPosition = _logoPosition;
            logoBase.transform.localScale = Vector3.one * _logoScale;

            _logoBaseRenderer = logoBase.AddComponent<SpriteRenderer>();
            _logoBaseRenderer.sprite = _logoSprite;
            _logoBaseRenderer.sortingOrder = 11;

            _logoEffect = logoGlow.AddComponent<LogoMoltenEffect>();
        }

        private void CreateVignette()
        {
            var vignette = new GameObject("Crimson_Vignette");
            vignette.transform.SetParent(_overlayLayer);
            vignette.transform.localPosition = Vector3.zero;
            vignette.transform.localScale = Vector3.one * _vignetteScale;

            _vignetteRenderer = vignette.AddComponent<SpriteRenderer>();
            _vignetteRenderer.sprite = _circleGlowSprite;
            _vignetteRenderer.color = new Color(0.8f, 0.1f, 0.1f, 0f); // Start invisible
            _vignetteRenderer.sortingOrder = 100;

            _ruptureEvent = vignette.AddComponent<CrimsonRuptureEvent>();
        }

        // =============================================================================
        // EFFECT SETUP
        // =============================================================================

        private void SetupEffects()
        {
            // Ensure pulse controller exists
            if (_pulseController == null)
            {
                _pulseController = GetComponent<MenuPulseController>();
                if (_pulseController == null)
                {
                    _pulseController = gameObject.AddComponent<MenuPulseController>();
                }
            }

            // Setup individual effects with their references
            if (_logoEffect != null && _logoGlowRenderer != null && _logoBaseRenderer != null)
            {
                _logoEffect.Setup(_logoBaseRenderer, _logoGlowRenderer);
            }

            if (_riftDistortion != null && _riftRenderer != null)
            {
                _riftDistortion.Setup(_riftRenderer.transform, _riftRenderer);
            }

            if (_chestBreathing != null && _chestGlowRenderer != null)
            {
                _chestBreathing.Setup(_chestGlowRenderer.transform, _chestGlowRenderer);
            }

            if (_ashEmbers != null && _particleSystem != null)
            {
                _ashEmbers.Setup(_particleSystem, _chestGlowOffset);
            }

            if (_ruptureEvent != null && _vignetteRenderer != null)
            {
                _ruptureEvent.Setup(_vignetteRenderer);
            }

            Debug.Log("[VB:VFX] Main Menu VFX setup complete");
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Force a pulse for testing or dramatic effect.
        /// </summary>
        public void ForcePulse()
        {
            if (_pulseController != null)
            {
                _pulseController.ForcePulse();
            }
        }

        /// <summary>
        /// Force a Crimson Rupture for testing.
        /// </summary>
        public void ForceRupture()
        {
            if (_pulseController != null)
            {
                _pulseController.TriggerCrimsonRupture();
            }
        }

        /// <summary>
        /// Set visibility of VFX (for menu transitions).
        /// </summary>
        public void SetVFXVisible(bool visible)
        {
            if (_vfxRoot != null)
            {
                _vfxRoot.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Update demon figure sprite at runtime.
        /// </summary>
        public void SetDemonSprite(Sprite sprite)
        {
            _demonFigureSprite = sprite;
            if (_demonRenderer != null)
            {
                _demonRenderer.sprite = sprite;
            }
        }

        /// <summary>
        /// Update logo sprite at runtime.
        /// </summary>
        public void SetLogoSprite(Sprite sprite, Sprite glowSprite = null)
        {
            _logoSprite = sprite;
            _logoGlowSprite = glowSprite ?? sprite;

            if (_logoBaseRenderer != null)
            {
                _logoBaseRenderer.sprite = sprite;
            }
            if (_logoGlowRenderer != null)
            {
                _logoGlowRenderer.sprite = _logoGlowSprite;
            }
        }
    }
}
