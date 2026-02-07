using UnityEngine;

namespace VeilBreakers.UI.Menus
{
    /// <summary>
    /// Lightweight FPS counter display using OnGUI.
    /// Controlled by the Settings panel's "Show FPS" toggle.
    /// </summary>
    public class FPSCounter : MonoBehaviour
    {
        private static FPSCounter _instance;
        public static FPSCounter Instance => _instance;

        private bool _visible = false;
        private float _deltaTime = 0f;
        private GUIStyle _style;

        private const string kPrefShowFPS = "Graphics_ShowFPS";

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Read initial state from PlayerPrefs
            _visible = PlayerPrefs.GetInt(kPrefShowFPS, 0) == 1;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void Update()
        {
            if (!_visible) return;
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        }

        private void OnGUI()
        {
            if (!_visible) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperRight,
                    padding = new RectOffset(4, 8, 4, 4)
                };
            }

            float fps = 1.0f / Mathf.Max(0.0001f, _deltaTime);
            Color fpsColor = fps >= 55f ? Color.green : fps >= 30f ? Color.yellow : Color.red;
            _style.normal.textColor = fpsColor;

            Rect rect = new Rect(Screen.width - 120, 4, 116, 28);

            // Background
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(rect, $"FPS: {fps:F0}", _style);
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
        }
    }
}
