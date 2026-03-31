using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;

namespace VeilBreakers.UI.VFX
{
    /// <summary>
    /// Title screen lightning subsystem.
    /// Manages lightning layer, strike elements, scheduling, and flash overlay.
    /// </summary>
    public class TitleScreenLightning : MonoBehaviour
    {
        // =============================================================================
        // SERIALIZEFIELD CONFIG
        // =============================================================================

        [Header("Lightning (AAA)")]
        [SerializeField] internal bool _enableLightning = true;
        [SerializeField] internal float _lightningIntervalMin = 2.0f;
        [SerializeField] internal float _lightningIntervalMax = 4.0f;
        [SerializeField] internal float _lightningStrikeDurationMin = 1.2f;
        [SerializeField] internal float _lightningStrikeDurationMax = 2.0f;
        [SerializeField, Range(0f, 1f)] internal float _lightningIntensity = 0.92f;
        [SerializeField] internal Color _lightningTint = new Color(1f, 0.55f, 0.25f, 1f);

        [Header("Lightning Textures")]
        [Tooltip("Lightning bolt textures. Auto-loads from Resources/UI/Lightning/ when empty.")]
        [SerializeField] internal Texture2D _lightningBoltTextureA;
        [SerializeField] internal Texture2D _lightningBoltTextureB;

        // =============================================================================
        // STATE
        // =============================================================================

        internal VisualElement _lightningLayer;
        internal VisualElement _lightningFlashOverlay;
        internal readonly List<LightningStrike> _lightningStrikes = new();
        internal float _nextLightningAt;
        internal float _lightningFlashOpacity;
        internal Texture2D _lightningMaskedA;
        internal Texture2D _lightningMaskedB;
        internal float _screenWidth;
        internal float _screenHeight;

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Initialize lightning with parent container and screen dimensions.
        /// </summary>
        public void Initialize(VisualElement vfxContainer, float screenWidth, float screenHeight)
        {
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            CreateLightningLayer(vfxContainer);
        }

        /// <summary>
        /// Enable lightning effects (re-prepare textures if needed).
        /// </summary>
        public void Enable()
        {
            _enableLightning = true;
        }

        /// <summary>
        /// Disable lightning effects.
        /// </summary>
        public void Disable()
        {
            _enableLightning = false;
        }

        /// <summary>
        /// Update lightning for one frame. Called by orchestrator's update coroutine.
        /// </summary>
        public void UpdateLightning(float deltaTime)
        {
            if (!_enableLightning || _lightningLayer == null) return;

            float now = Time.unscaledTime;
            if (now >= _nextLightningAt)
            {
                TriggerLightningStrike();
                ScheduleNextLightning();
            }

            float maxFlash = 0f;
            foreach (var strike in _lightningStrikes)
            {
                if (!strike.Active) continue;

                strike.Age += deltaTime;
                float t = strike.Age / Mathf.Max(0.0001f, strike.Duration);
                if (t >= 1f)
                {
                    strike.Active = false;
                    strike.Bolt.style.opacity = 0;
                    strike.Glow.style.opacity = 0;
                    continue;
                }

                float flashPeak = t < 0.1f ? t / 0.1f : 1f;
                float decay = Mathf.Pow(1f - t, 0.8f);
                float flicker = 0.85f + 0.15f * Mathf.PerlinNoise(strike.FlickerSeed, strike.Age * 25f);
                float alpha = Mathf.Clamp01(flashPeak * decay * flicker) * strike.BaseOpacity * _lightningIntensity;

                strike.Bolt.style.opacity = alpha;
                strike.Glow.style.opacity = alpha * 0.35f;
                maxFlash = Mathf.Max(maxFlash, alpha);
            }

            _lightningFlashOpacity = Mathf.Lerp(_lightningFlashOpacity, maxFlash, deltaTime * 10f);
            if (_lightningFlashOverlay != null)
            {
                _lightningFlashOverlay.style.opacity = _lightningFlashOpacity * 0.20f;
            }
        }

        /// <summary>
        /// Clean up masked textures created at runtime.
        /// </summary>
        public void Cleanup()
        {
            if (_lightningMaskedA != null) { Destroy(_lightningMaskedA); _lightningMaskedA = null; }
            if (_lightningMaskedB != null) { Destroy(_lightningMaskedB); _lightningMaskedB = null; }
        }

        // =============================================================================
        // INTERNAL
        // =============================================================================

        private void CreateLightningLayer(VisualElement vfxContainer)
        {
            if (!TryPrepareLightningTextures())
            {
                _enableLightning = false;
                return;
            }

            _lightningLayer = new VisualElement();
            _lightningLayer.name = "lightning-layer";
            _lightningLayer.style.position = Position.Absolute;
            _lightningLayer.style.left = 0;
            _lightningLayer.style.top = 0;
            _lightningLayer.style.right = 0;
            _lightningLayer.style.bottom = 0;
            _lightningLayer.pickingMode = PickingMode.Ignore;
            vfxContainer.Add(_lightningLayer);

            _lightningFlashOverlay = new VisualElement();
            _lightningFlashOverlay.name = "lightning-flash";
            _lightningFlashOverlay.style.position = Position.Absolute;
            _lightningFlashOverlay.style.left = 0;
            _lightningFlashOverlay.style.top = 0;
            _lightningFlashOverlay.style.right = 0;
            _lightningFlashOverlay.style.bottom = 0;
            _lightningFlashOverlay.style.backgroundColor = new Color(1f, 0.4f, 0.15f, 0.12f);
            _lightningFlashOverlay.style.opacity = 0;
            _lightningFlashOverlay.pickingMode = PickingMode.Ignore;
            _lightningLayer.Add(_lightningFlashOverlay);

            for (int i = 0; i < 2; i++)
            {
                CreateLightningStrikeElement();
            }

            ScheduleNextLightning(0.6f);
        }

        private void CreateLightningStrikeElement()
        {
            var glow = new VisualElement();
            glow.style.position = Position.Absolute;
            glow.pickingMode = PickingMode.Ignore;
            glow.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            glow.style.opacity = 0;

            var bolt = new VisualElement();
            bolt.style.position = Position.Absolute;
            bolt.pickingMode = PickingMode.Ignore;
            bolt.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            bolt.style.opacity = 0;

            _lightningLayer.Add(glow);
            _lightningLayer.Add(bolt);

            _lightningStrikes.Add(new LightningStrike
            {
                Bolt = bolt,
                Glow = glow,
                Age = 0,
                Duration = 0.5f,
                FlickerSeed = UnityEngine.Random.Range(0f, 1000f),
                Active = false,
                BaseOpacity = UnityEngine.Random.Range(0.7f, 1f),
                Texture = null
            });
        }

        private bool TryPrepareLightningTextures()
        {
            if (_lightningMaskedA != null || _lightningMaskedB != null) return true;
            if (_lightningBoltTextureA == null && _lightningBoltTextureB == null) return false;

            try
            {
                if (_lightningBoltTextureA != null)
                {
                    _lightningMaskedA = CreateWhitenessMaskedTexture(_lightningBoltTextureA, 0.28f, 0.72f, 1.6f);
                }

                if (_lightningBoltTextureB != null)
                {
                    _lightningMaskedB = CreateWhitenessMaskedTexture(_lightningBoltTextureB, 0.28f, 0.72f, 1.6f);
                }
            }
            catch (UnityException ex)
            {
                ErrorLogger.Warn($"[TitleScreenLightning] Lightning textures could not be processed (make sure they are Read/Write enabled). Disabling lightning. {ex.GetType().Name}: {ex.Message}");
                _lightningMaskedA = null;
                _lightningMaskedB = null;
                return false;
            }

            return _lightningMaskedA != null || _lightningMaskedB != null;
        }

        private static Texture2D CreateWhitenessMaskedTexture(Texture2D source, float minChannelLow, float minChannelHigh, float alphaPower)
        {
            if (source == null) return null;

            var pixels = source.GetPixels32();
            var output = new Color32[pixels.Length];

            byte low = (byte)Mathf.Clamp(Mathf.RoundToInt(minChannelLow * 255f), 0, 255);
            byte high = (byte)Mathf.Clamp(Mathf.RoundToInt(minChannelHigh * 255f), 0, 255);
            int range = Mathf.Max(1, high - low);

            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 c = pixels[i];
                byte min = c.r < c.g ? (c.r < c.b ? c.r : c.b) : (c.g < c.b ? c.g : c.b);

                float t = Mathf.Clamp01((min - low) / (float)range);
                t = Mathf.Pow(t, alphaPower);
                byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(t * 255f), 0, 255);

                output[i] = new Color32(c.r, c.g, c.b, a);
            }

            var tex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false);
            tex.name = $"{source.name}_whitenessMasked";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels32(output);
            tex.Apply(false, true);
            return tex;
        }

        private void ScheduleNextLightning(float biasSeconds = 0f)
        {
            _nextLightningAt = Time.unscaledTime + UnityEngine.Random.Range(_lightningIntervalMin, _lightningIntervalMax) + biasSeconds;
        }

        private void TriggerLightningStrike()
        {
            if (_lightningMaskedA == null && _lightningMaskedB == null) return;

            LightningStrike strike = null;
            foreach (var candidate in _lightningStrikes)
            {
                if (!candidate.Active)
                {
                    strike = candidate;
                    break;
                }
            }

            // If all strikes active, skip this trigger rather than interrupting an ongoing animation
            if (strike == null) return;

            var tex = UnityEngine.Random.value < 0.5f ? _lightningMaskedA : _lightningMaskedB;
            if (tex == null) tex = _lightningMaskedA ?? _lightningMaskedB;

            strike.Texture = tex;
            strike.Active = true;
            strike.Age = 0f;
            strike.Duration = UnityEngine.Random.Range(_lightningStrikeDurationMin, _lightningStrikeDurationMax);
            strike.FlickerSeed = UnityEngine.Random.Range(0f, 1000f);
            strike.BaseOpacity = UnityEngine.Random.Range(0.75f, 1f);

            float height = Mathf.Max(400f, _screenHeight * UnityEngine.Random.Range(0.85f, 1.1f));
            float width = UnityEngine.Random.Range(550f, 800f);

            bool sideBiased = UnityEngine.Random.value < 0.88f;
            float left;
            if (sideBiased)
            {
                bool leftSide = UnityEngine.Random.value < 0.5f;
                if (leftSide)
                {
                    left = UnityEngine.Random.Range(_screenWidth * 0.05f, _screenWidth * 0.28f);
                }
                else
                {
                    left = UnityEngine.Random.Range(_screenWidth * 0.72f, _screenWidth * 0.95f - width);
                }
            }
            else
            {
                left = UnityEngine.Random.Range(_screenWidth * 0.22f, _screenWidth * 0.78f - width);
            }
            float top = -_screenHeight * UnityEngine.Random.Range(0.05f, 0.18f);
            float rotation = UnityEngine.Random.Range(-12f, 12f);

            strike.Bolt.style.backgroundImage = new StyleBackground(tex);
            strike.Bolt.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
            strike.Bolt.style.unityBackgroundImageTintColor = _lightningTint;
            strike.Bolt.style.width = width;
            strike.Bolt.style.height = height;
            strike.Bolt.style.left = left;
            strike.Bolt.style.top = top;
            strike.Bolt.style.rotate = new Rotate(rotation);

            strike.Glow.style.backgroundImage = new StyleBackground(tex);
            strike.Glow.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
            strike.Glow.style.unityBackgroundImageTintColor = new Color(1f, 0.35f, 0.12f, 1f);
            strike.Glow.style.width = width * 1.6f;
            strike.Glow.style.height = height * 1.3f;
            strike.Glow.style.left = left - width * 0.3f;
            strike.Glow.style.top = top - height * 0.15f;
            strike.Glow.style.rotate = new Rotate(rotation);
        }

        // =============================================================================
        // NESTED TYPES
        // =============================================================================

        internal sealed class LightningStrike
        {
            public VisualElement Bolt;
            public VisualElement Glow;
            public float Age;
            public float Duration;
            public float FlickerSeed;
            public bool Active;
            public float BaseOpacity;
            public Texture2D Texture;
        }
    }
}
