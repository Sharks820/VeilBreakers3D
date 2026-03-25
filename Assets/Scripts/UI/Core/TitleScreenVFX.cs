using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;
using VeilBreakers.Managers;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// AAA Title Screen VFX Controller.
    /// Creates floating embers, ash particles, smoke wisps, and atmospheric effects
    /// for a dark fantasy horror aesthetic.
    ///
    /// v2.0 - Enhanced with Gemini-recommended AAA color palette and particle specs
    /// </summary>
    public class TitleScreenVFX : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Ember Settings (AAA Palette)")]
        [SerializeField] private int _emberCount = 140;
        [SerializeField] private float _emberSpeedMin = 12f;
        [SerializeField] private float _emberSpeedMax = 32f;
        [SerializeField] private float _emberSizeMin = 2f;
        [SerializeField] private float _emberSizeMax = 6f;
        [SerializeField] private Color _emberColorCore = new Color(1f, 0.75f, 0.35f, 1f);
        [SerializeField] private Color _emberColorBody = new Color(1f, 0.38f, 0.08f, 0.95f);
        [SerializeField] private Color _emberColorGlow = new Color(1f, 0.22f, 0f, 0.35f);

        [Header("Micro-Spark Settings (AAA)")]
        [SerializeField] private int _microSparkCount = 40;
        [SerializeField] private float _microSparkSpeedMin = 45f;
        [SerializeField] private float _microSparkSpeedMax = 110f;
        [SerializeField] private float _microSparkSizeMin = 1f;
        [SerializeField] private float _microSparkSizeMax = 2.5f;

        [Header("Ash Settings (AAA Palette)")]
        [SerializeField] private int _ashCount = 16;
        [SerializeField] private float _ashSpeedMin = 6f;
        [SerializeField] private float _ashSpeedMax = 16f;
        [SerializeField] private float _ashSizeMin = 3f;
        [SerializeField] private float _ashSizeMax = 10f;
        [SerializeField] private Color _ashColorLight = new Color(0.3f, 0.28f, 0.25f, 0.55f);
        [SerializeField] private Color _ashColorDark = new Color(0.18f, 0.16f, 0.14f, 0.35f);

        [Header("Smoke Wisp Settings (AAA)")]
        [SerializeField] private int _smokeCount = 0; // Disabled - looked like shadow spheres
        [SerializeField] private float _smokeSpeedMin = 2.5f;
        [SerializeField] private float _smokeSpeedMax = 6f;
        [SerializeField] private float _smokeSizeMin = 120f;
        [SerializeField] private float _smokeSizeMax = 280f;
        [SerializeField] private Color _smokeColor = new Color(0.18f, 0.14f, 0.12f, 0.22f);

        [Header("Spark Burst Settings")]
        [SerializeField] private int _sparkCount = 0;
        [SerializeField] private float _sparkSpeedMin = 100f;
        [SerializeField] private float _sparkSpeedMax = 200f;

        [Header("Menu Music")]
        [SerializeField] private AudioClip _menuMusic;
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.7f;
        [SerializeField] private float _musicFadeInDuration = 2f;
        private AudioSource _audioSource;
        private Coroutine _musicFadeCoroutine;
        private SettingsManager _settingsManager;
        private Coroutine _settingsBindCoroutine;
        private bool _isSettingsBound;

        [Header("Lightning (AAA)")]
        [SerializeField] private bool _enableLightning = true;
        [SerializeField] private float _lightningIntervalMin = 2.0f;
        [SerializeField] private float _lightningIntervalMax = 4.0f;
        [SerializeField] private float _lightningStrikeDurationMin = 1.2f;
        [SerializeField] private float _lightningStrikeDurationMax = 2.0f;
        [SerializeField, Range(0f, 1f)] private float _lightningIntensity = 0.92f;
        [SerializeField] private Color _lightningTint = new Color(1f, 0.55f, 0.25f, 1f);

        [Header("Interactions (AAA)")]
        [SerializeField] private bool _enableClickMonsterBurst = true;
        [SerializeField] private int _monsterBurstParticleCount = 32;
        [SerializeField] private float _monsterBurstForceMin = 260f;
        [SerializeField] private float _monsterBurstForceMax = 520f;
        [SerializeField] private float _monsterBurstLifetime = 0.75f;

        [SerializeField] private bool _enableEmberMouseAttraction = true;
        [SerializeField] private float _emberAttractRadius = 520f;
        [SerializeField] private float _emberAttractStrength = 260f;
        [SerializeField] private float _emberAttractVerticalInfluence = 0.12f;

        [Header("Logo (Reactive)")]
        [SerializeField] private bool _enableLogoPulse = true;
        [SerializeField] private bool _enableLogoSmoke = true;
        [SerializeField, Range(0f, 1f)] private float _logoGlowBaseOpacity = 0.28f;
        [SerializeField, Range(0f, 1f)] private float _logoGlowHoverOpacity = 0.46f;
        [SerializeField, Range(0f, 1f)] private float _logoGlowClickOpacity = 0.78f;
        [SerializeField] private float _logoPulseDuration = 0.22f;
        [SerializeField] private Color _logoSmokeTint = new Color(0.22f, 0.18f, 0.16f, 0.30f);

        [Header("Atmospheric Layers")]
        [SerializeField] private bool _enableVignette = true;
        [SerializeField, Range(0f, 1f)] private float _vignetteOpacity = 0.22f;
        [SerializeField] private bool _enableAtmosphereGradient = true;
        [SerializeField] private Color _atmosphereColor = new Color(0.12f, 0.03f, 0f, 0.12f);
        [SerializeField] private bool _enableGrungeOverlay = true;
        [SerializeField, Range(0f, 0.35f)] private float _grungeOpacity = 0.06f;

        [Header("Animation")]
        [SerializeField] private float _windStrength = 0.4f;
        [SerializeField] private float _windFrequency = 0.4f;
        [SerializeField] private float _flickerSpeed = 2.2f;
        [SerializeField] private float _turbulenceStrength = 0.15f;  // New: adds chaos to movement

        [Header("Spawn Area")]
        [SerializeField] private float _spawnMarginBottom = 0.15f;
        [SerializeField] private float _spawnMarginSides = 0.05f;

        [Header("Particle Textures (Assign in Inspector)")]
        [Tooltip("Smoke texture for smoke wisps - use smoke.png or smoke 2.png")]
        [SerializeField] private Texture2D _smokeTexture;
        [Tooltip("Dust/ash texture for ash particles - use dust.png")]
        [SerializeField] private Texture2D _ashTexture;
        [Tooltip("Ember/spark texture - use dirt 2.png for gritty embers")]
        [SerializeField] private Texture2D _emberTexture;
        [Tooltip("Grunge overlay texture - use grunge crack.png")]
        [SerializeField] private Texture2D _grungeTexture;
        [Tooltip("Lightning bolt textures (optional). If missing, auto-loads from Resources/UI/Lightning/.")]
        [SerializeField] private Texture2D _lightningBoltTextureA;
        [SerializeField] private Texture2D _lightningBoltTextureB;
        [Tooltip("Logo glow texture (optional). Auto-loads Art/UI/MainMenu/logo_veilbreakers_glow when empty.")]
        [SerializeField] private Texture2D _logoGlowTexture;

        [Header("Background Override")]
        [SerializeField] private bool _overrideBackgroundWithPortal = true;
        [SerializeField, Range(0f, 1f)] private float _backgroundDarken = 0.00f;
        [Tooltip("Optional. Auto-loads from Resources/Art/UI/MainMenu/mainmenu_background_portal when empty.")]
        [SerializeField] private Texture2D _backgroundPortalTexture;

        [Header("Background Video (Looping)")]
        [SerializeField] private bool _useVideoBackground = true;
        [SerializeField] private VideoClip _backgroundVideoClip;
        [Tooltip("Path to video in StreamingAssets or Resources if clip not assigned")]
        [SerializeField] private string _videoPath = "Art/UI/MainMenu/background_video";

        // PING-PONG VideoPlayer for seamless looping (forward video + reversed video)
        private VideoPlayer _videoPlayerForward;
        private VideoPlayer _videoPlayerReversed;
        private RenderTexture _videoRenderTextureForward;
        private RenderTexture _videoRenderTextureReversed;
        private bool _playingForward = true; // Which direction is currently showing
        private bool _usePingPongLoop;
        private double _videoLength;
        private bool _isVideoPlaying;
        private string _videoFilePath;
        private string _videoFilePathReversed;

        // =============================================================================
        // STATE
        // =============================================================================

        private VisualElement _vfxContainer;
        private VisualElement _frontVfxContainer;
        private VisualElement _atmosphereLayer;
        private VisualElement _vignetteLayer;
        private VisualElement _lightningLayer;
        private VisualElement _lightningFlashOverlay;
        private VisualElement _smokeLayer;
        private readonly List<EmberParticle> _embers = new();
        private readonly List<AshParticle> _ashes = new();
        private readonly List<SparkParticle> _sparks = new();
        private readonly List<MicroSparkParticle> _microSparks = new();
        private readonly List<SmokeParticle> _smokes = new();
        private readonly List<LightningStrike> _lightningStrikes = new();
        private bool _isActive;
        private Coroutine _updateCoroutine;
        private float _windOffset;
        private float _turbulenceOffset;
        private float _screenWidth;
        private float _screenHeight;
        private float _nextLightningAt;
        private float _lightningFlashOpacity;
        private Texture2D _lightningMaskedA;
        private Texture2D _lightningMaskedB;
        private VisualElement _host;
        private VisualElement _monsterElement;
        private VisualElement _logoContainer;
        private VisualElement _logoImage;
        private VisualElement _logoGlowElement;
        private VisualElement _logoFxLayer;
        private bool _logoHover;
        private float _logoPulseRemaining;
        private float _logoPulseStrength;
        private float _logoGlowCurrentOpacity;

        // Logo aura layers (programmatic glow behind logo)
        private VisualElement _logoAuraInner;
        private VisualElement _logoAuraOuter;
        private VisualElement _logoAuraPulse;
        private float _auraTime;
        private float _logoButtonHoverBoost;

        // Atmospheric fog layers
        private VisualElement _fogLayer1;
        private VisualElement _fogLayer2;
        private VisualElement _fogLayer3;
        private float _fogTime;
        private Vector2 _mousePosition;
        private bool _hasMouse;
        private readonly List<BurstParticle> _burstParticles = new();
        private readonly List<TransientSmokeParticle> _transientSmokes = new();
        private readonly List<GlowPulse> _glowPulses = new();
        private VisualElement _backgroundElement;
        private VisualElement _videoOverlayElement; // Sits on top of static bg — keeps bg visible at loop points
        private bool _isDestroyed;
        private bool _initializeQueued;
        private bool _videoSetupQueued;
        private Coroutine _deferredInitializeCoroutine;
        private Coroutine _deferredVideoSetupCoroutine;

        private sealed class LightningStrike
        {
            public VisualElement Bolt;
            public VisualElement Glow;
            public float Age;
            public float Duration;
            public float FlickerSeed;
            public bool Active;
            public float BaseOpacity;
            public Texture2D Texture;
        }

        private sealed class BurstParticle
        {
            public VisualElement Element;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Size;
            public float Width;
            public float Height;
            public float Lifetime;
            public float Age;
            public float FlickerSeed;
        }

        private sealed class TransientSmokeParticle
        {
            public VisualElement Element;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Size;
            public float Lifetime;
            public float Age;
            public float ExpansionRate;
            public float RotationSpeed;
        }

        private sealed class GlowPulse
        {
            public VisualElement Element;
            public Vector2 Position;
            public float StartSize;
            public float EndSize;
            public float Lifetime;
            public float Age;
            public float BaseOpacity;
        }


        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            // AUTO-LOAD textures from Resources if not assigned in Inspector
            LoadTexturesFromResources();
        }

        private void Start()
        {
            // DISABLE ALL SHADOW/VIGNETTE OVERLAYS
            _enableAtmosphereGradient = false;
            _enableVignette = false;

            // ENABLE VIDEO BACKGROUND
            _useVideoBackground = true;

            // PARTICLES SPAWN FROM FULL SCREEN (no margins)
            _spawnMarginSides = 0f;
            _spawnMarginBottom = 0f;

            // Keep visual density high but avoid first-frame spikes.
            _emberCount = Mathf.Clamp(_emberCount, 120, 220);
            _microSparkCount = Mathf.Clamp(_microSparkCount, 40, 80);
            _ashCount = Mathf.Clamp(_ashCount, 18, 36);
            _smokeCount = Mathf.Clamp(_smokeCount, 4, 8);
            _sparkCount = Mathf.Clamp(_sparkCount, 8, 16);

            // Match particle colors to fiery red/orange portal video
            _emberColorCore = new Color(1f, 0.6f, 0.2f, 1f);    // Hot orange-yellow core
            _emberColorBody = new Color(0.9f, 0.25f, 0.05f, 0.85f); // Deep red-orange
            _emberColorGlow = new Color(0.8f, 0.1f, 0f, 0.5f);  // Dark red glow

            // CURSOR ATTRACTION - embers follow the mouse
            _enableEmberMouseAttraction = true;
            _emberAttractRadius = 400f;      // Large attraction radius
            _emberAttractStrength = 350f;    // Strong pull toward cursor
            _emberAttractVerticalInfluence = 0.8f; // Allow vertical pull too

            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            if (_uiDocument != null)
            {
                _uiDocument.rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }

            StartSettingsBinding();
        }

        private void LoadTexturesFromResources()
        {
            // Auto-load from Resources/VFX/ParticleTextures/ if not assigned
            if (_smokeTexture == null)
            {
                _smokeTexture = Resources.Load<Texture2D>("VFX/ParticleTextures/smoke_kenney");
                if (_smokeTexture == null)
                    _smokeTexture = Resources.Load<Texture2D>("VFX/ParticleTextures/smoke");
                if (_smokeTexture == null)
                    _smokeTexture = Resources.Load<Texture2D>("VFX/ParticleTextures/smoke 2");
            }

            if (_ashTexture == null)
            {
                _ashTexture = Resources.Load<Texture2D>("VFX/ParticleTextures/dust");
                if (_ashTexture == null)
                    _ashTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/ash_particles");
            }

            if (_emberTexture == null)
            {
                _emberTexture = Resources.Load<Texture2D>("VFX/ParticleTextures/dirt 2");
                if (_emberTexture == null)
                    _emberTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/ember_particles");
            }

            if (_grungeTexture == null)
            {
                _grungeTexture = Resources.Load<Texture2D>("VFX/ParticleTextures/grunge crack");
                if (_grungeTexture == null)
                    _grungeTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/vignette_overlay");
            }

            if (_lightningBoltTextureA == null)
            {
                _lightningBoltTextureA = Resources.Load<Texture2D>("UI/Lightning/lightning_bolt_01");
            }

            if (_lightningBoltTextureB == null)
            {
                _lightningBoltTextureB = Resources.Load<Texture2D>("UI/Lightning/lightning_bolt_04");
            }

            if (_logoGlowTexture == null)
            {
                _logoGlowTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/logo_veilbreakers_glow");
            }

            if (_backgroundPortalTexture == null && _overrideBackgroundWithPortal)
            {
                _backgroundPortalTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/mainmenu_background_portal");
            }

            Debug.Log($"[TitleScreenVFX] Textures loaded - Smoke: {_smokeTexture != null}, Ash: {_ashTexture != null}, Ember: {_emberTexture != null}, Grunge: {_grungeTexture != null}");

            // Load menu music from Resources if not assigned
            if (_menuMusic == null)
            {
                _menuMusic = Resources.Load<AudioClip>("Audio/Music/menu_music");
            }

            // Setup and play menu music
            SetupMenuMusic();
        }

        private void SetupMenuMusic()
        {
            if (_menuMusic == null) return;

            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.clip = _menuMusic;
            _audioSource.loop = true;
            _audioSource.playOnAwake = false;
            _audioSource.volume = 0f; // Start silent for fade-in
            _audioSource.Play();

            // Start fade-in coroutine
            if (_musicFadeCoroutine != null)
            {
                StopCoroutine(_musicFadeCoroutine);
            }
            _musicFadeCoroutine = StartCoroutine(FadeMusicIn());

            // Apply latest settings immediately after startup fade setup.
            ApplyMenuMusicFromSettings(_isSettingsBound ? _settingsManager?.Settings : null);
        }

        private System.Collections.IEnumerator FadeMusicIn()
        {
            float elapsed = 0f;
            while (elapsed < _musicFadeInDuration)
            {
                if (_audioSource == null) yield break;

                elapsed += Time.deltaTime;
                GameSettings settings = _isSettingsBound ? _settingsManager?.Settings : null;
                bool muteAll = settings != null && settings.MuteAll;
                float targetVolume = ResolveTargetMusicVolume(settings);
                _audioSource.mute = muteAll;
                _audioSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / _musicFadeInDuration);
                yield return null;
            }

            if (_audioSource != null)
            {
                GameSettings settings = _isSettingsBound ? _settingsManager?.Settings : null;
                _audioSource.mute = settings != null && settings.MuteAll;
                _audioSource.volume = ResolveTargetMusicVolume(settings);
            }

            _musicFadeCoroutine = null;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (_isActive || _vfxContainer != null || _initializeQueued) return;

            _initializeQueued = true;
            _deferredInitializeCoroutine ??= StartCoroutine(InitializeDeferred());
        }

        private void OnEnable()
        {
            StartSettingsBinding();
            if (_vfxContainer != null)
            {
                StartVFX();
            }
        }

        private void OnDisable()
        {
            StopVFX();

            if (_uiDocument != null && _uiDocument.rootVisualElement != null)
                _uiDocument.rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private IEnumerator InitializeDeferred()
        {
            // Let UI Toolkit finish initial layout/style pass before heavy VFX creation.
            yield return null;
            yield return null;

            if (_isDestroyed || _isActive || _vfxContainer != null)
            {
                _initializeQueued = false;
                _deferredInitializeCoroutine = null;
                yield break;
            }

            // Run Initialize as a coroutine to stagger element creation across frames
            yield return StartCoroutine(InitializeStaggered());
            _initializeQueued = false;
            _deferredInitializeCoroutine = null;
        }

        /// <summary>
        /// Staggered version of Initialize() that spreads VisualElement creation
        /// across multiple frames to avoid a massive single-frame layout spike.
        /// </summary>
        private IEnumerator InitializeStaggered()
        {
            if (_uiDocument == null) yield break;

            var root = _uiDocument.rootVisualElement;
            var host = root.Q<VisualElement>("menu-root") ?? root;

            // --- Frame 1: Container setup & lightweight layers ---
            _vfxContainer = new VisualElement();
            _vfxContainer.name = "title-vfx-container";
            _vfxContainer.AddToClassList("vfx-container");
            _vfxContainer.style.position = Position.Absolute;
            _vfxContainer.style.left = 0;
            _vfxContainer.style.top = 0;
            _vfxContainer.style.right = 0;
            _vfxContainer.style.bottom = 0;
            _vfxContainer.style.overflow = Overflow.Hidden;
            _vfxContainer.pickingMode = PickingMode.Ignore;

            InsertBehindMonster(host, _vfxContainer);

            _frontVfxContainer = new VisualElement();
            _frontVfxContainer.name = "title-front-vfx-container";
            _frontVfxContainer.style.position = Position.Absolute;
            _frontVfxContainer.style.left = 0;
            _frontVfxContainer.style.top = 0;
            _frontVfxContainer.style.right = 0;
            _frontVfxContainer.style.bottom = 0;
            _frontVfxContainer.style.overflow = Overflow.Hidden;
            _frontVfxContainer.pickingMode = PickingMode.Ignore;
            InsertInFrontOfMonster(host, _frontVfxContainer);

            _host = host;

            var oldVignette = host.Q<VisualElement>("top-vignette");
            if (oldVignette != null) oldVignette.RemoveFromHierarchy();

            _screenWidth = host.resolvedStyle.width;
            _screenHeight = host.resolvedStyle.height;

            if (_screenWidth <= 0) _screenWidth = 1920;
            if (_screenHeight <= 0) _screenHeight = 1080;

            SetupInteractiveTargets(host);
            ApplyBackground();
            CreateTopVignette(host);

            if (_enableAtmosphereGradient)
            {
                CreateAtmosphereLayer();
            }

            if (_enableLightning)
            {
                CreateLightningLayer();
            }

            _smokeLayer = new VisualElement();
            _smokeLayer.name = "smoke-layer";
            _smokeLayer.style.position = Position.Absolute;
            _smokeLayer.style.left = 0;
            _smokeLayer.style.top = 0;
            _smokeLayer.style.right = 0;
            _smokeLayer.style.bottom = 0;
            _smokeLayer.pickingMode = PickingMode.Ignore;
            _vfxContainer.Add(_smokeLayer);

            yield return null; // End frame 1

            if (_isDestroyed) yield break;

            // --- Frame 2: Smoke + Ash ---
            for (int i = 0; i < _smokeCount; i++)
            {
                CreateSmoke();
            }
            for (int i = 0; i < _ashCount; i++)
            {
                CreateAsh();
            }

            yield return null; // End frame 2

            if (_isDestroyed) yield break;

            // --- Frames 3+: Embers in batches of 30 ---
            const int kEmberBatch = 30;
            for (int i = 0; i < _emberCount; i++)
            {
                CreateEmber();
                if ((i + 1) % kEmberBatch == 0 && i + 1 < _emberCount)
                {
                    yield return null;
                    if (_isDestroyed) yield break;
                }
            }

            yield return null;

            if (_isDestroyed) yield break;

            // --- Next frame: Micro-sparks + Sparks ---
            for (int i = 0; i < _microSparkCount; i++)
            {
                CreateMicroSpark();
            }
            for (int i = 0; i < _sparkCount; i++)
            {
                CreateSpark();
            }

            yield return null;

            if (_isDestroyed) yield break;

            // --- Final frame: Overlays + start ---
            if (_enableVignette)
            {
                CreateVignetteLayer();
            }
            if (_enableGrungeOverlay && _grungeTexture != null)
            {
                CreateGrungeOverlay();
            }

            // Atmospheric fog layers
            CreateFogLayers();

            StartVFX();
            Debug.Log($"[TitleScreenVFX] Staggered VFX init complete: {_emberCount} embers, {_microSparkCount} micro-sparks, {_ashCount} ash, {_smokeCount} smoke, {_sparkCount} sparks");
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private static void InsertBehindMonster(VisualElement host, VisualElement vfxContainer)
        {
            if (host == null || vfxContainer == null) return;

            var background = host.Q<VisualElement>("background");
            if (background != null && background.parent == host)
            {
                int index = host.IndexOf(background);
                if (index >= 0 && index + 1 <= host.childCount)
                {
                    host.Insert(index + 1, vfxContainer);
                    return;
                }
            }

            var monster = host.Q<VisualElement>("monster-image");
            if (monster != null && monster.parent == host)
            {
                int index = host.IndexOf(monster);
                if (index >= 0)
                {
                    host.Insert(index, vfxContainer);
                    return;
                }
            }

            host.Insert(0, vfxContainer);
        }

        private static void InsertInFrontOfMonster(VisualElement host, VisualElement vfxContainer)
        {
            if (host == null || vfxContainer == null) return;

            var monster = host.Q<VisualElement>("monster-image");
            if (monster != null && monster.parent == host)
            {
                int index = host.IndexOf(monster);
                if (index >= 0 && index + 1 <= host.childCount)
                {
                    host.Insert(index + 1, vfxContainer);
                    return;
                }
            }

            var logo = host.Q<VisualElement>("logo-container");
            if (logo != null && logo.parent == host)
            {
                int index = host.IndexOf(logo);
                if (index >= 0)
                {
                    host.Insert(index, vfxContainer);
                    return;
                }
            }

            host.Add(vfxContainer);
        }

        private void SetupInteractiveTargets(VisualElement host)
        {
            if (host == null) return;

            UnregisterInteractiveTargets();
            _host = host;
            host.RegisterCallback<PointerDownEvent>(OnPointerDown);
            host.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            host.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);

            _monsterElement = host.Q<VisualElement>("monster-image");
            if (_monsterElement != null)
            {
                _monsterElement.pickingMode = PickingMode.Position;
            }

            _logoContainer = host.Q<VisualElement>("logo-container");
            _logoImage = host.Q<VisualElement>("logo-image");

            if (_logoContainer != null)
            {
                _logoContainer.pickingMode = PickingMode.Position;

                _logoContainer.UnregisterCallback<MouseEnterEvent>(OnLogoEnter);
                _logoContainer.UnregisterCallback<MouseLeaveEvent>(OnLogoLeave);
                _logoContainer.UnregisterCallback<PointerDownEvent>(OnLogoPointerDown);

                _logoContainer.RegisterCallback<MouseEnterEvent>(OnLogoEnter);
                _logoContainer.RegisterCallback<MouseLeaveEvent>(OnLogoLeave);
                _logoContainer.RegisterCallback<PointerDownEvent>(OnLogoPointerDown);

                RemoveLogoArtifacts();
                AddLogoShadow();
                EnsureLogoGlow();
                EnsureLogoFxLayer();
            }
        }

        private void UnregisterInteractiveTargets()
        {
            if (_host != null)
            {
                _host.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                _host.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                _host.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
            }

            if (_logoContainer != null)
            {
                _logoContainer.UnregisterCallback<MouseEnterEvent>(OnLogoEnter);
                _logoContainer.UnregisterCallback<MouseLeaveEvent>(OnLogoLeave);
                _logoContainer.UnregisterCallback<PointerDownEvent>(OnLogoPointerDown);
            }
        }

        private void RemoveLogoArtifacts()
        {
            if (_logoContainer == null) return;

            // Defensive cleanup for cases where domain reload is disabled and old UI elements persist.
            var glow = _logoContainer.Q<VisualElement>("logo-glow");
            if (glow != null) glow.RemoveFromHierarchy();

            var backplate = _logoContainer.Q<VisualElement>("logo-backplate");
            if (backplate != null) backplate.RemoveFromHierarchy();

            var shadow = _logoContainer.Q<VisualElement>("logo-shadow");
            if (shadow != null) shadow.RemoveFromHierarchy();

            // Also remove old top vignette if it exists
            if (_host != null)
            {
                var vignette = _host.Q<VisualElement>("top-vignette");
                if (vignette != null) vignette.RemoveFromHierarchy();
            }

            _logoGlowElement = null;
        }

        private void EnsureLogoGlow()
        {
            // Texture-based glow was disabled (looked like muddy shadow/box).
            // Use programmatic aura layers instead for clean, pulsing glow.
            CreateLogoAura();
        }

        private void EnsureLogoFxLayer()
        {
            if (_logoContainer == null) return;
            if (_logoFxLayer != null) return;

            var fx = new VisualElement();
            fx.name = "logo-fx-layer";
            fx.style.position = Position.Absolute;
            fx.style.left = 0;
            fx.style.top = 0;
            fx.style.right = 0;
            fx.style.bottom = 0;
            fx.pickingMode = PickingMode.Ignore;
            _logoContainer.Add(fx);
            _logoFxLayer = fx;
        }

        /// <summary>
        /// Creates layered programmatic glow elements behind the logo.
        /// Each layer pulses at a different rate for organic, living feel.
        /// </summary>
        private void CreateLogoAura()
        {
            // Disabled - aura elements caused visual glitching during logo click pulse
            return;
        }

        private VisualElement CreateAuraElement(string elName, Color color, int extraWidth, int extraHeight, VisualElement parent)
        {
            var aura = new VisualElement();
            aura.name = elName;
            aura.pickingMode = PickingMode.Ignore;
            aura.style.position = Position.Absolute;
            // Extend beyond logo bounds for glow effect
            aura.style.left = new Length(-extraWidth / 2, LengthUnit.Pixel);
            aura.style.top = new Length(-extraHeight / 2, LengthUnit.Pixel);
            aura.style.right = new Length(-extraWidth / 2, LengthUnit.Pixel);
            aura.style.bottom = new Length(-extraHeight / 2, LengthUnit.Pixel);
            aura.style.backgroundColor = color;
            aura.style.borderTopLeftRadius = extraWidth;
            aura.style.borderTopRightRadius = extraWidth;
            aura.style.borderBottomLeftRadius = extraWidth;
            aura.style.borderBottomRightRadius = extraWidth;

            // Insert behind everything else in target parent
            if (parent.childCount > 0)
                parent.Insert(0, aura);
            else
                parent.Add(aura);

            return aura;
        }

        /// <summary>
        /// Creates semi-transparent fog overlay elements that drift slowly across the screen.
        /// Adds atmospheric depth between background and foreground.
        /// </summary>
        private void CreateFogLayers()
        {
            if (_vfxContainer == null) return;

            _fogLayer1 = CreateFogElement("fog-layer-1",
                new Color(0.15f, 0.08f, 0.05f, 0.08f), 0.4f, 0f);
            _fogLayer2 = CreateFogElement("fog-layer-2",
                new Color(0.2f, 0.1f, 0.06f, 0.05f), 0.6f, -0.3f);
            _fogLayer3 = CreateFogElement("fog-layer-3",
                new Color(0.1f, 0.06f, 0.04f, 0.06f), 0.3f, 0.5f);
        }

        private VisualElement CreateFogElement(string elName, Color color, float heightPercent, float startOffsetPercent)
        {
            var fog = new VisualElement();
            fog.name = elName;
            fog.pickingMode = PickingMode.Ignore;
            fog.style.position = Position.Absolute;
            fog.style.left = new Length(startOffsetPercent * 100f, LengthUnit.Percent);
            fog.style.top = new Length((1f - heightPercent) * 50f, LengthUnit.Percent);
            fog.style.width = new Length(120, LengthUnit.Percent);
            fog.style.height = new Length(heightPercent * 100f, LengthUnit.Percent);
            fog.style.backgroundColor = color;
            fog.style.borderTopLeftRadius = 200;
            fog.style.borderTopRightRadius = 200;
            fog.style.borderBottomLeftRadius = 200;
            fog.style.borderBottomRightRadius = 200;
            _vfxContainer.Add(fog);

            return fog;
        }

        private void ApplyBackground()
        {
            if (_host == null) return;

            _backgroundElement = _host.Q<VisualElement>("background");
            if (_backgroundElement == null) return;

            // Try video background first
            if (_useVideoBackground)
            {
                if (!_videoSetupQueued)
                {
                    _videoSetupQueued = true;
                    _deferredVideoSetupCoroutine = StartCoroutine(SetupVideoBackgroundDeferred());
                }
                return;
            }

            // Fall back to static image
            if (_overrideBackgroundWithPortal && _backgroundPortalTexture == null)
            {
                _backgroundPortalTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/mainmenu_background_portal");
            }

            if (_overrideBackgroundWithPortal && _backgroundPortalTexture != null)
            {
                _backgroundElement.style.backgroundImage = new StyleBackground(_backgroundPortalTexture);
                _backgroundElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                _backgroundElement.style.unityBackgroundImageTintColor = Color.white;
            }

            if (_overrideBackgroundWithPortal)
            {
                _backgroundElement.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            }
            else
            {
                _backgroundElement.style.backgroundColor = new Color(0f, 0f, 0f, Mathf.Clamp01(_backgroundDarken));
            }
        }

        private IEnumerator SetupVideoBackgroundDeferred()
        {
            // Delay heavy RenderTexture + VideoPlayer setup until after menu first paint.
            yield return null;
            yield return new WaitForSecondsRealtime(0.08f);

            _videoSetupQueued = false;
            _deferredVideoSetupCoroutine = null;

            if (_isDestroyed || _backgroundElement == null || !_useVideoBackground)
            {
                yield break;
            }

            SetupVideoBackground();
        }

        private void CreateTopVignette(VisualElement host)
        {
            // Disabled - user rejected screen-wide vignette
            // Logo shadow added directly to logo instead
        }

        private void AddLogoShadow()
        {
            if (_logoImage == null || _logoContainer == null) return;

            // Remove any existing shadow
            var existingShadow = _logoContainer.Q<VisualElement>("logo-shadow");
            if (existingShadow != null) existingShadow.RemoveFromHierarchy();

            // Load logo texture directly (UXML style.backgroundImage not accessible in code)
            var logoTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/logo_veilbreakers");
            if (logoTexture == null)
            {
                Debug.LogWarning("[TitleScreenVFX] Could not load logo texture for shadow");
                return;
            }

            // Create shadow - a dark copy of the logo, offset down/right
            // Use same centering as logo-image (left: 50%, translateX: -50%) plus shadow offset
            var shadow = new VisualElement();
            shadow.name = "logo-shadow";
            shadow.style.position = Position.Absolute;
            shadow.style.width = 1600;
            shadow.style.height = 400;
            shadow.style.left = Length.Percent(50);
            // Translate: center (-50% of 1600 = -800) plus small shadow offset
            shadow.style.translate = new Translate(new Length(-796, LengthUnit.Pixel), new Length(6, LengthUnit.Pixel));
            shadow.style.top = 0;
            shadow.style.backgroundImage = new StyleBackground(logoTexture);
            shadow.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            shadow.style.unityBackgroundImageTintColor = new Color(0f, 0f, 0f, 0.7f); // Dark shadow
            shadow.pickingMode = PickingMode.Ignore;

            // Insert shadow behind the logo image
            int logoIndex = _logoContainer.IndexOf(_logoImage);
            if (logoIndex >= 0)
            {
                _logoContainer.Insert(logoIndex, shadow);
            }
        }

        private void SetupVideoBackground()
        {
            // Find forward video file path
            _videoFilePath = System.IO.Path.Combine(Application.streamingAssetsPath, "background_video.mp4");
            if (!System.IO.File.Exists(_videoFilePath))
            {
                _videoFilePath = System.IO.Path.Combine(Application.dataPath, "Art/UI/MainMenu/background_video.mp4");
            }
            bool hasForwardFile = System.IO.File.Exists(_videoFilePath);

            // Find reversed video file path
            _videoFilePathReversed = System.IO.Path.Combine(Application.streamingAssetsPath, "background_video_reversed.mp4");
            if (!System.IO.File.Exists(_videoFilePathReversed))
            {
                _videoFilePathReversed = System.IO.Path.Combine(Application.dataPath, "Art/UI/MainMenu/background_video_reversed.mp4");
            }
            bool hasReversedFile = System.IO.File.Exists(_videoFilePathReversed);

            bool useVideoClipSource = !hasForwardFile && _backgroundVideoClip != null;
            _usePingPongLoop = !useVideoClipSource && hasForwardFile && hasReversedFile;

            if (!hasForwardFile && _backgroundVideoClip == null)
            {
                Debug.LogWarning($"[TitleScreenVFX] Video file not found at: {_videoFilePath}");
                _useVideoBackground = false;
                ApplyBackground();
                return;
            }

            if (!_usePingPongLoop)
            {
                Debug.LogWarning("[TitleScreenVFX] Reversed background video missing; falling back to single-player loop.");
            }

            // Create render textures
            int rtWidth = Mathf.Max(Screen.width, 1920);
            int rtHeight = Mathf.Max(Screen.height, 1080);
            _videoRenderTextureForward = CreateVideoRenderTexture(rtWidth, rtHeight, "BackgroundVideoRT_Forward");
            if (_usePingPongLoop)
            {
                _videoRenderTextureReversed = CreateVideoRenderTexture(rtWidth, rtHeight, "BackgroundVideoRT_Reversed");
            }

            // Create and configure players
            _videoPlayerForward = CreateVideoPlayer(
                _videoRenderTextureForward,
                useVideoClipSource ? null : _videoFilePath,
                useVideoClipSource ? _backgroundVideoClip : null);

            if (_usePingPongLoop)
            {
                _videoPlayerReversed = CreateVideoPlayer(_videoRenderTextureReversed, _videoFilePathReversed, null);
            }
            else
            {
                _videoPlayerReversed = null;
            }

            // Prepare forward player first (it will start playback)
            _videoPlayerForward.prepareCompleted += OnForwardVideoPrepared;
            _videoPlayerForward.Prepare();

            // Set background to black while video loads
            _backgroundElement.style.backgroundColor = Color.black;

            Debug.Log(_usePingPongLoop
                ? "[TitleScreenVFX] Ping-pong dual video setup initiated (forward + reversed)."
                : "[TitleScreenVFX] Single-player video setup initiated.");
        }

        private RenderTexture CreateVideoRenderTexture(int width, int height, string name)
        {
            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            rt.name = name;
            rt.filterMode = FilterMode.Bilinear;
            rt.antiAliasing = 1;
            rt.useMipMap = false;
            rt.Create();
            return rt;
        }

        private VideoPlayer CreateVideoPlayer(RenderTexture targetTexture, string videoUrl, VideoClip clip)
        {
            // Create a child GameObject to hold the VideoPlayer
            var playerObj = new GameObject(targetTexture.name + "_Player");
            playerObj.transform.SetParent(transform);
            var player = playerObj.AddComponent<VideoPlayer>();

            player.playOnAwake = false;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = targetTexture;
            player.isLooping = false;
            player.skipOnDrop = false;
            player.playbackSpeed = 1.0f; // Normal speed — 1.15x caused visible acceleration
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.aspectRatio = VideoAspectRatio.Stretch; // Stretch to fill RenderTexture exactly - no zoom

            if (clip != null)
            {
                player.source = VideoSource.VideoClip;
                player.clip = clip;
            }
            else
            {
                player.source = VideoSource.Url;
                player.url = videoUrl;
            }

            return player;
        }

        private void OnForwardVideoPrepared(VideoPlayer vp)
        {
            _videoLength = vp.length;
            _isVideoPlaying = true;
            _playingForward = true;

            Debug.Log($"[TitleScreenVFX] Forward video prepared - Duration: {_videoLength:F2}s");

            // COMPOSITING APPROACH: Keep the static bg.png on _backgroundElement (always crisp).
            // Create a video overlay element on top that shows the video.
            // This way loop-point glitches are masked by the static bg underneath.
            // Ensure static portal bg is loaded on the background element first.
            if (_overrideBackgroundWithPortal)
            {
                if (_backgroundPortalTexture == null)
                    _backgroundPortalTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/mainmenu_background_portal");
                if (_backgroundPortalTexture != null)
                {
                    _backgroundElement.style.backgroundImage = new StyleBackground(_backgroundPortalTexture);
                    _backgroundElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                    _backgroundElement.style.unityBackgroundImageTintColor = Color.white;
                    _backgroundElement.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
                }
            }

            if (_videoOverlayElement == null)
            {
                _videoOverlayElement = new VisualElement();
                _videoOverlayElement.name = "video-overlay";
                _videoOverlayElement.pickingMode = PickingMode.Ignore;
                _videoOverlayElement.style.position = Position.Absolute;
                _videoOverlayElement.style.left = 0;
                _videoOverlayElement.style.top = 0;
                _videoOverlayElement.style.right = 0;
                _videoOverlayElement.style.bottom = 0;

                // Insert video overlay right after the background element
                var parent = _backgroundElement.parent;
                if (parent != null)
                {
                    int bgIndex = parent.IndexOf(_backgroundElement);
                    parent.Insert(bgIndex + 1, _videoOverlayElement);
                }
            }

            _videoOverlayElement.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_videoRenderTextureForward));
            _videoOverlayElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);

            // Start playing forward video
            vp.Play();

            if (_usePingPongLoop && _videoPlayerReversed != null)
            {
                // Setup event-based ping-pong loop
                SetupPingPongEvents();

                // Prepare reversed video so it's ready for the swap
                _videoPlayerReversed.prepareCompleted += OnReversedVideoPrepared;
                _videoPlayerReversed.Prepare();
            }
            else
            {
                vp.isLooping = true;
            }
        }

        private void OnReversedVideoPrepared(VideoPlayer vp)
        {
            Debug.Log("[TitleScreenVFX] Reversed video prepared and standing by at frame 0");
            // Reversed video stays paused at time=0, ready to instantly play when forward ends
            vp.time = 0;
            vp.Pause();
        }

        private void SetupPingPongEvents()
        {
            if (!_usePingPongLoop || _videoPlayerForward == null || _videoPlayerReversed == null) return;

            // EVENT-BASED SEAMLESS LOOP
            // When one video finishes, instantly swap texture and start the other
            // The "next" video is always pre-positioned at time=0, no seeking needed

            _videoPlayerForward.loopPointReached += OnForwardVideoEnded;
            _videoPlayerReversed.loopPointReached += OnReversedVideoEnded;
        }

        private void OnForwardVideoEnded(VideoPlayer vp)
        {
            if (!_isVideoPlaying) return;
            if (!_usePingPongLoop || _videoPlayerReversed == null || _videoRenderTextureReversed == null)
            {
                vp.time = 0;
                vp.Play();
                return;
            }

            // INSTANT swap - reversed is already paused at time=0
            var targetEl = _videoOverlayElement ?? _backgroundElement;
            targetEl.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_videoRenderTextureReversed));
            _playingForward = false;

            // Play reversed (already at 0), pause forward and reset for next cycle
            _videoPlayerReversed.Play();
            _videoPlayerForward.Pause();
            _videoPlayerForward.time = 0; // Reset AFTER pause so it's ready for next swap
        }

        private void OnReversedVideoEnded(VideoPlayer vp)
        {
            if (!_isVideoPlaying) return;
            if (_videoPlayerForward == null || _videoRenderTextureForward == null)
            {
                return;
            }

            // INSTANT swap - forward is already paused at time=0
            var targetEl = _videoOverlayElement ?? _backgroundElement;
            targetEl.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_videoRenderTextureForward));
            _playingForward = true;

            // Play forward (already at 0), pause reversed and reset for next cycle
            _videoPlayerForward.Play();
            _videoPlayerReversed.Pause();
            _videoPlayerReversed.time = 0; // Reset AFTER pause so it's ready for next swap
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            UnbindSettingsManager();
            UnregisterInteractiveTargets();

            if (_musicFadeCoroutine != null)
            {
                StopCoroutine(_musicFadeCoroutine);
                _musicFadeCoroutine = null;
            }

            if (_uiDocument != null && _uiDocument.rootVisualElement != null)
            {
                _uiDocument.rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }

            if (_deferredInitializeCoroutine != null)
            {
                StopCoroutine(_deferredInitializeCoroutine);
                _deferredInitializeCoroutine = null;
            }

            if (_deferredVideoSetupCoroutine != null)
            {
                StopCoroutine(_deferredVideoSetupCoroutine);
                _deferredVideoSetupCoroutine = null;
            }
            // Stop video monitoring
            _isVideoPlaying = false;

            // Cleanup Forward VideoPlayer
            if (_videoPlayerForward != null)
            {
                _videoPlayerForward.prepareCompleted -= OnForwardVideoPrepared;
                _videoPlayerForward.loopPointReached -= OnForwardVideoEnded;
                _videoPlayerForward.Stop();
                if (_videoPlayerForward.gameObject != gameObject)
                {
                    Destroy(_videoPlayerForward.gameObject);
                }
            }

            // Cleanup Reversed VideoPlayer
            if (_videoPlayerReversed != null)
            {
                _videoPlayerReversed.prepareCompleted -= OnReversedVideoPrepared;
                _videoPlayerReversed.loopPointReached -= OnReversedVideoEnded;
                _videoPlayerReversed.Stop();
                if (_videoPlayerReversed.gameObject != gameObject)
                {
                    Destroy(_videoPlayerReversed.gameObject);
                }
            }

            // Cleanup RenderTextures
            if (_videoRenderTextureForward != null)
            {
                _videoRenderTextureForward.Release();
                Destroy(_videoRenderTextureForward);
            }
            if (_videoRenderTextureReversed != null)
            {
                _videoRenderTextureReversed.Release();
                Destroy(_videoRenderTextureReversed);
            }

            // Cleanup masked lightning textures
            if (_lightningMaskedA != null) { Destroy(_lightningMaskedA); _lightningMaskedA = null; }
            if (_lightningMaskedB != null) { Destroy(_lightningMaskedB); _lightningMaskedB = null; }

            // Stop menu music
            if (_audioSource != null)
            {
                _audioSource.Stop();
            }
        }

        private void StartSettingsBinding()
        {
            TryBindSettingsManager();
            if (_isSettingsBound || !isActiveAndEnabled) return;
            if (_settingsBindCoroutine != null) return;
            _settingsBindCoroutine = StartCoroutine(BindSettingsWhenReady());
        }

        private IEnumerator BindSettingsWhenReady()
        {
            while (!_isDestroyed && !_isSettingsBound)
            {
                TryBindSettingsManager();
                if (_isSettingsBound)
                {
                    _settingsBindCoroutine = null;
                    yield break;
                }

                yield return new WaitForSecondsRealtime(0.25f);
            }

            _settingsBindCoroutine = null;
        }

        private void TryBindSettingsManager()
        {
            if (_isSettingsBound || !SettingsManager.HasInstance) return;

            _settingsManager = SettingsManager.Instance;
            if (_settingsManager == null) return;

            _settingsManager.OnSettingsChanged += OnSettingsChanged;
            _isSettingsBound = true;
            ApplyMenuMusicFromSettings(_settingsManager.Settings);
        }

        private void UnbindSettingsManager()
        {
            if (_settingsBindCoroutine != null)
            {
                StopCoroutine(_settingsBindCoroutine);
                _settingsBindCoroutine = null;
            }

            if (_settingsManager != null)
            {
                _settingsManager.OnSettingsChanged -= OnSettingsChanged;
            }

            _settingsManager = null;
            _isSettingsBound = false;
        }

        private void OnSettingsChanged(GameSettings settings)
        {
            if (_musicFadeCoroutine != null)
            {
                StopCoroutine(_musicFadeCoroutine);
                _musicFadeCoroutine = null;
            }
            ApplyMenuMusicFromSettings(settings);
        }

        private void ApplyMenuMusicFromSettings(GameSettings settings)
        {
            if (_audioSource == null) return;

            bool muteAll = settings != null && settings.MuteAll;
            _audioSource.mute = muteAll;
            _audioSource.volume = ResolveTargetMusicVolume(settings);
        }

        private float ResolveTargetMusicVolume(GameSettings settings)
        {
            if (settings == null)
            {
                return _musicVolume;
            }

            if (settings.MuteAll)
            {
                return 0f;
            }

            return Mathf.Clamp01(settings.MasterVolume) * Mathf.Clamp01(settings.MusicVolume);
        }

        // Removed logo backplate: it created an obvious rectangle behind the logo.

        private void Initialize()
        {
            if (_uiDocument == null) return;

            var root = _uiDocument.rootVisualElement;
            var host = root.Q<VisualElement>("menu-root") ?? root;

            // Create VFX container
            _vfxContainer = new VisualElement();
            _vfxContainer.name = "title-vfx-container";
            _vfxContainer.AddToClassList("vfx-container");
            _vfxContainer.style.position = Position.Absolute;
            _vfxContainer.style.left = 0;
            _vfxContainer.style.top = 0;
            _vfxContainer.style.right = 0;
            _vfxContainer.style.bottom = 0;
            _vfxContainer.style.overflow = Overflow.Hidden;
            _vfxContainer.pickingMode = PickingMode.Ignore;

            // Insert behind monster/text (between background and monster-image when available)
            InsertBehindMonster(host, _vfxContainer);

            // Separate front layer for interactive bursts/cursor cinders (in front of monster, behind logo/buttons)
            _frontVfxContainer = new VisualElement();
            _frontVfxContainer.name = "title-front-vfx-container";
            _frontVfxContainer.style.position = Position.Absolute;
            _frontVfxContainer.style.left = 0;
            _frontVfxContainer.style.top = 0;
            _frontVfxContainer.style.right = 0;
            _frontVfxContainer.style.bottom = 0;
            _frontVfxContainer.style.overflow = Overflow.Hidden;
            _frontVfxContainer.pickingMode = PickingMode.Ignore;
            InsertInFrontOfMonster(host, _frontVfxContainer);

            _host = host;

            // Clean up any old vignette boxes from previous runs (domain reload disabled)
            var oldVignette = host.Q<VisualElement>("top-vignette");
            if (oldVignette != null) oldVignette.RemoveFromHierarchy();

            _screenWidth = host.resolvedStyle.width;
            _screenHeight = host.resolvedStyle.height;

            if (_screenWidth <= 0) _screenWidth = 1920;
            if (_screenHeight <= 0) _screenHeight = 1080;

            SetupInteractiveTargets(host);
            ApplyBackground();
            CreateTopVignette(host);

            // === AAA ATMOSPHERIC LAYERS ===

            // 1. Atmosphere gradient layer (radial dark red)
            if (_enableAtmosphereGradient)
            {
                CreateAtmosphereLayer();
            }

            // 2. Lightning strikes (behind smoke/embers, above atmosphere)
            if (_enableLightning)
            {
                CreateLightningLayer();
            }

            // 3. Smoke layer (behind particles)
            _smokeLayer = new VisualElement();
            _smokeLayer.name = "smoke-layer";
            _smokeLayer.style.position = Position.Absolute;
            _smokeLayer.style.left = 0;
            _smokeLayer.style.top = 0;
            _smokeLayer.style.right = 0;
            _smokeLayer.style.bottom = 0;
            _smokeLayer.pickingMode = PickingMode.Ignore;
            _vfxContainer.Add(_smokeLayer);

            // Create smoke wisps
            for (int i = 0; i < _smokeCount; i++)
            {
                CreateSmoke();
            }

            // 4. Create ash particles (behind embers)
            for (int i = 0; i < _ashCount; i++)
            {
                CreateAsh();
            }

            // 5. Create embers
            for (int i = 0; i < _emberCount; i++)
            {
                CreateEmber();
            }

            // 6. Create micro-sparks (fast turbulent particles)
            for (int i = 0; i < _microSparkCount; i++)
            {
                CreateMicroSpark();
            }

            // 7. Create sparks (burst particles)
            for (int i = 0; i < _sparkCount; i++)
            {
                CreateSpark();
            }

            // 8. Vignette overlay (on top of everything)
            if (_enableVignette)
            {
                CreateVignetteLayer();
            }

            // 9. Grunge overlay (film grain / grit effect)
            if (_enableGrungeOverlay && _grungeTexture != null)
            {
                CreateGrungeOverlay();
            }

            // 10. Atmospheric fog layers (drifting semi-transparent overlays)
            CreateFogLayers();

            StartVFX();
            Debug.Log($"[TitleScreenVFX] AAA VFX Initialized: {_emberCount} embers, {_microSparkCount} micro-sparks, {_ashCount} ash, {_smokeCount} smoke, {_sparkCount} sparks (Lightning: {_enableLightning}) (Textures: {(_smokeTexture != null ? "smoke " : "")}{(_ashTexture != null ? "ash " : "")}{(_emberTexture != null ? "ember " : "")}{(_grungeTexture != null ? "grunge" : "")})");
        }

        private void CreateAtmosphereLayer()
        {
            _atmosphereLayer = new VisualElement();
            _atmosphereLayer.name = "atmosphere-gradient";
            _atmosphereLayer.style.position = Position.Absolute;
            _atmosphereLayer.style.left = 0;
            _atmosphereLayer.style.top = 0;
            _atmosphereLayer.style.right = 0;
            _atmosphereLayer.style.bottom = 0;
            _atmosphereLayer.pickingMode = PickingMode.Ignore;

            // Create radial gradient effect using a centered element
            var gradientCenter = new VisualElement();
            gradientCenter.style.position = Position.Absolute;
            gradientCenter.style.width = Length.Percent(150);
            gradientCenter.style.height = Length.Percent(150);
            gradientCenter.style.left = Length.Percent(-25);
            gradientCenter.style.top = Length.Percent(-25);
            gradientCenter.style.backgroundColor = _atmosphereColor;
            gradientCenter.style.borderTopLeftRadius = Length.Percent(50);
            gradientCenter.style.borderTopRightRadius = Length.Percent(50);
            gradientCenter.style.borderBottomLeftRadius = Length.Percent(50);
            gradientCenter.style.borderBottomRightRadius = Length.Percent(50);
            gradientCenter.style.opacity = 0.5f;
            gradientCenter.pickingMode = PickingMode.Ignore;
            _atmosphereLayer.Add(gradientCenter);

            _vfxContainer.Add(_atmosphereLayer);
        }

        private void CreateLightningLayer()
        {
            if (!TryPrepareLightningTextures())
            {
                _enableLightning = false;
                return;
            }

            _lightningLayer = new VisualElement();
            _lightningLayer.name = "lightning-layer";
            _lightningLayer.style.position = Position.Absolute;
            _lightningLayer.style.left = 0;
            _lightningLayer.style.top = 0;
            _lightningLayer.style.right = 0;
            _lightningLayer.style.bottom = 0;
            _lightningLayer.pickingMode = PickingMode.Ignore;
            _vfxContainer.Add(_lightningLayer);

            _lightningFlashOverlay = new VisualElement();
            _lightningFlashOverlay.name = "lightning-flash";
            _lightningFlashOverlay.style.position = Position.Absolute;
            _lightningFlashOverlay.style.left = 0;
            _lightningFlashOverlay.style.top = 0;
            _lightningFlashOverlay.style.right = 0;
            _lightningFlashOverlay.style.bottom = 0;
            _lightningFlashOverlay.style.backgroundColor = new Color(1f, 0.4f, 0.15f, 0.12f);
            _lightningFlashOverlay.style.opacity = 0;
            _lightningFlashOverlay.pickingMode = PickingMode.Ignore;
            _lightningLayer.Add(_lightningFlashOverlay);

            for (int i = 0; i < 2; i++)
            {
                CreateLightningStrikeElement();
            }

            ScheduleNextLightning(0.6f);
        }

        private void CreateLightningStrikeElement()
        {
            var glow = new VisualElement();
            glow.style.position = Position.Absolute;
            glow.pickingMode = PickingMode.Ignore;
            glow.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            glow.style.opacity = 0;

            var bolt = new VisualElement();
            bolt.style.position = Position.Absolute;
            bolt.pickingMode = PickingMode.Ignore;
            bolt.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            bolt.style.opacity = 0;

            _lightningLayer.Add(glow);
            _lightningLayer.Add(bolt);

            _lightningStrikes.Add(new LightningStrike
            {
                Bolt = bolt,
                Glow = glow,
                Age = 0,
                Duration = 0.5f,
                FlickerSeed = UnityEngine.Random.Range(0f, 1000f),
                Active = false,
                BaseOpacity = UnityEngine.Random.Range(0.7f, 1f),
                Texture = null
            });
        }

        private bool TryPrepareLightningTextures()
        {
            if (_lightningMaskedA != null || _lightningMaskedB != null) return true;
            if (_lightningBoltTextureA == null && _lightningBoltTextureB == null) return false;

            try
            {
                if (_lightningBoltTextureA != null)
                {
                    _lightningMaskedA = CreateWhitenessMaskedTexture(_lightningBoltTextureA, 0.28f, 0.72f, 1.6f);
                }

                if (_lightningBoltTextureB != null)
                {
                    _lightningMaskedB = CreateWhitenessMaskedTexture(_lightningBoltTextureB, 0.28f, 0.72f, 1.6f);
                }
            }
            catch (UnityException ex)
            {
                Debug.LogWarning($"[TitleScreenVFX] Lightning textures could not be processed (make sure they are Read/Write enabled). Disabling lightning. {ex.GetType().Name}: {ex.Message}");
                _lightningMaskedA = null;
                _lightningMaskedB = null;
                return false;
            }

            return _lightningMaskedA != null || _lightningMaskedB != null;
        }

        private static Texture2D CreateWhitenessMaskedTexture(Texture2D source, float minChannelLow, float minChannelHigh, float alphaPower)
        {
            if (source == null) return null;

            var pixels = source.GetPixels32();
            var output = new Color32[pixels.Length];

            byte low = (byte)Mathf.Clamp(Mathf.RoundToInt(minChannelLow * 255f), 0, 255);
            byte high = (byte)Mathf.Clamp(Mathf.RoundToInt(minChannelHigh * 255f), 0, 255);
            int range = Mathf.Max(1, high - low);

            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 c = pixels[i];
                byte min = c.r < c.g ? (c.r < c.b ? c.r : c.b) : (c.g < c.b ? c.g : c.b);

                float t = Mathf.Clamp01((min - low) / (float)range);
                t = Mathf.Pow(t, alphaPower);
                byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(t * 255f), 0, 255);

                output[i] = new Color32(c.r, c.g, c.b, a);
            }

            var tex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false);
            tex.name = $"{source.name}_whitenessMasked";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels32(output);
            tex.Apply(false, true);
            return tex;
        }

        private void CreateVignetteLayer()
        {
            _vignetteLayer = new VisualElement();
            _vignetteLayer.name = "vignette-overlay";
            _vignetteLayer.style.position = Position.Absolute;
            _vignetteLayer.style.left = 0;
            _vignetteLayer.style.top = 0;
            _vignetteLayer.style.right = 0;
            _vignetteLayer.style.bottom = 0;
            _vignetteLayer.pickingMode = PickingMode.Ignore;

            // Create corner vignettes
            CreateVignetteCorner(_vignetteLayer, true, true);   // Top-left
            CreateVignetteCorner(_vignetteLayer, true, false);  // Top-right
            CreateVignetteCorner(_vignetteLayer, false, true);  // Bottom-left
            CreateVignetteCorner(_vignetteLayer, false, false); // Bottom-right

            // Top edge
            var topEdge = new VisualElement();
            topEdge.style.position = Position.Absolute;
            topEdge.style.top = 0;
            topEdge.style.left = Length.Percent(20);
            topEdge.style.right = Length.Percent(20);
            topEdge.style.height = Length.Percent(15);
            topEdge.style.backgroundColor = new Color(0, 0, 0, _vignetteOpacity * 0.5f);
            topEdge.style.opacity = 0.6f;
            topEdge.pickingMode = PickingMode.Ignore;
            _vignetteLayer.Add(topEdge);

            // Bottom edge (stronger)
            var bottomEdge = new VisualElement();
            bottomEdge.style.position = Position.Absolute;
            bottomEdge.style.bottom = 0;
            bottomEdge.style.left = Length.Percent(20);
            bottomEdge.style.right = Length.Percent(20);
            bottomEdge.style.height = Length.Percent(20);
            bottomEdge.style.backgroundColor = new Color(0, 0, 0, _vignetteOpacity * 0.6f);
            bottomEdge.style.opacity = 0.7f;
            bottomEdge.pickingMode = PickingMode.Ignore;
            _vignetteLayer.Add(bottomEdge);

            _vfxContainer.Add(_vignetteLayer);
        }

        private void CreateVignetteCorner(VisualElement parent, bool isTop, bool isLeft)
        {
            var corner = new VisualElement();
            corner.style.position = Position.Absolute;
            corner.style.width = Length.Percent(40);
            corner.style.height = Length.Percent(40);

            if (isTop) corner.style.top = 0;
            else corner.style.bottom = 0;

            if (isLeft) corner.style.left = 0;
            else corner.style.right = 0;

            corner.style.backgroundColor = new Color(0, 0, 0, _vignetteOpacity);
            corner.style.borderTopLeftRadius = isTop && isLeft ? 0 : Length.Percent(100);
            corner.style.borderTopRightRadius = isTop && !isLeft ? 0 : Length.Percent(100);
            corner.style.borderBottomLeftRadius = !isTop && isLeft ? 0 : Length.Percent(100);
            corner.style.borderBottomRightRadius = !isTop && !isLeft ? 0 : Length.Percent(100);
            corner.pickingMode = PickingMode.Ignore;

            parent.Add(corner);
        }

        private void CreateGrungeOverlay()
        {
            var grungeLayer = new VisualElement();
            grungeLayer.name = "grunge-overlay";
            grungeLayer.style.position = Position.Absolute;
            grungeLayer.style.left = 0;
            grungeLayer.style.top = 0;
            grungeLayer.style.right = 0;
            grungeLayer.style.bottom = 0;
            grungeLayer.style.backgroundImage = new StyleBackground(_grungeTexture);
            grungeLayer.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            grungeLayer.style.unityBackgroundImageTintColor = new Color(1f, 1f, 1f, 1f);
            grungeLayer.style.opacity = _grungeOpacity;
            grungeLayer.pickingMode = PickingMode.Ignore;

            _vfxContainer.Add(grungeLayer);
        }

        // =============================================================================
        // PARTICLE CREATION
        // =============================================================================

        private void CreateEmber()
        {
            // =================================================================
            // AAA EMBER PARTICLE - Multi-layer radial gradient with trail
            // Creates realistic rising ember with hot core and soft outer glow
            // =================================================================

            float depth = UnityEngine.Random.Range(0.35f, 1f);
            float baseSize = UnityEngine.Random.Range(_emberSizeMin, _emberSizeMax) * Mathf.Lerp(0.75f, 1.25f, depth);
            float elongation = UnityEngine.Random.Range(1.4f, 2.0f); // Varied elongation for natural look

            // Root container for the ember
            var root = new VisualElement();
            root.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
            root.style.position = Position.Absolute;
            root.pickingMode = PickingMode.Ignore;

            // === LAYER 1: Outermost soft glow (very faint, large) ===
            float layer1Size = baseSize * 6f;
            var layer1 = CreateEmberLayer(layer1Size, elongation, _emberColorGlow, 0.15f * depth);
            layer1.style.borderTopLeftRadius = Length.Percent(50);
            layer1.style.borderTopRightRadius = Length.Percent(50);
            layer1.style.borderBottomLeftRadius = Length.Percent(15);
            layer1.style.borderBottomRightRadius = Length.Percent(15);
            root.Add(layer1);

            // === LAYER 2: Mid glow ===
            float layer2Size = baseSize * 4f;
            var layer2 = CreateEmberLayer(layer2Size, elongation, _emberColorGlow, 0.25f * depth);
            layer2.style.borderTopLeftRadius = Length.Percent(50);
            layer2.style.borderTopRightRadius = Length.Percent(50);
            layer2.style.borderBottomLeftRadius = Length.Percent(18);
            layer2.style.borderBottomRightRadius = Length.Percent(18);
            CenterInParent(layer2, layer1Size, layer2Size, elongation);
            layer1.Add(layer2);

            // === LAYER 3: Inner glow (orange) ===
            float layer3Size = baseSize * 2.5f;
            var layer3 = CreateEmberLayer(layer3Size, elongation, _emberColorBody, 0.5f * depth);
            layer3.style.borderTopLeftRadius = Length.Percent(50);
            layer3.style.borderTopRightRadius = Length.Percent(50);
            layer3.style.borderBottomLeftRadius = Length.Percent(22);
            layer3.style.borderBottomRightRadius = Length.Percent(22);
            CenterInParent(layer3, layer2Size, layer3Size, elongation);
            layer2.Add(layer3);

            // === LAYER 4: Hot body (bright orange/yellow) ===
            float layer4Size = baseSize * 1.5f;
            Color hotColor = Color.Lerp(_emberColorBody, _emberColorCore, 0.5f);
            var layer4 = CreateEmberLayer(layer4Size, elongation, hotColor, 0.75f * depth);
            layer4.style.borderTopLeftRadius = Length.Percent(50);
            layer4.style.borderTopRightRadius = Length.Percent(50);
            layer4.style.borderBottomLeftRadius = Length.Percent(28);
            layer4.style.borderBottomRightRadius = Length.Percent(28);
            CenterInParent(layer4, layer3Size, layer4Size, elongation);
            layer3.Add(layer4);

            // === LAYER 5: White-hot core ===
            float coreSize = baseSize * 0.8f;
            var core = CreateEmberLayer(coreSize, elongation * 0.9f, _emberColorCore, 1f);
            core.style.borderTopLeftRadius = Length.Percent(50);
            core.style.borderTopRightRadius = Length.Percent(50);
            core.style.borderBottomLeftRadius = Length.Percent(35);
            core.style.borderBottomRightRadius = Length.Percent(35);
            CenterInParent(core, layer4Size, coreSize, elongation);
            layer4.Add(core);

            // === TRAILING TAIL (creates motion blur effect) ===
            var tail = new VisualElement();
            tail.style.position = Position.Absolute;
            tail.style.width = baseSize * 0.4f;
            tail.style.height = baseSize * elongation * 2f; // Long trailing tail
            tail.style.left = (layer1Size - baseSize * 0.4f) / 2f;
            tail.style.top = layer1Size * elongation * 0.85f; // Below the main ember
            var tailColor = _emberColorBody;
            tailColor.a = 0.15f * depth;
            tail.style.backgroundColor = tailColor;
            tail.style.borderTopLeftRadius = Length.Percent(50);
            tail.style.borderTopRightRadius = Length.Percent(50);
            tail.style.borderBottomLeftRadius = Length.Percent(80);
            tail.style.borderBottomRightRadius = Length.Percent(80);
            tail.pickingMode = PickingMode.Ignore;
            layer1.Add(tail);

            // Set root size
            root.style.width = layer1Size;
            root.style.height = layer1Size * elongation + baseSize * elongation * 2f; // Include tail

            _vfxContainer.Add(root);

            float speed = UnityEngine.Random.Range(_emberSpeedMin, _emberSpeedMax) * Mathf.Lerp(0.55f, 1.1f, depth);
            float opacityScale = Mathf.Lerp(0.25f, 1f, depth);
            float driftAmp = UnityEngine.Random.Range(10f, 30f) * Mathf.Lerp(0.7f, 1.4f, depth);
            float lifetime = UnityEngine.Random.Range(6f, 12f) * Mathf.Lerp(1.25f, 0.9f, depth);

            var ember = new EmberParticle
            {
                GlowElement = root,
                CoreElement = core,
                Size = baseSize,
                GlowSize = layer1Size,
                Depth = depth,
                OpacityScale = opacityScale,
                Speed = speed,
                Lifetime = lifetime,
                Age = UnityEngine.Random.Range(0f, 5f),
                FlickerPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                DriftPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                DriftAmplitude = driftAmp
            };

            ResetEmberPosition(ember);
            _embers.Add(ember);
        }

        private VisualElement CreateEmberLayer(float size, float elongation, Color color, float alphaMultiplier)
        {
            var layer = new VisualElement();
            layer.style.position = Position.Absolute;
            layer.style.width = size;
            layer.style.height = size * elongation;
            var c = color;
            c.a *= alphaMultiplier;
            layer.style.backgroundColor = c;
            layer.pickingMode = PickingMode.Ignore;
            return layer;
        }

        private void CenterInParent(VisualElement child, float parentSize, float childSize, float elongation)
        {
            child.style.left = (parentSize - childSize) / 2f;
            child.style.top = (parentSize * elongation - childSize * elongation) / 2f;
        }

        private void CreateAsh()
        {
            var element = new VisualElement();
            element.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
            element.style.position = Position.Absolute;
            element.pickingMode = PickingMode.Ignore;

            // USE ACTUAL TEXTURE if assigned
            if (_ashTexture != null)
            {
                element.style.backgroundImage = new StyleBackground(_ashTexture);
                element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                // Tint with ash color
                Color ashTint = Color.Lerp(_ashColorDark, _ashColorLight, UnityEngine.Random.Range(0f, 1f));
                element.style.unityBackgroundImageTintColor = ashTint;
            }
            else
            {
                // Fallback to solid color if no texture
                Color ashColor = Color.Lerp(_ashColorDark, _ashColorLight, UnityEngine.Random.Range(0f, 1f));
                element.style.backgroundColor = ashColor;
            }

            _vfxContainer.Add(element);

            float aspect = UnityEngine.Random.Range(0.6f, 1.4f);
            var sizeBase = UnityEngine.Random.Range(_ashSizeMin, _ashSizeMax);
            var sizeX = sizeBase * aspect;
            var sizeY = sizeBase;

            var ash = new AshParticle
            {
                Element = element,
                SizeX = sizeX,
                SizeY = sizeY,
                Speed = UnityEngine.Random.Range(_ashSpeedMin, _ashSpeedMax),
                Lifetime = UnityEngine.Random.Range(8f, 16f),
                Age = UnityEngine.Random.Range(0f, 8f),
                RotationSpeed = UnityEngine.Random.Range(-60f, 60f),
                Rotation = UnityEngine.Random.Range(0f, 360f),
                DriftPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                DriftAmplitude = UnityEngine.Random.Range(12f, 35f),
                TumblePhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                TumbleSpeed = UnityEngine.Random.Range(1.5f, 4f)
            };

            ResetAshPosition(ash);

            element.style.width = sizeX;
            element.style.height = sizeY;

            _ashes.Add(ash);
        }

        private void CreateSpark()
        {
            var element = new VisualElement();
            element.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
            element.style.position = Position.Absolute;
            // AAA: Bright yellow-white core
            element.style.backgroundColor = new Color(1f, 0.95f, 0.7f, 0.95f);
            element.style.borderTopLeftRadius = 50;
            element.style.borderTopRightRadius = 50;
            element.style.borderBottomLeftRadius = 50;
            element.style.borderBottomRightRadius = 50;
            element.pickingMode = PickingMode.Ignore;

            _vfxContainer.Add(element);

            var spark = new SparkParticle
            {
                Element = element,
                Size = UnityEngine.Random.Range(2f, 4f),
                Speed = UnityEngine.Random.Range(_sparkSpeedMin, _sparkSpeedMax),
                Lifetime = UnityEngine.Random.Range(0.4f, 1.2f),
                Age = UnityEngine.Random.Range(0f, 12f),
                Direction = new Vector2(
                    UnityEngine.Random.Range(-0.4f, 0.4f),
                    UnityEngine.Random.Range(-1f, -0.6f)
                ).normalized
            };

            ResetSparkPosition(spark);

            element.style.width = spark.Size;
            element.style.height = spark.Size;
            element.style.opacity = 0;

            _sparks.Add(spark);
        }

        private void CreateMicroSpark()
        {
            var element = new VisualElement();
            element.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
            element.style.position = Position.Absolute;
            element.pickingMode = PickingMode.Ignore;

            // Use ember texture for micro-sparks if available
            if (_emberTexture != null)
            {
                element.style.backgroundImage = new StyleBackground(_emberTexture);
                element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                element.style.unityBackgroundImageTintColor = new Color(1f, 0.85f, 0.4f, 0.9f);
            }
            else
            {
                element.style.backgroundColor = new Color(1f, 0.85f, 0.4f, 0.9f);
                element.style.borderTopLeftRadius = 50;
                element.style.borderTopRightRadius = 50;
                element.style.borderBottomLeftRadius = 50;
                element.style.borderBottomRightRadius = 50;
            }

            _vfxContainer.Add(element);

            var size = UnityEngine.Random.Range(_microSparkSizeMin, _microSparkSizeMax);

            var microSpark = new MicroSparkParticle
            {
                Element = element,
                Size = size,
                Speed = UnityEngine.Random.Range(_microSparkSpeedMin, _microSparkSpeedMax),
                Lifetime = UnityEngine.Random.Range(0.8f, 2f),  // Short life
                Age = UnityEngine.Random.Range(0f, 3f),
                TurbulencePhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                TurbulenceFrequency = UnityEngine.Random.Range(3f, 8f)
            };

            ResetMicroSparkPosition(microSpark);

            element.style.width = size;
            element.style.height = size;

            _microSparks.Add(microSpark);
        }

        private void CreateSmoke()
        {
            var element = new VisualElement();
            element.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
            element.style.position = Position.Absolute;
            element.pickingMode = PickingMode.Ignore;

            // USE ACTUAL SMOKE TEXTURE if assigned
            if (_smokeTexture != null)
            {
                element.style.backgroundImage = new StyleBackground(_smokeTexture);
                element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                element.style.unityBackgroundImageTintColor = _smokeColor;
            }
            else
            {
                // Fallback to solid color
                element.style.backgroundColor = _smokeColor;
                element.style.borderTopLeftRadius = Length.Percent(50);
                element.style.borderTopRightRadius = Length.Percent(50);
                element.style.borderBottomLeftRadius = Length.Percent(50);
                element.style.borderBottomRightRadius = Length.Percent(50);
            }

            _smokeLayer.Add(element);

            var size = UnityEngine.Random.Range(_smokeSizeMin, _smokeSizeMax);

            var smoke = new SmokeParticle
            {
                Element = element,
                Size = size,
                Speed = UnityEngine.Random.Range(_smokeSpeedMin, _smokeSpeedMax),
                Lifetime = UnityEngine.Random.Range(15f, 25f),
                Age = UnityEngine.Random.Range(0f, 15f),
                DriftPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                DriftAmplitude = UnityEngine.Random.Range(30f, 60f),
                ExpansionRate = UnityEngine.Random.Range(0.02f, 0.05f)
            };

            ResetSmokePosition(smoke);

            element.style.width = size;
            element.style.height = size;

            _smokes.Add(smoke);
        }

        // =============================================================================
        // POSITION RESET
        // =============================================================================

        private void ResetEmberPosition(EmberParticle ember)
        {
            // Spawn from ANYWHERE on screen (full coverage for animated background)
            ember.Position = new Vector2(
                UnityEngine.Random.Range(0f, _screenWidth),
                UnityEngine.Random.Range(0f, _screenHeight * 1.2f) // Some spawn off-screen top
            );
            ember.Age = UnityEngine.Random.Range(0f, ember.Lifetime * 0.5f); // Stagger ages
        }

        private void ResetAshPosition(AshParticle ash)
        {
            // Spawn from ANYWHERE on screen
            ash.Position = new Vector2(
                UnityEngine.Random.Range(0f, _screenWidth),
                UnityEngine.Random.Range(0f, _screenHeight * 1.1f)
            );
            ash.Age = UnityEngine.Random.Range(0f, ash.Lifetime * 0.5f);
        }

        private void ResetSparkPosition(SparkParticle spark)
        {
            // Sparks spawn from ANYWHERE on screen
            spark.Position = new Vector2(
                UnityEngine.Random.Range(0f, _screenWidth),
                UnityEngine.Random.Range(_screenHeight * 0.3f, _screenHeight)
            );
            spark.Age = 0;
            spark.Direction = new Vector2(
                UnityEngine.Random.Range(-0.5f, 0.5f),
                UnityEngine.Random.Range(-1f, -0.4f)
            ).normalized;
        }

        private void ResetMicroSparkPosition(MicroSparkParticle spark)
        {
            // Micro-sparks spawn ANYWHERE on screen
            spark.Position = new Vector2(
                UnityEngine.Random.Range(0f, _screenWidth),
                UnityEngine.Random.Range(0f, _screenHeight)
            );
            spark.Age = UnityEngine.Random.Range(0f, spark.Lifetime * 0.3f);
        }

        private void ResetSmokePosition(SmokeParticle smoke)
        {
            // Smoke spawns ANYWHERE across the screen
            smoke.Position = new Vector2(
                UnityEngine.Random.Range(-smoke.Size * 0.3f, _screenWidth + smoke.Size * 0.3f),
                UnityEngine.Random.Range(0f, _screenHeight * 1.1f)
            );
            smoke.Age = UnityEngine.Random.Range(0f, smoke.Lifetime * 0.4f);
            smoke.CurrentSize = smoke.Size;
        }

        // =============================================================================
        // VFX CONTROL
        // =============================================================================

        public void StartVFX()
        {
            if (_isActive) return;
            _isActive = true;
            _updateCoroutine = StartCoroutine(UpdateParticles());
        }

        public void StopVFX()
        {
            _isActive = false;
            if (_updateCoroutine != null)
            {
                StopCoroutine(_updateCoroutine);
                _updateCoroutine = null;
            }
        }

        private IEnumerator UpdateParticles()
        {
            while (_isActive)
            {
                float deltaTime = Time.unscaledDeltaTime;
                _windOffset += deltaTime * _windFrequency;
                _turbulenceOffset += deltaTime * 2.5f;

                float wind = Mathf.Sin(_windOffset) * _windStrength;
                float turbulence = Mathf.Sin(_turbulenceOffset * 1.7f) * _turbulenceStrength;

                UpdateLightning(deltaTime);
                UpdateLogo(deltaTime);
                UpdateBurstParticles(deltaTime, wind, turbulence);
                UpdateTransientSmokes(deltaTime, wind);

                // Update smoke (background layer)
                foreach (var smoke in _smokes)
                {
                    UpdateSmoke(smoke, deltaTime, wind);
                }

                // Update ash
                foreach (var ash in _ashes)
                {
                    UpdateAsh(ash, deltaTime, wind);
                }

                // Update embers
                foreach (var ember in _embers)
                {
                    UpdateEmber(ember, deltaTime, wind, turbulence);
                }

                // Update micro-sparks
                foreach (var microSpark in _microSparks)
                {
                    UpdateMicroSpark(microSpark, deltaTime, turbulence);
                }

                // Update sparks
                foreach (var spark in _sparks)
                {
                    UpdateSpark(spark, deltaTime);
                }

                // Animate atmospheric fog layers (slow horizontal drift)
                _fogTime += deltaTime;
                if (_fogLayer1 != null)
                {
                    float drift1 = Mathf.Sin(_fogTime * 0.15f) * 10f;
                    _fogLayer1.style.translate = new Translate(drift1, 0);
                }
                if (_fogLayer2 != null)
                {
                    float drift2 = Mathf.Sin(_fogTime * 0.1f + 1.5f) * 15f;
                    _fogLayer2.style.translate = new Translate(drift2, 0);
                }
                if (_fogLayer3 != null)
                {
                    float drift3 = Mathf.Sin(_fogTime * 0.08f + 3.0f) * 8f;
                    _fogLayer3.style.translate = new Translate(drift3, 0);
                }

                // Particle parallax response to cursor position
                if (_hasMouse && _screenWidth > 0 && _screenHeight > 0)
                {
                    float normalizedX = (_mousePosition.x - _screenWidth * 0.5f) / (_screenWidth * 0.5f);
                    float normalizedY = (_mousePosition.y - _screenHeight * 0.5f) / (_screenHeight * 0.5f);

                    // Apply subtle parallax to VFX containers (background particles shift opposite to cursor)
                    if (_vfxContainer != null)
                    {
                        float bgOffsetX = -normalizedX * 8f;
                        float bgOffsetY = normalizedY * 5f; // Inverted Y for natural feel
                        _vfxContainer.style.translate = new Translate(bgOffsetX, bgOffsetY);
                    }
                    // Front container shifts less for depth differentiation
                    if (_frontVfxContainer != null)
                    {
                        float fgOffsetX = -normalizedX * 4f;
                        float fgOffsetY = normalizedY * 3f;
                        _frontVfxContainer.style.translate = new Translate(fgOffsetX, fgOffsetY);
                    }
                }

                yield return null;
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!_enableClickMonsterBurst) return;
            if (evt.button != 0) return;

            if (evt.target is Button)
            {
                return;
            }

            if (_monsterElement != null && evt.target is VisualElement clicked)
            {
                if (!IsSelfOrChildOf(clicked, _monsterElement))
                {
                    return;
                }
            }

            SpawnMonsterBurstAtMonster();
        }

        private static bool IsSelfOrChildOf(VisualElement maybeChild, VisualElement parent)
        {
            if (maybeChild == null || parent == null) return false;
            var current = maybeChild;
            while (current != null)
            {
                if (current == parent) return true;
                current = current.parent;
            }
            return false;
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            _mousePosition = evt.localMousePosition;
            _hasMouse = true;
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            _hasMouse = false;
        }

        private void OnLogoEnter(MouseEnterEvent evt)
        {
            _logoHover = true;
        }

        private void OnLogoLeave(MouseLeaveEvent evt)
        {
            _logoHover = false;
        }

        private void OnLogoPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            if (_enableLogoPulse)
            {
                TriggerLogoPulse();
            }

            if (_enableLogoSmoke)
            {
                SpawnLogoSmokeBurst();
            }

            evt.StopPropagation();
        }

        private void TriggerLogoPulse()
        {
            _logoPulseRemaining = Mathf.Max(0.05f, _logoPulseDuration);
            _logoPulseStrength = 1f;
        }

        private void UpdateLogo(float deltaTime)
        {
            if (_logoContainer == null) return;

            float targetGlow = 0f;

            if (_logoPulseRemaining > 0f)
            {
                _logoPulseRemaining -= deltaTime;
                float t = 1f - Mathf.Clamp01(_logoPulseRemaining / Mathf.Max(0.01f, _logoPulseDuration));
                float bump = Mathf.Sin(t * Mathf.PI);

                float scale = 1f + bump * 0.05f;
                _logoContainer.style.scale = new Scale(new Vector2(scale, scale));

                if (_logoPulseRemaining <= 0f)
                {
                    _logoContainer.style.scale = new Scale(Vector2.one);
                }
            }

            if (_logoGlowElement != null)
            {
                _logoGlowCurrentOpacity = Mathf.Lerp(_logoGlowCurrentOpacity, targetGlow, deltaTime * 10f);
                _logoGlowElement.style.opacity = _logoGlowCurrentOpacity;
            }

            // Animate logo aura layers with staggered pulsing
            _auraTime += deltaTime;
            float hoverBoost = _logoButtonHoverBoost;

            if (_logoAuraInner != null)
            {
                float innerPulse = 0.12f + Mathf.Sin(_auraTime * 2.0f) * 0.04f + hoverBoost * 0.1f;
                _logoAuraInner.style.opacity = innerPulse;
            }
            if (_logoAuraOuter != null)
            {
                float outerPulse = 0.06f + Mathf.Sin(_auraTime * 1.2f) * 0.025f + hoverBoost * 0.05f;
                _logoAuraOuter.style.opacity = outerPulse;
            }
            if (_logoAuraPulse != null)
            {
                float pulsePulse = 0.08f + Mathf.Sin(_auraTime * 3.0f) * 0.05f + hoverBoost * 0.08f;
                _logoAuraPulse.style.opacity = pulsePulse;
            }

            // Decay hover boost smoothly
            if (_logoButtonHoverBoost > 0f)
            {
                _logoButtonHoverBoost = Mathf.Lerp(_logoButtonHoverBoost, 0f, deltaTime * 5f);
                if (_logoButtonHoverBoost < 0.001f) _logoButtonHoverBoost = 0f;
            }
        }

        private void SpawnMonsterBurstAtMonster()
        {
            if (_frontVfxContainer == null) return;

            Vector2 origin = new Vector2(_screenWidth * 0.5f, _screenHeight * 0.65f);
            Rect monsterWorld = default;
            bool hasMonster = _host != null && _monsterElement != null;
            if (hasMonster)
            {
                monsterWorld = _monsterElement.worldBound;
                var centerWorld = monsterWorld.center;
                origin = _host.WorldToLocal(new Vector2(centerWorld.x, centerWorld.y));
            }

            // Big chest flash (behind the flames) to sell power.
            SpawnGlowPulse(origin, _frontVfxContainer, 220f, 560f, 0.38f, 0.45f, insertBehind: true);

            // Eye flashes aligned from monster bounds (front layer).
            if (hasMonster)
            {
                Vector2 eyeLeft = _host.WorldToLocal(new Vector2(monsterWorld.xMin + monsterWorld.width * 0.44f, monsterWorld.yMin + monsterWorld.height * 0.19f));
                Vector2 eyeRight = _host.WorldToLocal(new Vector2(monsterWorld.xMin + monsterWorld.width * 0.56f, monsterWorld.yMin + monsterWorld.height * 0.19f));
                SpawnGlowPulse(eyeLeft, _frontVfxContainer, 26f, 86f, 0.72f, 0.28f, insertBehind: false);
                SpawnGlowPulse(eyeRight, _frontVfxContainer, 26f, 86f, 0.72f, 0.28f, insertBehind: false);
            }

            int count = Mathf.Clamp(_monsterBurstParticleCount, 0, 120);
            for (int i = 0; i < count; i++)
            {
                float size = UnityEngine.Random.Range(10f, 22f);

                // Make these more "flame-like" (taller than wide).
                float w = size * UnityEngine.Random.Range(0.55f, 0.85f);
                float h = size * UnityEngine.Random.Range(1.35f, 2.05f);

                var element = new VisualElement();
                element.style.position = Position.Absolute;
                element.style.width = w;
                element.style.height = h;
                element.pickingMode = PickingMode.Ignore;

                // Outer glow (bigger than the flame)
                var glow = new VisualElement();
                glow.style.position = Position.Absolute;
                glow.style.left = -w * 0.55f;
                glow.style.top = -h * 0.35f;
                glow.style.width = w * 2.1f;
                glow.style.height = h * 1.7f;
                glow.pickingMode = PickingMode.Ignore;

                // Inner core (hot center)
                var core = new VisualElement();
                core.style.position = Position.Absolute;
                core.style.left = 0;
                core.style.top = 0;
                core.style.width = w;
                core.style.height = h;
                core.pickingMode = PickingMode.Ignore;

                if (_emberTexture != null)
                {
                    glow.style.backgroundImage = new StyleBackground(_emberTexture);
                    glow.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                    glow.style.unityBackgroundImageTintColor = new Color(1f, 0.25f, 0.08f, 0.30f);

                    core.style.backgroundImage = new StyleBackground(_emberTexture);
                    core.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                    core.style.unityBackgroundImageTintColor = new Color(1f, 0.70f, 0.28f, 0.92f);
                }
                else
                {
                    glow.style.borderTopLeftRadius = Length.Percent(50);
                    glow.style.borderTopRightRadius = Length.Percent(50);
                    glow.style.borderBottomLeftRadius = Length.Percent(50);
                    glow.style.borderBottomRightRadius = Length.Percent(50);
                    glow.style.backgroundColor = new Color(1f, 0.22f, 0.06f, 0.30f);

                    core.style.borderTopLeftRadius = Length.Percent(50);
                    core.style.borderTopRightRadius = Length.Percent(50);
                    core.style.borderBottomLeftRadius = Length.Percent(50);
                    core.style.borderBottomRightRadius = Length.Percent(50);
                    core.style.backgroundColor = new Color(1f, 0.68f, 0.22f, 0.92f);
                }

                element.Add(glow);
                element.Add(core);

                element.style.left = origin.x - w * 0.5f;
                element.style.top = origin.y - h * 0.5f;
                element.style.opacity = 1f;
                element.style.rotate = new Rotate(UnityEngine.Random.Range(-18f, 18f));

                _frontVfxContainer.Add(element);

                Vector2 dir = (UnityEngine.Random.insideUnitCircle * 0.55f + new Vector2(0f, -1.15f)).normalized;
                float force = UnityEngine.Random.Range(_monsterBurstForceMin, _monsterBurstForceMax);

                _burstParticles.Add(new BurstParticle
                {
                    Element = element,
                    Position = origin,
                    Velocity = dir * force,
                    Size = Mathf.Max(w, h),
                    Width = w,
                    Height = h,
                    Lifetime = Mathf.Max(0.2f, _monsterBurstLifetime) * UnityEngine.Random.Range(0.85f, 1.15f),
                    Age = 0f,
                    FlickerSeed = UnityEngine.Random.Range(0f, 1000f)
                });
            }

            // Add smoke kick for volume (front + behind the monster)
            SpawnTransientSmoke(origin, _frontVfxContainer, 4, 90f, 160f, new Color(0.20f, 0.16f, 0.14f, 0.22f), 1.1f);

            if (hasMonster && _vfxContainer != null)
            {
                // Smoke from ears / behind head (behind monster)
                Vector2 earLeft = _host.WorldToLocal(new Vector2(monsterWorld.xMin + monsterWorld.width * 0.34f, monsterWorld.yMin + monsterWorld.height * 0.22f));
                Vector2 earRight = _host.WorldToLocal(new Vector2(monsterWorld.xMin + monsterWorld.width * 0.66f, monsterWorld.yMin + monsterWorld.height * 0.22f));
                Vector2 behindHead = _host.WorldToLocal(new Vector2(monsterWorld.center.x, monsterWorld.yMin + monsterWorld.height * 0.26f));

                var earTint = new Color(0.18f, 0.14f, 0.12f, 0.22f);
                SpawnTransientSmoke(earLeft, _vfxContainer, 5, 110f, 210f, earTint, 1.35f);
                SpawnTransientSmoke(earRight, _vfxContainer, 5, 110f, 210f, earTint, 1.35f);
                SpawnTransientSmoke(behindHead, _vfxContainer, 6, 140f, 260f, new Color(0.18f, 0.13f, 0.11f, 0.18f), 1.55f);
            }

            // Prevent unbounded growth if spam-clicked
            while (_burstParticles.Count > 260)
            {
                var p = _burstParticles[0];
                if (p.Element != null) p.Element.RemoveFromHierarchy();
                _burstParticles.RemoveAt(0);
            }
        }

        private void UpdateBurstParticles(float deltaTime, float wind, float turbulence)
        {
            if (_burstParticles.Count == 0) return;

            for (int i = _burstParticles.Count - 1; i >= 0; i--)
            {
                var p = _burstParticles[i];
                p.Age += deltaTime;
                float t = p.Age / Mathf.Max(0.0001f, p.Lifetime);
                if (t >= 1f)
                {
                    if (p.Element != null) p.Element.RemoveFromHierarchy();
                    _burstParticles.RemoveAt(i);
                    continue;
                }

                // UI coordinate: negative Y is "up"
                p.Velocity += new Vector2(wind * 160f, -520f) * deltaTime;
                p.Velocity += new Vector2(0f, turbulence * -160f) * deltaTime;
                p.Velocity *= Mathf.Clamp01(1f - deltaTime * 2.2f);

                p.Position += p.Velocity * deltaTime;

                float flicker = 0.70f + 0.30f * Mathf.PerlinNoise(p.FlickerSeed, p.Age * 18f);
                float opacity = (1f - t) * flicker;
                float scale = 1f - t * 0.25f;

                float w = p.Width > 0 ? p.Width : p.Element.resolvedStyle.width;
                float h = p.Height > 0 ? p.Height : p.Element.resolvedStyle.height;
                if (w <= 0) w = p.Size;
                if (h <= 0) h = p.Size;

                p.Element.style.left = p.Position.x - w * 0.5f;
                p.Element.style.top = p.Position.y - h * 0.5f;
                p.Element.style.opacity = opacity;
                p.Element.style.scale = new Scale(Vector2.one * scale);
            }
        }

        private void SpawnGlowPulse(Vector2 position, VisualElement parent, float startSize, float endSize, float baseOpacity, float lifetime, bool insertBehind)
        {
            if (parent == null) return;

            float s0 = Mathf.Max(4f, startSize);
            float s1 = Mathf.Max(s0, endSize);

            var el = new VisualElement();
            el.style.position = Position.Absolute;
            el.style.width = s0;
            el.style.height = s0;
            el.style.left = position.x - s0 * 0.5f;
            el.style.top = position.y - s0 * 0.5f;
            el.style.borderTopLeftRadius = Length.Percent(50);
            el.style.borderTopRightRadius = Length.Percent(50);
            el.style.borderBottomLeftRadius = Length.Percent(50);
            el.style.borderBottomRightRadius = Length.Percent(50);
            el.style.backgroundColor = new Color(1f, 0.35f, 0.12f, 1f);
            el.style.opacity = 0f;
            el.pickingMode = PickingMode.Ignore;

            if (insertBehind && parent.childCount > 0)
            {
                parent.Insert(0, el);
            }
            else
            {
                parent.Add(el);
            }

            _glowPulses.Add(new GlowPulse
            {
                Element = el,
                Position = position,
                StartSize = s0,
                EndSize = s1,
                Lifetime = Mathf.Max(0.05f, lifetime),
                Age = 0f,
                BaseOpacity = Mathf.Clamp01(baseOpacity)
            });

            while (_glowPulses.Count > 18)
            {
                var p = _glowPulses[0];
                if (p.Element != null) p.Element.RemoveFromHierarchy();
                _glowPulses.RemoveAt(0);
            }
        }

        private void UpdateGlowPulses(float deltaTime)
        {
            if (_glowPulses.Count == 0) return;

            for (int i = _glowPulses.Count - 1; i >= 0; i--)
            {
                var p = _glowPulses[i];
                p.Age += deltaTime;
                float t = p.Age / Mathf.Max(0.0001f, p.Lifetime);
                if (t >= 1f)
                {
                    if (p.Element != null) p.Element.RemoveFromHierarchy();
                    _glowPulses.RemoveAt(i);
                    continue;
                }

                float ease = 1f - Mathf.Pow(1f - t, 3f);
                float size = Mathf.Lerp(p.StartSize, p.EndSize, ease);
                float alpha = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * p.BaseOpacity;

                if (p.Element != null)
                {
                    p.Element.style.width = size;
                    p.Element.style.height = size;
                    p.Element.style.left = p.Position.x - size * 0.5f;
                    p.Element.style.top = p.Position.y - size * 0.5f;
                    p.Element.style.opacity = alpha;
                }
            }
        }

        private void SpawnLogoSmokeBurst()
        {
            if (_logoFxLayer == null || _host == null || _logoContainer == null) return;

            var b = _logoContainer.worldBound;
            Vector2 origin = _host.WorldToLocal(new Vector2(b.center.x, b.yMin + 40f));
            SpawnTransientSmoke(origin, _logoFxLayer, 8, 90f, 160f, _logoSmokeTint, 1.2f);
        }

        private void SpawnTransientSmoke(Vector2 origin, VisualElement parent, int count, float sizeMin, float sizeMax, Color tint, float lifetime)
        {
            if (parent == null) return;
            if (_smokeTexture == null) return;

            for (int i = 0; i < Mathf.Max(0, count); i++)
            {
                float size = UnityEngine.Random.Range(sizeMin, sizeMax);

                var element = new VisualElement();
                element.style.position = Position.Absolute;
                element.pickingMode = PickingMode.Ignore;
                element.style.width = size;
                element.style.height = size;
                element.style.backgroundImage = new StyleBackground(_smokeTexture);
                element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                element.style.unityBackgroundImageTintColor = tint;
                element.style.opacity = 0f;

                parent.Add(element);

                Vector2 offset = UnityEngine.Random.insideUnitCircle * 70f;
                Vector2 pos = origin + offset;
                Vector2 vel = new Vector2(UnityEngine.Random.Range(-20f, 20f), UnityEngine.Random.Range(-120f, -70f));

                _transientSmokes.Add(new TransientSmokeParticle
                {
                    Element = element,
                    Position = pos,
                    Velocity = vel,
                    Size = size,
                    Lifetime = Mathf.Max(0.2f, lifetime) * UnityEngine.Random.Range(0.85f, 1.2f),
                    Age = 0f,
                    ExpansionRate = UnityEngine.Random.Range(0.06f, 0.10f),
                    RotationSpeed = UnityEngine.Random.Range(-18f, 18f)
                });
            }

            while (_transientSmokes.Count > 140)
            {
                var s = _transientSmokes[0];
                if (s.Element != null) s.Element.RemoveFromHierarchy();
                _transientSmokes.RemoveAt(0);
            }
        }

        private void UpdateTransientSmokes(float deltaTime, float wind)
        {
            if (_transientSmokes.Count == 0) return;

            for (int i = _transientSmokes.Count - 1; i >= 0; i--)
            {
                var s = _transientSmokes[i];
                s.Age += deltaTime;
                float t = s.Age / Mathf.Max(0.0001f, s.Lifetime);
                if (t >= 1f)
                {
                    if (s.Element != null) s.Element.RemoveFromHierarchy();
                    _transientSmokes.RemoveAt(i);
                    continue;
                }

                s.Velocity.x += wind * 18f * deltaTime;
                s.Velocity *= Mathf.Clamp01(1f - deltaTime * 0.9f);

                s.Position += s.Velocity * deltaTime;
                s.Size *= 1f + s.ExpansionRate * deltaTime;

                float fadeIn = Mathf.Clamp01(t / 0.12f);
                float fadeOut = Mathf.Clamp01((1f - t) / 0.30f);
                float opacity = Mathf.Min(fadeIn, fadeOut) * 0.95f;

                s.Element.style.width = s.Size;
                s.Element.style.height = s.Size;
                s.Element.style.left = s.Position.x - s.Size * 0.5f;
                s.Element.style.top = s.Position.y - s.Size * 0.5f;
                s.Element.style.opacity = opacity;
                s.Element.style.rotate = new Rotate(s.RotationSpeed * s.Age);
            }
        }

        private void ScheduleNextLightning(float biasSeconds = 0f)
        {
            _nextLightningAt = Time.unscaledTime + UnityEngine.Random.Range(_lightningIntervalMin, _lightningIntervalMax) + biasSeconds;
        }

        private void UpdateLightning(float deltaTime)
        {
            if (!_enableLightning || _lightningLayer == null) return;

            float now = Time.unscaledTime;
            if (now >= _nextLightningAt)
            {
                TriggerLightningStrike();
                ScheduleNextLightning();
            }

            float maxFlash = 0f;
            foreach (var strike in _lightningStrikes)
            {
                if (!strike.Active) continue;

                strike.Age += deltaTime;
                float t = strike.Age / Mathf.Max(0.0001f, strike.Duration);
                if (t >= 1f)
                {
                    strike.Active = false;
                    strike.Bolt.style.opacity = 0;
                    strike.Glow.style.opacity = 0;
                    continue;
                }

                // Bright flash, hold, then fade - lingering lightning
                float flashPeak = t < 0.1f ? t / 0.1f : 1f; // Quick ramp up
                float decay = Mathf.Pow(1f - t, 0.8f); // Slower decay - stays visible longer
                float flicker = 0.85f + 0.15f * Mathf.PerlinNoise(strike.FlickerSeed, strike.Age * 25f);
                float alpha = Mathf.Clamp01(flashPeak * decay * flicker) * strike.BaseOpacity * _lightningIntensity;

                strike.Bolt.style.opacity = alpha;
                strike.Glow.style.opacity = alpha * 0.35f;
                maxFlash = Mathf.Max(maxFlash, alpha);
            }

            _lightningFlashOpacity = Mathf.Lerp(_lightningFlashOpacity, maxFlash, deltaTime * 10f);
            if (_lightningFlashOverlay != null)
            {
                _lightningFlashOverlay.style.opacity = _lightningFlashOpacity * 0.20f;
            }
        }

        private void TriggerLightningStrike()
        {
            if (_lightningMaskedA == null && _lightningMaskedB == null) return;

            LightningStrike strike = null;
            foreach (var candidate in _lightningStrikes)
            {
                if (!candidate.Active)
                {
                    strike = candidate;
                    break;
                }
            }

            if (strike == null && _lightningStrikes.Count > 0)
            {
                strike = _lightningStrikes[0];
            }

            if (strike == null) return;

            var tex = UnityEngine.Random.value < 0.5f ? _lightningMaskedA : _lightningMaskedB;
            if (tex == null) tex = _lightningMaskedA ?? _lightningMaskedB;

            strike.Texture = tex;
            strike.Active = true;
            strike.Age = 0f;
            strike.Duration = UnityEngine.Random.Range(_lightningStrikeDurationMin, _lightningStrikeDurationMax);
            strike.FlickerSeed = UnityEngine.Random.Range(0f, 1000f);
            strike.BaseOpacity = UnityEngine.Random.Range(0.75f, 1f);

            float height = Mathf.Max(400f, _screenHeight * UnityEngine.Random.Range(0.85f, 1.1f)); // Normal length
            // Width independent of aspect - wide = StretchToFill makes bolt LINE thicker
            float width = UnityEngine.Random.Range(550f, 800f); // Wide = thick bolt lines

            // Bias lightning to the sides so it frames the monster instead of sitting behind it.
            bool sideBiased = UnityEngine.Random.value < 0.88f;
            float left;
            if (sideBiased)
            {
                bool leftSide = UnityEngine.Random.value < 0.5f;
                if (leftSide)
                {
                    left = UnityEngine.Random.Range(_screenWidth * 0.05f, _screenWidth * 0.28f);
                }
                else
                {
                    left = UnityEngine.Random.Range(_screenWidth * 0.72f, _screenWidth * 0.95f - width);
                }
            }
            else
            {
                left = UnityEngine.Random.Range(_screenWidth * 0.22f, _screenWidth * 0.78f - width);
            }
            float top = -_screenHeight * UnityEngine.Random.Range(0.05f, 0.18f);
            float rotation = UnityEngine.Random.Range(-12f, 12f);

            strike.Bolt.style.backgroundImage = new StyleBackground(tex);
            strike.Bolt.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100)); // Stretch to make bolt THICKER
            strike.Bolt.style.unityBackgroundImageTintColor = _lightningTint;
            strike.Bolt.style.width = width;
            strike.Bolt.style.height = height;
            strike.Bolt.style.left = left;
            strike.Bolt.style.top = top;
            strike.Bolt.style.rotate = new Rotate(rotation);

            strike.Glow.style.backgroundImage = new StyleBackground(tex);
            strike.Glow.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100)); // Stretch glow too
            strike.Glow.style.unityBackgroundImageTintColor = new Color(1f, 0.35f, 0.12f, 1f);
            strike.Glow.style.width = width * 1.6f;  // Bigger glow for thick appearance
            strike.Glow.style.height = height * 1.3f;
            strike.Glow.style.left = left - width * 0.3f;
            strike.Glow.style.top = top - height * 0.15f;
            strike.Glow.style.rotate = new Rotate(rotation);
        }

        // =============================================================================
        // PARTICLE UPDATES
        // =============================================================================

        private void UpdateEmber(EmberParticle ember, float deltaTime, float wind, float turbulence)
        {
            ember.Age += deltaTime;

            // Calculate opacity with smooth fade in/out
            float normalizedAge = ember.Age / ember.Lifetime;
            float opacity;

            if (normalizedAge < 0.1f)
            {
                // Quick fade in
                opacity = normalizedAge / 0.1f;
            }
            else if (normalizedAge > 0.75f)
            {
                // Slow fade out
                opacity = (1f - normalizedAge) / 0.25f;
            }
            else
            {
                opacity = 1f;
            }

            // Gentle variation instead of harsh flicker
            float flicker1 = Mathf.Sin(ember.Age * _flickerSpeed + ember.FlickerPhase);
            float flicker2 = Mathf.Sin(ember.Age * _flickerSpeed * 1.8f + ember.FlickerPhase * 1.3f);
            float variation = 0.85f + 0.12f * flicker1 + 0.05f * flicker2;
            opacity *= variation;
            opacity *= ember.OpacityScale;

            // Move upward with drift and turbulence
            float drift = Mathf.Sin(ember.Age * 1.5f + ember.DriftPhase) * ember.DriftAmplitude * deltaTime;
            float turbDrift = turbulence * ember.DriftAmplitude * 0.5f * deltaTime;
            float lateral = wind * ember.Speed * deltaTime * Mathf.Lerp(0.25f, 1f, ember.Depth);
            ember.Position.x += drift + turbDrift + lateral;
            ember.Position.y -= ember.Speed * deltaTime;

            // Intelligent mouse attraction: existing embers are pulled toward the cursor (strongest near the bottom).
            if (_enableEmberMouseAttraction && _hasMouse)
            {
                Vector2 toMouse = _mousePosition - ember.Position;
                float dist = toMouse.magnitude;
                float radius = Mathf.Max(20f, _emberAttractRadius);
                if (dist < radius)
                {
                    float band = Mathf.InverseLerp(_screenHeight * 0.35f, _screenHeight * 1.05f, ember.Position.y);
                    float influence = (1f - (dist / radius));
                    influence *= influence;
                    influence *= band;
                    influence *= Mathf.Lerp(0.4f, 1f, ember.Depth);

                    Vector2 dir = dist > 0.001f ? (toMouse / dist) : Vector2.zero;
                    ember.Position.x += dir.x * _emberAttractStrength * influence * deltaTime;
                    ember.Position.y += dir.y * _emberAttractStrength * influence * deltaTime * _emberAttractVerticalInfluence;
                }
            }

            // Apply position and opacity
            ember.GlowElement.style.left = ember.Position.x - ember.GlowSize / 2f;
            ember.GlowElement.style.top = ember.Position.y - ember.GlowSize / 2f;
            ember.GlowElement.style.opacity = Mathf.Clamp01(opacity * 0.5f);  // Glow is softer
            ember.CoreElement.style.opacity = Mathf.Clamp01(opacity);

            // Reset if off screen or lifetime exceeded
            if (ember.Age >= ember.Lifetime || ember.Position.y < -50f)
            {
                ResetEmberPosition(ember);
            }
        }

        private void UpdateMicroSpark(MicroSparkParticle spark, float deltaTime, float turbulence)
        {
            spark.Age += deltaTime;

            float normalizedAge = spark.Age / spark.Lifetime;
            float opacity;

            // Very quick fade in, fast fade out
            if (normalizedAge < 0.05f)
            {
                opacity = normalizedAge / 0.05f;
            }
            else
            {
                opacity = 1f - Mathf.Pow(normalizedAge, 0.7f);
            }

            // Turbulent motion
            float turbX = Mathf.Sin(spark.Age * spark.TurbulenceFrequency + spark.TurbulencePhase) * 40f * deltaTime;
            float turbY = Mathf.Cos(spark.Age * spark.TurbulenceFrequency * 0.7f + spark.TurbulencePhase) * 20f * deltaTime;

            spark.Position.x += turbX + turbulence * 100f * deltaTime;
            spark.Position.y -= spark.Speed * deltaTime + turbY;

            spark.Element.style.left = spark.Position.x;
            spark.Element.style.top = spark.Position.y;
            spark.Element.style.opacity = Mathf.Clamp01(opacity * 0.85f);

            // Reset
            if (spark.Age >= spark.Lifetime || spark.Position.y < -20f)
            {
                ResetMicroSparkPosition(spark);
            }
        }

        private void UpdateSmoke(SmokeParticle smoke, float deltaTime, float wind)
        {
            smoke.Age += deltaTime;

            float normalizedAge = smoke.Age / smoke.Lifetime;
            float opacity;

            // Very slow fade in and out
            if (normalizedAge < 0.2f)
            {
                opacity = normalizedAge / 0.2f;
            }
            else if (normalizedAge > 0.7f)
            {
                opacity = (1f - normalizedAge) / 0.3f;
            }
            else
            {
                opacity = 1f;
            }

            // Slow drift
            float drift = Mathf.Sin(smoke.Age * 0.3f + smoke.DriftPhase) * smoke.DriftAmplitude * deltaTime;
            smoke.Position.x += drift + wind * smoke.Speed * 3f * deltaTime;
            smoke.Position.y -= smoke.Speed * deltaTime;

            // Expand slowly
            smoke.CurrentSize = smoke.Size * (1f + smoke.Age * smoke.ExpansionRate);

            smoke.Element.style.left = smoke.Position.x - smoke.CurrentSize / 2f;
            smoke.Element.style.top = smoke.Position.y - smoke.CurrentSize / 2f;
            smoke.Element.style.width = smoke.CurrentSize;
            smoke.Element.style.height = smoke.CurrentSize;
            smoke.Element.style.opacity = Mathf.Clamp01(opacity * 0.12f);  // Very subtle

            // Reset
            if (smoke.Age >= smoke.Lifetime || smoke.Position.y < -smoke.CurrentSize)
            {
                ResetSmokePosition(smoke);
            }
        }

        private void UpdateAsh(AshParticle ash, float deltaTime, float wind)
        {
            ash.Age += deltaTime;
            ash.Rotation += ash.RotationSpeed * deltaTime;

            // Calculate opacity
            float normalizedAge = ash.Age / ash.Lifetime;
            float opacity;

            if (normalizedAge < 0.1f)
            {
                opacity = normalizedAge / 0.1f;
            }
            else if (normalizedAge > 0.8f)
            {
                opacity = (1f - normalizedAge) / 0.2f;
            }
            else
            {
                opacity = 1f;
            }

            // Tumble effect - varies the visible size
            float tumble = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(ash.Age * ash.TumbleSpeed + ash.TumblePhase));

            // Move upward with wavy drift (ash floats more horizontally)
            float drift = Mathf.Sin(ash.Age * 0.8f + ash.DriftPhase) * ash.DriftAmplitude * deltaTime;
            ash.Position.x += drift + wind * ash.Speed * 2f * deltaTime;
            ash.Position.y -= ash.Speed * deltaTime;

            // Apply position, rotation, and scale
            ash.Element.style.left = ash.Position.x - ash.SizeX / 2f;
            ash.Element.style.top = ash.Position.y - ash.SizeY / 2f;
            ash.Element.style.opacity = opacity * 0.7f;
            ash.Element.style.rotate = new Rotate(ash.Rotation);
            ash.Element.style.scale = new Scale(new Vector2(1f, tumble));

            // Reset if off screen or lifetime exceeded
            if (ash.Age >= ash.Lifetime || ash.Position.y < -50f)
            {
                ResetAshPosition(ash);
            }
        }

        private void UpdateSpark(SparkParticle spark, float deltaTime)
        {
            spark.Age += deltaTime;

            // Sparks have a long dormant period then burst briefly
            float activePhase = spark.Age % (spark.Lifetime + 8f); // 8 second gap between sparks

            if (activePhase > spark.Lifetime)
            {
                spark.Element.style.opacity = 0;
                return;
            }

            float normalizedAge = activePhase / spark.Lifetime;
            float opacity;

            if (normalizedAge < 0.1f)
            {
                opacity = normalizedAge / 0.1f;
            }
            else
            {
                opacity = 1f - normalizedAge;
            }

            // Move in direction
            spark.Position += spark.Direction * spark.Speed * deltaTime;

            // Gravity pulls down slightly
            spark.Direction.y += 2f * deltaTime;
            spark.Direction = spark.Direction.normalized;

            spark.Element.style.left = spark.Position.x;
            spark.Element.style.top = spark.Position.y;
            spark.Element.style.opacity = opacity;

            // Reset when done
            if (normalizedAge >= 1f)
            {
                ResetSparkPosition(spark);
            }
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Set ember intensity (0-2). Default is 1.
        /// </summary>
        public void SetIntensity(float intensity)
        {
            intensity = Mathf.Clamp(intensity, 0f, 2f);
            foreach (var ember in _embers)
            {
                ember.Speed = UnityEngine.Random.Range(_emberSpeedMin, _emberSpeedMax) * intensity;
            }
        }

        /// <summary>
        /// Notify the VFX system that a menu button is being hovered.
        /// Boosts logo aura intensity for reactive feedback.
        /// </summary>
        public void OnButtonHovered(bool isHovered, bool isPrimary)
        {
            _logoButtonHoverBoost = isHovered ? (isPrimary ? 1.0f : 0.5f) : 0f;
        }

        /// <summary>
        /// Trigger a burst of sparks at a position.
        /// </summary>
        public void SparkBurst(Vector2 position, int count = 5)
        {
            int spawned = 0;
            foreach (var spark in _sparks)
            {
                if (spawned >= count) break;
                spark.Position = position;
                spark.Age = 0;
                spark.Direction = new Vector2(
                    UnityEngine.Random.Range(-0.5f, 0.5f),
                    UnityEngine.Random.Range(-1f, -0.5f)
                ).normalized;
                spawned++;
            }
        }

        // =============================================================================
        // NESTED TYPES
        // =============================================================================

        private class EmberParticle
        {
            public VisualElement GlowElement;
            public VisualElement CoreElement;
            public Vector2 Position;
            public float Size;
            public float GlowSize;
            public float Depth;
            public float OpacityScale;
            public float Speed;
            public float Lifetime;
            public float Age;
            public float FlickerPhase;
            public float DriftPhase;
            public float DriftAmplitude;
        }

        private class AshParticle
        {
            public VisualElement Element;
            public Vector2 Position;
            public float SizeX;
            public float SizeY;
            public float Speed;
            public float Lifetime;
            public float Age;
            public float Rotation;
            public float RotationSpeed;
            public float DriftPhase;
            public float DriftAmplitude;
            public float TumblePhase;
            public float TumbleSpeed;
        }

        private class SparkParticle
        {
            public VisualElement Element;
            public Vector2 Position;
            public Vector2 Direction;
            public float Size;
            public float Speed;
            public float Lifetime;
            public float Age;
        }

        private class MicroSparkParticle
        {
            public VisualElement Element;
            public Vector2 Position;
            public float Size;
            public float Speed;
            public float Lifetime;
            public float Age;
            public float TurbulencePhase;
            public float TurbulenceFrequency;
        }

        private class SmokeParticle
        {
            public VisualElement Element;
            public Vector2 Position;
            public float Size;
            public float CurrentSize;
            public float Speed;
            public float Lifetime;
            public float Age;
            public float DriftPhase;
            public float DriftAmplitude;
            public float ExpansionRate;
        }
    }
}
