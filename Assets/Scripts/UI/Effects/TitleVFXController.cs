using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace VeilBreakers.UI.Effects
{
    /// <summary>
    /// AAA Title Menu VFX Controller
    /// Creates and animates layered VFX sprites behind the demon figure.
    ///
    /// ALL animations are done via script (rotation, scale, alpha) - NO flipbooks.
    /// Uses Canvas with RawImage + Additive material for proper blending.
    /// </summary>
    public class TitleVFXController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("VFX Sprites (Static PNGs)")]
        [SerializeField] private Texture2D _fireRingTexture;
        [SerializeField] private Texture2D _fireRingSecondaryTexture;
        [SerializeField] private Texture2D _moltenCoreTexture;
        [SerializeField] private Texture2D _voidSmokeTexture;
        [SerializeField] private Texture2D _shockwaveTexture;

        [Header("Positioning (Screen %)")]
        [SerializeField] private Vector2 _vfxCenterOffset = new Vector2(0f, 0.1f); // Slightly above center

        [Header("Base Scales")]
        [SerializeField] private float _fireRingScale = 800f;
        [SerializeField] private float _fireRingSecondaryScale = 900f;
        [SerializeField] private float _moltenCoreScale = 400f;
        [SerializeField] private float _voidSmokeScale = 1200f;
        [SerializeField] private float _shockwaveStartScale = 200f;
        [SerializeField] private float _shockwaveEndScale = 1400f;

        [Header("Animation Speeds")]
        [SerializeField] private float _fireRingRotationSpeed = 8f; // degrees/sec
        [SerializeField] private float _fireRingSecondaryRotationSpeed = -5f; // counter-rotate
        [SerializeField] private float _moltenCorePulseSpeed = 0.8f;
        [SerializeField] private float _voidSmokeRotationSpeed = 3f;
        [SerializeField] private float _shockwaveDuration = 1.5f;

        [Header("Pulse Settings")]
        [SerializeField] private float _moltenCoreMinScale = 0.92f;
        [SerializeField] private float _moltenCoreMaxScale = 1.08f;
        [SerializeField] private float _moltenCoreMinAlpha = 0.7f;
        [SerializeField] private float _moltenCoreMaxAlpha = 1f;

        // =============================================================================
        // PRIVATE STATE
        // =============================================================================

        private Canvas _vfxCanvas;
        private RectTransform _vfxRoot;

        private RawImage _fireRingImage;
        private RawImage _fireRingSecondaryImage;
        private RawImage _moltenCoreImage;
        private RawImage _voidSmokeImage;
        private RawImage _shockwaveImage;

        private Material _additiveMaterial;
        private bool _isInitialized;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            LoadSpritesFromResources();
            CreateAdditiveMaterial();
            CreateVFXHierarchy();
            _isInitialized = true;
        }

        private void Start()
        {
            // Trigger shockwave on menu load
            StartCoroutine(PlayShockwave());
        }

        private void Update()
        {
            if (!_isInitialized) return;

            AnimateFireRings();
            AnimateMoltenCore();
            AnimateVoidSmoke();
        }

        private void OnDestroy()
        {
            // Clean up material instance
            if (_additiveMaterial != null)
            {
                Destroy(_additiveMaterial);
            }
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void LoadSpritesFromResources()
        {
            // Try to load from assigned fields first, then from path
            string basePath = "Assets/Art/UI/MainMenu/VFX/";

            if (_fireRingTexture == null)
                _fireRingTexture = LoadTexture(basePath + "FireRing_Energy.png");
            if (_fireRingSecondaryTexture == null)
                _fireRingSecondaryTexture = LoadTexture(basePath + "FireRing_Secondary.png");
            if (_moltenCoreTexture == null)
                _moltenCoreTexture = LoadTexture(basePath + "MoltenGlow_Core.png");
            if (_voidSmokeTexture == null)
                _voidSmokeTexture = LoadTexture(basePath + "VoidSmoke_Wisp.png");
            if (_shockwaveTexture == null)
                _shockwaveTexture = LoadTexture(basePath + "Shockwave_Ring.png");
        }

        private Texture2D LoadTexture(string path)
        {
            #if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            #else
            return null;
            #endif
        }

        private void CreateAdditiveMaterial()
        {
            // Create additive blend material for fire/glow effects
            // Using UI/Additive or Particles/Additive shader
            Shader additiveShader = Shader.Find("UI/Additive");
            if (additiveShader == null)
                additiveShader = Shader.Find("Particles/Additive");
            if (additiveShader == null)
                additiveShader = Shader.Find("UI/Default"); // Fallback

            _additiveMaterial = new Material(additiveShader);
            _additiveMaterial.name = "VFX_Additive_Instance";
        }

        private void CreateVFXHierarchy()
        {
            // Create Canvas (Screen Space - Overlay, renders BEHIND UI Toolkit)
            var canvasObj = new GameObject("VFX_Canvas");
            canvasObj.transform.SetParent(transform);

            _vfxCanvas = canvasObj.AddComponent<Canvas>();
            _vfxCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _vfxCanvas.sortingOrder = -10; // Behind UI Toolkit (which is typically 0+)

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Create VFX Root
            var rootObj = new GameObject("VFX_Root");
            _vfxRoot = rootObj.AddComponent<RectTransform>();
            _vfxRoot.SetParent(_vfxCanvas.transform, false);
            _vfxRoot.anchorMin = Vector2.zero;
            _vfxRoot.anchorMax = Vector2.one;
            _vfxRoot.offsetMin = Vector2.zero;
            _vfxRoot.offsetMax = Vector2.zero;

            // Calculate center position
            Vector2 screenCenter = new Vector2(0.5f + _vfxCenterOffset.x, 0.5f + _vfxCenterOffset.y);

            // Create layers in order (back to front):
            // 1. Void Smoke (furthest back)
            _voidSmokeImage = CreateVFXLayer("VoidSmoke", _voidSmokeTexture, _voidSmokeScale, screenCenter, 0);
            SetImageAlpha(_voidSmokeImage, 0.4f);

            // 2. Fire Ring Secondary (counter-rotating)
            _fireRingSecondaryImage = CreateVFXLayer("FireRing_Secondary", _fireRingSecondaryTexture, _fireRingSecondaryScale, screenCenter, 1);
            SetImageAlpha(_fireRingSecondaryImage, 0.6f);

            // 3. Fire Ring Primary
            _fireRingImage = CreateVFXLayer("FireRing_Primary", _fireRingTexture, _fireRingScale, screenCenter, 2);

            // 4. Molten Core (breathing pulse)
            _moltenCoreImage = CreateVFXLayer("MoltenCore", _moltenCoreTexture, _moltenCoreScale, screenCenter, 3);

            // 5. Shockwave (plays once, starts hidden)
            _shockwaveImage = CreateVFXLayer("Shockwave", _shockwaveTexture, _shockwaveStartScale, screenCenter, 4);
            _shockwaveImage.gameObject.SetActive(false);

            Debug.Log("[VB:VFX] Title VFX hierarchy created with 5 layers");
        }

        private RawImage CreateVFXLayer(string name, Texture2D texture, float size, Vector2 anchorPos, int siblingIndex)
        {
            var obj = new GameObject(name);
            var rect = obj.AddComponent<RectTransform>();
            rect.SetParent(_vfxRoot, false);

            // Position at anchor
            rect.anchorMin = anchorPos;
            rect.anchorMax = anchorPos;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(size, size);

            // Use RawImage for better texture control
            var rawImage = obj.AddComponent<RawImage>();
            rawImage.texture = texture;
            rawImage.raycastTarget = false;

            // Apply additive material for fire effects
            if (_additiveMaterial != null && name != "VoidSmoke")
            {
                rawImage.material = _additiveMaterial;
            }

            obj.transform.SetSiblingIndex(siblingIndex);
            return rawImage;
        }

        private void SetImageAlpha(RawImage image, float alpha)
        {
            if (image == null) return;
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }

        // =============================================================================
        // ANIMATION METHODS
        // =============================================================================

        private void AnimateFireRings()
        {
            // Primary fire ring - slow clockwise rotation
            if (_fireRingImage != null)
            {
                _fireRingImage.rectTransform.Rotate(0f, 0f, _fireRingRotationSpeed * Time.deltaTime);
            }

            // Secondary fire ring - slow counter-clockwise rotation
            if (_fireRingSecondaryImage != null)
            {
                _fireRingSecondaryImage.rectTransform.Rotate(0f, 0f, _fireRingSecondaryRotationSpeed * Time.deltaTime);
            }
        }

        private void AnimateMoltenCore()
        {
            if (_moltenCoreImage == null) return;

            // Breathing pulse using sin wave
            float t = Time.time * _moltenCorePulseSpeed;
            float pulse = (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f; // 0-1

            // Scale oscillation
            float scale = Mathf.Lerp(_moltenCoreMinScale, _moltenCoreMaxScale, pulse);
            _moltenCoreImage.rectTransform.localScale = Vector3.one * scale;

            // Alpha pulse
            float alpha = Mathf.Lerp(_moltenCoreMinAlpha, _moltenCoreMaxAlpha, pulse);
            SetImageAlpha(_moltenCoreImage, alpha);

            // Subtle rotation wobble (±2°)
            float rotationWobble = Mathf.Sin(t * Mathf.PI * 1.5f) * 2f;
            _moltenCoreImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotationWobble);
        }

        private void AnimateVoidSmoke()
        {
            if (_voidSmokeImage == null) return;

            // Very slow rotation
            _voidSmokeImage.rectTransform.Rotate(0f, 0f, _voidSmokeRotationSpeed * Time.deltaTime);

            // Subtle scale noise
            float t = Time.time * 0.3f;
            float scaleNoise = 1f + Mathf.Sin(t * Mathf.PI) * 0.03f;
            _voidSmokeImage.rectTransform.localScale = Vector3.one * scaleNoise;

            // Alpha drift
            float alphaDrift = 0.35f + Mathf.Sin(t * Mathf.PI * 0.7f) * 0.1f;
            SetImageAlpha(_voidSmokeImage, alphaDrift);
        }

        private IEnumerator PlayShockwave()
        {
            if (_shockwaveImage == null) yield break;

            // Delay slightly for dramatic effect
            yield return new WaitForSeconds(0.3f);

            _shockwaveImage.gameObject.SetActive(true);

            float elapsed = 0f;
            Vector2 startSize = new Vector2(_shockwaveStartScale, _shockwaveStartScale);
            Vector2 endSize = new Vector2(_shockwaveEndScale, _shockwaveEndScale);

            while (elapsed < _shockwaveDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _shockwaveDuration;

                // Ease out curve for more dramatic expansion
                float easeT = 1f - Mathf.Pow(1f - t, 3f);

                // Scale: small → large
                _shockwaveImage.rectTransform.sizeDelta = Vector2.Lerp(startSize, endSize, easeT);

                // Alpha: 1 → 0
                SetImageAlpha(_shockwaveImage, 1f - easeT);

                yield return null;
            }

            // Destroy shockwave after animation
            Destroy(_shockwaveImage.gameObject);
            _shockwaveImage = null;
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Manually trigger another shockwave (for dramatic moments)
        /// </summary>
        public void TriggerShockwave()
        {
            if (_shockwaveImage == null && _shockwaveTexture != null)
            {
                Vector2 screenCenter = new Vector2(0.5f + _vfxCenterOffset.x, 0.5f + _vfxCenterOffset.y);
                _shockwaveImage = CreateVFXLayer("Shockwave", _shockwaveTexture, _shockwaveStartScale, screenCenter, 10);
                StartCoroutine(PlayShockwave());
            }
        }

        /// <summary>
        /// Set overall VFX intensity (0-1)
        /// </summary>
        public void SetIntensity(float intensity)
        {
            intensity = Mathf.Clamp01(intensity);

            if (_fireRingImage != null)
                SetImageAlpha(_fireRingImage, intensity);
            if (_fireRingSecondaryImage != null)
                SetImageAlpha(_fireRingSecondaryImage, intensity * 0.6f);
            if (_moltenCoreImage != null)
                SetImageAlpha(_moltenCoreImage, intensity);
        }

        /// <summary>
        /// Enable/disable VFX
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            if (_vfxCanvas != null)
                _vfxCanvas.gameObject.SetActive(enabled);
        }
    }
}
