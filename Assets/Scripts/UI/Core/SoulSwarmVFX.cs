using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// Creates soul/wisp particles that are attracted to the mouse cursor
    /// when it enters certain "danger zones" on the screen.
    /// Particles spiral toward the cursor and dissolve when they reach it.
    /// </summary>
    public class SoulSwarmVFX : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        private const float kTrailOpacityVisible = 0.6f;

        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Particle Settings")]
        [SerializeField] private int _maxParticles = 30;
        [SerializeField] private float _particleSizeMin = 3f;
        [SerializeField] private float _particleSizeMax = 8f;

        [Header("Movement")]
        [SerializeField] private float _attractionStrength = 200f;
        [SerializeField] private float _spiralStrength = 150f;
        [SerializeField] private float _maxSpeed = 400f;
        [SerializeField] private float _dissolveDistance = 30f;

        [Header("Spawn Settings")]
        [SerializeField] private float _spawnRate = 0.1f; // Seconds between spawns when active
        [SerializeField] private float _spawnRadius = 200f;

        [Header("Colors")]
        [SerializeField] private Color _soulColorCore = new Color(1f, 0.6f, 0.2f, 1f);
        [SerializeField] private Color _soulColorGlow = new Color(1f, 0.4f, 0.1f, 0.5f);
        [SerializeField] private Color _soulColorTrail = new Color(1f, 0.3f, 0.05f, 0.3f);

        [Header("Danger Zones (Normalized 0-1)")]
        [SerializeField] private List<DangerZone> _dangerZones = new()
        {
            new DangerZone { Center = new Vector2(0.5f, 0.4f), Radius = 0.15f, Intensity = 1f, Name = "Chest" },
            new DangerZone { Center = new Vector2(0.35f, 0.25f), Radius = 0.08f, Intensity = 0.6f, Name = "LeftEye" },
            new DangerZone { Center = new Vector2(0.65f, 0.25f), Radius = 0.08f, Intensity = 0.6f, Name = "RightEye" }
        };

        // =============================================================================
        // STATE
        // =============================================================================

        private VisualElement _vfxContainer;
        private readonly List<SoulParticle> _particles = new();
        private readonly Queue<SoulParticle> _particlePool = new();
        private bool _isActive;
        private Coroutine _updateCoroutine;
        private Vector2 _mousePosition;
        private float _screenWidth;
        private float _screenHeight;
        private float _currentZoneIntensity;
        private float _spawnTimer;
        private bool _mouseInDangerZone;
        private DangerZone _activeZone;

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
                // Re-register mouse callbacks after disable/enable cycle
                if (_uiDocument != null && _uiDocument.rootVisualElement != null)
                {
                    _uiDocument.rootVisualElement.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                    _uiDocument.rootVisualElement.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
                }
                StartVFX();
            }
        }

        private void OnDisable()
        {
            StopVFX();

            // Unregister mouse callbacks to prevent leak if MonoBehaviour is destroyed while UIDocument persists
            if (_uiDocument != null && _uiDocument.rootVisualElement != null)
            {
                _uiDocument.rootVisualElement.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                _uiDocument.rootVisualElement.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
            }
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void Initialize()
        {
            if (_uiDocument == null) return;

            var root = _uiDocument.rootVisualElement;

            _screenWidth = root.resolvedStyle.width;
            _screenHeight = root.resolvedStyle.height;

            if (_screenWidth <= 0) _screenWidth = 1920;
            if (_screenHeight <= 0) _screenHeight = 1080;

            // Create VFX container
            _vfxContainer = new VisualElement();
            _vfxContainer.name = "soul-swarm-container";
            _vfxContainer.style.position = Position.Absolute;
            _vfxContainer.style.left = 0;
            _vfxContainer.style.top = 0;
            _vfxContainer.style.right = 0;
            _vfxContainer.style.bottom = 0;
            _vfxContainer.style.overflow = Overflow.Hidden;
            _vfxContainer.pickingMode = PickingMode.Ignore;

            // Insert on top of other VFX
            root.Add(_vfxContainer);

            // Pre-create particle pool
            for (int i = 0; i < _maxParticles; i++)
            {
                var particle = CreateParticleElement();
                particle.Element.style.display = DisplayStyle.None;
                _particlePool.Enqueue(particle);
            }

            // Register mouse tracking
            root.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            root.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);

            StartVFX();
            Debug.Log($"[SoulSwarmVFX] Initialized with {_maxParticles} pooled particles");
        }

        private SoulParticle CreateParticleElement()
        {
            // Glow layer
            var glow = new VisualElement();
            glow.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
            glow.style.position = Position.Absolute;
            glow.style.borderTopLeftRadius = 50;
            glow.style.borderTopRightRadius = 50;
            glow.style.borderBottomLeftRadius = 50;
            glow.style.borderBottomRightRadius = 50;
            glow.pickingMode = PickingMode.Ignore;
            glow.style.backgroundColor = _soulColorGlow;

            // Core
            var core = new VisualElement();
            core.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
            core.style.position = Position.Absolute;
            core.style.borderTopLeftRadius = 50;
            core.style.borderTopRightRadius = 50;
            core.style.borderBottomLeftRadius = 50;
            core.style.borderBottomRightRadius = 50;
            core.pickingMode = PickingMode.Ignore;
            core.style.backgroundColor = _soulColorCore;
            glow.Add(core);

            // Trail
            var trail = new VisualElement();
            trail.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
            trail.style.position = Position.Absolute;
            trail.style.borderTopLeftRadius = 2;
            trail.style.borderTopRightRadius = 2;
            trail.style.borderBottomLeftRadius = 2;
            trail.style.borderBottomRightRadius = 2;
            trail.pickingMode = PickingMode.Ignore;
            trail.style.backgroundColor = _soulColorTrail;
            trail.style.transformOrigin = new TransformOrigin(0, Length.Percent(50));
            glow.Add(trail);

            _vfxContainer.Add(glow);

            return new SoulParticle
            {
                Element = glow,
                CoreElement = core,
                TrailElement = trail
            };
        }

        // =============================================================================
        // MOUSE TRACKING
        // =============================================================================

        private void OnMouseMove(MouseMoveEvent evt)
        {
            _mousePosition = evt.localMousePosition;
            CheckDangerZones();
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            _mouseInDangerZone = false;
            _currentZoneIntensity = 0;
            _activeZone = null;
        }

        private void CheckDangerZones()
        {
            Vector2 normalizedMouse = new Vector2(
                _mousePosition.x / _screenWidth,
                _mousePosition.y / _screenHeight
            );

            _mouseInDangerZone = false;
            _currentZoneIntensity = 0;
            _activeZone = null;

            foreach (var zone in _dangerZones)
            {
                float distance = Vector2.Distance(normalizedMouse, zone.Center);
                if (distance < zone.Radius)
                {
                    _mouseInDangerZone = true;
                    float zoneIntensity = (1f - distance / zone.Radius) * zone.Intensity;
                    if (zoneIntensity > _currentZoneIntensity)
                    {
                        _currentZoneIntensity = zoneIntensity;
                        _activeZone = zone;
                    }
                }
            }
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

                // Spawn new particles when mouse in danger zone
                if (_mouseInDangerZone && _particlePool.Count > 0)
                {
                    _spawnTimer -= deltaTime;
                    if (_spawnTimer <= 0)
                    {
                        SpawnParticle();
                        _spawnTimer = _spawnRate / _currentZoneIntensity;
                    }
                }

                // Update active particles
                for (int i = _particles.Count - 1; i >= 0; i--)
                {
                    var particle = _particles[i];
                    UpdateParticle(particle, deltaTime);

                    // Return to pool if dead
                    if (!particle.IsAlive)
                    {
                        particle.Element.style.display = DisplayStyle.None;
                        _particles.RemoveAt(i);
                        _particlePool.Enqueue(particle);
                    }
                }

                yield return null;
            }
        }

        private void SpawnParticle()
        {
            if (_particlePool.Count == 0 || _activeZone == null) return;

            var particle = _particlePool.Dequeue();

            // Spawn at edge of active zone
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 zoneCenter = new Vector2(
                _activeZone.Center.x * _screenWidth,
                _activeZone.Center.y * _screenHeight
            );
            float spawnDist = _activeZone.Radius * _screenWidth * 0.8f + Random.Range(0, _spawnRadius);

            particle.Position = zoneCenter + new Vector2(
                Mathf.Cos(angle) * spawnDist,
                Mathf.Sin(angle) * spawnDist
            );

            particle.Velocity = Vector2.zero;
            particle.Size = Random.Range(_particleSizeMin, _particleSizeMax);
            particle.Lifetime = Random.Range(2f, 4f);
            particle.Age = 0;
            particle.IsAlive = true;
            particle.SpiralPhase = Random.Range(0f, Mathf.PI * 2f);
            particle.SpiralDirection = Random.value > 0.5f ? 1f : -1f;

            // Setup visuals
            float glowSize = particle.Size * 3f;
            particle.Element.style.width = glowSize;
            particle.Element.style.height = glowSize;
            particle.Element.style.display = DisplayStyle.Flex;
            particle.Element.style.opacity = 0;

            particle.CoreElement.style.width = particle.Size;
            particle.CoreElement.style.height = particle.Size;
            particle.CoreElement.style.left = (glowSize - particle.Size) / 2f;
            particle.CoreElement.style.top = (glowSize - particle.Size) / 2f;

            particle.TrailElement.style.width = 2;
            particle.TrailElement.style.height = particle.Size * 0.5f;
            particle.TrailElement.style.left = glowSize / 2f;
            particle.TrailElement.style.top = glowSize / 2f;
            particle.TrailElement.style.scale = new Scale(new Vector2(1f, 1f));

            // Reset dirty tracking for fresh spawn
            particle.PreviousPosition = new Vector2(float.MinValue, float.MinValue);
            particle.PreviousOpacity = -1f;
            particle.PreviousTrailWidth = -1f;

            _particles.Add(particle);
        }

        private void UpdateParticle(SoulParticle particle, float deltaTime)
        {
            particle.Age += deltaTime;

            // Check lifetime
            if (particle.Age >= particle.Lifetime)
            {
                particle.IsAlive = false;
                return;
            }

            // Calculate direction to mouse
            Vector2 toMouse = _mousePosition - particle.Position;
            float distanceToMouse = toMouse.magnitude;

            // Dissolve when close to cursor
            if (distanceToMouse < _dissolveDistance)
            {
                particle.IsAlive = false;
                return;
            }

            // Attraction force
            Vector2 attraction = toMouse.normalized * _attractionStrength;

            // Spiral force (perpendicular to attraction)
            Vector2 perpendicular = new Vector2(-toMouse.y, toMouse.x).normalized;
            float spiralAmount = _spiralStrength * (1f - distanceToMouse / 500f);
            Vector2 spiral = perpendicular * spiralAmount * particle.SpiralDirection;

            // Apply forces
            particle.Velocity += (attraction + spiral) * deltaTime;

            // Clamp speed
            if (particle.Velocity.magnitude > _maxSpeed)
            {
                particle.Velocity = particle.Velocity.normalized * _maxSpeed;
            }

            // Move
            particle.Position += particle.Velocity * deltaTime;

            // Calculate opacity (fade in, then based on distance)
            float fadeIn = Mathf.Min(1f, particle.Age / 0.3f);
            float distanceFade = Mathf.Clamp01(distanceToMouse / 300f);
            float opacity = fadeIn * (0.5f + 0.5f * distanceFade);

            // Batch-skip: skip all style writes for particles with negligible opacity
            if (opacity < 0.01f)
            {
                return;
            }

            // Dirty check: skip style writes when position delta is below threshold
            float positionDelta = (particle.Position - particle.PreviousPosition).magnitude;
            bool positionDirty = positionDelta >= 0.5f;
            bool opacityDirty = Mathf.Abs(opacity - particle.PreviousOpacity) >= 0.005f;

            if (positionDirty)
            {
                // Apply position via translate (paint-only, skips layout recalc unlike left/top)
                float halfWidth = particle.Element.resolvedStyle.width / 2f;
                float halfHeight = particle.Element.resolvedStyle.height / 2f;
                particle.Element.style.translate = new Translate(particle.Position.x - halfWidth, particle.Position.y - halfHeight);
                particle.PreviousPosition = particle.Position;
            }

            if (opacityDirty)
            {
                particle.Element.style.opacity = opacity;
                particle.PreviousOpacity = opacity;
            }

            // Rotate trail to face movement direction
            float speed = particle.Velocity.magnitude;
            if (speed > 10f)
            {
                float trailAngle = Mathf.Atan2(particle.Velocity.y, particle.Velocity.x) * Mathf.Rad2Deg;
                particle.TrailElement.style.rotate = new Rotate(trailAngle + 180f);

                // Use style.scale instead of style.width to avoid layout recalculation
                float trailScaleX = Mathf.Min(30f, speed * 0.1f) / 2f; // Normalize: base width is 2px
                if (Mathf.Abs(trailScaleX - particle.PreviousTrailWidth) >= 0.05f)
                {
                    particle.TrailElement.style.scale = new Scale(new Vector2(trailScaleX, 1f));
                    particle.PreviousTrailWidth = trailScaleX;
                }
                if (opacityDirty)
                {
                    particle.TrailElement.style.opacity = kTrailOpacityVisible;
                }
            }
            else
            {
                if (opacityDirty)
                {
                    particle.TrailElement.style.opacity = 0;
                }
            }
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Scatter all particles away from current position (demon reaction)
        /// </summary>
        public void ScatterParticles(Vector2 scatterOrigin, float force = 500f)
        {
            foreach (var particle in _particles)
            {
                Vector2 awayFromOrigin = (particle.Position - scatterOrigin).normalized;
                particle.Velocity += awayFromOrigin * force;
                particle.SpiralDirection *= -1f; // Reverse spiral
            }
        }

        /// <summary>
        /// Add or modify a danger zone at runtime
        /// </summary>
        public void SetDangerZone(string name, Vector2 normalizedCenter, float radius, float intensity)
        {
            var existing = _dangerZones.Find(z => z.Name == name);
            if (existing != null)
            {
                existing.Center = normalizedCenter;
                existing.Radius = radius;
                existing.Intensity = intensity;
            }
            else
            {
                _dangerZones.Add(new DangerZone
                {
                    Name = name,
                    Center = normalizedCenter,
                    Radius = radius,
                    Intensity = intensity
                });
            }
        }

        /// <summary>
        /// Burst spawn particles at a position
        /// </summary>
        public void BurstAt(Vector2 position, int count = 10)
        {
            for (int i = 0; i < count && _particlePool.Count > 0; i++)
            {
                var particle = _particlePool.Dequeue();

                float angle = Random.Range(0f, Mathf.PI * 2f);
                particle.Position = position + new Vector2(
                    Mathf.Cos(angle) * Random.Range(20f, 50f),
                    Mathf.Sin(angle) * Random.Range(20f, 50f)
                );

                particle.Velocity = new Vector2(
                    Mathf.Cos(angle) * Random.Range(50f, 150f),
                    Mathf.Sin(angle) * Random.Range(50f, 150f)
                );

                particle.Size = Random.Range(_particleSizeMin, _particleSizeMax);
                particle.Lifetime = Random.Range(1f, 2f);
                particle.Age = 0;
                particle.IsAlive = true;
                particle.SpiralPhase = Random.Range(0f, Mathf.PI * 2f);
                particle.SpiralDirection = Random.value > 0.5f ? 1f : -1f;

                float glowSize = particle.Size * 3f;
                particle.Element.style.width = glowSize;
                particle.Element.style.height = glowSize;
                particle.Element.style.display = DisplayStyle.Flex;
                particle.Element.style.opacity = 1f;

                particle.CoreElement.style.width = particle.Size;
                particle.CoreElement.style.height = particle.Size;
                particle.CoreElement.style.left = (glowSize - particle.Size) / 2f;
                particle.CoreElement.style.top = (glowSize - particle.Size) / 2f;

                // Reset dirty tracking for fresh spawn
                particle.PreviousPosition = new Vector2(float.MinValue, float.MinValue);
                particle.PreviousOpacity = -1f;
                particle.PreviousTrailWidth = -1f;

                _particles.Add(particle);
            }
        }

        // =============================================================================
        // NESTED TYPES
        // =============================================================================

        [System.Serializable]
        public class DangerZone
        {
            public string Name;
            public Vector2 Center;
            public float Radius;
            public float Intensity;
        }

        private class SoulParticle
        {
            public VisualElement Element;
            public VisualElement CoreElement;
            public VisualElement TrailElement;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Size;
            public float Lifetime;
            public float Age;
            public bool IsAlive;
            public float SpiralPhase;
            public float SpiralDirection;
            // Dirty tracking: skip style writes when changes are negligible
            public Vector2 PreviousPosition = new Vector2(float.MinValue, float.MinValue);
            public float PreviousOpacity = -1f;
            public float PreviousTrailWidth = -1f;
        }
    }
}
