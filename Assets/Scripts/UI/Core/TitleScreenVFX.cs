using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// AAA Title Screen VFX Controller.
    /// Creates floating embers, ash particles, and atmospheric effects
    /// for a dark fantasy horror aesthetic.
    /// </summary>
    public class TitleScreenVFX : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Ember Settings")]
        [SerializeField] private int _emberCount = 40;
        [SerializeField] private float _emberSpeedMin = 15f;
        [SerializeField] private float _emberSpeedMax = 45f;
        [SerializeField] private float _emberSizeMin = 2f;
        [SerializeField] private float _emberSizeMax = 6f;
        [SerializeField] private Color _emberColorCore = new Color(1f, 0.4f, 0.1f, 1f);
        [SerializeField] private Color _emberColorGlow = new Color(1f, 0.6f, 0.2f, 0.6f);

        [Header("Ash Settings")]
        [SerializeField] private int _ashCount = 30;
        [SerializeField] private float _ashSpeedMin = 8f;
        [SerializeField] private float _ashSpeedMax = 20f;
        [SerializeField] private float _ashSizeMin = 3f;
        [SerializeField] private float _ashSizeMax = 8f;
        [SerializeField] private Color _ashColor = new Color(0.3f, 0.28f, 0.25f, 0.7f);

        [Header("Spark Settings")]
        [SerializeField] private int _sparkCount = 15;
        [SerializeField] private float _sparkSpeedMin = 60f;
        [SerializeField] private float _sparkSpeedMax = 120f;

        [Header("Animation")]
        [SerializeField] private float _windStrength = 0.3f;
        [SerializeField] private float _windFrequency = 0.5f;
        [SerializeField] private float _flickerSpeed = 8f;

        [Header("Spawn Area")]
        [SerializeField] private float _spawnMarginBottom = 0.1f; // Spawn from bottom 10%
        [SerializeField] private float _spawnMarginSides = 0.2f;  // Avoid edges

        // =============================================================================
        // STATE
        // =============================================================================

        private VisualElement _vfxContainer;
        private readonly List<EmberParticle> _embers = new();
        private readonly List<AshParticle> _ashes = new();
        private readonly List<SparkParticle> _sparks = new();
        private bool _isActive;
        private Coroutine _updateCoroutine;
        private float _windOffset;
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

            // Create embers
            for (int i = 0; i < _emberCount; i++)
            {
                CreateEmber();
            }

            // Create ash particles
            for (int i = 0; i < _ashCount; i++)
            {
                CreateAsh();
            }

            // Create sparks
            for (int i = 0; i < _sparkCount; i++)
            {
                CreateSpark();
            }

            StartVFX();
            Debug.Log($"[TitleScreenVFX] Initialized: {_emberCount} embers, {_ashCount} ash, {_sparkCount} sparks");
        }

        // =============================================================================
        // PARTICLE CREATION
        // =============================================================================

        private void CreateEmber()
        {
            // Outer glow
            var glow = new VisualElement();
            glow.style.position = Position.Absolute;
            glow.style.borderTopLeftRadius = 50;
            glow.style.borderTopRightRadius = 50;
            glow.style.borderBottomLeftRadius = 50;
            glow.style.borderBottomRightRadius = 50;
            glow.pickingMode = PickingMode.Ignore;

            // Inner core
            var core = new VisualElement();
            core.style.position = Position.Absolute;
            core.style.borderTopLeftRadius = 50;
            core.style.borderTopRightRadius = 50;
            core.style.borderBottomLeftRadius = 50;
            core.style.borderBottomRightRadius = 50;
            core.pickingMode = PickingMode.Ignore;
            glow.Add(core);

            _vfxContainer.Add(glow);

            var size = UnityEngine.Random.Range(_emberSizeMin, _emberSizeMax);
            var glowSize = size * 3f;

            var ember = new EmberParticle
            {
                GlowElement = glow,
                CoreElement = core,
                Size = size,
                GlowSize = glowSize,
                Speed = UnityEngine.Random.Range(_emberSpeedMin, _emberSpeedMax),
                Lifetime = UnityEngine.Random.Range(4f, 8f),
                Age = UnityEngine.Random.Range(0f, 4f), // Stagger start times
                FlickerPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                DriftPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                DriftAmplitude = UnityEngine.Random.Range(20f, 50f)
            };

            ResetEmberPosition(ember);

            // Style core
            core.style.width = size;
            core.style.height = size;
            core.style.backgroundColor = _emberColorCore;
            core.style.left = (glowSize - size) / 2f;
            core.style.top = (glowSize - size) / 2f;

            // Style glow
            glow.style.width = glowSize;
            glow.style.height = glowSize;
            glow.style.backgroundColor = _emberColorGlow;

            _embers.Add(ember);
        }

        private void CreateAsh()
        {
            var element = new VisualElement();
            element.style.position = Position.Absolute;
            element.style.backgroundColor = _ashColor;
            element.pickingMode = PickingMode.Ignore;

            _vfxContainer.Add(element);

            var sizeX = UnityEngine.Random.Range(_ashSizeMin, _ashSizeMax);
            var sizeY = UnityEngine.Random.Range(_ashSizeMin * 0.3f, _ashSizeMax * 0.5f);

            var ash = new AshParticle
            {
                Element = element,
                SizeX = sizeX,
                SizeY = sizeY,
                Speed = UnityEngine.Random.Range(_ashSpeedMin, _ashSpeedMax),
                Lifetime = UnityEngine.Random.Range(6f, 12f),
                Age = UnityEngine.Random.Range(0f, 6f),
                RotationSpeed = UnityEngine.Random.Range(-90f, 90f),
                Rotation = UnityEngine.Random.Range(0f, 360f),
                DriftPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                DriftAmplitude = UnityEngine.Random.Range(30f, 80f),
                TumblePhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                TumbleSpeed = UnityEngine.Random.Range(1f, 3f)
            };

            ResetAshPosition(ash);

            element.style.width = sizeX;
            element.style.height = sizeY;
            var ashRadius = sizeY * 0.3f;
            element.style.borderTopLeftRadius = ashRadius;
            element.style.borderTopRightRadius = ashRadius;
            element.style.borderBottomLeftRadius = ashRadius;
            element.style.borderBottomRightRadius = ashRadius;

            _ashes.Add(ash);
        }

        private void CreateSpark()
        {
            var element = new VisualElement();
            element.style.position = Position.Absolute;
            element.style.backgroundColor = new Color(1f, 0.9f, 0.5f, 0.9f);
            element.style.borderTopLeftRadius = 50;
            element.style.borderTopRightRadius = 50;
            element.style.borderBottomLeftRadius = 50;
            element.style.borderBottomRightRadius = 50;
            element.pickingMode = PickingMode.Ignore;

            _vfxContainer.Add(element);

            var spark = new SparkParticle
            {
                Element = element,
                Size = UnityEngine.Random.Range(1f, 3f),
                Speed = UnityEngine.Random.Range(_sparkSpeedMin, _sparkSpeedMax),
                Lifetime = UnityEngine.Random.Range(0.5f, 1.5f),
                Age = UnityEngine.Random.Range(0f, 10f), // Long delay between sparks
                Direction = new Vector2(
                    UnityEngine.Random.Range(-0.3f, 0.3f),
                    UnityEngine.Random.Range(-1f, -0.7f)
                ).normalized
            };

            ResetSparkPosition(spark);

            element.style.width = spark.Size;
            element.style.height = spark.Size;
            element.style.opacity = 0;

            _sparks.Add(spark);
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
                UnityEngine.Random.Range(-0.3f, 0.3f),
                UnityEngine.Random.Range(-1f, -0.7f)
            ).normalized;
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

                float wind = Mathf.Sin(_windOffset) * _windStrength;

                // Update embers
                foreach (var ember in _embers)
                {
                    UpdateEmber(ember, deltaTime, wind);
                }

                // Update ash
                foreach (var ash in _ashes)
                {
                    UpdateAsh(ash, deltaTime, wind);
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

        private void UpdateEmber(EmberParticle ember, float deltaTime, float wind)
        {
            ember.Age += deltaTime;

            // Calculate opacity with fade in/out
            float normalizedAge = ember.Age / ember.Lifetime;
            float opacity;

            if (normalizedAge < 0.15f)
            {
                opacity = normalizedAge / 0.15f;
            }
            else if (normalizedAge > 0.7f)
            {
                opacity = (1f - normalizedAge) / 0.3f;
            }
            else
            {
                opacity = 1f;
            }

            // Flicker effect
            float flicker = 0.7f + 0.3f * Mathf.Sin(ember.Age * _flickerSpeed + ember.FlickerPhase);
            opacity *= flicker;

            // Move upward with drift
            float drift = Mathf.Sin(ember.Age * 1.5f + ember.DriftPhase) * ember.DriftAmplitude * deltaTime;
            ember.Position.x += drift + wind * ember.Speed * deltaTime;
            ember.Position.y -= ember.Speed * deltaTime;

            // Apply position and opacity
            ember.GlowElement.style.left = ember.Position.x - ember.GlowSize / 2f;
            ember.GlowElement.style.top = ember.Position.y - ember.GlowSize / 2f;
            ember.GlowElement.style.opacity = opacity * 0.6f;
            ember.CoreElement.style.opacity = opacity;

            // Reset if off screen or lifetime exceeded
            if (ember.Age >= ember.Lifetime || ember.Position.y < -50f)
            {
                ResetEmberPosition(ember);
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
    }
}
