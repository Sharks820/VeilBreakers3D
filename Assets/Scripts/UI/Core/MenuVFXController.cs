using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// AAA menu VFX controller for ambient particles and visual polish.
    /// Creates floating corruption particles, veil wisps, and ambient effects.
    /// </summary>
    public class MenuVFXController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Particle Settings")]
        [SerializeField] private int _corruptionParticleCount = 20;
        [SerializeField] private int _veilWispCount = 10;
        [SerializeField] private float _particleSpeedMin = 10f;
        [SerializeField] private float _particleSpeedMax = 30f;
        [SerializeField] private float _wispSpeedMin = 5f;
        [SerializeField] private float _wispSpeedMax = 15f;

        [Header("Animation")]
        [SerializeField] private float _fadeInDuration = 2f;
        [SerializeField] private float _fadeOutDuration = 1f;
        [SerializeField] private float _particleLifetimeMin = 4f;
        [SerializeField] private float _particleLifetimeMax = 8f;

        // =============================================================================
        // STATE
        // =============================================================================

        private VisualElement _vfxContainer;
        private readonly List<AmbientParticle> _particles = new();
        private bool _isActive;
        private Coroutine _updateCoroutine;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Start()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>(); // VB-IGNORE UNITY-05 -- optional fallback, UIDocument may be assigned via inspector
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

            if (_uiDocument != null && _uiDocument.rootVisualElement != null)
                _uiDocument.rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
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
            _vfxContainer.name = "vfx-container";
            _vfxContainer.AddToClassList("vfx-container");

            // Insert at the back (behind other elements)
            root.Insert(0, _vfxContainer);

            // Create corruption particles
            for (int i = 0; i < _corruptionParticleCount; i++)
            {
                CreateCorruptionParticle();
            }

            // Create veil wisps
            for (int i = 0; i < _veilWispCount; i++)
            {
                CreateVeilWisp();
            }

            StartVFX();
            Debug.Log($"[MenuVFXController] Initialized with {_particles.Count} particles");
        }

        // =============================================================================
        // PARTICLE CREATION
        // =============================================================================

        private void CreateCorruptionParticle()
        {
            var particle = new VisualElement();
            particle.AddToClassList("corruption-particle");
            _vfxContainer.Add(particle);

            var data = new AmbientParticle
            {
                Element = particle,
                Type = ParticleType.Corruption,
                Speed = Random.Range(_particleSpeedMin, _particleSpeedMax),
                Lifetime = Random.Range(_particleLifetimeMin, _particleLifetimeMax),
                Age = 0,
                Direction = new Vector2(
                    Random.Range(-0.3f, 0.3f),
                    Random.Range(-1f, -0.5f) // Drift upward
                ).normalized
            };

            ResetParticlePosition(data);
            _particles.Add(data);
        }

        private void CreateVeilWisp()
        {
            var particle = new VisualElement();
            particle.AddToClassList("veil-wisp");
            _vfxContainer.Add(particle);

            var data = new AmbientParticle
            {
                Element = particle,
                Type = ParticleType.VeilWisp,
                Speed = Random.Range(_wispSpeedMin, _wispSpeedMax),
                Lifetime = Random.Range(_particleLifetimeMin * 1.5f, _particleLifetimeMax * 1.5f),
                Age = 0,
                Direction = new Vector2(
                    Random.Range(-0.5f, 0.5f),
                    Random.Range(-0.8f, -0.3f)
                ).normalized,
                WobblePhase = Random.Range(0f, Mathf.PI * 2f),
                WobbleSpeed = Random.Range(1f, 2f)
            };

            ResetParticlePosition(data);
            _particles.Add(data);
        }

        private void ResetParticlePosition(AmbientParticle particle)
        {
            // Start from bottom of screen with random X position
            float screenWidth = _vfxContainer.resolvedStyle.width;
            float screenHeight = _vfxContainer.resolvedStyle.height;

            if (screenWidth <= 0) screenWidth = 1920;
            if (screenHeight <= 0) screenHeight = 1080;

            particle.Position = new Vector2(
                Random.Range(0, screenWidth),
                screenHeight + Random.Range(0, 50f)
            );
            particle.Age = 0;
            particle.Element.style.opacity = 0;

            // Randomize size slightly
            float size = particle.Type == ParticleType.Corruption
                ? Random.Range(3f, 6f)
                : Random.Range(6f, 12f);
            particle.Element.style.width = size;
            particle.Element.style.height = size;
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

                foreach (var particle in _particles)
                {
                    UpdateParticle(particle, deltaTime);
                }

                yield return null;
            }
        }

        private void UpdateParticle(AmbientParticle particle, float deltaTime)
        {
            particle.Age += deltaTime;

            // Calculate opacity based on lifetime (fade in, hold, fade out)
            float normalizedAge = particle.Age / particle.Lifetime;
            float opacity;

            if (normalizedAge < 0.2f)
            {
                // Fade in
                opacity = normalizedAge / 0.2f;
            }
            else if (normalizedAge > 0.8f)
            {
                // Fade out
                opacity = (1f - normalizedAge) / 0.2f;
            }
            else
            {
                opacity = 1f;
            }

            // Apply base opacity from particle type
            float baseOpacity = particle.Type == ParticleType.Corruption ? 0.6f : 0.4f;
            particle.Element.style.opacity = opacity * baseOpacity;

            // Move particle
            Vector2 movement = particle.Direction * particle.Speed * deltaTime;

            // Add wobble for wisps
            if (particle.Type == ParticleType.VeilWisp)
            {
                float wobble = Mathf.Sin(particle.Age * particle.WobbleSpeed + particle.WobblePhase) * 0.5f;
                movement.x += wobble * deltaTime * 20f;
            }

            particle.Position += movement;

            // Apply position
            particle.Element.style.left = particle.Position.x;
            particle.Element.style.top = particle.Position.y;

            // Reset if lifetime exceeded or off screen
            if (particle.Age >= particle.Lifetime || particle.Position.y < -50f)
            {
                ResetParticlePosition(particle);
            }
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Trigger a burst of particles at a specific position.
        /// Useful for button clicks or important events.
        /// </summary>
        public void BurstAt(Vector2 position, int count = 5)
        {
            int spawned = 0;
            foreach (var particle in _particles)
            {
                if (spawned >= count) break;

                // Only use particles that are nearly faded out
                if (particle.Age / particle.Lifetime > 0.7f)
                {
                    particle.Position = position;
                    particle.Age = 0;
                    particle.Direction = new Vector2(
                        Random.Range(-1f, 1f),
                        Random.Range(-1f, 0f)
                    ).normalized;
                    spawned++;
                }
            }
        }

        /// <summary>
        /// Set the corruption intensity (0-1) to adjust particle density.
        /// </summary>
        public void SetCorruptionIntensity(float intensity)
        {
            intensity = Mathf.Clamp01(intensity);

            // Adjust opacity of corruption particles based on intensity
            foreach (var particle in _particles)
            {
                if (particle.Type == ParticleType.Corruption)
                {
                    particle.Speed = Mathf.Lerp(_particleSpeedMin, _particleSpeedMax * 1.5f, intensity);
                }
            }
        }

        // =============================================================================
        // NESTED TYPES
        // =============================================================================

        private enum ParticleType
        {
            Corruption,
            VeilWisp
        }

        private class AmbientParticle
        {
            public VisualElement Element;
            public ParticleType Type;
            public Vector2 Position;
            public Vector2 Direction;
            public float Speed;
            public float Lifetime;
            public float Age;
            public float WobblePhase;
            public float WobbleSpeed;
        }
    }
}
