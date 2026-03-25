using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VeilBreakers.UI.Effects
{
    /// <summary>
    /// Creates a non-interactive Screen Space Overlay canvas for main menu VFX layers.
    /// This stays separate from UI Toolkit and must never block pointer input.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuVFXOverlayController : MonoBehaviour
    {
        private const int kMinOverlaySortingOrder = 1000;

        [Header("Overlay")]
        [SerializeField] private int _sortingOrder = kMinOverlaySortingOrder;
        [SerializeField] private Color _topLayerColor = new Color(0.95f, 0.45f, 0.20f, 0.06f);
        [SerializeField] private Color _bottomLayerColor = new Color(1.00f, 0.70f, 0.30f, 0.04f);

        private Canvas _overlayCanvas;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            // VFX overlay disabled — the orange bar columns detract from AAA quality.
            // Don't create the canvas; just disable this component.
            enabled = false;
        }

        private void OnEnable()
        {
            EnsureOverlayCanvas();
        }

        private void OnDestroy()
        {
            _overlayCanvas = null;
        }

        /// <summary>
        /// Fades out the VFX overlay during scene transitions to prevent
        /// orange bar artifacts being visible against the black transition.
        /// </summary>
        public void FadeOut(float duration = 0.3f)
        {
            if (_canvasGroup != null)
            {
                StartCoroutine(FadeOutCoroutine(duration));
            }
        }

        /// <summary>
        /// Immediately hides the VFX overlay.
        /// </summary>
        public void Hide()
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        }

        private IEnumerator FadeOutCoroutine(float duration)
        {
            float elapsed = 0f;
            float startAlpha = _canvasGroup.alpha;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                yield return null;
            }
            _canvasGroup.alpha = 0f;
        }

        private void EnsureOverlayCanvas()
        {
            if (_overlayCanvas != null) return;

            Transform existing = transform.Find("MainMenuVFXOverlayCanvas");
            if (existing != null)
            {
                _overlayCanvas = existing.GetComponent<Canvas>();
            }

            if (_overlayCanvas == null)
            {
                var go = new GameObject("MainMenuVFXOverlayCanvas");
                go.transform.SetParent(transform, false);
                _overlayCanvas = go.AddComponent<Canvas>();
            }

            _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _overlayCanvas.sortingOrder = _sortingOrder;

            // Add CanvasGroup for fade-out during transitions
            _canvasGroup = _overlayCanvas.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = _overlayCanvas.gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            // Explicitly avoid input interception.
            var raycaster = _overlayCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                Destroy(raycaster);
            }

            EnsureLayer("VFX_Layer_Back", _bottomLayerColor);
            EnsureLayer("VFX_Layer_Front", _topLayerColor);
        }

        private void EnsureLayer(string layerName, Color color)
        {
            Transform existing = _overlayCanvas.transform.Find(layerName);
            RawImage image;
            if (existing == null)
            {
                var layer = new GameObject(layerName);
                layer.transform.SetParent(_overlayCanvas.transform, false);
                image = layer.AddComponent<RawImage>();
            }
            else
            {
                image = existing.GetComponent<RawImage>() ?? existing.gameObject.AddComponent<RawImage>();
            }

            image.color = color;
            image.raycastTarget = false;

            var rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
