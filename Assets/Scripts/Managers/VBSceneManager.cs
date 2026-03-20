using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VeilBreakers.Core;

namespace VeilBreakers.Managers
{
    /// <summary>
    /// Manages scene loading, transitions, and loading screens.
    /// Handles async loading with fade effects and progress callbacks.
    /// </summary>
    public class VBSceneManager : SingletonMonoBehaviour<VBSceneManager>
    {
        // =============================================================================
        // SCENE NAMES (Constants)
        // =============================================================================

        public static class Scenes
        {
            public const string Bootstrap = "Bootstrap";
            public const string MainMenu = "MainMenu";
            public const string CharacterSelect = "CharacterSelect";
            public const string TestArena = "TestArena";
            public const string Battle = "Battle";
            public const string Overworld = "Overworld";
        }

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Fade Settings")]
        [SerializeField] private float _fadeOutDuration = 0.5f;
        [SerializeField] private float _fadeInDuration = 0.5f;
        [SerializeField] private Color _fadeColor = Color.black;

        [Header("Loading Screen")]
        [SerializeField] private float _minimumLoadTime = 0.5f;  // Minimum time to show loading screen
        [SerializeField] private bool _useLoadingScreen = true;

        // =============================================================================
        // STATE
        // =============================================================================

        private string _currentSceneName;
        private string _previousSceneName;
        private bool _isLoading;
        private float _loadProgress;
        private CanvasGroup _fadeCanvasGroup;
        private Canvas _fadeCanvas;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public string CurrentScene => _currentSceneName;
        public string PreviousScene => _previousSceneName;
        public bool IsLoading => _isLoading;
        public float LoadProgress => _loadProgress;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        protected override void OnSingletonAwake()
        {
            _currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            CreateFadeCanvas();
        }

        private void Start()
        {
            // Subscribe to Unity's scene loaded event
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        protected override void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            base.OnDestroy();
        }

        // =============================================================================
        // FADE CANVAS SETUP
        // =============================================================================

        private void CreateFadeCanvas()
        {
            // Create a canvas for fade effects
            var fadeObject = new GameObject("FadeCanvas");
            fadeObject.transform.SetParent(transform);

            _fadeCanvas = fadeObject.AddComponent<Canvas>();
            _fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _fadeCanvas.sortingOrder = 9999;  // Always on top

            fadeObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            fadeObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create fade image
            var imageObject = new GameObject("FadeImage");
            imageObject.transform.SetParent(fadeObject.transform);

            var rect = imageObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = imageObject.AddComponent<UnityEngine.UI.Image>();
            image.color = _fadeColor;
            image.raycastTarget = true;

            _fadeCanvasGroup = fadeObject.AddComponent<CanvasGroup>();
            _fadeCanvasGroup.alpha = 0f;
            _fadeCanvasGroup.blocksRaycasts = false;
            _fadeCanvasGroup.interactable = false;
        }

        // =============================================================================
        // SCENE LOADING - PUBLIC API
        // =============================================================================

        /// <summary>
        /// Load a scene immediately without any transition effects.
        /// </summary>
        public void LoadSceneImmediate(string sceneName)
        {
            if (_isLoading) return;

            _previousSceneName = _currentSceneName;
            _currentSceneName = sceneName;

            EventBus.SceneLoadStarted(sceneName);
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Load a scene with fade transition.
        /// </summary>
        public void LoadSceneWithFade(string sceneName)
        {
            if (_isLoading) return;
            StartCoroutine(LoadSceneWithFadeRoutine(sceneName));
        }

        /// <summary>
        /// Load a scene asynchronously with optional fade and loading screen.
        /// </summary>
        public void LoadSceneAsync(string sceneName, bool showLoadingScreen = true)
        {
            if (_isLoading) return;
            StartCoroutine(LoadSceneAsyncRoutine(sceneName, showLoadingScreen));
        }

        /// <summary>
        /// Reload the current scene.
        /// </summary>
        public void ReloadCurrentScene()
        {
            LoadSceneWithFade(_currentSceneName);
        }

        /// <summary>
        /// Return to the previous scene.
        /// </summary>
        public void ReturnToPreviousScene()
        {
            if (!string.IsNullOrEmpty(_previousSceneName))
            {
                LoadSceneWithFade(_previousSceneName);
            }
        }

        // =============================================================================
        // LOADING ROUTINES
        // =============================================================================

        private IEnumerator LoadSceneWithFadeRoutine(string sceneName)
        {
            _isLoading = true;
            _previousSceneName = _currentSceneName;
            _currentSceneName = sceneName;

            EventBus.SceneTransition(_previousSceneName, sceneName);
            EventBus.SceneLoadStarted(sceneName);

            // Fade out
            yield return StartCoroutine(FadeOutRoutine());

            // Load scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);

            // Wait a frame for scene to initialize
            yield return null;

            // Fade in
            yield return StartCoroutine(FadeInRoutine());

            _isLoading = false;
            EventBus.SceneLoadCompleted(sceneName);
        }

        private IEnumerator LoadSceneAsyncRoutine(string sceneName, bool showLoadingScreen)
        {
            _isLoading = true;
            _loadProgress = 0f;
            _previousSceneName = _currentSceneName;
            _currentSceneName = sceneName;

            EventBus.SceneTransition(_previousSceneName, sceneName);
            EventBus.SceneLoadStarted(sceneName);

            // Fade out
            yield return StartCoroutine(FadeOutRoutine());

            // Start async load
            float startTime = Time.unscaledTime;
            AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            // Track progress
            while (!asyncLoad.isDone)
            {
                // Unity's progress goes 0-0.9 during load, then jumps to 1.0 on activation
                _loadProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                EventBus.SceneLoadProgress(_loadProgress);

                // Check if load is ready (0.9 = ready to activate)
                if (asyncLoad.progress >= 0.9f)
                {
                    // Ensure minimum load time
                    float elapsed = Time.unscaledTime - startTime;
                    if (elapsed < _minimumLoadTime)
                    {
                        yield return new WaitForSecondsRealtime(_minimumLoadTime - elapsed);
                    }

                    _loadProgress = 1f;
                    EventBus.SceneLoadProgress(1f);

                    // Activate scene
                    asyncLoad.allowSceneActivation = true;
                }

                yield return null;
            }

            // Wait a frame for scene to initialize
            yield return null;

            // Fade in
            yield return StartCoroutine(FadeInRoutine());

            _isLoading = false;
            EventBus.SceneLoadCompleted(sceneName);
        }

        // =============================================================================
        // FADE ROUTINES
        // =============================================================================

        private IEnumerator FadeOutRoutine()
        {
            EventBus.FadeOutStarted();

            // Guard against missing fade canvas (headless builds, early destruction)
            if (_fadeCanvasGroup == null)
            {
                Debug.LogWarning("[VBSceneManager] FadeCanvasGroup is null, skipping fade out");
                yield break;
            }

            _fadeCanvasGroup.blocksRaycasts = true;

            float duration = Mathf.Max(0.01f, _fadeOutDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            _fadeCanvasGroup.alpha = 1f;
            EventBus.FadeOutCompleted();
        }

        private IEnumerator FadeInRoutine()
        {
            EventBus.FadeInStarted();

            // Guard against missing fade canvas (headless builds, early destruction)
            if (_fadeCanvasGroup == null)
            {
                Debug.LogWarning("[VBSceneManager] FadeCanvasGroup is null, skipping fade in");
                yield break;
            }

            float duration = Mathf.Max(0.01f, _fadeInDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _fadeCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            _fadeCanvasGroup.alpha = 0f;
            _fadeCanvasGroup.blocksRaycasts = false;
            EventBus.FadeInCompleted();
        }

        // =============================================================================
        // SCENE EVENT HANDLERS
        // =============================================================================

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _currentSceneName = scene.name;

            // NOTE: EventBus.ClearAllListeners() is NOT called here because persistent
            // singletons (VERASystem, AutoSaveManager, etc.) subscribe in Start() and
            // would silently stop receiving events after the first scene transition.
            // Instead, each scene-scoped subscriber is responsible for unsubscribing
            // in OnDisable/OnDestroy. CharSelectEvents.ClearAll() handles the CharSelect flow.

            Debug.Log($"[VBSceneManager] Scene loaded: {scene.name}");
        }

        // =============================================================================
        // UTILITY METHODS
        // =============================================================================

        /// <summary>
        /// Check if a scene exists in build settings.
        /// </summary>
        public static bool SceneExists(string sceneName)
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (sceneNameFromPath == sceneName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get the index of a scene in build settings (-1 if not found).
        /// </summary>
        public static int GetSceneIndex(string sceneName)
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (sceneNameFromPath == sceneName)
                {
                    return i;
                }
            }
            return -1;
        }

        // =============================================================================
        // CONVENIENCE METHODS
        // =============================================================================

        /// <summary>
        /// Load the main menu scene.
        /// </summary>
        public void GoToMainMenu() => LoadSceneWithFade(Scenes.MainMenu);

        /// <summary>
        /// Load the character select scene.
        /// </summary>
        public void GoToCharacterSelect() => LoadSceneWithFade(Scenes.CharacterSelect);

        /// <summary>
        /// Start a battle (loads battle scene).
        /// </summary>
        public void StartBattle() => LoadSceneAsync(Scenes.Battle);

        /// <summary>
        /// Load the test arena scene.
        /// </summary>
        public void GoToTestArena() => LoadSceneWithFade(Scenes.TestArena);
    }
}
