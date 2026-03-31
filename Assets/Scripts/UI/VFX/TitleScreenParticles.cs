using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;

namespace VeilBreakers.UI.VFX
{
    /// <summary>
    /// Title screen particle subsystem.
    /// Manages embers, ash, sparks, micro-sparks, smoke wisps, burst particles,
    /// transient smoke, and glow pulses.
    /// </summary>
    public class TitleScreenParticles : MonoBehaviour
    {
        // =============================================================================
        // SERIALIZEFIELD CONFIG
        // =============================================================================

        [Header("Ember Settings (AAA Palette)")]
        [SerializeField] internal int _emberCount = 140;
        [SerializeField] internal float _emberSpeedMin = 12f;
        [SerializeField] internal float _emberSpeedMax = 32f;
        [SerializeField] internal float _emberSizeMin = 2f;
        [SerializeField] internal float _emberSizeMax = 6f;
        [SerializeField] internal Color _emberColorCore = new Color(1f, 0.75f, 0.35f, 1f);
        [SerializeField] internal Color _emberColorBody = new Color(1f, 0.38f, 0.08f, 0.95f);
        [SerializeField] internal Color _emberColorGlow = new Color(1f, 0.22f, 0f, 0.35f);

        [Header("Micro-Spark Settings (AAA)")]
        [SerializeField] internal int _microSparkCount = 40;
        [SerializeField] internal float _microSparkSpeedMin = 45f;
        [SerializeField] internal float _microSparkSpeedMax = 110f;
        [SerializeField] internal float _microSparkSizeMin = 1f;
        [SerializeField] internal float _microSparkSizeMax = 2.5f;

        [Header("Ash Settings (AAA Palette)")]
        [SerializeField] internal int _ashCount = 16;
        [SerializeField] internal float _ashSpeedMin = 6f;
        [SerializeField] internal float _ashSpeedMax = 16f;
        [SerializeField] internal float _ashSizeMin = 3f;
        [SerializeField] internal float _ashSizeMax = 10f;
        [SerializeField] internal Color _ashColorLight = new Color(0.3f, 0.28f, 0.25f, 0.55f);
        [SerializeField] internal Color _ashColorDark = new Color(0.18f, 0.16f, 0.14f, 0.35f);

        [Header("Smoke Wisp Settings (AAA)")]
        [SerializeField] internal int _smokeCount = 0;
        [SerializeField] internal float _smokeSpeedMin = 2.5f;
        [SerializeField] internal float _smokeSpeedMax = 6f;
        [SerializeField] internal float _smokeSizeMin = 120f;
        [SerializeField] internal float _smokeSizeMax = 280f;
        [SerializeField] internal Color _smokeColor = new Color(0.18f, 0.14f, 0.12f, 0.22f);

        [Header("Spark Burst Settings")]
        [SerializeField] internal int _sparkCount = 0;
        [SerializeField] internal float _sparkSpeedMin = 100f;
        [SerializeField] internal float _sparkSpeedMax = 200f;

        [Header("Interactions (AAA)")]
        [SerializeField] internal bool _enableClickMonsterBurst = true;
        [SerializeField] internal int _monsterBurstParticleCount = 32;
        [SerializeField] internal float _monsterBurstForceMin = 260f;
        [SerializeField] internal float _monsterBurstForceMax = 520f;
        [SerializeField] internal float _monsterBurstLifetime = 0.75f;

        [SerializeField] internal bool _enableEmberMouseAttraction = true;
        [SerializeField] internal float _emberAttractRadius = 520f;
        [SerializeField] internal float _emberAttractStrength = 260f;
        [SerializeField] internal float _emberAttractVerticalInfluence = 0.12f;

        [Header("Animation")]
        [SerializeField] internal float _windStrength = 0.4f;
        [SerializeField] internal float _windFrequency = 0.4f;
        [SerializeField] internal float _flickerSpeed = 2.2f;
        [SerializeField] internal float _turbulenceStrength = 0.15f;

        [Header("Spawn Area")]
        [SerializeField] internal float _spawnMarginBottom = 0.15f;
        [SerializeField] internal float _spawnMarginSides = 0.05f;

        [Header("Particle Textures")]
        [Tooltip("Smoke texture for smoke wisps")]
        [SerializeField] internal Texture2D _smokeTexture;
        [Tooltip("Dust/ash texture for ash particles")]
        [SerializeField] internal Texture2D _ashTexture;
        [Tooltip("Ember/spark texture")]
        [SerializeField] internal Texture2D _emberTexture;

        // =============================================================================
        // STATE
        // =============================================================================

        internal VisualElement _vfxContainer;
        internal VisualElement _frontVfxContainer;
        internal VisualElement _smokeLayer;
        internal VisualElement _host;
        internal VisualElement _monsterElement;

        internal readonly List<EmberParticle> _embers = new();
        internal readonly List<AshParticle> _ashes = new();
        internal readonly List<SparkParticle> _sparks = new();
        internal readonly List<MicroSparkParticle> _microSparks = new();
        internal readonly List<SmokeParticle> _smokes = new();
        internal readonly List<BurstParticle> _burstParticles = new();
        internal readonly List<TransientSmokeParticle> _transientSmokes = new();
        internal readonly List<GlowPulse> _glowPulses = new();

        internal float _screenWidth;
        internal float _screenHeight;
        internal float _windOffset;
        internal float _turbulenceOffset;
        internal Vector2 _mousePosition;
        internal bool _hasMouse;

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Set the VFX containers and host references. Called by orchestrator during init.
        /// </summary>
        public void Initialize(VisualElement vfxContainer, VisualElement frontVfxContainer,
            VisualElement smokeLayer, VisualElement host, VisualElement monsterElement,
            float screenWidth, float screenHeight)
        {
            _vfxContainer = vfxContainer;
            _frontVfxContainer = frontVfxContainer;
            _smokeLayer = smokeLayer;
            _host = host;
            _monsterElement = monsterElement;
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
        }

        /// <summary>
        /// Create all particle types in batches (for staggered init).
        /// Call each CreateXxxBatch separately to spread across frames.
        /// </summary>
        public void CreateSmokeBatch()
        {
            for (int i = 0; i < _smokeCount; i++) CreateSmoke();
        }

        public void CreateAshBatch()
        {
            for (int i = 0; i < _ashCount; i++) CreateAsh();
        }

        public void CreateEmberBatch(int start, int count)
        {
            int end = Mathf.Min(start + count, _emberCount);
            for (int i = start; i < end; i++) CreateEmber();
        }

        public int EmberCount => _emberCount;

        public void CreateMicroSparkBatch()
        {
            for (int i = 0; i < _microSparkCount; i++) CreateMicroSpark();
        }

        public void CreateSparkBatch()
        {
            for (int i = 0; i < _sparkCount; i++) CreateSpark();
            // All initial batches created — subsequent resets use random ages for variety
            _isInitialSpawn = false;
        }

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

        /// <summary>
        /// Trigger a monster-click burst at the monster position.
        /// </summary>
        public void SpawnMonsterBurstAtMonster()
        {
            if (_frontVfxContainer == null) return;

            Vector2 origin = new Vector2(_screenWidth * 0.5f, _screenHeight * 0.65f);
            Rect monsterWorld = default;
            bool hasMonster = _host != null && _monsterElement != null;
            if (hasMonster)
            {
                monsterWorld = _monsterElement.worldBound;
                var centerWorld = monsterWorld.center;
                origin = _host.WorldToLocal(new Vector2(centerWorld.x, centerWorld.y));
            }

            // Big chest flash (behind the flames) to sell power.
            SpawnGlowPulse(origin, _frontVfxContainer, 220f, 560f, 0.38f, 0.45f, insertBehind: true);

            // Eye flashes aligned from monster bounds (front layer).
            if (hasMonster)
            {
                Vector2 eyeLeft = _host.WorldToLocal(new Vector2(monsterWorld.xMin + monsterWorld.width * 0.44f, monsterWorld.yMin + monsterWorld.height * 0.19f));
                Vector2 eyeRight = _host.WorldToLocal(new Vector2(monsterWorld.xMin + monsterWorld.width * 0.56f, monsterWorld.yMin + monsterWorld.height * 0.19f));
                SpawnGlowPulse(eyeLeft, _frontVfxContainer, 26f, 86f, 0.72f, 0.28f, insertBehind: false);
                SpawnGlowPulse(eyeRight, _frontVfxContainer, 26f, 86f, 0.72f, 0.28f, insertBehind: false);
            }

            int count = Mathf.Clamp(_monsterBurstParticleCount, 0, 120);
            for (int i = 0; i < count; i++)
            {
                float size = UnityEngine.Random.Range(10f, 22f);

                // Make these more "flame-like" (taller than wide).
                float w = size * UnityEngine.Random.Range(0.55f, 0.85f);
                float h = size * UnityEngine.Random.Range(1.35f, 2.05f);

                var element = new VisualElement();
                element.style.position = Position.Absolute;
                element.style.width = w;
                element.style.height = h;
                element.pickingMode = PickingMode.Ignore;

                // Outer glow (bigger than the flame)
                var glow = new VisualElement();
                glow.style.position = Position.Absolute;
                glow.style.left = -w * 0.55f;
                glow.style.top = -h * 0.35f;
                glow.style.width = w * 2.1f;
                glow.style.height = h * 1.7f;
                glow.pickingMode = PickingMode.Ignore;

                // Inner core (hot center)
                var core = new VisualElement();
                core.style.position = Position.Absolute;
                core.style.left = 0;
                core.style.top = 0;
                core.style.width = w;
                core.style.height = h;
                core.pickingMode = PickingMode.Ignore;

                if (_emberTexture != null)
                {
                    glow.style.backgroundImage = new StyleBackground(_emberTexture);
                    glow.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                    glow.style.unityBackgroundImageTintColor = new Color(1f, 0.25f, 0.08f, 0.30f);

                    core.style.backgroundImage = new StyleBackground(_emberTexture);
                    core.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                    core.style.unityBackgroundImageTintColor = new Color(1f, 0.70f, 0.28f, 0.92f);
                }
                else
                {
                    glow.style.borderTopLeftRadius = Length.Percent(50);
                    glow.style.borderTopRightRadius = Length.Percent(50);
                    glow.style.borderBottomLeftRadius = Length.Percent(50);
                    glow.style.borderBottomRightRadius = Length.Percent(50);
                    glow.style.backgroundColor = new Color(1f, 0.22f, 0.06f, 0.30f);

                    core.style.borderTopLeftRadius = Length.Percent(50);
                    core.style.borderTopRightRadius = Length.Percent(50);
                    core.style.borderBottomLeftRadius = Length.Percent(50);
                    core.style.borderBottomRightRadius = Length.Percent(50);
                    core.style.backgroundColor = new Color(1f, 0.68f, 0.22f, 0.92f);
                }

                element.Add(glow);
                element.Add(core);

                element.style.left = origin.x - w * 0.5f;
                element.style.top = origin.y - h * 0.5f;
                element.style.opacity = 1f;
                element.style.rotate = new Rotate(UnityEngine.Random.Range(-18f, 18f));

                _frontVfxContainer.Add(element);

                Vector2 dir = (UnityEngine.Random.insideUnitCircle * 0.55f + new Vector2(0f, -1.15f)).normalized;
                float force = UnityEngine.Random.Range(_monsterBurstForceMin, _monsterBurstForceMax);

                _burstParticles.Add(new BurstParticle
                {
                    Element = element,
                    Position = origin,
                    Velocity = dir * force,
                    Size = Mathf.Max(w, h),
                    Width = w,
                    Height = h,
                    Lifetime = Mathf.Max(0.2f, _monsterBurstLifetime) * UnityEngine.Random.Range(0.85f, 1.15f),
                    Age = 0f,
                    FlickerSeed = UnityEngine.Random.Range(0f, 1000f)
                });
            }

            // Add smoke kick for volume (front + behind the monster)
            SpawnTransientSmoke(origin, _frontVfxContainer, 4, 90f, 160f, new Color(0.20f, 0.16f, 0.14f, 0.22f), 1.1f);

            if (hasMonster && _vfxContainer != null)
            {
                // Smoke from ears / behind head (behind monster)
                Vector2 earLeft = _host.WorldToLocal(new Vector2(monsterWorld.xMin + monsterWorld.width * 0.34f, monsterWorld.yMin + monsterWorld.height * 0.22f));
                Vector2 earRight = _host.WorldToLocal(new Vector2(monsterWorld.xMin + monsterWorld.width * 0.66f, monsterWorld.yMin + monsterWorld.height * 0.22f));
                Vector2 behindHead = _host.WorldToLocal(new Vector2(monsterWorld.center.x, monsterWorld.yMin + monsterWorld.height * 0.26f));

                var earTint = new Color(0.18f, 0.14f, 0.12f, 0.22f);
                SpawnTransientSmoke(earLeft, _vfxContainer, 5, 110f, 210f, earTint, 1.35f);
                SpawnTransientSmoke(earRight, _vfxContainer, 5, 110f, 210f, earTint, 1.35f);
                SpawnTransientSmoke(behindHead, _vfxContainer, 6, 140f, 260f, new Color(0.18f, 0.13f, 0.11f, 0.18f), 1.55f);
            }

            // Prevent unbounded growth if spam-clicked
            while (_burstParticles.Count > 260)
            {
                var p = _burstParticles[0];
                if (p.Element != null) p.Element.RemoveFromHierarchy();
                _burstParticles.RemoveAt(0);
            }
        }

        /// <summary>
        /// Spawn smoke burst from logo click.
        /// </summary>
        public void SpawnLogoSmokeBurst(VisualElement logoFxLayer, VisualElement logoContainer)
        {
            if (logoFxLayer == null || _host == null || logoContainer == null) return;

            var b = logoContainer.worldBound;
            Vector2 origin = _host.WorldToLocal(new Vector2(b.center.x, b.yMin + 40f));
            SpawnTransientSmoke(origin, logoFxLayer, 8, 90f, 160f,
                new Color(0.22f, 0.18f, 0.16f, 0.30f), 1.2f);
        }

        /// <summary>
        /// Update all particles for one frame. Called by orchestrator's update coroutine.
        /// </summary>
        public void UpdateAll(float deltaTime)
        {
            _windOffset += deltaTime * _windFrequency;
            _turbulenceOffset += deltaTime * 2.5f;

            float wind = Mathf.Sin(_windOffset) * _windStrength;
            float turbulence = Mathf.Sin(_turbulenceOffset * 1.7f) * _turbulenceStrength;

            UpdateBurstParticles(deltaTime, wind, turbulence);
            UpdateTransientSmokes(deltaTime, wind);

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
        }

        /// <summary>
        /// Update mouse position for parallax and ember attraction.
        /// </summary>
        public void SetMousePosition(Vector2 position, bool hasMouse)
        {
            _mousePosition = position;
            _hasMouse = hasMouse;
        }

        // =============================================================================
        // PARTICLE CREATION
        // =============================================================================

        private void CreateEmber()
        {
            float depth = UnityEngine.Random.Range(0.35f, 1f);
            float baseSize = UnityEngine.Random.Range(_emberSizeMin, _emberSizeMax) * Mathf.Lerp(0.75f, 1.25f, depth);
            float elongation = UnityEngine.Random.Range(1.4f, 2.0f);

            var root = new VisualElement();
            root.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
            root.style.position = Position.Absolute;
            root.pickingMode = PickingMode.Ignore;

            float layer1Size = baseSize * 6f;
            var layer1 = CreateEmberLayer(layer1Size, elongation, _emberColorGlow, 0.15f * depth);
            layer1.style.borderTopLeftRadius = Length.Percent(50);
            layer1.style.borderTopRightRadius = Length.Percent(50);
            layer1.style.borderBottomLeftRadius = Length.Percent(15);
            layer1.style.borderBottomRightRadius = Length.Percent(15);
            root.Add(layer1);

            float layer2Size = baseSize * 4f;
            var layer2 = CreateEmberLayer(layer2Size, elongation, _emberColorGlow, 0.25f * depth);
            layer2.style.borderTopLeftRadius = Length.Percent(50);
            layer2.style.borderTopRightRadius = Length.Percent(50);
            layer2.style.borderBottomLeftRadius = Length.Percent(18);
            layer2.style.borderBottomRightRadius = Length.Percent(18);
            CenterInParent(layer2, layer1Size, layer2Size, elongation);
            layer1.Add(layer2);

            float layer3Size = baseSize * 2.5f;
            var layer3 = CreateEmberLayer(layer3Size, elongation, _emberColorBody, 0.5f * depth);
            layer3.style.borderTopLeftRadius = Length.Percent(50);
            layer3.style.borderTopRightRadius = Length.Percent(50);
            layer3.style.borderBottomLeftRadius = Length.Percent(22);
            layer3.style.borderBottomRightRadius = Length.Percent(22);
            CenterInParent(layer3, layer2Size, layer3Size, elongation);
            layer2.Add(layer3);

            float layer4Size = baseSize * 1.5f;
            Color hotColor = Color.Lerp(_emberColorBody, _emberColorCore, 0.5f);
            var layer4 = CreateEmberLayer(layer4Size, elongation, hotColor, 0.75f * depth);
            layer4.style.borderTopLeftRadius = Length.Percent(50);
            layer4.style.borderTopRightRadius = Length.Percent(50);
            layer4.style.borderBottomLeftRadius = Length.Percent(28);
            layer4.style.borderBottomRightRadius = Length.Percent(28);
            CenterInParent(layer4, layer3Size, layer4Size, elongation);
            layer3.Add(layer4);

            float coreSize = baseSize * 0.8f;
            var core = CreateEmberLayer(coreSize, elongation * 0.9f, _emberColorCore, 1f);
            core.style.borderTopLeftRadius = Length.Percent(50);
            core.style.borderTopRightRadius = Length.Percent(50);
            core.style.borderBottomLeftRadius = Length.Percent(35);
            core.style.borderBottomRightRadius = Length.Percent(35);
            CenterInParent(core, layer4Size, coreSize, elongation);
            layer4.Add(core);

            var tail = new VisualElement();
            tail.style.position = Position.Absolute;
            tail.style.width = baseSize * 0.4f;
            tail.style.height = baseSize * elongation * 2f;
            tail.style.left = (layer1Size - baseSize * 0.4f) / 2f;
            tail.style.top = layer1Size * elongation * 0.85f;
            var tailColor = _emberColorBody;
            tailColor.a = 0.15f * depth;
            tail.style.backgroundColor = tailColor;
            tail.style.borderTopLeftRadius = Length.Percent(50);
            tail.style.borderTopRightRadius = Length.Percent(50);
            tail.style.borderBottomLeftRadius = Length.Percent(80);
            tail.style.borderBottomRightRadius = Length.Percent(80);
            tail.pickingMode = PickingMode.Ignore;
            layer1.Add(tail);

            root.style.width = layer1Size;
            root.style.height = layer1Size * elongation + baseSize * elongation * 2f;

            _vfxContainer.Add(root);

            float speed = UnityEngine.Random.Range(_emberSpeedMin, _emberSpeedMax) * Mathf.Lerp(0.55f, 1.1f, depth);
            float opacityScale = Mathf.Lerp(0.25f, 1f, depth);
            float driftAmp = UnityEngine.Random.Range(10f, 30f) * Mathf.Lerp(0.7f, 1.4f, depth);
            float lifetime = UnityEngine.Random.Range(6f, 12f) * Mathf.Lerp(1.25f, 0.9f, depth);

            var ember = new EmberParticle
            {
                GlowElement = root,
                CoreElement = core,
                Size = baseSize,
                GlowSize = layer1Size,
                Depth = depth,
                OpacityScale = opacityScale,
                Speed = speed,
                Lifetime = lifetime,
                Age = UnityEngine.Random.Range(0f, 5f),
                FlickerPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                DriftPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                DriftAmplitude = driftAmp
            };

            ResetEmberPosition(ember);
            _embers.Add(ember);
        }

        private VisualElement CreateEmberLayer(float size, float elongation, Color color, float alphaMultiplier)
        {
            var layer = new VisualElement();
            layer.style.position = Position.Absolute;
            layer.style.width = size;
            layer.style.height = size * elongation;
            var c = color;
            c.a *= alphaMultiplier;
            layer.style.backgroundColor = c;
            layer.pickingMode = PickingMode.Ignore;
            return layer;
        }

        private void CenterInParent(VisualElement child, float parentSize, float childSize, float elongation)
        {
            child.style.left = (parentSize - childSize) / 2f;
            child.style.top = (parentSize * elongation - childSize * elongation) / 2f;
        }

        private void CreateAsh()
        {
            var element = new VisualElement();
            element.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
            element.style.position = Position.Absolute;
            element.pickingMode = PickingMode.Ignore;

            if (_ashTexture != null)
            {
                element.style.backgroundImage = new StyleBackground(_ashTexture);
                element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                Color ashTint = Color.Lerp(_ashColorDark, _ashColorLight, UnityEngine.Random.Range(0f, 1f));
                element.style.unityBackgroundImageTintColor = ashTint;
            }
            else
            {
                Color ashColor = Color.Lerp(_ashColorDark, _ashColorLight, UnityEngine.Random.Range(0f, 1f));
                element.style.backgroundColor = ashColor;
            }

            _vfxContainer.Add(element);

            float aspect = UnityEngine.Random.Range(0.6f, 1.4f);
            var sizeBase = UnityEngine.Random.Range(_ashSizeMin, _ashSizeMax);
            var sizeX = sizeBase * aspect;
            var sizeY = sizeBase;

            var ash = new AshParticle
            {
                Element = element,
                SizeX = sizeX,
                SizeY = sizeY,
                Speed = UnityEngine.Random.Range(_ashSpeedMin, _ashSpeedMax),
                Lifetime = UnityEngine.Random.Range(8f, 16f),
                Age = UnityEngine.Random.Range(0f, 8f),
                RotationSpeed = UnityEngine.Random.Range(-60f, 60f),
                Rotation = UnityEngine.Random.Range(0f, 360f),
                DriftPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                DriftAmplitude = UnityEngine.Random.Range(12f, 35f),
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
            element.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
            element.style.position = Position.Absolute;
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
            element.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
            element.style.position = Position.Absolute;
            element.pickingMode = PickingMode.Ignore;

            if (_emberTexture != null)
            {
                element.style.backgroundImage = new StyleBackground(_emberTexture);
                element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
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

            var size = UnityEngine.Random.Range(_microSparkSizeMin, _microSparkSizeMax);

            var microSpark = new MicroSparkParticle
            {
                Element = element,
                Size = size,
                Speed = UnityEngine.Random.Range(_microSparkSpeedMin, _microSparkSpeedMax),
                Lifetime = UnityEngine.Random.Range(0.8f, 2f),
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
            element.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
            element.style.position = Position.Absolute;
            element.pickingMode = PickingMode.Ignore;

            if (_smokeTexture != null)
            {
                element.style.backgroundImage = new StyleBackground(_smokeTexture);
                element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                element.style.unityBackgroundImageTintColor = _smokeColor;
            }
            else
            {
                element.style.backgroundColor = _smokeColor;
                element.style.borderTopLeftRadius = Length.Percent(50);
                element.style.borderTopRightRadius = Length.Percent(50);
                element.style.borderBottomLeftRadius = Length.Percent(50);
                element.style.borderBottomRightRadius = Length.Percent(50);
            }

            _smokeLayer.Add(element);

            var size = UnityEngine.Random.Range(_smokeSizeMin, _smokeSizeMax);

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

        private bool _isInitialSpawn = true; // Smooth first-frame: all particles start at age 0

        private void ResetEmberPosition(EmberParticle ember)
        {
            ember.Position = new Vector2(
                UnityEngine.Random.Range(0f, _screenWidth),
                UnityEngine.Random.Range(0f, _screenHeight * 1.2f)
            );
            // Initial spawn: age 0 for smooth coordinated fade-in
            // Subsequent resets: random age for organic variety
            ember.Age = _isInitialSpawn ? 0f : UnityEngine.Random.Range(0f, ember.Lifetime * 0.5f);
        }

        private void ResetAshPosition(AshParticle ash)
        {
            ash.Position = new Vector2(
                UnityEngine.Random.Range(0f, _screenWidth),
                UnityEngine.Random.Range(0f, _screenHeight * 1.1f)
            );
            ash.Age = UnityEngine.Random.Range(0f, ash.Lifetime * 0.5f);
        }

        private void ResetSparkPosition(SparkParticle spark)
        {
            spark.Position = new Vector2(
                UnityEngine.Random.Range(0f, _screenWidth),
                UnityEngine.Random.Range(_screenHeight * 0.3f, _screenHeight)
            );
            spark.Age = 0;
            spark.Direction = new Vector2(
                UnityEngine.Random.Range(-0.5f, 0.5f),
                UnityEngine.Random.Range(-1f, -0.4f)
            ).normalized;
        }

        private void ResetMicroSparkPosition(MicroSparkParticle spark)
        {
            spark.Position = new Vector2(
                UnityEngine.Random.Range(0f, _screenWidth),
                UnityEngine.Random.Range(0f, _screenHeight)
            );
            spark.Age = UnityEngine.Random.Range(0f, spark.Lifetime * 0.3f);
        }

        private void ResetSmokePosition(SmokeParticle smoke)
        {
            smoke.Position = new Vector2(
                UnityEngine.Random.Range(-smoke.Size * 0.3f, _screenWidth + smoke.Size * 0.3f),
                UnityEngine.Random.Range(0f, _screenHeight * 1.1f)
            );
            smoke.Age = UnityEngine.Random.Range(0f, smoke.Lifetime * 0.4f);
            smoke.CurrentSize = smoke.Size;
        }

        // =============================================================================
        // PARTICLE UPDATES
        // =============================================================================

        private void UpdateEmber(EmberParticle ember, float deltaTime, float wind, float turbulence)
        {
            ember.Age += deltaTime;

            float normalizedAge = ember.Age / ember.Lifetime;
            float opacity;

            if (normalizedAge < 0.1f)
            {
                opacity = normalizedAge / 0.1f;
            }
            else if (normalizedAge > 0.75f)
            {
                opacity = (1f - normalizedAge) / 0.25f;
            }
            else
            {
                opacity = 1f;
            }

            float flicker1 = Mathf.Sin(ember.Age * _flickerSpeed + ember.FlickerPhase);
            float flicker2 = Mathf.Sin(ember.Age * _flickerSpeed * 1.8f + ember.FlickerPhase * 1.3f);
            float variation = 0.85f + 0.12f * flicker1 + 0.05f * flicker2;
            opacity *= variation;
            opacity *= ember.OpacityScale;

            float drift = Mathf.Sin(ember.Age * 1.5f + ember.DriftPhase) * ember.DriftAmplitude * deltaTime;
            float turbDrift = turbulence * ember.DriftAmplitude * 0.5f * deltaTime;
            float lateral = wind * ember.Speed * deltaTime * Mathf.Lerp(0.25f, 1f, ember.Depth);
            ember.Position.x += drift + turbDrift + lateral;
            ember.Position.y -= ember.Speed * deltaTime;

            if (_enableEmberMouseAttraction && _hasMouse)
            {
                Vector2 toMouse = _mousePosition - ember.Position;
                float dist = toMouse.magnitude;
                float radius = Mathf.Max(20f, _emberAttractRadius);
                if (dist < radius)
                {
                    float band = Mathf.InverseLerp(_screenHeight * 0.35f, _screenHeight * 1.05f, ember.Position.y);
                    float influence = (1f - (dist / radius));
                    influence *= influence;
                    influence *= band;
                    influence *= Mathf.Lerp(0.4f, 1f, ember.Depth);

                    Vector2 dir = dist > 0.001f ? (toMouse / dist) : Vector2.zero;
                    ember.Position.x += dir.x * _emberAttractStrength * influence * deltaTime;
                    ember.Position.y += dir.y * _emberAttractStrength * influence * deltaTime * _emberAttractVerticalInfluence;
                }
            }

            ember.GlowElement.style.left = ember.Position.x - ember.GlowSize / 2f;
            ember.GlowElement.style.top = ember.Position.y - ember.GlowSize / 2f;
            ember.GlowElement.style.opacity = Mathf.Clamp01(opacity * 0.5f);
            ember.CoreElement.style.opacity = Mathf.Clamp01(opacity);

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

            if (normalizedAge < 0.05f)
            {
                opacity = normalizedAge / 0.05f;
            }
            else
            {
                opacity = 1f - Mathf.Pow(normalizedAge, 0.7f);
            }

            float turbX = Mathf.Sin(spark.Age * spark.TurbulenceFrequency + spark.TurbulencePhase) * 40f * deltaTime;
            float turbY = Mathf.Cos(spark.Age * spark.TurbulenceFrequency * 0.7f + spark.TurbulencePhase) * 20f * deltaTime;

            spark.Position.x += turbX + turbulence * 100f * deltaTime;
            spark.Position.y -= spark.Speed * deltaTime + turbY;

            spark.Element.style.left = spark.Position.x;
            spark.Element.style.top = spark.Position.y;
            spark.Element.style.opacity = Mathf.Clamp01(opacity * 0.85f);

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

            float drift = Mathf.Sin(smoke.Age * 0.3f + smoke.DriftPhase) * smoke.DriftAmplitude * deltaTime;
            smoke.Position.x += drift + wind * smoke.Speed * 3f * deltaTime;
            smoke.Position.y -= smoke.Speed * deltaTime;

            smoke.CurrentSize = smoke.Size * (1f + smoke.Age * smoke.ExpansionRate);

            smoke.Element.style.left = smoke.Position.x - smoke.CurrentSize / 2f;
            smoke.Element.style.top = smoke.Position.y - smoke.CurrentSize / 2f;
            smoke.Element.style.width = smoke.CurrentSize;
            smoke.Element.style.height = smoke.CurrentSize;
            smoke.Element.style.opacity = Mathf.Clamp01(opacity * 0.12f);

            if (smoke.Age >= smoke.Lifetime || smoke.Position.y < -smoke.CurrentSize)
            {
                ResetSmokePosition(smoke);
            }
        }

        private void UpdateAsh(AshParticle ash, float deltaTime, float wind)
        {
            ash.Age += deltaTime;
            ash.Rotation += ash.RotationSpeed * deltaTime;

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

            float tumble = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(ash.Age * ash.TumbleSpeed + ash.TumblePhase));

            float drift = Mathf.Sin(ash.Age * 0.8f + ash.DriftPhase) * ash.DriftAmplitude * deltaTime;
            ash.Position.x += drift + wind * ash.Speed * 2f * deltaTime;
            ash.Position.y -= ash.Speed * deltaTime;

            ash.Element.style.left = ash.Position.x - ash.SizeX / 2f;
            ash.Element.style.top = ash.Position.y - ash.SizeY / 2f;
            ash.Element.style.opacity = opacity * 0.7f;
            ash.Element.style.rotate = new Rotate(ash.Rotation);
            ash.Element.style.scale = new Scale(new Vector2(1f, tumble));

            if (ash.Age >= ash.Lifetime || ash.Position.y < -50f)
            {
                ResetAshPosition(ash);
            }
        }

        private void UpdateSpark(SparkParticle spark, float deltaTime)
        {
            spark.Age += deltaTime;

            float activePhase = spark.Age % (spark.Lifetime + 8f);

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

            spark.Position += spark.Direction * spark.Speed * deltaTime;
            spark.Direction.y += 2f * deltaTime;
            spark.Direction = spark.Direction.normalized;

            spark.Element.style.left = spark.Position.x;
            spark.Element.style.top = spark.Position.y;
            spark.Element.style.opacity = opacity;

            if (normalizedAge >= 1f)
            {
                ResetSparkPosition(spark);
            }
        }

        private void UpdateBurstParticles(float deltaTime, float wind, float turbulence)
        {
            if (_burstParticles.Count == 0) return;

            for (int i = _burstParticles.Count - 1; i >= 0; i--)
            {
                var p = _burstParticles[i];
                p.Age += deltaTime;
                float t = p.Age / Mathf.Max(0.0001f, p.Lifetime);
                if (t >= 1f)
                {
                    if (p.Element != null) p.Element.RemoveFromHierarchy();
                    _burstParticles.RemoveAt(i);
                    continue;
                }

                p.Velocity += new Vector2(wind * 160f, -520f) * deltaTime;
                p.Velocity += new Vector2(0f, turbulence * -160f) * deltaTime;
                p.Velocity *= Mathf.Clamp01(1f - deltaTime * 2.2f);

                p.Position += p.Velocity * deltaTime;

                float flicker = 0.70f + 0.30f * Mathf.PerlinNoise(p.FlickerSeed, p.Age * 18f);
                float opacity = (1f - t) * flicker;
                float scale = 1f - t * 0.25f;

                float w = p.Width > 0 ? p.Width : p.Element.resolvedStyle.width;
                float h = p.Height > 0 ? p.Height : p.Element.resolvedStyle.height;
                if (w <= 0) w = p.Size;
                if (h <= 0) h = p.Size;

                p.Element.style.left = p.Position.x - w * 0.5f;
                p.Element.style.top = p.Position.y - h * 0.5f;
                p.Element.style.opacity = opacity;
                p.Element.style.scale = new Scale(Vector2.one * scale);
            }
        }

        // =============================================================================
        // GLOW PULSES
        // =============================================================================

        private void SpawnGlowPulse(Vector2 position, VisualElement parent, float startSize, float endSize, float baseOpacity, float lifetime, bool insertBehind)
        {
            if (parent == null) return;

            float s0 = Mathf.Max(4f, startSize);
            float s1 = Mathf.Max(s0, endSize);

            var el = new VisualElement();
            el.style.position = Position.Absolute;
            el.style.width = s0;
            el.style.height = s0;
            el.style.left = position.x - s0 * 0.5f;
            el.style.top = position.y - s0 * 0.5f;
            el.style.borderTopLeftRadius = Length.Percent(50);
            el.style.borderTopRightRadius = Length.Percent(50);
            el.style.borderBottomLeftRadius = Length.Percent(50);
            el.style.borderBottomRightRadius = Length.Percent(50);
            el.style.backgroundColor = new Color(1f, 0.35f, 0.12f, 1f);
            el.style.opacity = 0f;
            el.pickingMode = PickingMode.Ignore;

            if (insertBehind && parent.childCount > 0)
            {
                parent.Insert(0, el);
            }
            else
            {
                parent.Add(el);
            }

            _glowPulses.Add(new GlowPulse
            {
                Element = el,
                Position = position,
                StartSize = s0,
                EndSize = s1,
                Lifetime = Mathf.Max(0.05f, lifetime),
                Age = 0f,
                BaseOpacity = Mathf.Clamp01(baseOpacity)
            });

            while (_glowPulses.Count > 18)
            {
                var p = _glowPulses[0];
                if (p.Element != null) p.Element.RemoveFromHierarchy();
                _glowPulses.RemoveAt(0);
            }
        }

        private void UpdateGlowPulses(float deltaTime)
        {
            if (_glowPulses.Count == 0) return;

            for (int i = _glowPulses.Count - 1; i >= 0; i--)
            {
                var p = _glowPulses[i];
                p.Age += deltaTime;
                float t = p.Age / Mathf.Max(0.0001f, p.Lifetime);
                if (t >= 1f)
                {
                    if (p.Element != null) p.Element.RemoveFromHierarchy();
                    _glowPulses.RemoveAt(i);
                    continue;
                }

                float ease = 1f - Mathf.Pow(1f - t, 3f);
                float size = Mathf.Lerp(p.StartSize, p.EndSize, ease);
                float alpha = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * p.BaseOpacity;

                if (p.Element != null)
                {
                    p.Element.style.width = size;
                    p.Element.style.height = size;
                    p.Element.style.left = p.Position.x - size * 0.5f;
                    p.Element.style.top = p.Position.y - size * 0.5f;
                    p.Element.style.opacity = alpha;
                }
            }
        }

        // =============================================================================
        // TRANSIENT SMOKE
        // =============================================================================

        private void SpawnTransientSmoke(Vector2 origin, VisualElement parent, int count, float sizeMin, float sizeMax, Color tint, float lifetime)
        {
            if (parent == null) return;
            if (_smokeTexture == null) return;

            for (int i = 0; i < Mathf.Max(0, count); i++)
            {
                float size = UnityEngine.Random.Range(sizeMin, sizeMax);

                var element = new VisualElement();
                element.style.position = Position.Absolute;
                element.pickingMode = PickingMode.Ignore;
                element.style.width = size;
                element.style.height = size;
                element.style.backgroundImage = new StyleBackground(_smokeTexture);
                element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                element.style.unityBackgroundImageTintColor = tint;
                element.style.opacity = 0f;

                parent.Add(element);

                Vector2 offset = UnityEngine.Random.insideUnitCircle * 70f;
                Vector2 pos = origin + offset;
                Vector2 vel = new Vector2(UnityEngine.Random.Range(-20f, 20f), UnityEngine.Random.Range(-120f, -70f));

                _transientSmokes.Add(new TransientSmokeParticle
                {
                    Element = element,
                    Position = pos,
                    Velocity = vel,
                    Size = size,
                    Lifetime = Mathf.Max(0.2f, lifetime) * UnityEngine.Random.Range(0.85f, 1.2f),
                    Age = 0f,
                    ExpansionRate = UnityEngine.Random.Range(0.06f, 0.10f),
                    RotationSpeed = UnityEngine.Random.Range(-18f, 18f)
                });
            }

            while (_transientSmokes.Count > 140)
            {
                var s = _transientSmokes[0];
                if (s.Element != null) s.Element.RemoveFromHierarchy();
                _transientSmokes.RemoveAt(0);
            }
        }

        private void UpdateTransientSmokes(float deltaTime, float wind)
        {
            if (_transientSmokes.Count == 0) return;

            for (int i = _transientSmokes.Count - 1; i >= 0; i--)
            {
                var s = _transientSmokes[i];
                s.Age += deltaTime;
                float t = s.Age / Mathf.Max(0.0001f, s.Lifetime);
                if (t >= 1f)
                {
                    if (s.Element != null) s.Element.RemoveFromHierarchy();
                    _transientSmokes.RemoveAt(i);
                    continue;
                }

                s.Velocity.x += wind * 18f * deltaTime;
                s.Velocity *= Mathf.Clamp01(1f - deltaTime * 0.9f);

                s.Position += s.Velocity * deltaTime;
                s.Size *= 1f + s.ExpansionRate * deltaTime;

                float fadeIn = Mathf.Clamp01(t / 0.12f);
                float fadeOut = Mathf.Clamp01((1f - t) / 0.30f);
                float opacity = Mathf.Min(fadeIn, fadeOut) * 0.95f;

                s.Element.style.width = s.Size;
                s.Element.style.height = s.Size;
                s.Element.style.left = s.Position.x - s.Size * 0.5f;
                s.Element.style.top = s.Position.y - s.Size * 0.5f;
                s.Element.style.opacity = opacity;
                s.Element.style.rotate = new Rotate(s.RotationSpeed * s.Age);
            }
        }

        // =============================================================================
        // NESTED TYPES
        // =============================================================================

        internal sealed class EmberParticle
        {
            public VisualElement GlowElement;
            public VisualElement CoreElement;
            public Vector2 Position;
            public float Size;
            public float GlowSize;
            public float Depth;
            public float OpacityScale;
            public float Speed;
            public float Lifetime;
            public float Age;
            public float FlickerPhase;
            public float DriftPhase;
            public float DriftAmplitude;
        }

        internal sealed class AshParticle
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

        internal sealed class SparkParticle
        {
            public VisualElement Element;
            public Vector2 Position;
            public Vector2 Direction;
            public float Size;
            public float Speed;
            public float Lifetime;
            public float Age;
        }

        internal sealed class MicroSparkParticle
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

        internal sealed class SmokeParticle
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

        internal sealed class BurstParticle
        {
            public VisualElement Element;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Size;
            public float Width;
            public float Height;
            public float Lifetime;
            public float Age;
            public float FlickerSeed;
        }

        internal sealed class TransientSmokeParticle
        {
            public VisualElement Element;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Size;
            public float Lifetime;
            public float Age;
            public float ExpansionRate;
            public float RotationSpeed;
        }

        internal sealed class GlowPulse
        {
            public VisualElement Element;
            public Vector2 Position;
            public float StartSize;
            public float EndSize;
            public float Lifetime;
            public float Age;
            public float BaseOpacity;
        }
    }
}
