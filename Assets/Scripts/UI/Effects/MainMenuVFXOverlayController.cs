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

        private void Awake()
        {
            EnsureOverlayCanvas();
        }

        private void OnEnable()
        {
            EnsureOverlayCanvas();
        }

        private void OnDestroy()
        {
            _overlayCanvas = null;
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
