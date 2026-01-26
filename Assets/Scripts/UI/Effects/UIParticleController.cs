using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Effects
{
    /// <summary>
    /// Controls animated particle effects for UI Toolkit menus.
    /// Creates floating embers, dust particles, and veil pulse effects.
    /// </summary>
    /// <summary>
    /// Controls animated particle effects for UI Toolkit menus.
    /// Creates dark ominous particles: crimson embers, lightning sparks, and veil pulses.
    /// OPTIMIZED: Object pooling, no GC allocations in Update loop.
    /// </summary>
    public class UIParticleController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Particle Counts")]
        [SerializeField] private int _emberCount = 18;
        [SerializeField] private int _dustCount = 30;
        [SerializeField] private int _sparkCount = 12;
        [SerializeField] private int _lightningBoltCount = 5;

        [Header("Particle Speeds")]
        [SerializeField] private float _emberSpeed = 25f;
        [SerializeField] private float _dustSpeed = 12f;
        [SerializeField] private float _sparkSpeed = 80f;
        [SerializeField] private float _veilPulseSpeed = 0.4f;

        [Header("Dark Crimson Theme - Ominous")]
        [SerializeField] private Color _emberColor = new Color(0.50f, 0.12f, 0.18f, 0.80f);      // Dark crimson ember (slightly brighter)
        [SerializeField] private Color _emberGlowColor = new Color(0.85f, 0.25f, 0.35f, 0.75f); // Vivid crimson glow
        [SerializeField] private Color _dustColor = new Color(0.20f, 0.12f, 0.15f, 0.25f);      // Very dark dust
        [SerializeField] private Color _sparkColor = new Color(0.95f, 0.35f, 0.45f, 0.95f);     // Brighter crimson spark
        [SerializeField] private Color _lightningColor = new Color(0.98f, 0.40f, 0.50f, 1.0f);  // Vivid lightning bolt
        [SerializeField] private Color _veilColor = new Color(0.50f, 0.12f, 0.18f, 0.05f);      // Veil wash (darker)

        // =============================================================================
        // PRIVATE STATE
        // =============================================================================

        private VisualElement _root;
        private VisualElement _particleContainer;
        private VisualElement _emberContainer;
        private VisualElement _sparkContainer;
        private List<VisualElement> _veilPulses;
        private List<ParticleData> _embers;
        private List<ParticleData> _dustParticles;
        private List<ParticleData> _sparks;
        private List<LightningData> _lightningBolts;
        private float _screenWidth;
        private float _screenHeight;
        private bool _isInitialized;
        
        // Object pooling for optimization
        private Queue<VisualElement> _particlePool;
        private bool _poolInitialized;
        private const int kPoolSize = 150; // Increased for more particles

        // Lightning timing
        private float _nextLightningTime;
        private const float kLightningInterval = 2.5f; // More frequent lightning flashes
        private const int kMaxLightningSegments = 24; // More segments for smoother paths
        private const float kLightningMinSegLength = 18f;
        private const float kLightningMaxSegLength = 55f;

        // AAA QUALITY: Much thicker, more dramatic lightning
        private const float kLightningMinThickness = 8f;   // Increased from 1.2 → 8 (6.7x thicker!)
        private const float kLightningMaxThickness = 18f;  // Increased from 3 → 18 (6x thicker!)
        private const float kLightningGlowPadding = 20f;   // Increased from 7 → 20 (3x bigger glow!)
        private const float kLightningMaxAngleJitter = 3.5f; // More dramatic jitter

        // Branch parameters
        private const float kBranchMinThickness = 3f;
        private const float kBranchMaxThickness = 9f;

        // =============================================================================
        // DATA STRUCTURES
        // =============================================================================

        private enum ParticleType
        {
            Ember,
            Dust,
            Spark,
            Lightning
        }

        private class ParticleData
        {
            public VisualElement Element;
            public ParticleType Type;
            public float X;
            public float Y;
            public float SpeedX;
            public float SpeedY;
            public float Size;
            public float Alpha;
            public float AlphaSpeed;
            public float Rotation;
            public float RotationSpeed;
            public bool Active;
        }

        private class LightningData
        {
            public VisualElement Root;
            public List<LightningSegment> Segments;
            public float Lifetime;
            public float MaxLifetime;
            public bool Active;
        }

        private class LightningSegment
        {
            public VisualElement Container;
            public VisualElement Glow;
            public VisualElement Core;
            public float BaseAlpha;
            public float FlickerSpeed;
            public float FlickerOffset;
            public float BaseX;
            public float BaseY;
            public float BaseLength;
            public float BaseThickness;
            public float BaseAngle;
        }

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            // Initialize collections (pre-sized for no reallocation)
            _embers = new List<ParticleData>(_emberCount);
            _dustParticles = new List<ParticleData>(_dustCount);
            _sparks = new List<ParticleData>(_sparkCount);
            _lightningBolts = new List<LightningData>(_lightningBoltCount);
            _veilPulses = new List<VisualElement>(3);
            _particlePool = new Queue<VisualElement>(kPoolSize);
            
            _nextLightningTime = Time.unscaledTime + Random.Range(2f, 4f);
        }

        private void OnEnable()
        {
            if (_uiDocument != null && _uiDocument.rootVisualElement != null)
            {
                Initialize();
            }
        }

        private void Update()
        {
            if (!_isInitialized) return;

            float deltaTime = Time.unscaledDeltaTime;
            float timeNow = Time.unscaledTime;

            UpdateEmbers(deltaTime);
            UpdateDustParticles(deltaTime);
            UpdateSparks(deltaTime);
            UpdateLightningBolts(deltaTime);
            UpdateVeilPulses(deltaTime);
            
            // Trigger random lightning
            if (timeNow >= _nextLightningTime)
            {
                TriggerLightningBolt();
                _nextLightningTime = timeNow + Random.Range(kLightningInterval, kLightningInterval * 2f);
            }
        }

        private void OnDisable()
        {
            ClearParticles();
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        public void Initialize()
        {
            if (_uiDocument == null) return;

            _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            PrepareForInitialize();

            _particleContainer = _root.Q<VisualElement>("particle-container");
            _emberContainer = _root.Q<VisualElement>("ember-container");

            // Fallback: create containers if missing in UXML
            if (_particleContainer == null)
            {
                _particleContainer = new VisualElement { name = "particle-container" };
                _particleContainer.style.position = Position.Absolute;
                _particleContainer.style.left = 0;
                _particleContainer.style.top = 0;
                _particleContainer.style.right = 0;
                _particleContainer.style.bottom = 0;
                _particleContainer.style.overflow = Overflow.Hidden;
                _root.Add(_particleContainer);
            }
            if (_emberContainer == null)
            {
                _emberContainer = new VisualElement { name = "ember-container" };
                _emberContainer.style.position = Position.Absolute;
                _emberContainer.style.left = 0;
                _emberContainer.style.top = 0;
                _emberContainer.style.right = 0;
                _emberContainer.style.bottom = 0;
                _emberContainer.style.overflow = Overflow.Hidden;
                _root.Add(_emberContainer);
            }

            // CRITICAL: Remove old spark-container if it exists
            _sparkContainer = _root.Q<VisualElement>("spark-container");
            if (_sparkContainer != null)
            {
                _sparkContainer.RemoveFromHierarchy();
                Debug.Log("[VB:Lightning] Removed existing spark-container");
            }
            
            // Create NEW spark container - add it LAST so it renders on top
            _sparkContainer = new VisualElement { name = "spark-container" };
            _sparkContainer.style.position = Position.Absolute;
            _sparkContainer.style.left = 0;
            _sparkContainer.style.top = 0;
            _sparkContainer.style.width = Length.Percent(100);
            _sparkContainer.style.height = Length.Percent(100);
            _sparkContainer.style.overflow = Overflow.Visible; // CRITICAL - allow content to show
            _sparkContainer.pickingMode = PickingMode.Ignore; // Don't block clicks
            _sparkContainer.style.display = DisplayStyle.Flex; // Explicitly visible
            _sparkContainer.style.backgroundColor = new StyleColor(StyleKeyword.Null); // Truly unset, not just transparent
            // Add AFTER all other UI initialization
            // We'll add it at the very end after creating particles
            Debug.Log("[VB:Lightning] Created new spark-container (will add last)");

            // Get screen dimensions
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;
            _nextLightningTime = Time.unscaledTime + Random.Range(2f, 4f);

            // Find veil pulse elements
            var veilPulseLayer = _root.Q<VisualElement>("veil-pulse-layer");
            if (veilPulseLayer != null)
            {
                var p1 = _root.Q<VisualElement>("veil-pulse-1");
                var p2 = _root.Q<VisualElement>("veil-pulse-2");
                var p3 = _root.Q<VisualElement>("veil-pulse-3");
                if (p1 != null) _veilPulses.Add(p1);
                if (p2 != null) _veilPulses.Add(p2);
                if (p3 != null) _veilPulses.Add(p3);
            }

            // Pre-populate object pool
            InitializeObjectPool();

            // Create particles
            CreateEmbers();
            CreateDustParticles();
            CreateSparks();
            CreateLightningBolts();
            
            if (_particleContainer != null)
            {
                _particleContainer.Add(_sparkContainer);
            }
            else
            {
                _root.Add(_sparkContainer);
            }
            
            Debug.Log($"[VB:Lightning] Initialization complete. Added spark-container as LAST child. Screen: {_screenWidth}x{_screenHeight}, Bolts: {_lightningBolts.Count}, Container child count: {_root.childCount}");

            _isInitialized = true;
        }

        public void Initialize(VisualElement root)
    {
        _root = root;
        if (_root == null) return;

        PrepareForInitialize();

        _particleContainer = _root.Q<VisualElement>("particle-container");
        _emberContainer = _root.Q<VisualElement>("ember-container");
        
        // CRITICAL: Remove old spark-container if it exists
        _sparkContainer = _root.Q<VisualElement>("spark-container");
        if (_sparkContainer != null)
        {
            _sparkContainer.RemoveFromHierarchy();
            Debug.Log("[VB:Lightning] Removed existing spark-container");
        }
        
        // Create NEW spark container - same as parameterless Initialize()
        _sparkContainer = new VisualElement { name = "spark-container" };
        _sparkContainer.style.position = Position.Absolute;
        _sparkContainer.style.left = 0;
        _sparkContainer.style.top = 0;
        _sparkContainer.style.width = Length.Percent(100);
        _sparkContainer.style.height = Length.Percent(100);
        _sparkContainer.style.overflow = Overflow.Visible;
        _sparkContainer.pickingMode = PickingMode.Ignore;
        _sparkContainer.style.display = DisplayStyle.Flex;
        _sparkContainer.style.backgroundColor = new StyleColor(StyleKeyword.Null); // Truly unset!
        Debug.Log("[VB:Lightning] Created new spark-container in Initialize(root)");

        _screenWidth = Screen.width;
        _screenHeight = Screen.height;
        _nextLightningTime = Time.unscaledTime + Random.Range(2f, 4f);

        var veilPulseLayer = _root.Q<VisualElement>("veil-pulse-layer");
        if (veilPulseLayer != null)
        {
            var p1 = _root.Q<VisualElement>("veil-pulse-1");
            var p2 = _root.Q<VisualElement>("veil-pulse-2");
            var p3 = _root.Q<VisualElement>("veil-pulse-3");
            if (p1 != null) _veilPulses.Add(p1);
            if (p2 != null) _veilPulses.Add(p2);
            if (p3 != null) _veilPulses.Add(p3);
        }

        InitializeObjectPool();
        CreateEmbers();
        CreateDustParticles();
        CreateSparks();
        CreateLightningBolts();
        
        if (_particleContainer != null)
        {
            _particleContainer.Add(_sparkContainer);
        }
        else
        {
            _root.Add(_sparkContainer);
        }
        
        Debug.Log($"[VB:Lightning] Initialize(root) complete. Added spark-container as LAST child.");

        _isInitialized = true;
    }

        private void PrepareForInitialize()
        {
            ClearParticles();
        }

        // =============================================================================
        // OBJECT POOLING (OPTIMIZATION)
        // =============================================================================

        private void InitializeObjectPool()
        {
            if (_poolInitialized) return;
            for (int i = 0; i < kPoolSize; i++)
            {
                var element = new VisualElement();
                element.style.display = DisplayStyle.None;
                _particlePool.Enqueue(element);
            }
            _poolInitialized = true;
        }

        private VisualElement GetPooledParticle()
        {
            if (_particlePool.Count > 0)
            {
                var element = _particlePool.Dequeue();
                element.style.display = DisplayStyle.Flex;
                return element;
            }
            return new VisualElement(); // Fallback if pool exhausted
        }

        private void ReturnToPool(VisualElement element)
        {
            element.style.display = DisplayStyle.None;
            element.RemoveFromHierarchy();
            _particlePool.Enqueue(element);
        }

        // =============================================================================
        // PARTICLE CREATION
        // =============================================================================

        private void CreateEmbers()
        {
            if (_emberContainer == null) return;

            for (int i = 0; i < _emberCount; i++)
            {
                var ember = CreateEnhancedParticle(
                    Random.Range(3f, 6f),
                    _emberColor,
                    _emberGlowColor,
                    ParticleType.Ember
                );

                var data = new ParticleData
                {
                    Element = ember,
                    Type = ParticleType.Ember,
                    X = Random.Range(0f, _screenWidth),
                    Y = Random.Range(0f, _screenHeight),
                    SpeedX = Random.Range(-8f, 8f),
                    SpeedY = Random.Range(-_emberSpeed, -_emberSpeed * 0.6f),
                    Size = Random.Range(3f, 6f),
                    Alpha = Random.Range(0.4f, 0.8f),
                    AlphaSpeed = Random.Range(0.4f, 0.9f),
                    Rotation = Random.Range(0f, 360f),
                    RotationSpeed = Random.Range(-30f, 30f),
                    Active = true
                };

                ember.style.left = data.X;
                ember.style.top = data.Y;

                _emberContainer.Add(ember);
                _embers.Add(data);
            }
        }

        private void CreateDustParticles()
        {
            if (_particleContainer == null) return;

            for (int i = 0; i < _dustCount; i++)
            {
                var dust = CreateEnhancedParticle(
                    Random.Range(1f, 2.5f),
                    _dustColor,
                    Color.clear,
                    ParticleType.Dust
                );

                var data = new ParticleData
                {
                    Element = dust,
                    Type = ParticleType.Dust,
                    X = Random.Range(0f, _screenWidth),
                    Y = Random.Range(0f, _screenHeight),
                    SpeedX = Random.Range(-4f, 4f),
                    SpeedY = Random.Range(-_dustSpeed, _dustSpeed * 0.4f),
                    Size = Random.Range(1f, 2.5f),
                    Alpha = Random.Range(0.1f, 0.3f),
                    AlphaSpeed = Random.Range(0.15f, 0.35f),
                    Rotation = 0,
                    RotationSpeed = 0,
                    Active = true
                };

                dust.style.left = data.X;
                dust.style.top = data.Y;

                _particleContainer.Add(dust);
                _dustParticles.Add(data);
            }
        }

        private void CreateSparks()
        {
            if (_sparkContainer == null) return;

            for (int i = 0; i < _sparkCount; i++)
            {
                var spark = CreateLightningSpark(
                    Random.Range(2f, 4f),
                    _sparkColor
                );

                var data = new ParticleData
                {
                    Element = spark,
                    Type = ParticleType.Spark,
                    X = Random.Range(0f, _screenWidth),
                    Y = Random.Range(_screenHeight, _screenHeight + 100),
                    SpeedX = Random.Range(-40f, 40f),
                    SpeedY = Random.Range(-_sparkSpeed, -_sparkSpeed * 0.7f),
                    Size = Random.Range(2f, 4f),
                    Alpha = Random.Range(0.6f, 1.0f),
                    AlphaSpeed = Random.Range(1.5f, 3.0f),
                    Rotation = Random.Range(0f, 360f),
                    RotationSpeed = Random.Range(-180f, 180f),
                    Active = true
                };

                spark.style.left = data.X;
                spark.style.top = data.Y;

                _sparkContainer.Add(spark);
                _sparks.Add(data);
            }
        }

        private void CreateLightningBolts()
        {
            if (_sparkContainer == null)
            {
                Debug.LogError("[VB:Lightning] ERROR - _sparkContainer is NULL! Cannot create lightning.");
                return;
            }

            for (int i = 0; i < _lightningBoltCount; i++)
            {
                var bolt = CreateLightningBolt();
                
                // CRITICAL: Check for null in case sprite loading fails
                if (bolt == null)
                {
                    Debug.LogError($"[VB:Lightning] Failed to create bolt #{i} - sprite loading failed");
                    continue;
                }
                
                bolt.Root.style.display = DisplayStyle.None; // Hidden until triggered
                bolt.Root.pickingMode = PickingMode.Ignore; // Don't block clicks

                _sparkContainer.Add(bolt.Root);
                _lightningBolts.Add(bolt);
                
                Debug.Log($"[VB:Lightning] Added bolt #{i} to _sparkContainer (sprite-based)");
            }
            
            Debug.Log($"[VB:Lightning] Created {_lightningBoltCount} sprite-based bolts in _sparkContainer, container child count: {_sparkContainer.childCount}");
        }

        // =============================================================================
        // ENHANCED PARTICLE RENDERING
        // =============================================================================

        private VisualElement CreateEnhancedParticle(float size, Color color, Color glowColor, ParticleType type)
        {
            // Container for layered particle
            var container = new VisualElement();
            container.style.position = Position.Absolute;
            container.style.width = size * 3; // Make room for glow layers
            container.style.height = size * 3;
            
            if (type == ParticleType.Ember)
            {
                // OUTER GLOW LAYER (largest, darkest)
                var outerGlow = new VisualElement();
                outerGlow.style.position = Position.Absolute;
                outerGlow.style.width = size * 3;
                outerGlow.style.height = size * 3;
                outerGlow.style.left = 0;
                outerGlow.style.top = 0;
                outerGlow.style.borderTopLeftRadius = size * 1.5f;
                outerGlow.style.borderTopRightRadius = size * 1.5f;
                outerGlow.style.borderBottomLeftRadius = size * 1.5f;
                outerGlow.style.borderBottomRightRadius = size * 1.5f;
                outerGlow.style.backgroundColor = new Color(color.r, color.g, color.b, color.a * 0.2f);
                container.Add(outerGlow);
                
                // MIDDLE GLOW LAYER
                var middleGlow = new VisualElement();
                middleGlow.style.position = Position.Absolute;
                middleGlow.style.width = size * 2;
                middleGlow.style.height = size * 2;
                middleGlow.style.left = size * 0.5f;
                middleGlow.style.top = size * 0.5f;
                middleGlow.style.borderTopLeftRadius = size;
                middleGlow.style.borderTopRightRadius = size;
                middleGlow.style.borderBottomLeftRadius = size;
                middleGlow.style.borderBottomRightRadius = size;
                middleGlow.style.backgroundColor = new Color(glowColor.r, glowColor.g, glowColor.b, glowColor.a * 0.5f);
                container.Add(middleGlow);
                
                // BRIGHT CORE (smallest, brightest)
                var core = new VisualElement();
                core.style.position = Position.Absolute;
                core.style.width = size;
                core.style.height = size;
                core.style.left = size;
                core.style.top = size;
                core.style.borderTopLeftRadius = size / 2;
                core.style.borderTopRightRadius = size / 2;
                core.style.borderBottomLeftRadius = size / 2;
                core.style.borderBottomRightRadius = size / 2;
                core.style.backgroundColor = new Color(1f, 0.9f, 0.85f, 1f); // Bright white-orange core
                container.Add(core);
            }
            else // Dust
            {
                // Dust: single soft layer, very subtle
                var dustParticle = new VisualElement();
                dustParticle.style.position = Position.Absolute;
                dustParticle.style.width = size;
                dustParticle.style.height = size;
                dustParticle.style.left = size;
                dustParticle.style.top = size;
                dustParticle.style.borderTopLeftRadius = size / 2;
                dustParticle.style.borderTopRightRadius = size / 2;
                dustParticle.style.borderBottomLeftRadius = size / 2;
                dustParticle.style.borderBottomRightRadius = size / 2;
                dustParticle.style.backgroundColor = color;
                container.Add(dustParticle);
            }

            return container;
        }

        private VisualElement CreateLightningSpark(float size, Color color)
        {
            // Container for layered spark with trailing glow
            var container = new VisualElement();
            container.style.position = Position.Absolute;
            container.style.width = size * 2.5f;
            container.style.height = size;
            
            // OUTER GLOW (stretched, faint)
            var outerGlow = new VisualElement();
            outerGlow.style.position = Position.Absolute;
            outerGlow.style.width = size * 2.5f;
            outerGlow.style.height = size;
            outerGlow.style.left = 0;
            outerGlow.style.top = 0;
            outerGlow.style.backgroundColor = new Color(color.r, color.g, color.b, color.a * 0.15f);
            outerGlow.style.borderTopLeftRadius = size * 0.3f;
            outerGlow.style.borderBottomLeftRadius = size * 0.3f;
            outerGlow.style.rotate = new Rotate(Random.Range(-15f, 15f));
            container.Add(outerGlow);
            
            // MIDDLE TRAIL (angular, medium glow)
            var middleTrail = new VisualElement();
            middleTrail.style.position = Position.Absolute;
            middleTrail.style.width = size * 1.8f;
            middleTrail.style.height = size * 0.7f;
            middleTrail.style.left = size * 0.35f;
            middleTrail.style.top = size * 0.15f;
            middleTrail.style.backgroundColor = new Color(color.r, color.g, color.b, color.a * 0.4f);
            container.Add(middleTrail);
            
            // BRIGHT CORE (diamond-shaped, very bright)
            var core = new VisualElement();
            core.style.position = Position.Absolute;
            core.style.width = size;
            core.style.height = size * 0.4f;
            core.style.left = size * 0.75f;
            core.style.top = size * 0.3f;
            core.style.backgroundColor = new Color(1f, 0.95f, 0.9f, 1f); // Almost white
            container.Add(core);
            
            // Rotate entire container for random angle
            container.style.rotate = new Rotate(Random.Range(0f, 360f));
            
            return container;
        }

        private LightningData CreateLightningBolt()
        {
            // IMAGE-BASED LIGHTNING (v2.75+)
            // Load random lightning sprite from Resources
            int spriteIndex = Random.Range(1, 11); // lightning_bolt_01 through lightning_bolt_10
            string spritePath = $"UI/Lightning/lightning_bolt_{spriteIndex:D2}";
            
            Sprite lightningSprite = Resources.Load<Sprite>(spritePath);
            
            if (lightningSprite == null)
            {
                Debug.LogError($"[VB:Lightning] Failed to load sprite: {spritePath}");
                return null;
            }
            
            // Single image element for the lightning bolt
            var boltElement = new VisualElement();
            boltElement.style.position = Position.Absolute;
            boltElement.style.width = 40;  // Width in pixels
            boltElement.style.height = 600; // Tall vertical bolt
            
            // Apply sprite as background
            boltElement.style.backgroundImage = new StyleBackground(lightningSprite);
            boltElement.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;
            
            // Tint to CRIMSON (our brand color)
            boltElement.style.unityBackgroundImageTintColor = _lightningColor;
            
            Debug.Log($"[VB:Lightning] Created sprite-based bolt: {spritePath}");
            
            return new LightningData
            {
                Root = boltElement,
                Segments = new List<LightningSegment>(), // Empty - not used for sprites
                Lifetime = 0f,
                MaxLifetime = 0.6f,
                Active = false
            };
        }

        // =============================================================================
        // PARTICLE UPDATES
        // =============================================================================

        private void UpdateEmbers(float deltaTime)
        {
            for (int i = 0; i < _embers.Count; i++)
            {
                var ember = _embers[i];
                if (!ember.Active) continue;

                // Move upward with horizontal drift
                ember.Y += ember.SpeedY * deltaTime;
                ember.X += ember.SpeedX * deltaTime;

                // Perlin noise for organic movement (no GC)
                float noise = (Mathf.PerlinNoise(Time.unscaledTime * 0.8f + i * 0.15f, 0f) - 0.5f) * 35f;
                ember.SpeedX += noise * deltaTime;
                ember.SpeedX = Mathf.Clamp(ember.SpeedX, -12f, 12f);

                // Rotate slowly
                ember.Rotation += ember.RotationSpeed * deltaTime;
                ember.Element.style.rotate = new Rotate(ember.Rotation);

                // Pulse alpha (faster flicker for ominous effect)
                float time = Time.unscaledTime * ember.AlphaSpeed;
                float alpha = ember.Alpha * (0.4f + 0.6f * Mathf.Sin(time));

                // Reset if off screen
                if (ember.Y < -30 || ember.X < -30 || ember.X > _screenWidth + 30)
                {
                    ember.Y = _screenHeight + Random.Range(20f, 80f);
                    ember.X = Random.Range(0f, _screenWidth);
                    ember.SpeedX = Random.Range(-8f, 8f);
                    ember.Rotation = Random.Range(0f, 360f);
                }

                // Apply
                ember.Element.style.left = ember.X;
                ember.Element.style.top = ember.Y;
                ember.Element.style.opacity = alpha;
            }
        }

        private void UpdateDustParticles(float deltaTime)
        {
            for (int i = 0; i < _dustParticles.Count; i++)
            {
                var dust = _dustParticles[i];
                if (!dust.Active) continue;

                // Slow drift
                dust.Y += dust.SpeedY * deltaTime;
                dust.X += dust.SpeedX * deltaTime;

                // Very subtle Perlin noise (no GC)
                float noiseX = (Mathf.PerlinNoise(Time.unscaledTime * 0.4f + i * 0.25f, 0f) - 0.5f) * 8f;
                float noiseY = (Mathf.PerlinNoise(Time.unscaledTime * 0.4f + i * 0.25f, 1f) - 0.5f) * 5f;
                dust.SpeedX += noiseX * deltaTime;
                dust.SpeedY += noiseY * deltaTime;
                dust.SpeedX = Mathf.Clamp(dust.SpeedX, -6f, 6f);
                dust.SpeedY = Mathf.Clamp(dust.SpeedY, -_dustSpeed, _dustSpeed * 0.5f);

                // Very slow alpha pulse
                float time = Time.unscaledTime * dust.AlphaSpeed * 0.4f;
                float alpha = dust.Alpha * (0.5f + 0.5f * Mathf.Sin(time));

                // Wrap around screen
                if (dust.Y < -15) dust.Y = _screenHeight + 15;
                if (dust.Y > _screenHeight + 15) dust.Y = -15;
                if (dust.X < -15) dust.X = _screenWidth + 15;
                if (dust.X > _screenWidth + 15) dust.X = -15;

                // Apply
                dust.Element.style.left = dust.X;
                dust.Element.style.top = dust.Y;
                dust.Element.style.opacity = alpha;
            }
        }

        private void UpdateSparks(float deltaTime)
        {
            for (int i = 0; i < _sparks.Count; i++)
            {
                var spark = _sparks[i];
                if (!spark.Active) continue;

                // Fast diagonal movement
                spark.Y += spark.SpeedY * deltaTime;
                spark.X += spark.SpeedX * deltaTime;

                // Rapid rotation for electric effect
                spark.Rotation += spark.RotationSpeed * deltaTime;
                spark.Element.style.rotate = new Rotate(spark.Rotation);

                // Fast flicker
                float time = Time.unscaledTime * spark.AlphaSpeed;
                float alpha = spark.Alpha * (0.3f + 0.7f * Mathf.Abs(Mathf.Sin(time * 3f)));

                // Reset if off screen (top)
                if (spark.Y < -50)
                {
                    spark.Y = _screenHeight + Random.Range(50f, 150f);
                    spark.X = Random.Range(0f, _screenWidth);
                    spark.SpeedX = Random.Range(-40f, 40f);
                    spark.SpeedY = Random.Range(-_sparkSpeed, -_sparkSpeed * 0.7f);
                    spark.Rotation = Random.Range(0f, 360f);
                }

                // Apply
                spark.Element.style.left = spark.X;
                spark.Element.style.top = spark.Y;
                spark.Element.style.opacity = alpha;
            }
        }

        private void UpdateLightningBolts(float deltaTime)
        {
            for (int i = 0; i < _lightningBolts.Count; i++)
            {
                var bolt = _lightningBolts[i];
                if (!bolt.Active) continue;

                bolt.Lifetime += deltaTime;

                // SPRITE-BASED ANIMATION (v2.75+)
                // Simple fade in/out with flicker
                float t = bolt.Lifetime / bolt.MaxLifetime;
                float alpha;
                
                if (t < 0.1f)
                {
                    // Fast fade in
                    alpha = t / 0.1f;
                }
                else if (t < 0.8f)
                {
                    // Hold bright with subtle flicker
                    float flicker = 0.9f + 0.1f * Mathf.Sin(Time.unscaledTime * 15f);
                    alpha = flicker;
                }
                else
                {
                    // Fast fade out
                    alpha = (1f - t) / 0.2f;
                }
                
                alpha = Mathf.Clamp01(alpha);
                bolt.Root.style.opacity = alpha;

                // Deactivate when done
                if (bolt.Lifetime >= bolt.MaxLifetime)
                {
                    bolt.Active = false;
                    bolt.Root.style.display = DisplayStyle.None;
                }
            }
        }

        private void UpdateVeilPulses(float deltaTime)
        {
            float time = Time.unscaledTime * _veilPulseSpeed;

            for (int i = 0; i < _veilPulses.Count; i++)
            {
                var pulse = _veilPulses[i];
                if (pulse == null) continue;

                // Each pulse has different phase - slower, more ominous
                float phase = i * 2.5f;
                float scale = 1f + 0.12f * Mathf.Sin(time + phase);
                float alpha = 0.02f + 0.015f * Mathf.Sin(time * 0.6f + phase);

                pulse.style.scale = new Scale(new Vector2(scale, scale));
                pulse.style.opacity = alpha;
            }
        }

        // =============================================================================
        // LIGHTNING EFFECTS
        // =============================================================================

        private void TriggerLightningBolt()
    {
        if (_sparkContainer == null || _sparkContainer.parent == null)
        {
            Debug.LogWarning("[VB:Lightning] Container not initialized");
            return;
        }

        for (int i = 0; i < _lightningBolts.Count; i++)
        {
            var bolt = _lightningBolts[i];
            if (!bolt.Active)
            {
                bolt.Active = true;
                bolt.Lifetime = 0f;
                bolt.MaxLifetime = Random.Range(0.45f, 0.75f);
                
                // SPRITE-BASED: Position randomly across screen width
                float randomX = Random.Range(0f, _screenWidth);
                float randomY = Random.Range(-100f, 100f); // Slight vertical offset
                
                bolt.Root.style.left = randomX;
                bolt.Root.style.top = randomY;
                bolt.Root.style.display = DisplayStyle.Flex;
                bolt.Root.style.opacity = 1f;
                
                Debug.Log($"[VB:Lightning] Sprite bolt #{i} triggered at ({randomX}, {randomY})");
                
                return;
            }
        }
    }

        private void GenerateLightningPath(LightningData bolt)
        {
            float startX = Random.Range(90f, _screenWidth - 90f);
            float endX = Mathf.Clamp(startX + Random.Range(-280f, 280f), 80f, _screenWidth - 80f);
            float topY = -20f;
            float bottomY = _screenHeight + 40f;

            // AAA QUALITY: More points for smoother curves, more dramatic noise
            int pointCount = Random.Range(11, 16);
            float step = (bottomY - topY) / Mathf.Max(1, pointCount - 1);
            float noiseFreq = Random.Range(3.0f, 6.0f);
            float noiseAmp = Random.Range(90f, 180f); // Doubled amplitude for MORE jaggedness!
            float noiseSeed = Random.Range(0f, 1000f);

            var points = new List<Vector2>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                float y = topY + step * i;
                float drift = Mathf.Lerp(startX, endX, t);
                float noise = (Mathf.PerlinNoise(noiseSeed, t * noiseFreq) - 0.5f) * noiseAmp;
                float jag = Random.Range(-40f, 40f); // Doubled jag for MORE chaos!
                float x = Mathf.Clamp(drift + noise + jag, 40f, _screenWidth - 40f);
                points.Add(new Vector2(x, y));
            }

            int mainSegments = Mathf.Min(points.Count - 1, bolt.Segments.Count);
            for (int i = 0; i < bolt.Segments.Count; i++)
            {
                var segment = bolt.Segments[i];
                if (i >= mainSegments)
                {
                    segment.Container.style.display = DisplayStyle.None;
                    continue;
                }

                Vector2 a = points[i];
                Vector2 b = points[i + 1];
                float length = Vector2.Distance(a, b);
                float angle = Mathf.Atan2(b.x - a.x, b.y - a.y) * Mathf.Rad2Deg;
                float t = i / (float)Mathf.Max(1, mainSegments - 1);

                // AAA QUALITY: Thicker main bolt with variation
                float thickness = Mathf.Lerp(kLightningMaxThickness, kLightningMinThickness, t) + Random.Range(-1f, 1f);
                thickness = Mathf.Clamp(thickness, kLightningMinThickness, kLightningMaxThickness);

                float centerX = (a.x + b.x) * 0.5f;
                float centerY = (a.y + b.y) * 0.5f;

                segment.Container.style.display = DisplayStyle.Flex;
                segment.Container.style.left = centerX - (thickness * 0.5f);
                segment.Container.style.top = centerY - (length * 0.5f);
                segment.Container.style.width = thickness;
                segment.Container.style.height = length;
                segment.Container.style.rotate = new Rotate(new Angle(angle, AngleUnit.Degree));

                segment.BaseX = centerX - (thickness * 0.5f);
                segment.BaseY = centerY - (length * 0.5f);
                segment.BaseThickness = thickness;
                segment.BaseLength = length;
                segment.BaseAngle = angle;
                segment.BaseAlpha = Mathf.Lerp(1f, 0.7f, t) * Random.Range(0.85f, 1f); // Brighter!
                segment.FlickerSpeed = Random.Range(15f, 24f); // Faster flicker for more energy!
                segment.FlickerOffset = Random.Range(0f, 6.28318f);

                // AAA QUALITY: Much brighter glow and core!
                var glowColor = new Color(_lightningColor.r, _lightningColor.g, _lightningColor.b, 0.7f); // 0.4 → 0.7 (75% more opaque!)
                var coreColor = Color.Lerp(_lightningColor, Color.white, 0.75f); // 0.45 → 0.75 (almost pure white!)

                segment.Glow.style.left = -kLightningGlowPadding;
                segment.Glow.style.top = -kLightningGlowPadding;
                segment.Glow.style.width = thickness + (kLightningGlowPadding * 2f);
                segment.Glow.style.height = length + (kLightningGlowPadding * 2f);
                segment.Glow.style.backgroundColor = glowColor;

                segment.Core.style.left = 0;
                segment.Core.style.top = 0;
                segment.Core.style.width = thickness;
                segment.Core.style.height = length;
                segment.Core.style.backgroundColor = coreColor;
            }

            // AAA QUALITY: Create 2-3 dramatic branches with proper thickness!
            int branchStartIndex = mainSegments;
            int available = bolt.Segments.Count - branchStartIndex;
            int segmentsUsed = 0;

            // Try to create 2-3 branches
            int branchesToCreate = Mathf.Min(Random.Range(2, 4), available / 2);
            for (int branchNum = 0; branchNum < branchesToCreate && segmentsUsed < available; branchNum++)
            {
                int segsPerBranch = Random.Range(2, 4);
                if (segmentsUsed + segsPerBranch > available) break;

                // Attach at different points along main bolt
                int attachIndex = Random.Range(2 + branchNum, Mathf.Max(3, pointCount - 3));
                Vector2 attach = points[Mathf.Clamp(attachIndex, 0, points.Count - 1)];

                float branchLength = Random.Range(100f, 200f); // Longer branches!
                Vector2 dir = new Vector2(Random.Range(-1f, 1f), Random.Range(0.3f, 0.95f)).normalized;
                Vector2 p0 = attach;
                Vector2 p1 = attach + dir * (branchLength * 0.5f) + new Vector2(Random.Range(-25f, 25f), Random.Range(-15f, 15f));
                Vector2 p2 = attach + dir * branchLength + new Vector2(Random.Range(-35f, 35f), Random.Range(-20f, 20f));

                var branchPoints = new List<Vector2> { p0, p1, p2 };
                int branchCount = Mathf.Min(branchPoints.Count - 1, segsPerBranch);

                for (int i = 0; i < branchCount; i++)
                {
                    var segment = bolt.Segments[branchStartIndex + segmentsUsed + i];
                    Vector2 a = branchPoints[i];
                    Vector2 b = branchPoints[i + 1];
                    float length = Vector2.Distance(a, b);
                    float angle = Mathf.Atan2(b.x - a.x, b.y - a.y) * Mathf.Rad2Deg;

                    // AAA QUALITY: Much thicker branches!
                    float t = i / (float)Mathf.Max(1, branchCount - 1);
                    float thickness = Mathf.Lerp(kBranchMaxThickness, kBranchMinThickness, t);

                    float centerX = (a.x + b.x) * 0.5f;
                    float centerY = (a.y + b.y) * 0.5f;

                    segment.Container.style.display = DisplayStyle.Flex;
                    segment.Container.style.left = centerX - (thickness * 0.5f);
                    segment.Container.style.top = centerY - (length * 0.5f);
                    segment.Container.style.width = thickness;
                    segment.Container.style.height = length;
                    segment.Container.style.rotate = new Rotate(new Angle(angle, AngleUnit.Degree));

                    segment.BaseX = centerX - (thickness * 0.5f);
                    segment.BaseY = centerY - (length * 0.5f);
                    segment.BaseThickness = thickness;
                    segment.BaseLength = length;
                    segment.BaseAngle = angle;
                    segment.BaseAlpha = Random.Range(0.7f, 0.95f); // Brighter branches!
                    segment.FlickerSpeed = Random.Range(14f, 22f);
                    segment.FlickerOffset = Random.Range(0f, 6.28318f);

                    // AAA QUALITY: Brighter branch glow and core!
                    var glowColor = new Color(_lightningColor.r, _lightningColor.g, _lightningColor.b, 0.5f); // 0.25 → 0.5 (2x brighter!)
                    var coreColor = Color.Lerp(_lightningColor, Color.white, 0.65f); // 0.35 → 0.65 (much whiter!)

                    segment.Glow.style.left = -kLightningGlowPadding;
                    segment.Glow.style.top = -kLightningGlowPadding;
                    segment.Glow.style.width = thickness + (kLightningGlowPadding * 2f);
                    segment.Glow.style.height = length + (kLightningGlowPadding * 2f);
                    segment.Glow.style.backgroundColor = glowColor;

                    segment.Core.style.left = 0;
                    segment.Core.style.top = 0;
                    segment.Core.style.width = thickness;
                    segment.Core.style.height = length;
                    segment.Core.style.backgroundColor = coreColor;
                }

                segmentsUsed += branchCount;
            }

            // Hide unused segments
            for (int i = branchStartIndex + segmentsUsed; i < bolt.Segments.Count; i++)
            {
                bolt.Segments[i].Container.style.display = DisplayStyle.None;
            }
        }

        // =============================================================================
        // CLEANUP
        // =============================================================================

        private void ClearParticles()
        {
            foreach (var ember in _embers)
            {
                ReturnToPool(ember.Element);
            }
            _embers.Clear();

            foreach (var dust in _dustParticles)
            {
                ReturnToPool(dust.Element);
            }
            _dustParticles.Clear();

            foreach (var spark in _sparks)
            {
                ReturnToPool(spark.Element);
            }
            _sparks.Clear();

            foreach (var bolt in _lightningBolts)
            {
                bolt.Root?.RemoveFromHierarchy();
            }
            _lightningBolts.Clear();

            _veilPulses.Clear();
            _isInitialized = false;
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        public void SetEmberCount(int count)
        {
            _emberCount = count;
            if (_isInitialized)
            {
                ClearParticles();
                Initialize();
            }
        }

        public void SetIntensity(float intensity)
        {
            _emberSpeed = 25f * intensity;
            _dustSpeed = 12f * intensity;
            _sparkSpeed = 80f * intensity;
            _veilPulseSpeed = 0.4f * intensity;
        }

        public void TriggerManualLightning()
    {
        Debug.Log("[VB:Lightning] MANUAL TEST - Triggering 3 lightning bolts in sequence");
        for (int i = 0; i < 3; i++)
        {
            TriggerLightningBolt();
        }
        Debug.Log($"[VB:Lightning] MANUAL TEST - Root has {_root.childCount} children, screen size: {_screenWidth}x{_screenHeight}");
    }

    /// <summary>
    /// Diagnostic method to inspect lightning bolt states and positions
    /// </summary>
        public void DiagnoseLightning()
    {
        Debug.Log("=== LIGHTNING DIAGNOSTIC START ===");
        Debug.Log($"Screen size: {_screenWidth} x {_screenHeight}");
        Debug.Log($"Root element: {(_root != null ? "EXISTS" : "NULL")}");
        if (_root != null)
        {
            Debug.Log($"Root child count: {_root.childCount}");
            Debug.Log($"Root position: {_root.style.position.value}");
            Debug.Log($"Root display: {_root.style.display.value}");
        }
        
        Debug.Log($"Spark container: {(_sparkContainer != null ? "EXISTS" : "NULL")}");
        if (_sparkContainer != null)
        {
            Debug.Log($"  Spark container child count: {_sparkContainer.childCount}");
            Debug.Log($"  Spark container display: {_sparkContainer.style.display.value}");
            Debug.Log($"  Spark container overflow: {_sparkContainer.style.overflow.value}");
            Debug.Log($"  Spark container position: {_sparkContainer.style.position.value}");
            Debug.Log($"  Spark container size: {_sparkContainer.style.width.value} x {_sparkContainer.style.height.value}");
            Debug.Log($"  Spark container parent: {(_sparkContainer.parent != null ? _sparkContainer.parent.name : "NULL")}");
        }
        
        Debug.Log($"Lightning bolts count: {_lightningBolts.Count}");
        for (int i = 0; i < _lightningBolts.Count; i++)
        {
            var bolt = _lightningBolts[i];
            Debug.Log($"  Bolt #{i}:");
            Debug.Log($"    Active: {bolt.Active}");
            Debug.Log($"    Display: {bolt.Root.style.display.value}");
            Debug.Log($"    Position: ({bolt.Root.style.left.value}, {bolt.Root.style.top.value})");
            Debug.Log($"    Size: {bolt.Root.style.width.value} x {bolt.Root.style.height.value}");
            Debug.Log($"    Opacity: {bolt.Root.style.opacity.value}");
            Debug.Log($"    Parent: {(bolt.Root.parent != null ? bolt.Root.parent.name : "NULL")}");
            Debug.Log($"    Segments: {bolt.Segments?.Count ?? 0}");
        }
        Debug.Log("=== LIGHTNING DIAGNOSTIC END ===");
    }
    }
}
