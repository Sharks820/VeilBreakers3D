using UnityEngine;
using UnityEngine.UI;

namespace VeilBreakers.UI.Effects
{
    /// <summary>
    /// Manages Canvas-based VFX overlay that renders ON TOP of UI Toolkit.
    /// This solves the issue where 3D VFX quads render behind UI Toolkit's Screen Space Overlay.
    /// </summary>
    public class UIVFXManager : MonoBehaviour
    {
        [Header("RawImage References")]
        [SerializeField] private RawImage _vignetteImage;
        [SerializeField] private RawImage _smokeImage;
        [SerializeField] private RawImage _particlesImage;

        [Header("Vignette Settings")]
        [SerializeField] private Color _vignetteColor = new Color(0.02f, 0.01f, 0.03f, 0.85f);
        [SerializeField] private float _vignetteIntensity = 2.5f;
        [SerializeField] private float _vignetteSoftness = 0.4f;
        [SerializeField] private float _vignettePulseSpeed = 0.5f;
        [SerializeField] private float _vignettePulseAmount = 0.08f;

        [Header("Smoke Settings")]
        [SerializeField] private Color _smokeColor = new Color(1f, 0.4f, 0.15f, 0.7f);
        [SerializeField] private float _smokeIntensity = 5.0f;

        [Header("Particles Settings")]
        [SerializeField] private Color _particleColor = new Color(1f, 0.5f, 0.2f, 0.9f);
        [SerializeField] private float _particleIntensity = 8.0f;
        [SerializeField] private float _particleGlowStrength = 5.0f;

        private Material _vignetteMatInstance;
        private Material _smokeMatInstance;
        private Material _particleMatInstance;

        // Shader property IDs
        private static readonly int ColorProp = Shader.PropertyToID("_Color");
        private static readonly int IntensityProp = Shader.PropertyToID("_Intensity");
        private static readonly int SoftnessProp = Shader.PropertyToID("_Softness");
        private static readonly int PulseSpeedProp = Shader.PropertyToID("_PulseSpeed");
        private static readonly int PulseAmountProp = Shader.PropertyToID("_PulseAmount");
        private static readonly int GlowStrengthProp = Shader.PropertyToID("_GlowStrength");

        private void Awake()
        {
            // Find RawImages if not assigned
            if (_vignetteImage == null)
                _vignetteImage = transform.Find("VFX_Vignette_UI")?.GetComponent<RawImage>();
            if (_smokeImage == null)
                _smokeImage = transform.Find("VFX_Smoke_UI")?.GetComponent<RawImage>();
            if (_particlesImage == null)
                _particlesImage = transform.Find("VFX_Particles_UI")?.GetComponent<RawImage>();

            CreateMaterials();
            ApplySettings();
        }

        private void CreateMaterials()
        {
            // Create Vignette material
            Shader vignetteShader = Shader.Find("VeilBreakers/UI/Vignette");
            if (vignetteShader != null && _vignetteImage != null)
            {
                _vignetteMatInstance = new Material(vignetteShader);
                _vignetteImage.material = _vignetteMatInstance;
                Debug.Log("[UIVFXManager] Vignette material created and assigned");
            }
            else
            {
                Debug.LogWarning($"[UIVFXManager] Vignette shader found: {vignetteShader != null}, Image: {_vignetteImage != null}");
            }

            // Create Smoke material
            Shader smokeShader = Shader.Find("VeilBreakers/VFX/ScrollingSmoke");
            if (smokeShader != null && _smokeImage != null)
            {
                _smokeMatInstance = new Material(smokeShader);
                _smokeImage.material = _smokeMatInstance;
                Debug.Log("[UIVFXManager] Smoke material created and assigned");
            }
            else
            {
                Debug.LogWarning($"[UIVFXManager] Smoke shader found: {smokeShader != null}, Image: {_smokeImage != null}");
            }

            // Create Particles material
            Shader particleShader = Shader.Find("VeilBreakers/VFX/FloatingParticles");
            if (particleShader != null && _particlesImage != null)
            {
                _particleMatInstance = new Material(particleShader);
                _particlesImage.material = _particleMatInstance;
                Debug.Log("[UIVFXManager] Particles material created and assigned");
            }
            else
            {
                Debug.LogWarning($"[UIVFXManager] Particle shader found: {particleShader != null}, Image: {_particlesImage != null}");
            }
        }

        private void ApplySettings()
        {
            // Apply Vignette settings
            if (_vignetteMatInstance != null)
            {
                _vignetteMatInstance.SetColor(ColorProp, _vignetteColor);
                _vignetteMatInstance.SetFloat(IntensityProp, _vignetteIntensity);
                _vignetteMatInstance.SetFloat(SoftnessProp, _vignetteSoftness);
                _vignetteMatInstance.SetFloat(PulseSpeedProp, _vignettePulseSpeed);
                _vignetteMatInstance.SetFloat(PulseAmountProp, _vignettePulseAmount);
            }

            // Apply Smoke settings
            if (_smokeMatInstance != null)
            {
                _smokeMatInstance.SetColor(ColorProp, _smokeColor);
                _smokeMatInstance.SetFloat(IntensityProp, _smokeIntensity);
            }

            // Apply Particle settings
            if (_particleMatInstance != null)
            {
                _particleMatInstance.SetColor(ColorProp, _particleColor);
                _particleMatInstance.SetFloat(IntensityProp, _particleIntensity);
                _particleMatInstance.SetFloat(GlowStrengthProp, _particleGlowStrength);
            }

            Debug.Log("[UIVFXManager] All VFX settings applied!");
        }

        public void SetGlobalIntensity(float multiplier)
        {
            multiplier = Mathf.Clamp01(multiplier);

            if (_vignetteMatInstance != null)
                _vignetteMatInstance.SetFloat(IntensityProp, _vignetteIntensity * multiplier);

            if (_smokeMatInstance != null)
                _smokeMatInstance.SetFloat(IntensityProp, _smokeIntensity * multiplier);

            if (_particleMatInstance != null)
                _particleMatInstance.SetFloat(IntensityProp, _particleIntensity * multiplier);
        }

        private void OnDestroy()
        {
            if (_vignetteMatInstance != null) Destroy(_vignetteMatInstance);
            if (_smokeMatInstance != null) Destroy(_smokeMatInstance);
            if (_particleMatInstance != null) Destroy(_particleMatInstance);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                ApplySettings();
            }
        }
#endif
    }
}
