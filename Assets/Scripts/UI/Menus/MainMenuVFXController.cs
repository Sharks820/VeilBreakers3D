using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Menus
{
    /// <summary>
    /// Simple controller for main menu demon breathing animation.
    /// </summary>
    [ExecuteAlways]
    public class MainMenuVFXController : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Breathing Settings")]
        [SerializeField] private float _pulseSpeed = 0.5f;
        [SerializeField] private float _pulseIntensity = 0.02f; // 2% scale variation

        private VisualElement _monsterImage;

        private void Update()
        {
            // Auto-find UIDocument if not assigned
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();

            if (_uiDocument == null) return;

            // Find monster image if not cached
            if (_monsterImage == null)
            {
                var root = _uiDocument.rootVisualElement;
                _monsterImage = root?.Q<VisualElement>("monster-image");
            }

            if (_monsterImage == null) return;

            // Subtle scale pulse using sine wave
#if UNITY_EDITOR
            float time = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;
#else
            float time = Time.time;
#endif
            float pulse = 1f + (Mathf.Sin(time * _pulseSpeed) * _pulseIntensity);
            _monsterImage.style.scale = new Scale(new Vector2(pulse, pulse));
        }
    }
}
