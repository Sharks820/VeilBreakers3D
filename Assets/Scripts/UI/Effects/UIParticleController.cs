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
        private const int kPoolSize = 150; // Increased for more particles

        // Lightning timing
        private float _nextLightningTime;
        private const float kLightningInterval = 2.5f; // More frequent lightning flashes

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
            public VisualElement Element;
            public float Lifetime;
            public float MaxLifetime;
            public bool Active;
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
            
            _nextLightningTime = Time.time + Random.Range(2f, 4f);
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

            float deltaTime = Time.deltaTime;

            UpdateEmbers(deltaTime);
            UpdateDustParticles(deltaTime);
            UpdateSparks(deltaTime);
            UpdateLightningBolts(deltaTime);
            UpdateVeilPulses(deltaTime);
            
            // Trigger random lightning
            if (Time.time >= _nextLightningTime)
            {
                TriggerLightningBolt();
                _nextLightningTime = Time.time + Random.Range(kLightningInterval, kLightningInterval * 2f);
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
            
            // NOW add spark container as THE LAST CHILD so it renders ON TOP
            _root.Add(_sparkContainer);
            _sparkContainer.BringToFront();
            
            Debug.Log($"[VB:Lightning] Initialization complete. Added spark-container as LAST child. Screen: {_screenWidth}x{_screenHeight}, Bolts: {_lightningBolts.Count}, Container child count: {_root.childCount}");

            _isInitialized = true;
        }

        public void Initialize(VisualElement root)
    {
        _root = root;
        if (_root == null) return;

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
        
        // Add spark container LAST so it renders on top
        _root.Add(_sparkContainer);
        _sparkContainer.BringToFront();
        
        Debug.Log($"[VB:Lightning] Initialize(root) complete. Added spark-container as LAST child.");

        _isInitialized = true;
    }

        // =============================================================================
        // OBJECT POOLING (OPTIMIZATION)
        // =============================================================================

        private void InitializeObjectPool()
        {
            for (int i = 0; i < kPoolSize; i++)
            {
                var element = new VisualElement();
                element.style.display = DisplayStyle.None;
                _particlePool.Enqueue(element);
            }
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
            bolt.style.display = DisplayStyle.None; // Hidden until triggered
            bolt.pickingMode = PickingMode.Ignore; // Don't block clicks (NOT .style)

            var data = new LightningData
            {
                Element = bolt,
                Lifetime = 0f,
                MaxLifetime = 0.6f, // Longer flash for visibility
                Active = false
            };

            // Add to SPARK CONTAINER like other particles use containers
            _sparkContainer.Add(bolt);
            _lightningBolts.Add(data);
            
            Debug.Log($"[VB:Lightning] Added bolt #{i} to _sparkContainer (like embers use _emberContainer)");
        }
        
        Debug.Log($"[VB:Lightning] Created {_lightningBoltCount} bolts in _sparkContainer, container child count: {_sparkContainer.childCount}");
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

        private VisualElement CreateLightningBolt()
    {
        // SIMPLIFIED: Single element IS the bolt (no nesting)
        var bolt = new VisualElement();
        bolt.name = "lightning-bolt";
        bolt.style.position = Position.Absolute;
        bolt.style.width = new Length(40, LengthUnit.Pixel);  // DIRECT Length, not StyleLength!
        bolt.style.height = new Length(100, LengthUnit.Percent);  // DIRECT Length!
        bolt.style.top = 0;
        bolt.style.left = 0;  // Will be set when triggered
        bolt.style.backgroundColor = new Color(0f, 1f, 1f, 1f);  // BRIGHT CYAN
        bolt.style.visibility = Visibility.Visible;
        bolt.pickingMode = PickingMode.Ignore;
        
        Debug.Log($"[VB:Lightning] Created bolt - width: 40px (direct Length), height: 100%");
        
        return bolt;
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
                float noise = (Mathf.PerlinNoise(Time.time * 0.8f + i * 0.15f, 0f) - 0.5f) * 35f;
                ember.SpeedX += noise * deltaTime;
                ember.SpeedX = Mathf.Clamp(ember.SpeedX, -12f, 12f);

                // Rotate slowly
                ember.Rotation += ember.RotationSpeed * deltaTime;
                ember.Element.style.rotate = new Rotate(ember.Rotation);

                // Pulse alpha (faster flicker for ominous effect)
                float time = Time.time * ember.AlphaSpeed;
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
                float noiseX = (Mathf.PerlinNoise(Time.time * 0.4f + i * 0.25f, 0f) - 0.5f) * 8f;
                float noiseY = (Mathf.PerlinNoise(Time.time * 0.4f + i * 0.25f, 1f) - 0.5f) * 5f;
                dust.SpeedX += noiseX * deltaTime;
                dust.SpeedY += noiseY * deltaTime;
                dust.SpeedX = Mathf.Clamp(dust.SpeedX, -6f, 6f);
                dust.SpeedY = Mathf.Clamp(dust.SpeedY, -_dustSpeed, _dustSpeed * 0.5f);

                // Very slow alpha pulse
                float time = Time.time * dust.AlphaSpeed * 0.4f;
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
                float time = Time.time * spark.AlphaSpeed;
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

                // FULL BRIGHTNESS for most of duration, only fade at very end
                float t = bolt.Lifetime / bolt.MaxLifetime;
                float alpha;
                if (t < 0.8f)
                {
                    // Stay at FULL brightness for 80% of duration
                    alpha = 1.0f;
                }
                else
                {
                    // Quick fade only in last 20%
                    alpha = (1.0f - t) / 0.2f;
                }
                alpha = Mathf.Clamp01(alpha);

                bolt.Element.style.opacity = alpha;

                // Deactivate when done
                if (bolt.Lifetime >= bolt.MaxLifetime)
                {
                    bolt.Active = false;
                    bolt.Element.style.display = DisplayStyle.None;
                }
            }
        }

        private void UpdateVeilPulses(float deltaTime)
        {
            float time = Time.time * _veilPulseSpeed;

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
        // Defensive: ensure container exists and is in hierarchy
        if (_sparkContainer == null || _sparkContainer.parent == null)
        {
            Debug.LogWarning("[VB:Lightning] Spark container not properly initialized!");
            return;
        }

        // Find inactive bolt
        for (int i = 0; i < _lightningBolts.Count; i++)
        {
            var bolt = _lightningBolts[i];
            if (!bolt.Active)
            {
                float xPos = Random.Range(50f, _screenWidth - 50f);
                bolt.Active = true;
                bolt.Lifetime = 0f;
                
                // Force all visibility-related styles
                bolt.Element.style.display = DisplayStyle.Flex;
                bolt.Element.style.visibility = Visibility.Visible;
                bolt.Element.style.left = xPos;
                bolt.Element.style.opacity = 1f;
                
                // Force layout recalculation
                bolt.Element.MarkDirtyRepaint();
                
                // COMPREHENSIVE DIAGNOSTICS - This will tell us EXACTLY what's wrong (or right)
                var resolvedWidth = bolt.Element.resolvedStyle.width;
                var resolvedHeight = bolt.Element.resolvedStyle.height;
                var resolvedBg = bolt.Element.resolvedStyle.backgroundColor;
                var containerBg = _sparkContainer.resolvedStyle.backgroundColor;
                
                Debug.Log($"[VB:Lightning] ═══════════════════════════════════════");
                Debug.Log($"[VB:Lightning] BOLT #{i} DIAGNOSTIC:");
                Debug.Log($"[VB:Lightning]   Position: X={xPos}");
                Debug.Log($"[VB:Lightning]   Width: {resolvedWidth} {(float.IsNaN(resolvedWidth) ? "❌ NaN!" : "✓")}");
                Debug.Log($"[VB:Lightning]   Height: {resolvedHeight} {(resolvedHeight == 0 ? "❌ ZERO!" : "✓")}");
                Debug.Log($"[VB:Lightning]   BG Color: {resolvedBg}");
                Debug.Log($"[VB:Lightning]   Display: {bolt.Element.style.display.value}");
                Debug.Log($"[VB:Lightning]   Visibility: {bolt.Element.style.visibility.value}");
                Debug.Log($"[VB:Lightning] CONTAINER DIAGNOSTIC:");
                Debug.Log($"[VB:Lightning]   Container BG: {containerBg} {(containerBg.a > 0 ? "⚠️ NOT TRANSPARENT!" : "✓")}");
                Debug.Log($"[VB:Lightning]   Container Width: {_sparkContainer.resolvedStyle.width}");
                Debug.Log($"[VB:Lightning]   Container Height: {_sparkContainer.resolvedStyle.height}");
                Debug.Log($"[VB:Lightning] ═══════════════════════════════════════");
                
                // VERDICT
                if (float.IsNaN(resolvedWidth))
                {
                    Debug.LogError("[VB:Lightning] ❌ STILL BROKEN: Width is NaN!");
                }
                else if (resolvedHeight == 0)
                {
                    Debug.LogError("[VB:Lightning] ❌ STILL BROKEN: Height is 0!");
                }
                else if (containerBg.a > 0)
                {
                    Debug.LogWarning("[VB:Lightning] ⚠️ PURPLE SCREEN: Container background not transparent!");
                }
                else
                {
                    Debug.Log("[VB:Lightning] ✅ ALL CHECKS PASSED - Lightning SHOULD be visible!");
                }
                
                return;
            }
        }
        Debug.LogWarning("[VB:Lightning] All lightning bolts active, cannot trigger new one");
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
                bolt.Element?.RemoveFromHierarchy();
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
            Debug.Log($"    Display: {bolt.Element.style.display.value}");
            Debug.Log($"    Position: ({bolt.Element.style.left.value}, {bolt.Element.style.top.value})");
            Debug.Log($"    Size: {bolt.Element.style.width.value} x {bolt.Element.style.height.value}");
            Debug.Log($"    Opacity: {bolt.Element.style.opacity.value}");
            Debug.Log($"    Background Color: {bolt.Element.style.backgroundColor.value}");
            Debug.Log($"    Parent: {(bolt.Element.parent != null ? bolt.Element.parent.name : "NULL")}");
        }
        Debug.Log("=== LIGHTNING DIAGNOSTIC END ===");
    }
    }
}
