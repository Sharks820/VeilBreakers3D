using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
        [SerializeField] private int _emberCount = 70;  // Gemini: 60-80
        [SerializeField] private float _emberSpeedMin = 20f;
        [SerializeField] private float _emberSpeedMax = 55f;
        [SerializeField] private float _emberSizeMin = 3f;
        [SerializeField] private float _emberSizeMax = 8f;
        // AAA Colors: #FFF5E1 (hot core) → #FF4400 (body) → #8A0303 (outer)
        [SerializeField] private Color _emberColorCore = new Color(1f, 0.96f, 0.88f, 1f);       // #FFF5E1
        [SerializeField] private Color _emberColorBody = new Color(1f, 0.27f, 0f, 0.9f);        // #FF4400
        [SerializeField] private Color _emberColorGlow = new Color(0.26f, 0f, 0f, 0.4f);        // #420000 blood red bloom

        [Header("Micro-Spark Settings (AAA)")]
        [SerializeField] private int _microSparkCount = 120;  // Gemini: 200-300, but UI Toolkit has limits
        [SerializeField] private float _microSparkSpeedMin = 80f;
        [SerializeField] private float _microSparkSpeedMax = 180f;
        [SerializeField] private float _microSparkSizeMin = 1f;
        [SerializeField] private float _microSparkSizeMax = 2.5f;

        [Header("Ash Settings (AAA Palette)")]
        [SerializeField] private int _ashCount = 100;  // Gemini: 120-150
        [SerializeField] private float _ashSpeedMin = 6f;
        [SerializeField] private float _ashSpeedMax = 18f;
        [SerializeField] private float _ashSizeMin = 4f;
        [SerializeField] private float _ashSizeMax = 12f;
        // AAA Colors: #363636 to #121212 (dark grey charcoal)
        [SerializeField] private Color _ashColorLight = new Color(0.21f, 0.21f, 0.21f, 0.5f);   // #363636
        [SerializeField] private Color _ashColorDark = new Color(0.07f, 0.07f, 0.07f, 0.4f);    // #121212

        [Header("Smoke Wisp Settings (AAA)")]
        [SerializeField] private int _smokeCount = 18;  // Gemini: 15-20
        [SerializeField] private float _smokeSpeedMin = 3f;
        [SerializeField] private float _smokeSpeedMax = 8f;
        [SerializeField] private float _smokeSizeMin = 80f;
        [SerializeField] private float _smokeSizeMax = 200f;
        // AAA: #0A0A0A near black with very low opacity
        [SerializeField] private Color _smokeColor = new Color(0.04f, 0.03f, 0.03f, 0.12f);

        [Header("Spark Burst Settings")]
        [SerializeField] private int _sparkCount = 25;
        [SerializeField] private float _sparkSpeedMin = 100f;
        [SerializeField] private float _sparkSpeedMax = 200f;

        [Header("Atmospheric Layers")]
        [SerializeField] private bool _enableVignette = true;
        [SerializeField] private float _vignetteOpacity = 0.75f;  // Gemini: 0.8 at corners
        [SerializeField] private bool _enableAtmosphereGradient = true;
        [SerializeField] private Color _atmosphereColor = new Color(0.1f, 0f, 0f, 0.3f);  // Dark red #1A0000

        [Header("Animation")]
        [SerializeField] private float _windStrength = 0.4f;
        [SerializeField] private float _windFrequency = 0.4f;
        [SerializeField] private float _flickerSpeed = 10f;
        [SerializeField] private float _turbulenceStrength = 0.15f;  // New: adds chaos to movement

        [Header("Spawn Area")]
        [SerializeField] private float _spawnMarginBottom = 0.15f;
        [SerializeField] private float _spawnMarginSides = 0.1f;

        [Header("Particle Textures (Assign in Inspector)")]
        [Tooltip("Smoke texture for smoke wisps - use smoke.png or smoke 2.png")]
        [SerializeField] private Texture2D _smokeTexture;
        [Tooltip("Dust/ash texture for ash particles - use dust.png")]
        [SerializeField] private Texture2D _ashTexture;
        [Tooltip("Ember/spark texture - use dirt 2.png for gritty embers")]
        [SerializeField] private Texture2D _emberTexture;
        [Tooltip("Grunge overlay texture - use grunge crack.png")]
        [SerializeField] private Texture2D _grungeTexture;

        // =============================================================================
        // STATE
        // =============================================================================

        private VisualElement _vfxContainer;
        private VisualElement _atmosphereLayer;
        private VisualElement _vignetteLayer;
        private VisualElement _smokeLayer;
        private readonly List<EmberParticle> _embers = new();
        private readonly List<AshParticle> _ashes = new();
        private readonly List<SparkParticle> _sparks = new();
        private readonly List<MicroSparkParticle> _microSparks = new();
        private readonly List<SmokeParticle> _smokes = new();
        private bool _isActive;
        private Coroutine _updateCoroutine;
        private float _windOffset;
        private float _turbulenceOffset;
        private float _screenWidth;
        private float _screenHeight;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Start()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            if (_uiDocument != null)
            {
                _uiDocument.rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (!_isActive && _vfxContainer == null)
            {
                Initialize();
            }
        }

        private void OnEnable()
        {
            if (_vfxContainer != null)
            {
                StartVFX();
            }
        }

        private void OnDisable()
        {
            StopVFX();
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void Initialize()
        {
            if (_uiDocument == null) return;

            var root = _uiDocument.rootVisualElement;

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

            // Insert behind other elements
            root.Insert(0, _vfxContainer);

            _screenWidth = root.resolvedStyle.width;
            _screenHeight = root.resolvedStyle.height;

            if (_screenWidth <= 0) _screenWidth = 1920;
            if (_screenHeight <= 0) _screenHeight = 1080;

            // === AAA ATMOSPHERIC LAYERS ===

            // 1. Atmosphere gradient layer (radial dark red)
            if (_enableAtmosphereGradient)
            {
                CreateAtmosphereLayer();
            }

            // 2. Smoke layer (behind particles)
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

            // 3. Create ash particles (behind embers)
            for (int i = 0; i < _ashCount; i++)
            {
                CreateAsh();
            }

            // 4. Create embers
            for (int i = 0; i < _emberCount; i++)
            {
                CreateEmber();
            }

            // 5. Create micro-sparks (fast turbulent particles)
            for (int i = 0; i < _microSparkCount; i++)
            {
                CreateMicroSpark();
            }

            // 6. Create sparks (burst particles)
            for (int i = 0; i < _sparkCount; i++)
            {
                CreateSpark();
            }

            // 7. Vignette overlay (on top of everything)
            if (_enableVignette)
            {
                CreateVignetteLayer();
            }

            // 8. Grunge overlay (film grain / grit effect)
            if (_grungeTexture != null)
            {
                CreateGrungeOverlay();
            }

            StartVFX();
            Debug.Log($"[TitleScreenVFX] AAA VFX Initialized: {_emberCount} embers, {_microSparkCount} micro-sparks, {_ashCount} ash, {_smokeCount} smoke, {_sparkCount} sparks (Textures: {(_smokeTexture != null ? "smoke " : "")}{(_ashTexture != null ? "ash " : "")}{(_emberTexture != null ? "ember " : "")}{(_grungeTexture != null ? "grunge" : "")})");
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
            grungeLayer.style.unityBackgroundScaleMode = ScaleMode.ScaleAndCrop;
            grungeLayer.style.unityBackgroundImageTintColor = new Color(1f, 1f, 1f, 0.08f);  // Very subtle
            grungeLayer.style.opacity = 0.15f;  // Film grain level
            grungeLayer.pickingMode = PickingMode.Ignore;

            _vfxContainer.Add(grungeLayer);
        }

        // =============================================================================
        // PARTICLE CREATION
        // =============================================================================

        private void CreateEmber()
        {
            // AAA: Multi-layer ember with outer glow, body, and hot core

            // Outer blood-red glow (largest, softest)
            var glow = new VisualElement();
            glow.style.position = Position.Absolute;
            glow.style.borderTopLeftRadius = Length.Percent(50);
            glow.style.borderTopRightRadius = Length.Percent(50);
            glow.style.borderBottomLeftRadius = Length.Percent(50);
            glow.style.borderBottomRightRadius = Length.Percent(50);
            glow.pickingMode = PickingMode.Ignore;

            // Middle body layer (orange/red)
            var body = new VisualElement();
            body.style.position = Position.Absolute;
            body.style.borderTopLeftRadius = Length.Percent(50);
            body.style.borderTopRightRadius = Length.Percent(50);
            body.style.borderBottomLeftRadius = Length.Percent(50);
            body.style.borderBottomRightRadius = Length.Percent(50);
            body.pickingMode = PickingMode.Ignore;
            glow.Add(body);

            // Inner hot core (brightest)
            var core = new VisualElement();
            core.style.position = Position.Absolute;
            core.style.borderTopLeftRadius = Length.Percent(50);
            core.style.borderTopRightRadius = Length.Percent(50);
            core.style.borderBottomLeftRadius = Length.Percent(50);
            core.style.borderBottomRightRadius = Length.Percent(50);
            core.pickingMode = PickingMode.Ignore;
            body.Add(core);

            _vfxContainer.Add(glow);

            var size = UnityEngine.Random.Range(_emberSizeMin, _emberSizeMax);
            var bodySize = size * 2f;
            var glowSize = size * 4f;

            var ember = new EmberParticle
            {
                GlowElement = glow,
                CoreElement = core,
                Size = size,
                GlowSize = glowSize,
                Speed = UnityEngine.Random.Range(_emberSpeedMin, _emberSpeedMax),
                Lifetime = UnityEngine.Random.Range(5f, 10f),
                Age = UnityEngine.Random.Range(0f, 5f),
                FlickerPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                DriftPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                DriftAmplitude = UnityEngine.Random.Range(25f, 60f)
            };

            ResetEmberPosition(ember);

            // AAA Style: Core (hot white/yellow center)
            core.style.width = size;
            core.style.height = size;
            core.style.backgroundColor = _emberColorCore;
            core.style.left = (bodySize - size) / 2f;
            core.style.top = (bodySize - size) / 2f;

            // AAA Style: Body (orange/red) - USE TEXTURE if assigned
            body.style.width = bodySize;
            body.style.height = bodySize;
            if (_emberTexture != null)
            {
                body.style.backgroundImage = new StyleBackground(_emberTexture);
                body.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                body.style.unityBackgroundImageTintColor = _emberColorBody;
            }
            else
            {
                body.style.backgroundColor = _emberColorBody;
            }
            body.style.left = (glowSize - bodySize) / 2f;
            body.style.top = (glowSize - bodySize) / 2f;

            // AAA Style: Outer glow (deep blood red) - USE TEXTURE if assigned
            glow.style.width = glowSize;
            glow.style.height = glowSize;
            if (_emberTexture != null)
            {
                glow.style.backgroundImage = new StyleBackground(_emberTexture);
                glow.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                glow.style.unityBackgroundImageTintColor = _emberColorGlow;
            }
            else
            {
                glow.style.backgroundColor = _emberColorGlow;
            }

            _embers.Add(ember);
        }

        private void CreateAsh()
        {
            var element = new VisualElement();
            element.style.position = Position.Absolute;
            element.pickingMode = PickingMode.Ignore;

            // USE ACTUAL TEXTURE if assigned
            if (_ashTexture != null)
            {
                element.style.backgroundImage = new StyleBackground(_ashTexture);
                element.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
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

            // Larger size when using textures for better visibility
            var sizeX = UnityEngine.Random.Range(_ashSizeMin * 2f, _ashSizeMax * 2.5f);
            var sizeY = UnityEngine.Random.Range(_ashSizeMin * 1.5f, _ashSizeMax * 2f);

            var ash = new AshParticle
            {
                Element = element,
                SizeX = sizeX,
                SizeY = sizeY,
                Speed = UnityEngine.Random.Range(_ashSpeedMin, _ashSpeedMax),
                Lifetime = UnityEngine.Random.Range(8f, 16f),
                Age = UnityEngine.Random.Range(0f, 8f),
                RotationSpeed = UnityEngine.Random.Range(-120f, 120f),
                Rotation = UnityEngine.Random.Range(0f, 360f),
                DriftPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                DriftAmplitude = UnityEngine.Random.Range(40f, 100f),
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
            element.style.position = Position.Absolute;
            element.pickingMode = PickingMode.Ignore;

            // Use ember texture for micro-sparks if available
            if (_emberTexture != null)
            {
                element.style.backgroundImage = new StyleBackground(_emberTexture);
                element.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
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

            // Slightly larger when using textures
            var size = _emberTexture != null
                ? UnityEngine.Random.Range(_microSparkSizeMin * 2f, _microSparkSizeMax * 3f)
                : UnityEngine.Random.Range(_microSparkSizeMin, _microSparkSizeMax);

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
            element.style.position = Position.Absolute;
            element.pickingMode = PickingMode.Ignore;

            // USE ACTUAL SMOKE TEXTURE if assigned
            if (_smokeTexture != null)
            {
                element.style.backgroundImage = new StyleBackground(_smokeTexture);
                element.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
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

            // Larger smoke puffs when using textures
            var size = UnityEngine.Random.Range(_smokeSizeMin * 1.5f, _smokeSizeMax * 2f);

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
            float marginX = _screenWidth * _spawnMarginSides;
            ember.Position = new Vector2(
                UnityEngine.Random.Range(marginX, _screenWidth - marginX),
                _screenHeight + UnityEngine.Random.Range(20f, 100f)
            );
            ember.Age = 0;
        }

        private void ResetAshPosition(AshParticle ash)
        {
            float marginX = _screenWidth * _spawnMarginSides;
            ash.Position = new Vector2(
                UnityEngine.Random.Range(marginX, _screenWidth - marginX),
                _screenHeight + UnityEngine.Random.Range(20f, 80f)
            );
            ash.Age = 0;
        }

        private void ResetSparkPosition(SparkParticle spark)
        {
            float marginX = _screenWidth * _spawnMarginSides;
            float bottomArea = _screenHeight * _spawnMarginBottom;
            spark.Position = new Vector2(
                UnityEngine.Random.Range(marginX, _screenWidth - marginX),
                _screenHeight - UnityEngine.Random.Range(0f, bottomArea)
            );
            spark.Age = 0;
            spark.Direction = new Vector2(
                UnityEngine.Random.Range(-0.4f, 0.4f),
                UnityEngine.Random.Range(-1f, -0.6f)
            ).normalized;
        }

        private void ResetMicroSparkPosition(MicroSparkParticle spark)
        {
            // Micro-sparks spawn anywhere on screen for ambient effect
            spark.Position = new Vector2(
                UnityEngine.Random.Range(0f, _screenWidth),
                _screenHeight + UnityEngine.Random.Range(10f, 50f)
            );
            spark.Age = 0;
        }

        private void ResetSmokePosition(SmokeParticle smoke)
        {
            // Smoke spawns from bottom and sides
            float spawnSide = UnityEngine.Random.Range(0f, 1f);
            if (spawnSide < 0.7f)
            {
                // Spawn from bottom
                smoke.Position = new Vector2(
                    UnityEngine.Random.Range(-smoke.Size * 0.5f, _screenWidth + smoke.Size * 0.5f),
                    _screenHeight + smoke.Size * 0.5f
                );
            }
            else if (spawnSide < 0.85f)
            {
                // Spawn from left
                smoke.Position = new Vector2(
                    -smoke.Size * 0.3f,
                    UnityEngine.Random.Range(_screenHeight * 0.3f, _screenHeight)
                );
            }
            else
            {
                // Spawn from right
                smoke.Position = new Vector2(
                    _screenWidth + smoke.Size * 0.3f,
                    UnityEngine.Random.Range(_screenHeight * 0.3f, _screenHeight)
                );
            }
            smoke.Age = 0;
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

                yield return null;
            }
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

            // AAA flicker effect - more complex pattern
            float flicker1 = Mathf.Sin(ember.Age * _flickerSpeed + ember.FlickerPhase);
            float flicker2 = Mathf.Sin(ember.Age * _flickerSpeed * 2.3f + ember.FlickerPhase * 1.7f);
            float flicker = 0.6f + 0.25f * flicker1 + 0.15f * flicker2;
            opacity *= flicker;

            // Move upward with drift and turbulence
            float drift = Mathf.Sin(ember.Age * 1.5f + ember.DriftPhase) * ember.DriftAmplitude * deltaTime;
            float turbDrift = turbulence * ember.DriftAmplitude * 0.5f * deltaTime;
            ember.Position.x += drift + turbDrift + wind * ember.Speed * deltaTime;
            ember.Position.y -= ember.Speed * deltaTime;

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
