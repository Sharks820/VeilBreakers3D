using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// AAA screen transition system for smooth scene changes.
    /// Provides fade in/out with customizable timing and callbacks.
    /// </summary>
    public class ScreenTransition : SingletonMonoBehaviour<ScreenTransition>
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Transition Settings")]
        [SerializeField] private float _fadeOutDuration = 0.35f;
        [SerializeField] private float _fadeInDuration = 0.35f;
        [SerializeField] private float _holdDuration = 0.1f;

        [Header("UI Document")]
        [SerializeField] private UIDocument _transitionDocument;

        // =============================================================================
        // STATE
        // =============================================================================

        private VisualElement _overlay;
        private bool _isTransitioning;

        public bool IsTransitioning => _isTransitioning;

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action OnFadeOutComplete;
        public event Action OnFadeInComplete;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        protected override void OnSingletonAwake()
        {
            CreateOverlay();
        }

        private void CreateOverlay()
        {
            // Create a minimal UI Document for the transition overlay if not assigned
            if (_transitionDocument == null)
            {
                var go = new GameObject("TransitionOverlay");
                go.transform.SetParent(transform);
                _transitionDocument = go.AddComponent<UIDocument>();

                // Create panel settings
                var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = new Vector2Int(1920, 1080);
                panelSettings.sortingOrder = 9999; // Always on top
                _transitionDocument.panelSettings = panelSettings;
            }

            // Create overlay element
            _overlay = new VisualElement();
            _overlay.name = "transition-overlay";
            _overlay.AddToClassList("screen-transition-overlay");
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.top = 0;
            _overlay.style.right = 0;
            _overlay.style.bottom = 0;
            _overlay.style.backgroundColor = new Color(0.02f, 0.01f, 0.03f, 1f);
            _overlay.style.opacity = 0;
            _overlay.style.display = DisplayStyle.None;

            _transitionDocument.rootVisualElement.Add(_overlay);
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Fade to black with callback when complete.
        /// </summary>
        public void FadeOut(Action onComplete = null)
        {
            if (_isTransitioning) return;
            StartCoroutine(FadeOutCoroutine(onComplete));
        }

        /// <summary>
        /// Fade from black with callback when complete.
        /// </summary>
        public void FadeIn(Action onComplete = null)
        {
            if (_isTransitioning) return;
            StartCoroutine(FadeInCoroutine(onComplete));
        }

        /// <summary>
        /// Complete fade out, execute action, then fade in.
        /// </summary>
        public void Transition(Action duringBlack, Action onComplete = null)
        {
            if (_isTransitioning) return;
            StartCoroutine(FullTransitionCoroutine(duringBlack, onComplete));
        }

        /// <summary>
        /// Immediately set overlay to black (for scene start).
        /// </summary>
        public void SetBlack()
        {
            _overlay.style.display = DisplayStyle.Flex;
            _overlay.style.opacity = 1;
        }

        /// <summary>
        /// Immediately hide overlay.
        /// </summary>
        public void SetClear()
        {
            _overlay.style.opacity = 0;
            _overlay.style.display = DisplayStyle.None;
        }

        // =============================================================================
        // COROUTINES
        // =============================================================================

        private IEnumerator FadeOutCoroutine(Action onComplete)
        {
            _isTransitioning = true;
            _overlay.style.display = DisplayStyle.Flex;
            _overlay.style.opacity = 0;

            // Use unscaled time for menu transitions
            float elapsed = 0;
            while (elapsed < _fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeOutDuration);
                // Ease in for fade out (accelerating)
                float eased = t * t;
                _overlay.style.opacity = eased;
                yield return null;
            }

            _overlay.style.opacity = 1;
            _isTransitioning = false;

            OnFadeOutComplete?.Invoke();
            onComplete?.Invoke();
        }

        private IEnumerator FadeInCoroutine(Action onComplete)
        {
            _isTransitioning = true;
            _overlay.style.display = DisplayStyle.Flex;
            _overlay.style.opacity = 1;

            float elapsed = 0;
            while (elapsed < _fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeInDuration);
                // Ease out for fade in (decelerating)
                float eased = 1f - ((1f - t) * (1f - t));
                _overlay.style.opacity = 1f - eased;
                yield return null;
            }

            _overlay.style.opacity = 0;
            _overlay.style.display = DisplayStyle.None;
            _isTransitioning = false;

            OnFadeInComplete?.Invoke();
            onComplete?.Invoke();
        }

        private IEnumerator FullTransitionCoroutine(Action duringBlack, Action onComplete)
        {
            // Fade out
            yield return FadeOutCoroutine(null);

            // Hold at black
            yield return new WaitForSecondsRealtime(_holdDuration);

            // Execute action during black
            duringBlack?.Invoke();

            // Small delay for scene load
            yield return new WaitForSecondsRealtime(0.05f);

            // Fade in
            yield return FadeInCoroutine(null);

            onComplete?.Invoke();
        }
    }
}
