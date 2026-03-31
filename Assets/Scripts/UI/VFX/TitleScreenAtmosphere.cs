using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.VFX
{
    /// <summary>
    /// Title screen atmosphere subsystem.
    /// Manages atmosphere gradient, vignette, grunge overlay, fog layers, and top vignette.
    /// </summary>
    public class TitleScreenAtmosphere : MonoBehaviour
    {
        // =============================================================================
        // SERIALIZEFIELD CONFIG
        // =============================================================================

        [Header("Atmospheric Layers")]
        [SerializeField] internal bool _enableVignette = true;
        [SerializeField, Range(0f, 1f)] internal float _vignetteOpacity = 0.22f;
        [SerializeField] internal bool _enableAtmosphereGradient = true;
        [SerializeField] internal Color _atmosphereColor = new Color(0.12f, 0.03f, 0f, 0.12f);
        [SerializeField] internal bool _enableGrungeOverlay = true;
        [SerializeField, Range(0f, 0.35f)] internal float _grungeOpacity = 0.06f;

        [Header("Grunge Texture")]
        [Tooltip("Grunge overlay texture")]
        [SerializeField] internal Texture2D _grungeTexture;

        // =============================================================================
        // STATE
        // =============================================================================

        internal VisualElement _atmosphereLayer;
        internal VisualElement _vignetteLayer;

        // Atmospheric fog layers
        internal VisualElement _fogLayer1;
        internal VisualElement _fogLayer2;
        internal VisualElement _fogLayer3;
        internal float _fogTime;

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Create all atmosphere layers in the VFX container.
        /// </summary>
        public void Initialize(VisualElement vfxContainer)
        {
            if (_enableAtmosphereGradient)
            {
                CreateAtmosphereLayer(vfxContainer);
            }

            if (_enableVignette)
            {
                CreateVignetteLayer(vfxContainer);
            }

            if (_enableGrungeOverlay && _grungeTexture != null)
            {
                CreateGrungeOverlay(vfxContainer);
            }

            // Atmospheric fog layers -- DISABLED: creates visible semi-transparent rounded boxes
            // CreateFogLayers(vfxContainer);
        }

        /// <summary>
        /// Create the top vignette on the host element.
        /// </summary>
        public void CreateTopVignette(VisualElement host)
        {
            // Disabled - user rejected screen-wide vignette
            // Logo shadow added directly to logo instead
        }

        /// <summary>
        /// Update fog layer drift. Called by orchestrator's update coroutine.
        /// </summary>
        public void UpdateFog(float deltaTime)
        {
            _fogTime += deltaTime;
            if (_fogLayer1 != null)
            {
                float drift1 = Mathf.Sin(_fogTime * 0.15f) * 10f;
                _fogLayer1.style.translate = new Translate(drift1, 0);
            }
            if (_fogLayer2 != null)
            {
                float drift2 = Mathf.Sin(_fogTime * 0.1f + 1.5f) * 15f;
                _fogLayer2.style.translate = new Translate(drift2, 0);
            }
            if (_fogLayer3 != null)
            {
                float drift3 = Mathf.Sin(_fogTime * 0.08f + 3.0f) * 8f;
                _fogLayer3.style.translate = new Translate(drift3, 0);
            }
        }

        // =============================================================================
        // INTERNAL
        // =============================================================================

        private void CreateAtmosphereLayer(VisualElement vfxContainer)
        {
            // DISABLED: The circle-shaped atmosphere element created a visible edge on screen.
            _atmosphereLayer = new VisualElement();
            _atmosphereLayer.name = "atmosphere-gradient";
            _atmosphereLayer.style.position = Position.Absolute;
            _atmosphereLayer.style.left = 0;
            _atmosphereLayer.style.top = 0;
            _atmosphereLayer.style.right = 0;
            _atmosphereLayer.style.bottom = 0;
            _atmosphereLayer.pickingMode = PickingMode.Ignore;
            vfxContainer.Add(_atmosphereLayer);
        }

        private void CreateVignetteLayer(VisualElement vfxContainer)
        {
            _vignetteLayer = new VisualElement();
            _vignetteLayer.name = "vignette-overlay";
            _vignetteLayer.style.position = Position.Absolute;
            _vignetteLayer.style.left = 0;
            _vignetteLayer.style.top = 0;
            _vignetteLayer.style.right = 0;
            _vignetteLayer.style.bottom = 0;
            _vignetteLayer.pickingMode = PickingMode.Ignore;

            // DISABLED: Rectangle-based vignette corners create visible hard-edged dark boxes.
            vfxContainer.Add(_vignetteLayer);
        }

        private void CreateVignetteCorner(VisualElement parent, bool isTop, bool isLeft)
        {
            var corner = new VisualElement();
            corner.style.position = Position.Absolute;
            corner.style.width = Length.Percent(40);
            corner.style.height = Length.Percent(40);

            if (isTop) corner.style.top = 0;
            else corner.style.bottom = 0;

            if (isLeft) corner.style.left = 0;
            else corner.style.right = 0;

            corner.style.backgroundColor = new Color(0, 0, 0, _vignetteOpacity);
            corner.style.borderTopLeftRadius = isTop && isLeft ? 0 : Length.Percent(100);
            corner.style.borderTopRightRadius = isTop && !isLeft ? 0 : Length.Percent(100);
            corner.style.borderBottomLeftRadius = !isTop && isLeft ? 0 : Length.Percent(100);
            corner.style.borderBottomRightRadius = !isTop && !isLeft ? 0 : Length.Percent(100);
            corner.pickingMode = PickingMode.Ignore;

            parent.Add(corner);
        }

        private void CreateGrungeOverlay(VisualElement vfxContainer)
        {
            var grungeLayer = new VisualElement();
            grungeLayer.name = "grunge-overlay";
            grungeLayer.style.position = Position.Absolute;
            grungeLayer.style.left = 0;
            grungeLayer.style.top = 0;
            grungeLayer.style.right = 0;
            grungeLayer.style.bottom = 0;
            grungeLayer.style.backgroundImage = new StyleBackground(_grungeTexture);
            grungeLayer.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            grungeLayer.style.unityBackgroundImageTintColor = new Color(1f, 1f, 1f, 1f);
            grungeLayer.style.opacity = _grungeOpacity;
            grungeLayer.pickingMode = PickingMode.Ignore;

            vfxContainer.Add(grungeLayer);
        }

        private void CreateFogLayers(VisualElement vfxContainer)
        {
            if (vfxContainer == null) return;

            _fogLayer1 = CreateFogElement("fog-layer-1",
                new Color(0.15f, 0.08f, 0.05f, 0.08f), 0.4f, 0f, vfxContainer);
            _fogLayer2 = CreateFogElement("fog-layer-2",
                new Color(0.2f, 0.1f, 0.06f, 0.05f), 0.6f, -0.3f, vfxContainer);
            _fogLayer3 = CreateFogElement("fog-layer-3",
                new Color(0.1f, 0.06f, 0.04f, 0.06f), 0.3f, 0.5f, vfxContainer);
        }

        private VisualElement CreateFogElement(string elName, Color color, float heightPercent, float startOffsetPercent, VisualElement vfxContainer)
        {
            var fog = new VisualElement();
            fog.name = elName;
            fog.pickingMode = PickingMode.Ignore;
            fog.style.position = Position.Absolute;
            fog.style.left = new Length(startOffsetPercent * 100f, LengthUnit.Percent);
            fog.style.top = new Length((1f - heightPercent) * 50f, LengthUnit.Percent);
            fog.style.width = new Length(120, LengthUnit.Percent);
            fog.style.height = new Length(heightPercent * 100f, LengthUnit.Percent);
            fog.style.backgroundColor = color;
            fog.style.borderTopLeftRadius = 200;
            fog.style.borderTopRightRadius = 200;
            fog.style.borderBottomLeftRadius = 200;
            fog.style.borderBottomRightRadius = 200;
            vfxContainer.Add(fog);

            return fog;
        }
    }
}
