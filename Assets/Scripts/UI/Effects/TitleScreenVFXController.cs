using System.Collections;
using UnityEngine;

namespace VeilBreakers.UI.Effects
{
    /// <summary>
    /// Controls all VFX layers for the title screen.
    /// Creates AAA-quality atmosphere with vignette, smoke, particles, and heat distortion.
    /// </summary>
    public class TitleScreenVFXController : MonoBehaviour
    {
        [Header("VFX Materials")]
        [SerializeField] private Material _vignetteMaterial;
        [SerializeField] private Material _smokeMaterial;
        [SerializeField] private Material _particlesMaterial;
        [SerializeField] private Material _heatDistortionMaterial;

        [Header("VFX Renderers")]
        [SerializeField] private MeshRenderer _vignetteRenderer;
        [SerializeField] private MeshRenderer _smokeRenderer;
        [SerializeField] private MeshRenderer _particlesRenderer;
        [SerializeField] private MeshRenderer _heatDistortionRenderer;

        [Header("Vignette Settings")]
        [SerializeField] private float _vignetteIntensity = 0.6f;
        [SerializeField] private float _vignettePulseSpeed = 0.3f;
        [SerializeField] private float _vignettePulseAmount = 0.05f;
        [SerializeField] private Color _vignetteColor = new Color(0.02f, 0.01f, 0.03f, 1f);

        [Header("Smoke Settings")]
        [SerializeField] private Color _smokeColor = new Color(1f, 0.4f, 0.15f, 0.25f);
        [SerializeField] private Vector2 _smokeScrollSpeed = new Vector2(0.04f, 0.015f);
        [SerializeField] private float _smokeIntensity = 0.4f;
        [SerializeField] private float _smokeFadeTop = 0.5f;
        [SerializeField] private float _smokeFadeBottom = 0.0f;

        [Header("Particle Settings")]
        [SerializeField] private Color _particleColor = new Color(1f, 0.5f, 0.2f, 0.5f);
        [SerializeField] private Vector4 _particleScrollSpeed = new Vector4(0.015f, -0.025f, 0.01f, -0.018f);
        [SerializeField] private float _particleIntensity = 0.8f;
        [SerializeField] private float _particleGlowStrength = 0.8f;

        [Header("Heat Distortion Settings")]
        [SerializeField] private float _distortionStrength = 0.015f;
        [SerializeField] private Vector2 _distortionScrollSpeed = new Vector2(0.08f, 0.12f);

        [Header("Animation")]
        [SerializeField] private float _fadeInDuration = 2f;
        [SerializeField] private AnimationCurve _fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Performance")]
        [SerializeField] private bool _enableHeatDistortion = true;
        [SerializeField] private bool _enableParticles = true;

        // Shader property IDs (cached for performance)
        private static readonly int _colorProp = Shader.PropertyToID("_Color");
        private static readonly int _intensityProp = Shader.PropertyToID("_Intensity");
        private static readonly int _scrollSpeedProp = Shader.PropertyToID("_ScrollSpeed");
        private static readonly int _pulseSpeedProp = Shader.PropertyToID("_PulseSpeed");
        private static readonly int _pulseAmountProp = Shader.PropertyToID("_PulseAmount");
        private static readonly int _fadeTopProp = Shader.PropertyToID("_FadeTop");
        private static readonly int _fadeBottomProp = Shader.PropertyToID("_FadeBottom");
        private static readonly int _glowStrengthProp = Shader.PropertyToID("_GlowStrength");
        private static readonly int _distortStrengthProp = Shader.PropertyToID("_DistortStrength");

        private float _currentIntensityMultiplier = 0f;
        private Coroutine _fadeCoroutine;
        private Coroutine _pulseCoroutine;

        private void Awake()
        {
            // Create material instances to avoid modifying shared materials
            CreateMaterialInstances();

            // Check quality settings for performance adjustments
            AdjustForQualitySettings();
        }

        private void Start()
        {
            Debug.Log("[TitleScreenVFX] Start() called - initializing VFX");

            // Start at full intensity immediately (no fade, for visibility testing)
            SetGlobalIntensity(1f);
            Debug.Log("[TitleScreenVFX] VFX now at full intensity");
        }

        private void OnEnable()
        {
            // Apply settings when enabled
            ApplyAllSettings();
        }

        private void OnDisable()
        {
            // Stop any running coroutines
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            // Clean up material instances
            DestroyMaterialInstances();
        }

        /// <summary>
        /// Creates instances of materials to avoid modifying shared assets.
        /// </summary>
        private void CreateMaterialInstances()
        {
            Debug.Log("[TitleScreenVFX] Creating material instances...");

            if (_vignetteMaterial != null)
            {
                _vignetteMaterial = new Material(_vignetteMaterial);
                if (_vignetteRenderer != null)
                    _vignetteRenderer.material = _vignetteMaterial;
                Debug.Log($"[TitleScreenVFX] Vignette material created, shader: {_vignetteMaterial.shader.name}");
            }
            else Debug.LogWarning("[TitleScreenVFX] Vignette material is NULL!");

            if (_smokeMaterial != null)
            {
                _smokeMaterial = new Material(_smokeMaterial);
                if (_smokeRenderer != null)
                    _smokeRenderer.material = _smokeMaterial;
                Debug.Log($"[TitleScreenVFX] Smoke material created, shader: {_smokeMaterial.shader.name}");
            }
            else Debug.LogWarning("[TitleScreenVFX] Smoke material is NULL!");

            if (_particlesMaterial != null)
            {
                _particlesMaterial = new Material(_particlesMaterial);
                if (_particlesRenderer != null)
                    _particlesRenderer.material = _particlesMaterial;
                Debug.Log($"[TitleScreenVFX] Particles material created, shader: {_particlesMaterial.shader.name}");
            }
            else Debug.LogWarning("[TitleScreenVFX] Particles material is NULL!");

            if (_heatDistortionMaterial != null)
            {
                _heatDistortionMaterial = new Material(_heatDistortionMaterial);
                if (_heatDistortionRenderer != null)
                    _heatDistortionRenderer.material = _heatDistortionMaterial;
                Debug.Log($"[TitleScreenVFX] HeatDistortion material created, shader: {_heatDistortionMaterial.shader.name}");
            }
            else Debug.LogWarning("[TitleScreenVFX] HeatDistortion material is NULL!");
        }

        /// <summary>
        /// Destroys material instances to prevent memory leaks.
        /// </summary>
        private void DestroyMaterialInstances()
        {
            if (_vignetteMaterial != null) Destroy(_vignetteMaterial);
            if (_smokeMaterial != null) Destroy(_smokeMaterial);
            if (_particlesMaterial != null) Destroy(_particlesMaterial);
            if (_heatDistortionMaterial != null) Destroy(_heatDistortionMaterial);
        }

        /// <summary>
        /// Adjusts VFX based on quality settings for performance.
        /// Note: We keep all effects enabled for AAA quality - use Inspector toggles for manual control.
        /// </summary>
        private void AdjustForQualitySettings()
        {
            int qualityLevel = QualitySettings.GetQualityLevel();
            Debug.Log($"[TitleScreenVFX] Quality level: {qualityLevel}, Particles: {_enableParticles}, HeatDistortion: {_enableHeatDistortion}");

            // Always enable VFX regardless of quality for AAA experience
            // Performance tuning should be done via Inspector toggles if needed

            // Update renderer states based on Inspector settings
            if (_heatDistortionRenderer != null)
                _heatDistortionRenderer.enabled = _enableHeatDistortion;

            if (_particlesRenderer != null)
                _particlesRenderer.enabled = _enableParticles;

            if (_vignetteRenderer != null)
                _vignetteRenderer.enabled = true;

            if (_smokeRenderer != null)
                _smokeRenderer.enabled = true;
        }

        /// <summary>
        /// Applies all VFX settings to materials.
        /// </summary>
        public void ApplyAllSettings()
        {
            ApplyVignetteSettings();
            ApplySmokeSettings();
            ApplyParticleSettings();
            ApplyHeatDistortionSettings();
        }

        private void ApplyVignetteSettings()
        {
            if (_vignetteMaterial == null) return;

            _vignetteMaterial.SetColor(_colorProp, _vignetteColor);
            _vignetteMaterial.SetFloat(_intensityProp, _vignetteIntensity * _currentIntensityMultiplier);
            _vignetteMaterial.SetFloat(_pulseSpeedProp, _vignettePulseSpeed);
            _vignetteMaterial.SetFloat(_pulseAmountProp, _vignettePulseAmount);
        }

        private void ApplySmokeSettings()
        {
            if (_smokeMaterial == null) return;

            _smokeMaterial.SetColor(_colorProp, _smokeColor);
            _smokeMaterial.SetVector(_scrollSpeedProp, new Vector4(_smokeScrollSpeed.x, _smokeScrollSpeed.y, 0, 0));
            _smokeMaterial.SetFloat(_intensityProp, _smokeIntensity * _currentIntensityMultiplier);
            _smokeMaterial.SetFloat(_fadeTopProp, _smokeFadeTop);
            _smokeMaterial.SetFloat(_fadeBottomProp, _smokeFadeBottom);
        }

        private void ApplyParticleSettings()
        {
            if (_particlesMaterial == null) return;

            _particlesMaterial.SetColor(_colorProp, _particleColor);
            _particlesMaterial.SetVector(_scrollSpeedProp, _particleScrollSpeed);
            _particlesMaterial.SetFloat(_intensityProp, _particleIntensity * _currentIntensityMultiplier);
            _particlesMaterial.SetFloat(_glowStrengthProp, _particleGlowStrength);
        }

        private void ApplyHeatDistortionSettings()
        {
            if (_heatDistortionMaterial == null) return;

            _heatDistortionMaterial.SetFloat(_distortStrengthProp, _distortionStrength * _currentIntensityMultiplier);
            _heatDistortionMaterial.SetVector(_scrollSpeedProp, new Vector4(_distortionScrollSpeed.x, _distortionScrollSpeed.y, 0, 0));
        }

        /// <summary>
        /// Sets the global intensity multiplier for all VFX.
        /// </summary>
        /// <param name="intensity">0-1 intensity value</param>
        public void SetGlobalIntensity(float intensity)
        {
            _currentIntensityMultiplier = Mathf.Clamp01(intensity);
            ApplyAllSettings();
        }

        /// <summary>
        /// Fades in all VFX over the configured duration.
        /// </summary>
        public void FadeIn()
        {
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeCoroutine(0f, 1f, _fadeInDuration));
        }

        /// <summary>
        /// Fades out all VFX over the specified duration.
        /// </summary>
        /// <param name="duration">Duration in seconds</param>
        public void FadeOut(float duration = 1f)
        {
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeCoroutine(_currentIntensityMultiplier, 0f, duration));
        }

        private IEnumerator FadeCoroutine(float from, float to, float duration)
        {
            Debug.Log($"[TitleScreenVFX] FadeCoroutine started: {from} -> {to} over {duration}s");

            // Guard against division by zero
            if (duration <= 0f)
            {
                SetGlobalIntensity(to);
                _fadeCoroutine = null;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curveT = _fadeInCurve.Evaluate(t);

                SetGlobalIntensity(Mathf.Lerp(from, to, curveT));

                yield return null;
            }

            SetGlobalIntensity(to);
            _fadeCoroutine = null;
            Debug.Log($"[TitleScreenVFX] FadeCoroutine complete, intensity now: {_currentIntensityMultiplier}");
        }

        /// <summary>
        /// Triggers a pulse effect (e.g., when entering a new area or on button click).
        /// </summary>
        public void TriggerPulse(float intensity = 1.5f, float duration = 0.3f)
        {
            // Stop any existing pulse to prevent overlapping
            if (_pulseCoroutine != null)
                StopCoroutine(_pulseCoroutine);

            _pulseCoroutine = StartCoroutine(PulseCoroutine(intensity, duration));
        }

        private IEnumerator PulseCoroutine(float peakIntensity, float duration)
        {
            // Guard against division by zero
            if (duration <= 0f)
            {
                _pulseCoroutine = null;
                yield break;
            }

            float originalIntensity = _currentIntensityMultiplier;
            float halfDuration = duration * 0.5f;

            // Pulse up
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                SetGlobalIntensity(Mathf.Lerp(originalIntensity, peakIntensity, t));
                yield return null;
            }

            // Pulse down
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                SetGlobalIntensity(Mathf.Lerp(peakIntensity, originalIntensity, t));
                yield return null;
            }

            SetGlobalIntensity(originalIntensity);
            _pulseCoroutine = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor helper to preview settings changes.
        /// </summary>
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                ApplyAllSettings();
            }
        }
#endif
    }
}
