using UnityEngine;
using UnityEngine.UI;

namespace VeilBreakers.UI.Effects
{
    /// <summary>
    /// Canvas-based VFX controller that renders ON TOP of UI Toolkit.
    /// Uses Screen Space Overlay Canvas with high sorting order.
    /// </summary>
    public class CanvasVFXController : MonoBehaviour
    {
        [Header("Canvas Settings")]
        [SerializeField] private int _sortingOrder = 9999; // Renders on top of everything

        [Header("VFX Elements")]
        [SerializeField] private RawImage _vignetteImage;
        [SerializeField] private RawImage _smokeImage;
        [SerializeField] private RawImage _particlesImage;

        [Header("Materials")]
        [SerializeField] private Material _vignetteMaterial;
        [SerializeField] private Material _smokeMaterial;
        [SerializeField] private Material _particlesMaterial;

        [Header("Vignette Settings")]
        [SerializeField] private float _vignetteIntensity = 1.5f;
        [SerializeField] private Color _vignetteColor = new Color(0.02f, 0.01f, 0.03f, 0.8f);

        [Header("Smoke Settings")]
        [SerializeField] private Color _smokeColor = new Color(1f, 0.5f, 0.2f, 0.6f);
        [SerializeField] private float _smokeIntensity = 3.0f;

        [Header("Particle Settings")]
        [SerializeField] private Color _particleColor = new Color(1f, 0.6f, 0.3f, 0.8f);
        [SerializeField] private float _particleIntensity = 5.0f;
        [SerializeField] private float _particleGlowStrength = 3.0f;

        private Canvas _canvas;
        private Material _vignetteMatInstance;
        private Material _smokeMatInstance;
        private Material _particleMatInstance;

        // Shader property IDs
        private static readonly int _colorProp = Shader.PropertyToID("_Color");
        private static readonly int _intensityProp = Shader.PropertyToID("_Intensity");
        private static readonly int _glowStrengthProp = Shader.PropertyToID("_GlowStrength");

        private void Awake()
        {
            SetupCanvas();
            CreateMaterialInstances();
            CreateVFXElements();
        }

        private void Start()
        {
            Debug.Log("[CanvasVFX] VFX Canvas initialized and rendering on top!");
            ApplyAllSettings();
        }

        private void SetupCanvas()
        {
            // Get or create Canvas component
            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
            }

            // Configure for Screen Space Overlay (renders on top of everything)
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = _sortingOrder;

            // Add CanvasScaler for proper scaling
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Add GraphicRaycaster (required for Canvas)
            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            Debug.Log($"[CanvasVFX] Canvas created with sortingOrder: {_sortingOrder}");
        }

        private void CreateMaterialInstances()
        {
            if (_vignetteMaterial != null)
            {
                _vignetteMatInstance = new Material(_vignetteMaterial);
            }

            if (_smokeMaterial != null)
            {
                _smokeMatInstance = new Material(_smokeMaterial);
            }

            if (_particlesMaterial != null)
            {
                _particleMatInstance = new Material(_particlesMaterial);
            }
        }

        private void CreateVFXElements()
        {
            // Create Smoke layer (bottom layer - behind other VFX)
            if (_smokeImage == null && _smokeMatInstance != null)
            {
                _smokeImage = CreateVFXRawImage("VFX_Smoke_Canvas", 0);
                _smokeImage.material = _smokeMatInstance;
            }

            // Create Particles layer (middle layer)
            if (_particlesImage == null && _particleMatInstance != null)
            {
                _particlesImage = CreateVFXRawImage("VFX_Particles_Canvas", 1);
                _particlesImage.material = _particleMatInstance;
            }

            // Create Vignette layer (top layer - always on top)
            if (_vignetteImage == null && _vignetteMatInstance != null)
            {
                _vignetteImage = CreateVFXRawImage("VFX_Vignette_Canvas", 2);
                _vignetteImage.material = _vignetteMatInstance;
            }
        }

        private RawImage CreateVFXRawImage(string name, int siblingIndex)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.SetSiblingIndex(siblingIndex);

            var rectTransform = go.AddComponent<RectTransform>();
            // Stretch to fill entire screen
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var rawImage = go.AddComponent<RawImage>();
            rawImage.raycastTarget = false; // Don't block clicks

            Debug.Log($"[CanvasVFX] Created {name} RawImage");
            return rawImage;
        }

        public void ApplyAllSettings()
        {
            // Apply Vignette settings
            if (_vignetteMatInstance != null)
            {
                _vignetteMatInstance.SetColor(_colorProp, _vignetteColor);
                _vignetteMatInstance.SetFloat(_intensityProp, _vignetteIntensity);
            }

            // Apply Smoke settings
            if (_smokeMatInstance != null)
            {
                _smokeMatInstance.SetColor(_colorProp, _smokeColor);
                _smokeMatInstance.SetFloat(_intensityProp, _smokeIntensity);
            }

            // Apply Particle settings
            if (_particleMatInstance != null)
            {
                _particleMatInstance.SetColor(_colorProp, _particleColor);
                _particleMatInstance.SetFloat(_intensityProp, _particleIntensity);
                _particleMatInstance.SetFloat(_glowStrengthProp, _particleGlowStrength);
            }

            Debug.Log("[CanvasVFX] Applied all VFX settings");
        }

        public void SetGlobalIntensity(float intensity)
        {
            float mult = Mathf.Clamp01(intensity);

            if (_vignetteMatInstance != null)
                _vignetteMatInstance.SetFloat(_intensityProp, _vignetteIntensity * mult);

            if (_smokeMatInstance != null)
                _smokeMatInstance.SetFloat(_intensityProp, _smokeIntensity * mult);

            if (_particleMatInstance != null)
                _particleMatInstance.SetFloat(_intensityProp, _particleIntensity * mult);
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
                ApplyAllSettings();

                if (_canvas != null)
                    _canvas.sortingOrder = _sortingOrder;
            }
        }
#endif
    }
}
