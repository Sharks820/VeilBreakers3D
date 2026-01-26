using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Effects
{
    /// <summary>
    /// Controls animated particle effects for UI Toolkit menus.
    /// Creates floating embers, dust particles, and veil pulse effects.
    /// </summary>
    public class UIParticleController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Particle Settings")]
        [SerializeField] private int _emberCount = 15;
        [SerializeField] private int _dustCount = 25;
        [SerializeField] private float _emberSpeed = 30f;
        [SerializeField] private float _dustSpeed = 15f;
        [SerializeField] private float _veilPulseSpeed = 0.5f;

        [Header("Colors")]
        [SerializeField] private Color _emberColor = new Color(1f, 0.7f, 0.3f, 0.8f);
        [SerializeField] private Color _dustColor = new Color(0.8f, 0.7f, 0.6f, 0.3f);
        [SerializeField] private Color _veilColor = new Color(0.5f, 0.27f, 0.7f, 0.05f);

        // =============================================================================
        // PRIVATE STATE
        // =============================================================================

        private VisualElement _root;
        private VisualElement _particleContainer;
        private VisualElement _emberContainer;
        private List<VisualElement> _veilPulses;
        private List<ParticleData> _embers;
        private List<ParticleData> _dustParticles;
        private float _screenWidth;
        private float _screenHeight;
        private bool _isInitialized;

        // =============================================================================
        // DATA STRUCTURES
        // =============================================================================

        private class ParticleData
        {
            public VisualElement Element;
            public float X;
            public float Y;
            public float SpeedX;
            public float SpeedY;
            public float Size;
            public float Alpha;
            public float AlphaSpeed;
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

            _embers = new List<ParticleData>();
            _dustParticles = new List<ParticleData>();
            _veilPulses = new List<VisualElement>();
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
            UpdateVeilPulses(deltaTime);
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

            // Get screen dimensions
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;

            // Find veil pulse elements
            var veilPulseLayer = _root.Q<VisualElement>("veil-pulse-layer");
            if (veilPulseLayer != null)
            {
                _veilPulses.Add(_root.Q<VisualElement>("veil-pulse-1"));
                _veilPulses.Add(_root.Q<VisualElement>("veil-pulse-2"));
                _veilPulses.Add(_root.Q<VisualElement>("veil-pulse-3"));
            }

            // Create particles
            CreateEmbers();
            CreateDustParticles();

            _isInitialized = true;
        }

        public void Initialize(VisualElement root)
        {
            _root = root;
            if (_root == null) return;

            _particleContainer = _root.Q<VisualElement>("particle-container");
            _emberContainer = _root.Q<VisualElement>("ember-container");

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

            CreateEmbers();
            CreateDustParticles();

            _isInitialized = true;
        }

        // =============================================================================
        // PARTICLE CREATION
        // =============================================================================

        private void CreateEmbers()
        {
            if (_emberContainer == null) return;

            for (int i = 0; i < _emberCount; i++)
            {
                var ember = CreateParticle(
                    Random.Range(2f, 5f),
                    _emberColor,
                    true
                );

                var data = new ParticleData
                {
                    Element = ember,
                    X = Random.Range(0f, _screenWidth),
                    Y = Random.Range(0f, _screenHeight),
                    SpeedX = Random.Range(-10f, 10f),
                    SpeedY = Random.Range(-_emberSpeed, -_emberSpeed * 0.5f),
                    Size = Random.Range(2f, 5f),
                    Alpha = Random.Range(0.3f, 0.9f),
                    AlphaSpeed = Random.Range(0.3f, 0.8f)
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
                var dust = CreateParticle(
                    Random.Range(1f, 3f),
                    _dustColor,
                    false
                );

                var data = new ParticleData
                {
                    Element = dust,
                    X = Random.Range(0f, _screenWidth),
                    Y = Random.Range(0f, _screenHeight),
                    SpeedX = Random.Range(-5f, 5f),
                    SpeedY = Random.Range(-_dustSpeed, _dustSpeed * 0.3f),
                    Size = Random.Range(1f, 3f),
                    Alpha = Random.Range(0.1f, 0.4f),
                    AlphaSpeed = Random.Range(0.1f, 0.3f)
                };

                dust.style.left = data.X;
                dust.style.top = data.Y;

                _particleContainer.Add(dust);
                _dustParticles.Add(data);
            }
        }

        private VisualElement CreateParticle(float size, Color color, bool glow)
        {
            var particle = new VisualElement();
            particle.style.position = Position.Absolute;
            particle.style.width = size;
            particle.style.height = size;
            particle.style.borderTopLeftRadius = size / 2;
            particle.style.borderTopRightRadius = size / 2;
            particle.style.borderBottomLeftRadius = size / 2;
            particle.style.borderBottomRightRadius = size / 2;
            particle.style.backgroundColor = color;

            if (glow)
            {
                // Add a subtle glow effect via border
                particle.style.borderTopWidth = 1;
                particle.style.borderBottomWidth = 1;
                particle.style.borderLeftWidth = 1;
                particle.style.borderRightWidth = 1;
                particle.style.borderTopColor = new Color(color.r, color.g, color.b, color.a * 0.5f);
                particle.style.borderBottomColor = new Color(color.r, color.g, color.b, color.a * 0.5f);
                particle.style.borderLeftColor = new Color(color.r, color.g, color.b, color.a * 0.5f);
                particle.style.borderRightColor = new Color(color.r, color.g, color.b, color.a * 0.5f);
            }

            return particle;
        }

        // =============================================================================
        // PARTICLE UPDATES
        // =============================================================================

        private void UpdateEmbers(float deltaTime)
        {
            foreach (var ember in _embers)
            {
                // Move upward with slight horizontal drift
                ember.Y += ember.SpeedY * deltaTime;
                ember.X += ember.SpeedX * deltaTime;

                // Slight horizontal oscillation
                ember.SpeedX += Random.Range(-20f, 20f) * deltaTime;
                ember.SpeedX = Mathf.Clamp(ember.SpeedX, -15f, 15f);

                // Pulse alpha
                float time = Time.time * ember.AlphaSpeed;
                float alpha = ember.Alpha * (0.5f + 0.5f * Mathf.Sin(time));

                // Reset if off screen
                if (ember.Y < -20 || ember.X < -20 || ember.X > _screenWidth + 20)
                {
                    ember.Y = _screenHeight + Random.Range(10f, 50f);
                    ember.X = Random.Range(0f, _screenWidth);
                    ember.SpeedX = Random.Range(-10f, 10f);
                }

                // Apply position
                ember.Element.style.left = ember.X;
                ember.Element.style.top = ember.Y;
                ember.Element.style.opacity = alpha;
            }
        }

        private void UpdateDustParticles(float deltaTime)
        {
            foreach (var dust in _dustParticles)
            {
                // Slow drift
                dust.Y += dust.SpeedY * deltaTime;
                dust.X += dust.SpeedX * deltaTime;

                // Very subtle movement changes
                dust.SpeedX += Random.Range(-5f, 5f) * deltaTime;
                dust.SpeedY += Random.Range(-3f, 3f) * deltaTime;
                dust.SpeedX = Mathf.Clamp(dust.SpeedX, -8f, 8f);
                dust.SpeedY = Mathf.Clamp(dust.SpeedY, -_dustSpeed, _dustSpeed * 0.5f);

                // Slow alpha pulse
                float time = Time.time * dust.AlphaSpeed * 0.5f;
                float alpha = dust.Alpha * (0.6f + 0.4f * Mathf.Sin(time));

                // Wrap around screen
                if (dust.Y < -10) dust.Y = _screenHeight + 10;
                if (dust.Y > _screenHeight + 10) dust.Y = -10;
                if (dust.X < -10) dust.X = _screenWidth + 10;
                if (dust.X > _screenWidth + 10) dust.X = -10;

                // Apply
                dust.Element.style.left = dust.X;
                dust.Element.style.top = dust.Y;
                dust.Element.style.opacity = alpha;
            }
        }

        private void UpdateVeilPulses(float deltaTime)
        {
            float time = Time.time * _veilPulseSpeed;

            for (int i = 0; i < _veilPulses.Count; i++)
            {
                var pulse = _veilPulses[i];
                if (pulse == null) continue;

                // Each pulse has a different phase
                float phase = i * 2.1f;
                float scale = 1f + 0.15f * Mathf.Sin(time + phase);
                float alpha = 0.03f + 0.02f * Mathf.Sin(time * 0.7f + phase);

                pulse.style.scale = new Scale(new Vector2(scale, scale));
                pulse.style.opacity = alpha;
            }
        }

        // =============================================================================
        // CLEANUP
        // =============================================================================

        private void ClearParticles()
        {
            foreach (var ember in _embers)
            {
                ember.Element?.RemoveFromHierarchy();
            }
            _embers.Clear();

            foreach (var dust in _dustParticles)
            {
                dust.Element?.RemoveFromHierarchy();
            }
            _dustParticles.Clear();

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
            _emberSpeed = 30f * intensity;
            _dustSpeed = 15f * intensity;
            _veilPulseSpeed = 0.5f * intensity;
        }
    }
}
