using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VeilBreakers.Combat;
using VeilBreakers.Capture;

namespace VeilBreakers.UI.Combat
{
    /// <summary>
    /// Banner state for capture UI.
    /// </summary>
    public enum CaptureBannerState
    {
        HIDDEN,
        MARKED,     // Target marked, waiting for threshold
        READY,      // Threshold reached, can capture
        CAPTURING   // Capture attempt in progress
    }

    /// <summary>
    /// Controls the capture banner (bottom-right) showing capture availability.
    /// </summary>
    public class CaptureBannerController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Components")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _bannerTransform;
        [SerializeField] private Image _bannerBackground;
        [SerializeField] private TextMeshProUGUI _captureText;
        [SerializeField] private TextMeshProUGUI _keybindText;
        [SerializeField] private Image _keybindIcon;

        [Header("Colors")]
        [SerializeField] private Color _markedColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        [SerializeField] private Color _readyColor = new Color(1f, 0.84f, 0f, 1f);
        [SerializeField] private Color _capturingColor = new Color(1f, 1f, 1f, 1f);

        [Header("Animation")]
        [SerializeField] private float _breathDuration = 1.5f;
        [SerializeField] private float _breathScale = 1.03f;
        [SerializeField] private float _fadeInDuration = 0.2f;
        [SerializeField] private float _fadeOutDuration = 0.15f;
        [SerializeField] private float _flashDuration = 0.1f;
        [SerializeField] private int _flashCount = 3;

        // =============================================================================
        // STATE
        // =============================================================================

        private CaptureBannerState _currentState = CaptureBannerState.HIDDEN;
        private Vector3 _baseScale;
        private Coroutine _animationCoroutine;
        private bool _isSubscribed = false;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public CaptureBannerState State => _currentState;
        public bool IsVisible => _currentState != CaptureBannerState.HIDDEN;

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action OnCaptureRequested;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            _baseScale = _bannerTransform != null ? _bannerTransform.localScale : Vector3.one;

            if (_keybindText != null)
            {
                _keybindText.text = "C";
            }

            SetState(CaptureBannerState.HIDDEN);
        }

        private void Start()
        {
            SubscribeToCaptureEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromCaptureEvents();
            StopAnimations();
        }

        private void Update()
        {
            // C key handles capture
            if (Input.GetKeyDown(CombatUIDefaults.CaptureKey))
            {
                HandleCaptureInput();
            }
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Set the banner state.
        /// </summary>
        public void SetState(CaptureBannerState state)
        {
            if (_currentState == state) return;

            var previousState = _currentState;
            _currentState = state;

            StopAnimations();

            switch (state)
            {
                case CaptureBannerState.HIDDEN:
                    FadeOut();
                    break;

                case CaptureBannerState.MARKED:
                    SetMarkedState();
                    break;

                case CaptureBannerState.READY:
                    SetReadyState();
                    break;

                case CaptureBannerState.CAPTURING:
                    SetCapturingState();
                    break;
            }
        }

        /// <summary>
        /// Update based on current target.
        /// </summary>
        public void UpdateForTarget(Combatant target)
        {
            if (target == null || CaptureManager.Instance == null)
            {
                SetState(CaptureBannerState.HIDDEN);
                return;
            }

            // Check if target is marked
            bool isMarked = CaptureManager.Instance.IsMarkedForCapture(target);
            if (!isMarked)
            {
                SetState(CaptureBannerState.HIDDEN);
                return;
            }

            // Check if target can be bound (at threshold)
            var nearestAlly = GetNearestAlly();
            bool canBind = BindThresholdCalculator.CanBind(target, nearestAlly);

            if (canBind && !CaptureManager.Instance.IsBound(target))
            {
                SetState(CaptureBannerState.READY);
            }
            else
            {
                SetState(CaptureBannerState.MARKED);
            }
        }

        /// <summary>
        /// Show capture in progress.
        /// </summary>
        public void ShowCapturing()
        {
            SetState(CaptureBannerState.CAPTURING);
        }

        /// <summary>
        /// Hide the banner.
        /// </summary>
        public void Hide()
        {
            SetState(CaptureBannerState.HIDDEN);
        }

        // =============================================================================
        // STATE VISUALS
        // =============================================================================

        private void SetMarkedState()
        {
            FadeIn();
            SetColors(_markedColor);

            if (_captureText != null)
            {
                _captureText.text = "CAPTURE";
            }
        }

        private void SetReadyState()
        {
            FadeIn();
            SetColors(_readyColor);

            if (_captureText != null)
            {
                _captureText.text = "CAPTURE!";
            }

            // Start breathing animation
            _animationCoroutine = StartCoroutine(BreathingAnimation());
        }

        private void SetCapturingState()
        {
            SetColors(_capturingColor);

            if (_captureText != null)
            {
                _captureText.text = "CAPTURING...";
            }

            // Flash animation
            _animationCoroutine = StartCoroutine(FlashAnimation());
        }

        private void SetColors(Color color)
        {
            if (_bannerBackground != null)
            {
                _bannerBackground.color = color;
            }

            if (_keybindIcon != null)
            {
                _keybindIcon.color = color;
            }
        }

        // =============================================================================
        // ANIMATIONS
        // =============================================================================

        private void FadeIn()
        {
            gameObject.SetActive(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                StartCoroutine(FadeCoroutine(1f, _fadeInDuration));
            }
        }

        private void FadeOut()
        {
            if (_canvasGroup != null)
            {
                StartCoroutine(FadeOutCoroutine());
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private IEnumerator FadeCoroutine(float targetAlpha, float duration)
        {
            if (_canvasGroup == null) yield break;

            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
        }

        private IEnumerator FadeOutCoroutine()
        {
            yield return FadeCoroutine(0f, _fadeOutDuration);
            gameObject.SetActive(false);
        }

        private IEnumerator BreathingAnimation()
        {
            if (_bannerTransform == null) yield break;

            while (_currentState == CaptureBannerState.READY)
            {
                float t = (Mathf.Sin(Time.time * Mathf.PI * 2f / _breathDuration) + 1f) / 2f;
                float scale = 1f + (t * (_breathScale - 1f));

                _bannerTransform.localScale = _baseScale * scale;
                yield return null;
            }

            _bannerTransform.localScale = _baseScale;
        }

        private IEnumerator FlashAnimation()
        {
            if (_canvasGroup == null) yield break;

            for (int i = 0; i < _flashCount; i++)
            {
                _canvasGroup.alpha = 0.5f;
                yield return new WaitForSeconds(_flashDuration);
                _canvasGroup.alpha = 1f;
                yield return new WaitForSeconds(_flashDuration);
            }
        }

        private void StopAnimations()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }

            if (_bannerTransform != null)
            {
                _bannerTransform.localScale = _baseScale;
            }
        }

        // =============================================================================
        // INPUT
        // =============================================================================

        private void HandleCaptureInput()
        {
            if (CaptureManager.Instance == null) return;

            switch (_currentState)
            {
                case CaptureBannerState.HIDDEN:
                    // Mark current target
                    var target = BattleManager.Instance?.CurrentTarget;
                    if (target != null && CaptureManager.Instance.IsValidCaptureTarget(target))
                    {
                        CaptureManager.Instance.MarkForCapture(target);
                        SetState(CaptureBannerState.MARKED);
                    }
                    break;

                case CaptureBannerState.MARKED:
                    // Toggle mark off
                    var markedTarget = BattleManager.Instance?.CurrentTarget;
                    if (markedTarget != null)
                    {
                        CaptureManager.Instance.ToggleMark(markedTarget);
                        if (!CaptureManager.Instance.IsMarkedForCapture(markedTarget))
                        {
                            SetState(CaptureBannerState.HIDDEN);
                        }
                    }
                    break;

                case CaptureBannerState.READY:
                    // Request capture
                    OnCaptureRequested?.Invoke();
                    break;
            }
        }

        // =============================================================================
        // CAPTURE EVENTS
        // =============================================================================

        private void SubscribeToCaptureEvents()
        {
            if (_isSubscribed || CaptureManager.Instance == null) return;

            CaptureManager.Instance.OnTargetMarked += HandleTargetMarked;
            CaptureManager.Instance.OnTargetUnmarked += HandleTargetUnmarked;
            CaptureManager.Instance.OnBindThresholdReached += HandleThresholdReached;
            CaptureManager.Instance.OnCapturePhaseStarted += HandleCapturePhaseStarted;
            CaptureManager.Instance.OnCapturePhaseEnded += HandleCapturePhaseEnded;

            _isSubscribed = true;
        }

        private void UnsubscribeFromCaptureEvents()
        {
            if (!_isSubscribed || CaptureManager.Instance == null) return;

            CaptureManager.Instance.OnTargetMarked -= HandleTargetMarked;
            CaptureManager.Instance.OnTargetUnmarked -= HandleTargetUnmarked;
            CaptureManager.Instance.OnBindThresholdReached -= HandleThresholdReached;
            CaptureManager.Instance.OnCapturePhaseStarted -= HandleCapturePhaseStarted;
            CaptureManager.Instance.OnCapturePhaseEnded -= HandleCapturePhaseEnded;

            _isSubscribed = false;
        }

        private void HandleTargetMarked(Combatant target)
        {
            // Check if this is the current target
            if (BattleManager.Instance?.CurrentTarget == target)
            {
                SetState(CaptureBannerState.MARKED);
            }
        }

        private void HandleTargetUnmarked(Combatant target)
        {
            if (BattleManager.Instance?.CurrentTarget == target)
            {
                SetState(CaptureBannerState.HIDDEN);
            }
        }

        private void HandleThresholdReached(Combatant target, float threshold)
        {
            if (BattleManager.Instance?.CurrentTarget == target)
            {
                SetState(CaptureBannerState.READY);
            }
        }

        private void HandleCapturePhaseStarted()
        {
            SetState(CaptureBannerState.CAPTURING);
        }

        private void HandleCapturePhaseEnded()
        {
            SetState(CaptureBannerState.HIDDEN);
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        private Combatant GetNearestAlly()
        {
            // Return player as fallback for threshold calculation
            // In full implementation, this would get the actual nearest ally
            return BattleManager.Instance?.Player;
        }
    }
}
