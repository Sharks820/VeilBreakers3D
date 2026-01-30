using UnityEngine;
using UnityEngine.UI;

namespace VeilBreakers.UI
{
    /// <summary>
    /// Controls VFX effects on the main menu (embers, ash, vignette).
    /// Creates a Canvas that renders behind UI Toolkit.
    /// </summary>
    public class MainMenuVFXController : MonoBehaviour
    {
        [Header("VFX Textures")]
        [SerializeField] private Texture2D _emberTexture;
        [SerializeField] private Texture2D _ashTexture;
        [SerializeField] private Texture2D _vignetteTexture;

        [Header("Shaders")]
        [SerializeField] private Shader _emberShader;
        [SerializeField] private Shader _ashShader;

        [Header("Settings")]
        [SerializeField] private int _sortOrder = 100; // Render ON TOP of UI Toolkit

        private Canvas _vfxCanvas;
        private RawImage _emberImage;
        private RawImage _ashImage;
        private RawImage _vignetteImage;
        private Material _emberMaterial;
        private Material _ashMaterial;

        private void Awake()
        {
            CreateVFXCanvas();
        }

        private void CreateVFXCanvas()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("VFX_Canvas");
            canvasObj.transform.SetParent(transform);
            _vfxCanvas = canvasObj.AddComponent<Canvas>();
            _vfxCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _vfxCanvas.sortingOrder = _sortOrder;

            // Add CanvasScaler
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // NO GraphicRaycaster - we want clicks to pass through to UI Toolkit

            // Create VFX layers (order matters - first is furthest back)
            CreateAshLayer(canvasObj.transform);
            CreateEmberLayer(canvasObj.transform);
            CreateVignetteLayer(canvasObj.transform);

            Debug.Log("[VeilBreakers] MainMenuVFXController initialized");
        }

        private void CreateEmberLayer(Transform parent)
        {
            GameObject obj = new GameObject("Embers");
            obj.transform.SetParent(parent, false);

            _emberImage = obj.AddComponent<RawImage>();

            // Stretch to fill screen
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Load texture if not assigned
            if (_emberTexture == null)
            {
                _emberTexture = LoadTextureFromPath("Assets/Art/UI/MainMenu/ember_particles.png");
                if (_emberTexture == null)
                {
                    _emberTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/ember_particles");
                }
            }

            // Load shader if not assigned
            if (_emberShader == null)
            {
                _emberShader = Shader.Find("VeilBreakers/UI/RisingEmbers");
            }

            if (_emberShader != null)
            {
                _emberMaterial = new Material(_emberShader);
                _emberMaterial.SetTexture("_MainTex", _emberTexture);
                _emberMaterial.SetColor("_Color", new Color(1f, 0.4f, 0.1f, 1f));
                _emberMaterial.SetFloat("_Speed", 0.08f);
                _emberMaterial.SetFloat("_Sway", 0.03f);
                _emberMaterial.SetFloat("_Intensity", 1.5f);
                _emberMaterial.SetFloat("_Glow", 1.0f);
                _emberImage.material = _emberMaterial;
            }

            _emberImage.texture = _emberTexture;
            _emberImage.color = new Color(1f, 1f, 1f, 0.8f);
            _emberImage.raycastTarget = false;

            Debug.Log($"[VFX] Embers - Texture: {(_emberTexture != null ? "LOADED" : "NULL")}, Shader: {(_emberShader != null ? "LOADED" : "NULL")}");
        }

        private void CreateAshLayer(Transform parent)
        {
            GameObject obj = new GameObject("Ash");
            obj.transform.SetParent(parent, false);

            _ashImage = obj.AddComponent<RawImage>();

            // Stretch to fill screen
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Load texture if not assigned
            if (_ashTexture == null)
            {
                _ashTexture = LoadTextureFromPath("Assets/Art/UI/MainMenu/ash_particles.png");
                if (_ashTexture == null)
                {
                    _ashTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/ash_particles");
                }
            }

            // Load shader if not assigned
            if (_ashShader == null)
            {
                _ashShader = Shader.Find("VeilBreakers/UI/FloatingAsh");
            }

            if (_ashShader != null)
            {
                _ashMaterial = new Material(_ashShader);
                _ashMaterial.SetTexture("_MainTex", _ashTexture);
                _ashMaterial.SetColor("_Color", new Color(0.5f, 0.45f, 0.4f, 0.4f));
                _ashMaterial.SetFloat("_RiseSpeed", 0.025f);
                _ashMaterial.SetFloat("_DriftAmount", 0.04f);
                _ashMaterial.SetFloat("_Intensity", 0.8f);
                _ashImage.material = _ashMaterial;
            }

            _ashImage.texture = _ashTexture;
            _ashImage.color = new Color(1f, 1f, 1f, 0.6f);
            _ashImage.raycastTarget = false;

            Debug.Log($"[VFX] Ash - Texture: {(_ashTexture != null ? "LOADED" : "NULL")}, Shader: {(_ashShader != null ? "LOADED" : "NULL")}");
        }

        private void CreateVignetteLayer(Transform parent)
        {
            GameObject obj = new GameObject("Vignette");
            obj.transform.SetParent(parent, false);

            _vignetteImage = obj.AddComponent<RawImage>();

            // Stretch to fill screen
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Load texture if not assigned
            if (_vignetteTexture == null)
            {
                _vignetteTexture = LoadTextureFromPath("Assets/Art/UI/MainMenu/vignette_overlay.png");
                if (_vignetteTexture == null)
                {
                    _vignetteTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/vignette_overlay");
                }
            }

            _vignetteImage.texture = _vignetteTexture;
            _vignetteImage.color = Color.white;
            _vignetteImage.raycastTarget = false;

            Debug.Log($"[VFX] Vignette - Texture: {(_vignetteTexture != null ? "LOADED" : "NULL")}");
        }

        private Texture2D LoadTextureFromPath(string path)
        {
            #if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            #else
            return null;
            #endif
        }

        private void OnDestroy()
        {
            if (_emberMaterial != null) Destroy(_emberMaterial);
            if (_ashMaterial != null) Destroy(_ashMaterial);
        }
    }
}
