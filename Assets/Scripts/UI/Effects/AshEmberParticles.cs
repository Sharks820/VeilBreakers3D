using UnityEngine;

namespace VeilBreakers.UI.Effects
{
    /// <summary>
    /// EFFECT 4: ASH & EMBERS PARTICLE FIELD
    ///
    /// Floating debris that reacts to the Veil's pressure.
    /// - At rest: ash drifts slowly INWARD (toward demon chest)
    /// - During Pulse: ash violently PUSHED OUTWARD
    /// - Embers (glowing) are RARE, not constant
    ///
    /// Rules:
    /// - Low spawn rate (quality over quantity)
    /// - Direction matters more than particle count
    /// - NEVER symmetrical
    /// - GPU particles only
    /// </summary>
    public class AshEmberParticles : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Particle System Reference")]
        [SerializeField] private ParticleSystem _particleSystem;

        [Header("Spawn Rates")]
        [SerializeField] private float _idleSpawnRate = 3f;
        [SerializeField] private float _pulseSpawnRate = 15f;

        [Header("Movement - Idle (Inward Drift)")]
        [SerializeField] private float _idleDriftSpeed = 0.3f;
        [SerializeField] private Vector3 _driftCenter = Vector3.zero; // Demon chest position

        [Header("Movement - Pulse (Outward Push)")]
        [SerializeField] private float _pulseForceMin = 2f;
        [SerializeField] private float _pulseForceMax = 5f;

        [Header("Ember Settings (Rare Glowing Particles)")]
        [SerializeField] [Range(0f, 0.2f)] private float _emberChance = 0.08f;
        [SerializeField] private Color _ashColor = new Color(0.2f, 0.18f, 0.15f, 0.6f);
        [SerializeField] private Color _emberColor = new Color(1f, 0.4f, 0.1f, 0.9f);

        [Header("Size")]
        [SerializeField] private float _minSize = 0.02f;
        [SerializeField] private float _maxSize = 0.08f;

        [Header("Lifetime")]
        [SerializeField] private float _minLifetime = 3f;
        [SerializeField] private float _maxLifetime = 6f;

        // =============================================================================
        // PRIVATE STATE
        // =============================================================================

        private ParticleSystem.MainModule _mainModule;
        private ParticleSystem.EmissionModule _emissionModule;
        private ParticleSystem.VelocityOverLifetimeModule _velocityModule;
        private ParticleSystem.ColorOverLifetimeModule _colorModule;
        private ParticleSystem.ExternalForcesModule _externalForcesModule;

        private float _currentPulse;
        private bool _isPulsing;
        private ParticleSystem.Particle[] _particles;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_particleSystem == null)
            {
                _particleSystem = GetComponent<ParticleSystem>();
            }

            if (_particleSystem != null)
            {
                InitializeParticleSystem();
            }
        }

        private void OnEnable()
        {
            if (MenuPulseController.Instance != null)
            {
                MenuPulseController.Instance.OnPulseChanged += OnPulseChanged;
                MenuPulseController.Instance.OnCrimsonRupture += OnCrimsonRupture;
            }
        }

        private void OnDisable()
        {
            if (MenuPulseController.Instance != null)
            {
                MenuPulseController.Instance.OnPulseChanged -= OnPulseChanged;
                MenuPulseController.Instance.OnCrimsonRupture -= OnCrimsonRupture;
            }
        }

        private void Update()
        {
            if (_particleSystem == null) return;

            float pulse = MenuPulseController.Instance != null
                ? MenuPulseController.Instance.NormalizedPulse
                : 0f;

            _isPulsing = pulse > 0.3f;

            UpdateEmissionRate(pulse);
            UpdateParticleMovement(pulse);
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void InitializeParticleSystem()
        {
            _mainModule = _particleSystem.main;
            _emissionModule = _particleSystem.emission;
            _velocityModule = _particleSystem.velocityOverLifetime;
            _colorModule = _particleSystem.colorOverLifetime;

            // Configure main module
            _mainModule.startLifetime = new ParticleSystem.MinMaxCurve(_minLifetime, _maxLifetime);
            _mainModule.startSize = new ParticleSystem.MinMaxCurve(_minSize, _maxSize);
            _mainModule.startColor = _ashColor;
            _mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
            _mainModule.maxParticles = 100;

            // Start with idle emission
            _emissionModule.rateOverTime = _idleSpawnRate;

            // Enable velocity over lifetime for direction control
            _velocityModule.enabled = true;

            // Allocate particle array for custom updates
            _particles = new ParticleSystem.Particle[_mainModule.maxParticles];
        }

        // =============================================================================
        // PULSE RESPONSE
        // =============================================================================

        private void OnPulseChanged(float pulse)
        {
            _currentPulse = pulse;
        }

        private void OnCrimsonRupture()
        {
            // Burst of particles during rupture
            if (_particleSystem != null)
            {
                _particleSystem.Emit(20);

                // Force all particles outward
                int count = _particleSystem.GetParticles(_particles);
                for (int i = 0; i < count; i++)
                {
                    Vector3 dir = (_particles[i].position - transform.TransformPoint(_driftCenter)).normalized;
                    _particles[i].velocity = dir * _pulseForceMax * 2f;

                    // Some become embers during rupture
                    if (Random.value < 0.3f)
                    {
                        _particles[i].startColor = _emberColor;
                    }
                }
                _particleSystem.SetParticles(_particles, count);
            }
        }

        // =============================================================================
        // PARTICLE UPDATES
        // =============================================================================

        private void UpdateEmissionRate(float pulse)
        {
            // Emission increases with pulse
            float rate = Mathf.Lerp(_idleSpawnRate, _pulseSpawnRate, pulse);
            _emissionModule.rateOverTime = rate;
        }

        private void UpdateParticleMovement(float pulse)
        {
            if (_particleSystem == null) return;

            int count = _particleSystem.GetParticles(_particles);
            if (count == 0) return;

            Vector3 centerWorld = transform.TransformPoint(_driftCenter);

            for (int i = 0; i < count; i++)
            {
                Vector3 toCenter = centerWorld - _particles[i].position;
                Vector3 fromCenter = -toCenter.normalized;
                float distanceToCenter = toCenter.magnitude;

                if (_isPulsing)
                {
                    // PULSE: Push outward violently
                    float force = Mathf.Lerp(_pulseForceMin, _pulseForceMax, pulse);

                    // Add asymmetry - particles don't all move the same
                    float asymmetry = Mathf.PerlinNoise(
                        _particles[i].position.x * 10f + Time.unscaledTime,
                        _particles[i].position.y * 10f
                    );
                    force *= 0.5f + asymmetry;

                    // Perpendicular component for swirling
                    Vector3 perpendicular = new Vector3(-fromCenter.y, fromCenter.x, 0f);
                    float swirlAmount = (Mathf.PerlinNoise(i * 0.1f, Time.unscaledTime) - 0.5f) * 0.5f;

                    Vector3 pushDir = fromCenter + perpendicular * swirlAmount;
                    _particles[i].velocity = Vector3.Lerp(
                        _particles[i].velocity,
                        pushDir.normalized * force,
                        Time.unscaledDeltaTime * 8f
                    );
                }
                else
                {
                    // IDLE: Drift slowly inward toward chest
                    float driftStrength = _idleDriftSpeed;

                    // Closer to center = slower drift (don't clump at center)
                    if (distanceToCenter < 1f)
                    {
                        driftStrength *= distanceToCenter;
                    }

                    // Add subtle wandering
                    float wanderX = (Mathf.PerlinNoise(i * 0.3f, Time.unscaledTime * 0.5f) - 0.5f) * 0.2f;
                    float wanderY = (Mathf.PerlinNoise(Time.unscaledTime * 0.5f, i * 0.3f) - 0.5f) * 0.2f;
                    Vector3 wander = new Vector3(wanderX, wanderY, 0f);

                    Vector3 targetVelocity = toCenter.normalized * driftStrength + wander;
                    _particles[i].velocity = Vector3.Lerp(
                        _particles[i].velocity,
                        targetVelocity,
                        Time.unscaledDeltaTime * 2f
                    );
                }

                // Rare ember conversion
                if (Random.value < _emberChance * Time.unscaledDeltaTime && pulse > 0.5f)
                {
                    _particles[i].startColor = _emberColor;
                }
            }

            _particleSystem.SetParticles(_particles, count);
        }

        // =============================================================================
        // PUBLIC SETUP
        // =============================================================================

        /// <summary>
        /// Setup at runtime with particle system reference.
        /// </summary>
        public void Setup(ParticleSystem particles, Vector3 driftCenter)
        {
            _particleSystem = particles;
            _driftCenter = driftCenter;

            if (_particleSystem != null)
            {
                InitializeParticleSystem();
            }
        }

        /// <summary>
        /// Set the center point (demon chest) that particles drift toward.
        /// </summary>
        public void SetDriftCenter(Vector3 center)
        {
            _driftCenter = center;
        }
    }
}
