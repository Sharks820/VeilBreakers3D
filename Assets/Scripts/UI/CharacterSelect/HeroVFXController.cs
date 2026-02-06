using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Per-hero particle overlays rendered as UI Toolkit VisualElements.
    /// Follows the TitleScreenVFX pattern but with much lower particle counts (30-50).
    /// Each hero has a unique particle style:
    ///   Vex: drifting embers (orange/red)
    ///   Seraphina: floating spores (green/gold)
    ///   Orion: lightning sparks (blue/white)
    ///   Nyx: shadow wisps (purple/dark)
    /// </summary>
    public class HeroVFXController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Particle Counts")]
        [SerializeField] private int _particleCount = 35;

        [Header("Speed")]
        [SerializeField] private float _speedMin = 8f;
        [SerializeField] private float _speedMax = 25f;

        [Header("Size")]
        [SerializeField] private float _sizeMin = 2f;
        [SerializeField] private float _sizeMax = 5f;

        // =============================================================================
        // STATE
        // =============================================================================

        private VisualElement _vfxOverlay;
        private readonly List<VisualElement> _particles = new List<VisualElement>(50);
        private readonly List<ParticleState> _states = new List<ParticleState>(50);
        private string _currentHeroId;
        private bool _isDestroyed;
        private bool _isRunning;
        private Coroutine _animationCoroutine;

        // =============================================================================
        // PARTICLE STATE
        // =============================================================================

        private struct ParticleState
        {
            public float X;
            public float Y;
            public float SpeedX;
            public float SpeedY;
            public float Size;
            public float Lifetime;
            public float MaxLifetime;
            public float FlickerPhase;
            public float FlickerSpeed;
        }

        // =============================================================================
        // HERO VFX PROFILES
        // =============================================================================

        private struct VFXProfile
        {
            public Color ColorA;
            public Color ColorB;
            public float SpeedMultiplier;
            public float DriftX; // Horizontal drift bias
            public float DriftY; // Vertical drift bias (-1 = down, 1 = up)
            public float FlickerIntensity;
        }

        private static readonly Dictionary<string, VFXProfile> Profiles = new Dictionary<string, VFXProfile>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["vex"] = new VFXProfile
            {
                ColorA = new Color(1f, 0.55f, 0.1f, 0.85f),   // Hot ember orange
                ColorB = new Color(1f, 0.25f, 0f, 0.5f),       // Deep red
                SpeedMultiplier = 1f,
                DriftX = 0.3f,
                DriftY = 1f, // Rise up
                FlickerIntensity = 0.6f
            },
            ["seraphina"] = new VFXProfile
            {
                ColorA = new Color(0.4f, 0.9f, 0.3f, 0.7f),   // Nature green
                ColorB = new Color(0.85f, 0.8f, 0.2f, 0.4f),   // Golden pollen
                SpeedMultiplier = 0.7f,
                DriftX = 0.5f,
                DriftY = 0.3f, // Gentle float
                FlickerIntensity = 0.3f
            },
            ["orion"] = new VFXProfile
            {
                ColorA = new Color(0.4f, 0.6f, 1f, 0.9f),     // Electric blue
                ColorB = new Color(0.8f, 0.9f, 1f, 0.6f),     // White-blue
                SpeedMultiplier = 1.5f,
                DriftX = 0.8f,
                DriftY = 0.2f, // Erratic
                FlickerIntensity = 0.9f // Sparky
            },
            ["nyx"] = new VFXProfile
            {
                ColorA = new Color(0.5f, 0.2f, 0.8f, 0.6f),   // Shadow purple
                ColorB = new Color(0.15f, 0.05f, 0.2f, 0.35f), // Deep void
                SpeedMultiplier = 0.5f,
                DriftX = 0.2f,
                DriftY = -0.5f, // Sink down
                FlickerIntensity = 0.4f
            }
        };

        // =============================================================================
        // LIFECYCLE
        // =============================================================================

        public void Initialize(VisualElement vfxOverlay)
        {
            _vfxOverlay = vfxOverlay;
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            ClearParticles();
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Switch to the given hero's VFX profile. Clears existing particles
        /// and spawns new ones with the hero's style.
        /// </summary>
        public void SetHero(HeroData hero)
        {
            if (_isDestroyed || hero == null || _vfxOverlay == null) return;

            string heroId = hero.hero_id;
            if (heroId == _currentHeroId) return;

            _currentHeroId = heroId;

            ClearParticles();

            if (!Profiles.TryGetValue(heroId, out var profile))
            {
                // Unknown hero - use hero color as fallback
                Color c = hero.color_palette != null ? hero.color_palette.ToColor() : Color.white;
                profile = new VFXProfile
                {
                    ColorA = new Color(c.r, c.g, c.b, 0.7f),
                    ColorB = new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, 0.3f),
                    SpeedMultiplier = 1f, DriftX = 0.3f, DriftY = 0.5f, FlickerIntensity = 0.5f
                };
            }

            SpawnParticles(profile);

            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            _isRunning = true;
            _animationCoroutine = StartCoroutine(AnimateParticles(profile));
        }

        // =============================================================================
        // PARTICLE MANAGEMENT
        // =============================================================================

        private void SpawnParticles(VFXProfile profile)
        {
            for (int i = 0; i < _particleCount; i++)
            {
                var particle = new VisualElement();
                particle.style.position = Position.Absolute;
                particle.pickingMode = PickingMode.Ignore;

                // Random properties
                float size = Random.Range(_sizeMin, _sizeMax);
                float lifetime = Random.Range(1.5f, 4f);
                Color color = Color.Lerp(profile.ColorA, profile.ColorB, Random.value);

                particle.style.width = size;
                particle.style.height = size;
                particle.style.borderTopLeftRadius = size * 0.5f;
                particle.style.borderTopRightRadius = size * 0.5f;
                particle.style.borderBottomLeftRadius = size * 0.5f;
                particle.style.borderBottomRightRadius = size * 0.5f;
                particle.style.backgroundColor = color;

                float x = Random.Range(5f, 95f); // % of parent width
                float y = Random.Range(10f, 90f);

                particle.style.left = new Length(x, LengthUnit.Percent);
                particle.style.top = new Length(y, LengthUnit.Percent);
                particle.style.opacity = 0; // Start invisible

                _vfxOverlay.Add(particle);
                _particles.Add(particle);

                _states.Add(new ParticleState
                {
                    X = x,
                    Y = y,
                    SpeedX = Random.Range(-_speedMax, _speedMax) * profile.SpeedMultiplier * profile.DriftX,
                    SpeedY = Random.Range(_speedMin, _speedMax) * profile.SpeedMultiplier * profile.DriftY * -1f,
                    Size = size,
                    Lifetime = Random.Range(0f, lifetime), // Stagger start
                    MaxLifetime = lifetime,
                    FlickerPhase = Random.Range(0f, Mathf.PI * 2f),
                    FlickerSpeed = Random.Range(1f, 3f)
                });
            }
        }

        private void ClearParticles()
        {
            _isRunning = false;
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);

            foreach (var p in _particles)
            {
                p.RemoveFromHierarchy();
            }
            _particles.Clear();
            _states.Clear();
        }

        // =============================================================================
        // ANIMATION LOOP
        // =============================================================================

        private IEnumerator AnimateParticles(VFXProfile profile)
        {
            while (_isRunning && !_isDestroyed)
            {
                float dt = Time.unscaledDeltaTime;

                for (int i = 0; i < _particles.Count; i++)
                {
                    var state = _states[i];
                    state.Lifetime += dt;

                    // Respawn if expired
                    if (state.Lifetime >= state.MaxLifetime)
                    {
                        state.Lifetime = 0f;
                        state.X = Random.Range(5f, 95f);
                        state.Y = profile.DriftY > 0 ? Random.Range(70f, 95f) : Random.Range(5f, 30f);
                        state.SpeedX = Random.Range(-_speedMax, _speedMax) * profile.SpeedMultiplier * profile.DriftX;
                        state.SpeedY = Random.Range(_speedMin, _speedMax) * profile.SpeedMultiplier * profile.DriftY * -1f;
                        state.MaxLifetime = Random.Range(1.5f, 4f);
                        state.FlickerPhase = Random.Range(0f, Mathf.PI * 2f);
                    }

                    // Move
                    state.X += state.SpeedX * dt * 0.1f; // Scale down since we're in percent
                    state.Y += state.SpeedY * dt * 0.1f;

                    // Wrap horizontally
                    if (state.X < -5f) state.X = 105f;
                    if (state.X > 105f) state.X = -5f;

                    // Age-based alpha (fade in, hold, fade out)
                    float lifeT = state.Lifetime / state.MaxLifetime;
                    float alpha;
                    if (lifeT < 0.15f) alpha = lifeT / 0.15f;          // Fade in
                    else if (lifeT > 0.75f) alpha = (1f - lifeT) / 0.25f; // Fade out
                    else alpha = 1f;                                     // Hold

                    // Flicker
                    float flicker = 1f - profile.FlickerIntensity * 0.5f *
                        (1f + Mathf.Sin(Time.time * state.FlickerSpeed + state.FlickerPhase));
                    alpha *= flicker;

                    _states[i] = state;

                    // Apply
                    var particle = _particles[i];
                    particle.style.left = new Length(state.X, LengthUnit.Percent);
                    particle.style.top = new Length(state.Y, LengthUnit.Percent);
                    particle.style.opacity = Mathf.Clamp01(alpha);
                }

                yield return null;
            }
        }
    }
}
