using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// Generic UI animation controller for AAA-quality transitions.
    /// Provides fade, scale, slide, and staggered animations for UI Toolkit elements.
    /// </summary>
    public class UIAnimationController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Default Timings")]
        [SerializeField] private float _defaultDuration = 0.3f;
        [SerializeField] private float _staggerDelay = 0.05f;

        [Header("Easing")]
        [SerializeField] private AnimationCurve _easeOut = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve _easeIn = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static UIAnimationController _instance;
        public static UIAnimationController Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("UIAnimationController");
                    _instance = go.AddComponent<UIAnimationController>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Setup default easing curves
            SetupDefaultCurves();
        }

        private void SetupDefaultCurves()
        {
            // Smooth ease out (fast start, slow end)
            _easeOut = new AnimationCurve(
                new Keyframe(0, 0, 0, 2),
                new Keyframe(1, 1, 0, 0)
            );

            // Smooth ease in (slow start, fast end)
            _easeIn = new AnimationCurve(
                new Keyframe(0, 0, 0, 0),
                new Keyframe(1, 1, 2, 0)
            );
        }

        // =============================================================================
        // FADE ANIMATIONS
        // =============================================================================

        /// <summary>
        /// Fade in an element from transparent to opaque.
        /// </summary>
        public void FadeIn(VisualElement element, float duration = -1, Action onComplete = null)
        {
            if (duration < 0) duration = _defaultDuration;
            StartCoroutine(FadeCoroutine(element, 0, 1, duration, onComplete));
        }

        /// <summary>
        /// Fade out an element from opaque to transparent.
        /// </summary>
        public void FadeOut(VisualElement element, float duration = -1, Action onComplete = null)
        {
            if (duration < 0) duration = _defaultDuration;
            StartCoroutine(FadeCoroutine(element, 1, 0, duration, onComplete));
        }

        /// <summary>
        /// Fade element to specific opacity.
        /// </summary>
        public void FadeTo(VisualElement element, float targetOpacity, float duration = -1, Action onComplete = null)
        {
            if (duration < 0) duration = _defaultDuration;
            float startOpacity = element.resolvedStyle.opacity;
            StartCoroutine(FadeCoroutine(element, startOpacity, targetOpacity, duration, onComplete));
        }

        private IEnumerator FadeCoroutine(VisualElement element, float from, float to, float duration, Action onComplete)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _easeOut.Evaluate(elapsed / duration);
                element.style.opacity = Mathf.Lerp(from, to, t);
                yield return null;
            }
            element.style.opacity = to;
            onComplete?.Invoke();
        }

        // =============================================================================
        // SCALE ANIMATIONS
        // =============================================================================

        /// <summary>
        /// Scale in an element (grow from small to full size).
        /// </summary>
        public void ScaleIn(VisualElement element, float fromScale = 0.9f, float duration = -1, Action onComplete = null)
        {
            if (duration < 0) duration = _defaultDuration;
            StartCoroutine(ScaleCoroutine(element, fromScale, 1, duration, onComplete));
        }

        /// <summary>
        /// Scale out an element (shrink to small).
        /// </summary>
        public void ScaleOut(VisualElement element, float toScale = 0.9f, float duration = -1, Action onComplete = null)
        {
            if (duration < 0) duration = _defaultDuration;
            StartCoroutine(ScaleCoroutine(element, 1, toScale, duration, onComplete));
        }

        /// <summary>
        /// Punch scale (scale up then back to normal).
        /// </summary>
        public void PunchScale(VisualElement element, float punchScale = 1.1f, float duration = -1, Action onComplete = null)
        {
            if (duration < 0) duration = _defaultDuration;
            StartCoroutine(PunchScaleCoroutine(element, punchScale, duration, onComplete));
        }

        private IEnumerator ScaleCoroutine(VisualElement element, float from, float to, float duration, Action onComplete)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _easeOut.Evaluate(elapsed / duration);
                float scale = Mathf.Lerp(from, to, t);
                element.style.scale = new Scale(new Vector2(scale, scale));
                yield return null;
            }
            element.style.scale = new Scale(new Vector2(to, to));
            onComplete?.Invoke();
        }

        private IEnumerator PunchScaleCoroutine(VisualElement element, float punchScale, float duration, Action onComplete)
        {
            float half = duration * 0.4f;

            // Scale up
            float elapsed = 0;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _easeOut.Evaluate(elapsed / half);
                float scale = Mathf.Lerp(1, punchScale, t);
                element.style.scale = new Scale(new Vector2(scale, scale));
                yield return null;
            }

            // Scale back
            elapsed = 0;
            float remaining = duration - half;
            while (elapsed < remaining)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _easeOut.Evaluate(elapsed / remaining);
                float scale = Mathf.Lerp(punchScale, 1, t);
                element.style.scale = new Scale(new Vector2(scale, scale));
                yield return null;
            }

            element.style.scale = new Scale(Vector2.one);
            onComplete?.Invoke();
        }

        // =============================================================================
        // SLIDE ANIMATIONS
        // =============================================================================

        public enum SlideDirection { Left, Right, Up, Down }

        /// <summary>
        /// Slide in an element from a direction.
        /// </summary>
        public void SlideIn(VisualElement element, SlideDirection direction, float distance = 50, float duration = -1, Action onComplete = null)
        {
            if (duration < 0) duration = _defaultDuration;
            Vector2 offset = GetDirectionOffset(direction, distance);
            StartCoroutine(SlideCoroutine(element, offset, Vector2.zero, duration, onComplete));
        }

        /// <summary>
        /// Slide out an element to a direction.
        /// </summary>
        public void SlideOut(VisualElement element, SlideDirection direction, float distance = 50, float duration = -1, Action onComplete = null)
        {
            if (duration < 0) duration = _defaultDuration;
            Vector2 offset = GetDirectionOffset(direction, distance);
            StartCoroutine(SlideCoroutine(element, Vector2.zero, offset, duration, onComplete));
        }

        private Vector2 GetDirectionOffset(SlideDirection direction, float distance)
        {
            return direction switch
            {
                SlideDirection.Left => new Vector2(-distance, 0),
                SlideDirection.Right => new Vector2(distance, 0),
                SlideDirection.Up => new Vector2(0, -distance),
                SlideDirection.Down => new Vector2(0, distance),
                _ => Vector2.zero
            };
        }

        private IEnumerator SlideCoroutine(VisualElement element, Vector2 from, Vector2 to, float duration, Action onComplete)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _easeOut.Evaluate(elapsed / duration);
                Vector2 pos = Vector2.Lerp(from, to, t);
                element.style.translate = new Translate(pos.x, pos.y);
                yield return null;
            }
            element.style.translate = new Translate(to.x, to.y);
            onComplete?.Invoke();
        }

        // =============================================================================
        // COMBINED ANIMATIONS
        // =============================================================================

        /// <summary>
        /// Fade and scale in simultaneously (common panel entrance).
        /// </summary>
        public void FadeScaleIn(VisualElement element, float fromScale = 0.95f, float duration = -1, Action onComplete = null)
        {
            if (duration < 0) duration = _defaultDuration;
            element.style.opacity = 0;
            element.style.scale = new Scale(new Vector2(fromScale, fromScale));

            StartCoroutine(FadeScaleCoroutine(element, 0, 1, fromScale, 1, duration, onComplete));
        }

        /// <summary>
        /// Fade and scale out simultaneously (common panel exit).
        /// </summary>
        public void FadeScaleOut(VisualElement element, float toScale = 1.05f, float duration = -1, Action onComplete = null)
        {
            if (duration < 0) duration = _defaultDuration * 0.7f;
            StartCoroutine(FadeScaleCoroutine(element, 1, 0, 1, toScale, duration, onComplete));
        }

        /// <summary>
        /// Fade and slide in (common list item entrance).
        /// </summary>
        public void FadeSlideIn(VisualElement element, SlideDirection direction = SlideDirection.Up, float distance = 20, float duration = -1, Action onComplete = null)
        {
            if (duration < 0) duration = _defaultDuration;
            element.style.opacity = 0;
            Vector2 offset = GetDirectionOffset(direction, distance);
            element.style.translate = new Translate(offset.x, offset.y);

            StartCoroutine(FadeSlideCoroutine(element, 0, 1, offset, Vector2.zero, duration, onComplete));
        }

        private IEnumerator FadeScaleCoroutine(VisualElement element, float fromAlpha, float toAlpha, float fromScale, float toScale, float duration, Action onComplete)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _easeOut.Evaluate(elapsed / duration);

                element.style.opacity = Mathf.Lerp(fromAlpha, toAlpha, t);
                float scale = Mathf.Lerp(fromScale, toScale, t);
                element.style.scale = new Scale(new Vector2(scale, scale));

                yield return null;
            }

            element.style.opacity = toAlpha;
            element.style.scale = new Scale(new Vector2(toScale, toScale));
            onComplete?.Invoke();
        }

        private IEnumerator FadeSlideCoroutine(VisualElement element, float fromAlpha, float toAlpha, Vector2 fromPos, Vector2 toPos, float duration, Action onComplete)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _easeOut.Evaluate(elapsed / duration);

                element.style.opacity = Mathf.Lerp(fromAlpha, toAlpha, t);
                Vector2 pos = Vector2.Lerp(fromPos, toPos, t);
                element.style.translate = new Translate(pos.x, pos.y);

                yield return null;
            }

            element.style.opacity = toAlpha;
            element.style.translate = new Translate(toPos.x, toPos.y);
            onComplete?.Invoke();
        }

        // =============================================================================
        // STAGGERED ANIMATIONS
        // =============================================================================

        /// <summary>
        /// Animate multiple elements with staggered timing.
        /// </summary>
        public void StaggeredFadeIn(List<VisualElement> elements, float staggerDelay = -1, float duration = -1, Action onComplete = null)
        {
            if (staggerDelay < 0) staggerDelay = _staggerDelay;
            if (duration < 0) duration = _defaultDuration;

            StartCoroutine(StaggeredAnimationCoroutine(elements, staggerDelay, (element) =>
            {
                FadeSlideIn(element, SlideDirection.Up, 15, duration);
            }, onComplete));
        }

        /// <summary>
        /// Animate list items with stagger.
        /// </summary>
        public void StaggeredListAppear(VisualElement container, float staggerDelay = -1, float duration = -1, Action onComplete = null)
        {
            if (staggerDelay < 0) staggerDelay = _staggerDelay;
            if (duration < 0) duration = _defaultDuration;

            var elements = new List<VisualElement>();
            foreach (var child in container.Children())
            {
                elements.Add(child);
                child.style.opacity = 0;
            }

            StaggeredFadeIn(elements, staggerDelay, duration, onComplete);
        }

        private IEnumerator StaggeredAnimationCoroutine(List<VisualElement> elements, float staggerDelay, Action<VisualElement> animateFunc, Action onComplete)
        {
            for (int i = 0; i < elements.Count; i++)
            {
                animateFunc(elements[i]);
                if (i < elements.Count - 1)
                {
                    yield return new WaitForSecondsRealtime(staggerDelay);
                }
            }

            // Wait for last animation to complete
            yield return new WaitForSecondsRealtime(_defaultDuration);
            onComplete?.Invoke();
        }

        // =============================================================================
        // UTILITY METHODS
        // =============================================================================

        /// <summary>
        /// Reset all transform properties on an element.
        /// </summary>
        public void ResetTransform(VisualElement element)
        {
            element.style.opacity = 1;
            element.style.scale = new Scale(Vector2.one);
            element.style.translate = new Translate(0, 0);
            element.style.rotate = new Rotate(0);
        }

        /// <summary>
        /// Prepare element for animation (set initial hidden state).
        /// </summary>
        public void PrepareForFadeIn(VisualElement element)
        {
            element.style.opacity = 0;
        }

        /// <summary>
        /// Prepare element for scale animation.
        /// </summary>
        public void PrepareForScaleIn(VisualElement element, float initialScale = 0.9f)
        {
            element.style.scale = new Scale(new Vector2(initialScale, initialScale));
        }

        /// <summary>
        /// Stop all animations on this controller.
        /// </summary>
        public void StopAllAnimations()
        {
            StopAllCoroutines();
        }
    }
}
