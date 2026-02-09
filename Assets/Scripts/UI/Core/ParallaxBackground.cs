using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// AAA parallax background system for menu depth.
    /// Creates layered movement based on mouse position for immersive menus.
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Parallax Settings")]
        [SerializeField] private float _baseIntensity = 20f;
        [SerializeField] private float _smoothing = 5f;
        [SerializeField] private bool _invertX = false;
        [SerializeField] private bool _invertY = true;

        [Header("Layer Configuration")]
        [SerializeField] private List<ParallaxLayerConfig> _layerConfigs = new()
        {
            new() { ClassName = "parallax-layer-1", DepthMultiplier = 0.3f },
            new() { ClassName = "parallax-layer-2", DepthMultiplier = 0.6f },
            new() { ClassName = "parallax-layer-3", DepthMultiplier = 1.0f },
            new() { ClassName = "parallax-layer-4", DepthMultiplier = 1.5f }
        };

        // =============================================================================
        // STATE
        // =============================================================================

        private readonly List<ParallaxLayer> _layers = new();
        private Vector2 _targetOffset;
        private Vector2 _currentOffset;
        private Vector2 _previousOffset;
        private Vector2 _screenCenter;
        private bool _isInitialized;
        private const float kDirtyThreshold = 0.1f;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Start()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            // Delay initialization to ensure UI is ready
            if (_uiDocument != null)
            {
                _uiDocument.rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        private void Initialize()
        {
            if (_uiDocument == null) return;

            var root = _uiDocument.rootVisualElement;
            _layers.Clear();

            foreach (var config in _layerConfigs)
            {
                var elements = root.Query<VisualElement>(className: config.ClassName).ToList();
                foreach (var element in elements)
                {
                    _layers.Add(new ParallaxLayer
                    {
                        Element = element,
                        DepthMultiplier = config.DepthMultiplier,
                        OriginalPosition = new Vector2(
                            element.resolvedStyle.left,
                            element.resolvedStyle.top
                        )
                    });
                }
            }

            _screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            _isInitialized = true;

            Debug.Log($"[ParallaxBackground] Initialized with {_layers.Count} layers");
        }

        private void Update()
        {
            if (!_isInitialized || _layers.Count == 0) return;

            UpdateParallax();
        }

        private void OnDisable()
        {
            // Reset all layers to original position
            foreach (var layer in _layers)
            {
                if (layer.Element != null)
                {
                    layer.Element.style.translate = new Translate(0, 0);
                }
            }
        }

        // =============================================================================
        // PARALLAX LOGIC
        // =============================================================================

        private void UpdateParallax()
        {
            // Get mouse position relative to screen center
            Vector2 mousePos = Input.mousePosition;
            Vector2 normalizedOffset = new Vector2(
                (mousePos.x - _screenCenter.x) / _screenCenter.x,
                (mousePos.y - _screenCenter.y) / _screenCenter.y
            );

            // Apply inversion
            if (_invertX) normalizedOffset.x *= -1;
            if (_invertY) normalizedOffset.y *= -1;

            // Calculate target offset
            _targetOffset = normalizedOffset * _baseIntensity;

            // Smooth interpolation
            _currentOffset = Vector2.Lerp(_currentOffset, _targetOffset, Time.unscaledDeltaTime * _smoothing);

            // Skip style updates if offset hasn't changed meaningfully
            if (Mathf.Abs(_currentOffset.x - _previousOffset.x) < kDirtyThreshold &&
                Mathf.Abs(_currentOffset.y - _previousOffset.y) < kDirtyThreshold)
                return;
            _previousOffset = _currentOffset;

            // Apply to all layers
            foreach (var layer in _layers)
            {
                if (layer.Element == null) continue;

                float offsetX = _currentOffset.x * layer.DepthMultiplier;
                float offsetY = _currentOffset.y * layer.DepthMultiplier;

                layer.Element.style.translate = new Translate(offsetX, offsetY);
            }
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Force refresh of layer detection.
        /// Call after dynamically adding parallax elements.
        /// </summary>
        public void RefreshLayers()
        {
            _isInitialized = false;
            Initialize();
        }

        /// <summary>
        /// Set parallax intensity at runtime.
        /// </summary>
        public void SetIntensity(float intensity)
        {
            _baseIntensity = Mathf.Max(0, intensity);
        }

        /// <summary>
        /// Enable or disable parallax effect.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled)
            {
                // Reset positions
                foreach (var layer in _layers)
                {
                    if (layer.Element != null)
                    {
                        layer.Element.style.translate = new Translate(0, 0);
                    }
                }
            }
        }

        // =============================================================================
        // NESTED TYPES
        // =============================================================================

        [Serializable]
        public class ParallaxLayerConfig
        {
            public string ClassName = "parallax-layer";
            [Range(0f, 3f)]
            public float DepthMultiplier = 1f;
        }

        private class ParallaxLayer
        {
            public VisualElement Element;
            public float DepthMultiplier;
            public Vector2 OriginalPosition;
        }
    }
}
