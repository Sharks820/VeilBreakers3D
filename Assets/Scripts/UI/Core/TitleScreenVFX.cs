using System;
using System.Collections;
using UnityEngine;
using VeilBreakers.Core;
using UnityEngine.UIElements;
using VeilBreakers.Managers;
using VeilBreakers.UI.VFX;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// AAA Title Screen VFX Orchestrator.
    /// Slim coordinator that delegates to 5 focused subsystems:
    ///   - TitleScreenParticles (embers, ash, sparks, smoke, burst particles)
    ///   - TitleScreenLightning (lightning strikes, flash overlay)
    ///   - TitleScreenLogoVFX (logo glow, pulse, aura, shadow)
    ///   - TitleScreenAtmosphere (vignette, grunge, fog)
    ///   - TitleScreenVideoBackground (ping-pong video loop)
    ///
    /// Public API preserved: StartVFX, StopVFX, SetIntensity, OnButtonHovered, SparkBurst
    /// </summary>
    public class TitleScreenVFX : MonoBehaviour
    {
        // =============================================================================
        // UI DOCUMENT
        // =============================================================================

        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        // =============================================================================
        // SUBSYSTEM REFERENCES
        // =============================================================================

        [Header("VFX Subsystems")]
        [SerializeField] private TitleScreenParticles _particles;
        [SerializeField] private TitleScreenLightning _lightning;
        [SerializeField] private TitleScreenLogoVFX _logo;
        [SerializeField] private TitleScreenAtmosphere _atmosphere;
        [SerializeField] private TitleScreenVideoBackground _videoBackground;

        // =============================================================================
        // MENU MUSIC (kept here -- TitleScreenAudio handles most audio)
        // =============================================================================

        [Header("Menu Music")]
        [SerializeField] private AudioClip _menuMusic;
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.7f;
        [SerializeField] private float _musicFadeInDuration = 2f;
        private AudioSource _audioSource;
        private Coroutine _musicFadeCoroutine;
        private SettingsManager _settingsManager;
        private Coroutine _settingsBindCoroutine;
        private bool _isSettingsBound;

        // =============================================================================
        // ORCHESTRATOR STATE
        // =============================================================================

        private VisualElement _vfxContainer;
        private VisualElement _frontVfxContainer;
        private VisualElement _smokeLayer;
        private VisualElement _host;
        private VisualElement _monsterElement;
        private VisualElement _logoContainer;
        private VisualElement _logoImage;
        private bool _isActive;
        private Coroutine _updateCoroutine;
        private float _screenWidth;
        private float _screenHeight;
        private Vector2 _mousePosition;
        private bool _hasMouse;
        private bool _isDestroyed;
        private bool _initializeQueued;
        private Coroutine _deferredInitializeCoroutine;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            LoadTexturesFromResources();
        }

        private void Start()
        {
            // DISABLE ALL SHADOW/VIGNETTE OVERLAYS
            _atmosphere._enableAtmosphereGradient = false;
            _atmosphere._enableVignette = false;

            // ENABLE VIDEO BACKGROUND
            _videoBackground._useVideoBackground = true;

            // PARTICLES SPAWN FROM FULL SCREEN (no margins)
            _particles._spawnMarginSides = 0f;
            _particles._spawnMarginBottom = 0f;

            // Keep visual density high but avoid first-frame spikes.
            _particles._emberCount = Mathf.Clamp(_particles._emberCount, 80, 220);
            _particles._microSparkCount = Mathf.Clamp(_particles._microSparkCount, 20, 80);
            _particles._ashCount = Mathf.Clamp(_particles._ashCount, 10, 36);
            _particles._smokeCount = Mathf.Clamp(_particles._smokeCount, 0, 8);
            _particles._sparkCount = Mathf.Clamp(_particles._sparkCount, 0, 16);

            // Match particle colors to fiery red/orange portal video
            _particles._emberColorCore = new Color(1f, 0.6f, 0.2f, 1f);
            _particles._emberColorBody = new Color(0.9f, 0.25f, 0.05f, 0.85f);
            _particles._emberColorGlow = new Color(0.8f, 0.1f, 0f, 0.5f);

            // CURSOR ATTRACTION - embers follow the mouse
            _particles._enableEmberMouseAttraction = true;
            _particles._emberAttractRadius = 400f;
            _particles._emberAttractStrength = 350f;
            _particles._emberAttractVerticalInfluence = 0.8f;

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

            _lightning.Cleanup();
            _videoBackground.MarkDestroyed();
            _videoBackground.Cleanup();

            if (_audioSource != null)
            {
                _audioSource.Stop();
            }
        }

        // =============================================================================
        // TEXTURE LOADING (distributes to subsystems)
        // =============================================================================

        private void LoadTexturesFromResources()
        {
            // Particle textures
            if (_particles._smokeTexture == null)
            {
                _particles._smokeTexture = Resources.Load<Texture2D>("VFX/ParticleTextures/smoke_kenney");
                if (_particles._smokeTexture == null)
                    _particles._smokeTexture = Resources.Load<Texture2D>("VFX/ParticleTextures/smoke");
                if (_particles._smokeTexture == null)
                    _particles._smokeTexture = Resources.Load<Texture2D>("VFX/ParticleTextures/smoke 2");
            }

            if (_particles._ashTexture == null)
            {
                _particles._ashTexture = Resources.Load<Texture2D>("VFX/ParticleTextures/dust");
                if (_particles._ashTexture == null)
                    _particles._ashTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/ash_particles");
            }

            if (_particles._emberTexture == null)
            {
                _particles._emberTexture = Resources.Load<Texture2D>("VFX/ParticleTextures/dirt 2");
                if (_particles._emberTexture == null)
                    _particles._emberTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/ember_particles");
            }

            // Atmosphere textures
            if (_atmosphere._grungeTexture == null)
            {
                _atmosphere._grungeTexture = Resources.Load<Texture2D>("VFX/ParticleTextures/grunge crack");
                if (_atmosphere._grungeTexture == null)
                    _atmosphere._grungeTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/vignette_overlay");
            }

            // Lightning textures
            if (_lightning._lightningBoltTextureA == null)
            {
                _lightning._lightningBoltTextureA = Resources.Load<Texture2D>("UI/Lightning/lightning_bolt_01");
            }

            if (_lightning._lightningBoltTextureB == null)
            {
                _lightning._lightningBoltTextureB = Resources.Load<Texture2D>("UI/Lightning/lightning_bolt_04");
            }

            // Logo textures
            if (_logo._logoGlowTexture == null)
            {
                _logo._logoGlowTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/logo_veilbreakers_glow");
            }

            // Video background textures
            if (_videoBackground._backgroundPortalTexture == null && _videoBackground._overrideBackgroundWithPortal)
            {
                _videoBackground._backgroundPortalTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/mainmenu_background_portal");
            }

            ErrorLogger.Log($"[TitleScreenVFX] Textures loaded - Smoke: {_particles._smokeTexture != null}, Ash: {_particles._ashTexture != null}, Ember: {_particles._emberTexture != null}, Grunge: {_atmosphere._grungeTexture != null}");
        }

        // =============================================================================
        // DEFERRED INITIALIZATION
        // =============================================================================

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (_isActive || _vfxContainer != null || _initializeQueued) return;

            _initializeQueued = true;
            _deferredInitializeCoroutine ??= StartCoroutine(InitializeDeferred());
        }

        private IEnumerator InitializeDeferred()
        {
            yield return null;
            yield return null;

            if (_isDestroyed || _isActive || _vfxContainer != null)
            {
                _initializeQueued = false;
                _deferredInitializeCoroutine = null;
                yield break;
            }

            yield return StartCoroutine(InitializeStaggered());
            _initializeQueued = false;
            _deferredInitializeCoroutine = null;
        }

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

            // Background
            var backgroundElement = host.Q<VisualElement>("background");
            _videoBackground.Initialize(backgroundElement);
            _videoBackground.StartPlayback();

            _atmosphere.CreateTopVignette(host);

            // Atmosphere layers (disabled by Start override, but call anyway)
            _atmosphere.Initialize(_vfxContainer);

            // Lightning
            if (_lightning._enableLightning)
            {
                _lightning.Initialize(_vfxContainer, _screenWidth, _screenHeight);
            }

            // Smoke layer
            _smokeLayer = new VisualElement();
            _smokeLayer.name = "smoke-layer";
            _smokeLayer.style.position = Position.Absolute;
            _smokeLayer.style.left = 0;
            _smokeLayer.style.top = 0;
            _smokeLayer.style.right = 0;
            _smokeLayer.style.bottom = 0;
            _smokeLayer.pickingMode = PickingMode.Ignore;
            _vfxContainer.Add(_smokeLayer);

            // Initialize particles with references
            _particles.Initialize(_vfxContainer, _frontVfxContainer, _smokeLayer, _host, _monsterElement, _screenWidth, _screenHeight);

            yield return null;
            if (_isDestroyed) yield break;

            // --- Frame 2: Smoke + Ash ---
            _particles.CreateSmokeBatch();
            _particles.CreateAshBatch();

            yield return null;
            if (_isDestroyed) yield break;

            // --- Frames 3+: Embers in batches of 30 ---
            const int kEmberBatch = 30;
            int totalEmbers = _particles.EmberCount;
            for (int i = 0; i < totalEmbers; i += kEmberBatch)
            {
                _particles.CreateEmberBatch(i, kEmberBatch);
                if (i + kEmberBatch < totalEmbers)
                {
                    yield return null;
                    if (_isDestroyed) yield break;
                }
            }

            yield return null;
            if (_isDestroyed) yield break;

            // --- Next frame: Micro-sparks + Sparks ---
            _particles.CreateMicroSparkBatch();
            _particles.CreateSparkBatch();

            yield return null;
            if (_isDestroyed) yield break;

            // Initialize logo
            _logo.Initialize(_host, _logoContainer, _logoImage, _particles);

            StartVFX();
            ErrorLogger.Log($"[TitleScreenVFX] Staggered VFX init complete: {_particles.EmberCount} embers, {_particles._microSparkCount} micro-sparks, {_particles._ashCount} ash, {_particles._smokeCount} smoke, {_particles._sparkCount} sparks");
        }

        // =============================================================================
        // HIERARCHY HELPERS
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

        // =============================================================================
        // INTERACTIVE TARGETS
        // =============================================================================

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

        // =============================================================================
        // INPUT CALLBACKS
        // =============================================================================

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!_particles._enableClickMonsterBurst) return;
            if (evt.button != 0) return;

            if (evt.target is Button) return;

            if (_monsterElement != null && evt.target is VisualElement clicked)
            {
                if (!IsSelfOrChildOf(clicked, _monsterElement)) return;
            }

            _particles.SpawnMonsterBurstAtMonster();
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
            _particles.SetMousePosition(_mousePosition, _hasMouse);
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            _hasMouse = false;
            _particles.SetMousePosition(_mousePosition, _hasMouse);
        }

        private void OnLogoEnter(MouseEnterEvent evt)
        {
            _logo._logoHover = true;
        }

        private void OnLogoLeave(MouseLeaveEvent evt)
        {
            _logo._logoHover = false;
        }

        private void OnLogoPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            _logo.OnLogoPointerDown();
            evt.StopPropagation();
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

                _particles.UpdateAll(deltaTime);
                _lightning.UpdateLightning(deltaTime);
                _logo.UpdateLogo(deltaTime);
                _atmosphere.UpdateFog(deltaTime);

                // Particle parallax response to cursor position
                if (_hasMouse && _screenWidth > 0 && _screenHeight > 0)
                {
                    float normalizedX = (_mousePosition.x - _screenWidth * 0.5f) / (_screenWidth * 0.5f);
                    float normalizedY = (_mousePosition.y - _screenHeight * 0.5f) / (_screenHeight * 0.5f);

                    if (_vfxContainer != null)
                    {
                        float bgOffsetX = -normalizedX * 8f;
                        float bgOffsetY = normalizedY * 5f;
                        _vfxContainer.style.translate = new Translate(bgOffsetX, bgOffsetY);
                    }
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

        // =============================================================================
        // PUBLIC API (preserved from original)
        // =============================================================================

        /// <summary>
        /// Set ember intensity (0-2). Default is 1.
        /// </summary>
        public void SetIntensity(float intensity)
        {
            _particles.SetIntensity(intensity);
        }

        /// <summary>
        /// Notify the VFX system that a menu button is being hovered.
        /// Boosts logo aura intensity for reactive feedback.
        /// </summary>
        public void OnButtonHovered(bool isHovered, bool isPrimary)
        {
            _logo.OnButtonHovered(isHovered, isPrimary);
        }

        /// <summary>
        /// Trigger a burst of sparks at a position.
        /// </summary>
        public void SparkBurst(Vector2 position, int count = 5)
        {
            _particles.SparkBurst(position, count);
        }

        // =============================================================================
        // SETTINGS BINDING
        // =============================================================================

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
            if (settings == null) return _musicVolume;
            if (settings.MuteAll) return 0f;
            return Mathf.Clamp01(settings.MasterVolume) * Mathf.Clamp01(settings.MusicVolume);
        }
    }
}
