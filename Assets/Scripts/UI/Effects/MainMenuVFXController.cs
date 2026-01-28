using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace VeilBreakers.UI.Effects
{
    /// <summary>
    /// Molten-themed VFX for main menu matching the demon's aesthetic.
    /// Creates subtle amber/orange glow effects and rising embers.
    /// </summary>
    public class MainMenuVFXController : MonoBehaviour
    {
        [Header("Pulse Settings")]
        [SerializeField] private float _pulseFrequency = 0.12f;
        [SerializeField] private float _pulseMin = 0.4f;
        [SerializeField] private float _pulseMax = 1.0f;

        [Header("Molten Colors")]
        [SerializeField] private Color _moltenCore = new Color(1f, 0.5f, 0.15f, 0.4f);    // Bright orange
        [SerializeField] private Color _moltenGlow = new Color(0.9f, 0.35f, 0.1f, 0.2f);  // Deep amber
        [SerializeField] private Color _emberColor = new Color(1f, 0.6f, 0.2f, 0.8f);     // Ember orange

        [Header("Embers")]
        [SerializeField] private int _emberCount = 20;
        [SerializeField] private float _emberSpeed = 25f;

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _chestGlow;
        private VisualElement _ambientGlow;
        private VisualElement _embersContainer;
        private VisualElement[] _embers;

        private float _pulsePhase;
        private float _currentPulse;
        private bool _initialized;

        private void Start()
        {
            StartCoroutine(InitializeDelayed());
        }

        private IEnumerator InitializeDelayed()
        {
            yield return null;

            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument?.rootVisualElement == null) yield break;

            _root = _uiDocument.rootVisualElement.Q<VisualElement>("menu-root");
            if (_root == null) yield break;

            CreateMoltenEffects();
            _initialized = true;
            Debug.Log("[VB:VFX] Molten VFX initialized");
        }

        private void Update()
        {
            if (!_initialized) return;

            // Update pulse
            _pulsePhase += Time.deltaTime * _pulseFrequency * Mathf.PI * 2f;
            float raw = (Mathf.Sin(_pulsePhase) + 1f) * 0.5f;
            _currentPulse = Mathf.Lerp(_pulseMin, _pulseMax, raw);

            UpdateChestGlow();
            UpdateEmbers();
        }

        private void CreateMoltenEffects()
        {
            // Insert VFX after background but before monster
            var bg = _root.Q<VisualElement>("background");
            int insertIndex = bg != null ? _root.IndexOf(bg) + 1 : 0;

            // Container for all VFX
            var vfxContainer = new VisualElement();
            vfxContainer.name = "molten-vfx";
            vfxContainer.pickingMode = PickingMode.Ignore;
            vfxContainer.style.position = Position.Absolute;
            vfxContainer.style.left = 0;
            vfxContainer.style.top = 0;
            vfxContainer.style.right = 0;
            vfxContainer.style.bottom = 0;
            _root.Insert(insertIndex, vfxContainer);

            // Chest glow - positioned where the demon's chest/core glows
            CreateChestGlow(vfxContainer);

            // Ambient glow around chest area
            CreateAmbientGlow(vfxContainer);

            // Rising embers
            CreateEmbers(vfxContainer);
        }

        private void CreateChestGlow(VisualElement container)
        {
            _chestGlow = new VisualElement();
            _chestGlow.name = "chest-glow";
            _chestGlow.pickingMode = PickingMode.Ignore;
            _chestGlow.style.position = Position.Absolute;
            _chestGlow.style.left = new StyleLength(new Length(50, LengthUnit.Percent));
            _chestGlow.style.top = new StyleLength(new Length(55, LengthUnit.Percent));
            _chestGlow.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
            _chestGlow.style.width = 200;
            _chestGlow.style.height = 200;
            _chestGlow.style.borderTopLeftRadius = new Length(50, LengthUnit.Percent);
            _chestGlow.style.borderTopRightRadius = new Length(50, LengthUnit.Percent);
            _chestGlow.style.borderBottomLeftRadius = new Length(50, LengthUnit.Percent);
            _chestGlow.style.borderBottomRightRadius = new Length(50, LengthUnit.Percent);
            _chestGlow.style.backgroundColor = _moltenCore;
            container.Add(_chestGlow);
        }

        private void CreateAmbientGlow(VisualElement container)
        {
            _ambientGlow = new VisualElement();
            _ambientGlow.name = "ambient-glow";
            _ambientGlow.pickingMode = PickingMode.Ignore;
            _ambientGlow.style.position = Position.Absolute;
            _ambientGlow.style.left = new StyleLength(new Length(50, LengthUnit.Percent));
            _ambientGlow.style.top = new StyleLength(new Length(55, LengthUnit.Percent));
            _ambientGlow.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
            _ambientGlow.style.width = 500;
            _ambientGlow.style.height = 400;
            _ambientGlow.style.borderTopLeftRadius = new Length(50, LengthUnit.Percent);
            _ambientGlow.style.borderTopRightRadius = new Length(50, LengthUnit.Percent);
            _ambientGlow.style.borderBottomLeftRadius = new Length(50, LengthUnit.Percent);
            _ambientGlow.style.borderBottomRightRadius = new Length(50, LengthUnit.Percent);
            _ambientGlow.style.backgroundColor = _moltenGlow;
            container.Add(_ambientGlow);
        }

        private void CreateEmbers(VisualElement container)
        {
            _embersContainer = new VisualElement();
            _embersContainer.name = "embers";
            _embersContainer.pickingMode = PickingMode.Ignore;
            _embersContainer.style.position = Position.Absolute;
            _embersContainer.style.left = 0;
            _embersContainer.style.top = 0;
            _embersContainer.style.right = 0;
            _embersContainer.style.bottom = 0;
            _embersContainer.style.overflow = Overflow.Hidden;
            container.Add(_embersContainer);

            _embers = new VisualElement[_emberCount];
            for (int i = 0; i < _emberCount; i++)
            {
                var ember = new VisualElement();
                ember.pickingMode = PickingMode.Ignore;
                ember.style.position = Position.Absolute;

                float size = Random.Range(2f, 6f);
                ember.style.width = size;
                ember.style.height = size;
                ember.style.borderTopLeftRadius = new Length(50, LengthUnit.Percent);
                ember.style.borderTopRightRadius = new Length(50, LengthUnit.Percent);
                ember.style.borderBottomLeftRadius = new Length(50, LengthUnit.Percent);
                ember.style.borderBottomRightRadius = new Length(50, LengthUnit.Percent);

                // Vary ember colors from orange to yellow
                float hueShift = Random.Range(-0.05f, 0.05f);
                var color = _emberColor;
                color.r = Mathf.Clamp01(color.r + hueShift);
                color.g = Mathf.Clamp01(color.g + hueShift * 0.5f);
                ember.style.backgroundColor = color;

                // Start position - around chest area
                float startX = Random.Range(35f, 65f);
                float startY = Random.Range(55f, 75f);
                ember.style.left = new StyleLength(new Length(startX, LengthUnit.Percent));
                ember.style.top = new StyleLength(new Length(startY, LengthUnit.Percent));
                ember.style.opacity = Random.Range(0.4f, 0.9f);

                ember.userData = new EmberData
                {
                    speed = Random.Range(0.6f, 1.4f),
                    drift = Random.Range(-0.4f, 0.4f),
                    startX = startX,
                    startY = startY,
                    phase = Random.Range(0f, Mathf.PI * 2f)
                };

                _embersContainer.Add(ember);
                _embers[i] = ember;
            }
        }

        private void UpdateChestGlow()
        {
            if (_chestGlow == null) return;

            // Pulse the glow
            float alpha = _moltenCore.a * (0.7f + _currentPulse * 0.3f);
            _chestGlow.style.backgroundColor = new Color(_moltenCore.r, _moltenCore.g, _moltenCore.b, alpha);

            float scale = 1f + (_currentPulse - 0.5f) * 0.15f;
            _chestGlow.style.scale = new Scale(new Vector3(scale, scale, 1f));

            // Ambient also pulses
            if (_ambientGlow != null)
            {
                float ambientAlpha = _moltenGlow.a * (0.6f + _currentPulse * 0.4f);
                _ambientGlow.style.backgroundColor = new Color(_moltenGlow.r, _moltenGlow.g, _moltenGlow.b, ambientAlpha);
            }
        }

        private void UpdateEmbers()
        {
            if (_embers == null) return;

            for (int i = 0; i < _embers.Length; i++)
            {
                var ember = _embers[i];
                if (ember?.userData is not EmberData data) continue;

                float currentTop = ember.resolvedStyle.top;
                float currentLeft = ember.resolvedStyle.left;

                // Convert to percentage-based movement
                float topPercent = (currentTop / _embersContainer.resolvedStyle.height) * 100f;
                float leftPercent = (currentLeft / _embersContainer.resolvedStyle.width) * 100f;

                // Rise up
                topPercent -= _emberSpeed * data.speed * Time.deltaTime * 0.08f;

                // Drift sideways
                float drift = Mathf.Sin(Time.time * 1.5f + data.phase) * data.drift * 0.3f;
                leftPercent += drift * Time.deltaTime;

                // Fade as rising
                float fadeStart = 40f;
                float opacity = topPercent > fadeStart ? 0.8f : Mathf.Lerp(0f, 0.8f, topPercent / fadeStart);
                ember.style.opacity = opacity;

                // Reset when off screen
                if (topPercent < 5f || opacity < 0.05f)
                {
                    topPercent = data.startY + Random.Range(-5f, 5f);
                    leftPercent = data.startX + Random.Range(-5f, 5f);
                    data.phase = Random.Range(0f, Mathf.PI * 2f);
                }

                ember.style.top = new StyleLength(new Length(topPercent, LengthUnit.Percent));
                ember.style.left = new StyleLength(new Length(leftPercent, LengthUnit.Percent));
            }
        }

        private class EmberData
        {
            public float speed;
            public float drift;
            public float startX;
            public float startY;
            public float phase;
        }
    }
}
