using System;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Implements hold-to-confirm embark. Tracks mouse click-hold on btn-embark and gamepad
    /// A-button hold via InputManager. Fills progress ring over 1.5s, triggers embark on completion.
    /// Replaces the old click -> confirm popup flow.
    /// </summary>
    public class HoldToEmbarkController : MonoBehaviour
    {
        // =============================================================================
        // CONSTANTS
        // =============================================================================

        private const float kHoldDuration = 1.5f;
        private const string kBtnEmbark = "btn-embark";
        private const string kEmbarkProgressRing = "embark-progress-ring";
        private const string kHoldActiveClass = "hold-active";

        // =============================================================================
        // SERIALIZED FIELDS
        // =============================================================================

        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private CharSelectFocusManager _focusManager;

        /// <summary>
        /// Programmatically wires dependencies. Called by CharacterSelectManager.EnsureCharSelectComponents().
        /// </summary>
        public void AutoWire(UIDocument uiDocument, CharSelectFocusManager focusManager)
        {
            if (_uiDocument == null) _uiDocument = uiDocument;
            if (_focusManager == null) _focusManager = focusManager;
        }

        // =============================================================================
        // STATE
        // =============================================================================

        private float _holdProgress;
        private bool _isMouseHolding;
        private bool _isEmbarking;
        private bool _isInitialized;
        private VisualElement _btnEmbark;
        private VisualElement _progressRing;

        // =============================================================================
        // AUDIO
        // =============================================================================

        private AudioClip _holdDroneClip;
        private AudioClip _embarkCompleteClip;
        private AudioSource _sfxSource;

        // =============================================================================
        // LIFECYCLE
        // =============================================================================

        private void OnEnable()
        {
            CharSelectEvents.OnScreenReady += HandleScreenReady;
            CharSelectEvents.OnEmbarkTriggered += HandleEmbarkTriggered;

            // Get or create AudioSource (guard against re-enable adding duplicates)
            _sfxSource = GetComponent<AudioSource>();
            if (_sfxSource == null) _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;

            // Generate placeholder audio clips
            _holdDroneClip = GenerateRisingTone(200f, 400f, kHoldDuration, 0.2f);
            _embarkCompleteClip = GeneratePlaceholderTone(600f, 0.3f, 0.5f);
        }

        private void OnDisable()
        {
            CharSelectEvents.OnScreenReady -= HandleScreenReady;
            CharSelectEvents.OnEmbarkTriggered -= HandleEmbarkTriggered;

            // Unregister UI callbacks
            if (_btnEmbark != null)
            {
                _btnEmbark.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                _btnEmbark.UnregisterCallback<PointerUpEvent>(OnPointerUp);
                _btnEmbark.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
            }

            // Cleanup generated audio clips (owned by this component)
            if (_holdDroneClip != null) { Destroy(_holdDroneClip); _holdDroneClip = null; }
            if (_embarkCompleteClip != null) { Destroy(_embarkCompleteClip); _embarkCompleteClip = null; }
            // Don't destroy shared AudioSource - other components may reference it
            _sfxSource = null;

            // Reset state
            _isInitialized = false;
            _isEmbarking = false;
            _isMouseHolding = false;
            _holdProgress = 0f;
            _btnEmbark = null;
            _progressRing = null;
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void HandleScreenReady()
        {
            if (_uiDocument == null) return;
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            _btnEmbark = root.Q<VisualElement>(kBtnEmbark);
            Debug.Assert(_btnEmbark != null, $"[HoldToEmbarkController] Element '{kBtnEmbark}' not found in UXML");

            // Try to find existing progress ring, create one if missing
            _progressRing = root.Q<VisualElement>(kEmbarkProgressRing);
            if (_progressRing == null && _btnEmbark != null)
            {
                _progressRing = new VisualElement();
                _progressRing.name = kEmbarkProgressRing;
                _progressRing.pickingMode = PickingMode.Ignore;
                _progressRing.style.position = Position.Absolute;
                _progressRing.style.top = 0;
                _progressRing.style.left = 0;
                _progressRing.style.right = 0;
                _progressRing.style.bottom = 0;
                _progressRing.style.opacity = 0f;
                _progressRing.style.scale = new Scale(Vector2.one * 0.8f);
                _progressRing.style.borderTopWidth = 3f;
                _progressRing.style.borderBottomWidth = 3f;
                _progressRing.style.borderLeftWidth = 3f;
                _progressRing.style.borderRightWidth = 3f;
                _progressRing.style.borderTopColor = new Color(1f, 0.7f, 0.2f, 0.8f);
                _progressRing.style.borderBottomColor = new Color(1f, 0.7f, 0.2f, 0.8f);
                _progressRing.style.borderLeftColor = new Color(1f, 0.7f, 0.2f, 0.8f);
                _progressRing.style.borderRightColor = new Color(1f, 0.7f, 0.2f, 0.8f);
                _progressRing.style.borderTopLeftRadius = 8f;
                _progressRing.style.borderTopRightRadius = 8f;
                _progressRing.style.borderBottomLeftRadius = 8f;
                _progressRing.style.borderBottomRightRadius = 8f;
                _progressRing.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
                _btnEmbark.Add(_progressRing);
            }

            // Register pointer events on btn-embark for mouse hold detection
            if (_btnEmbark != null)
            {
                _btnEmbark.RegisterCallback<PointerDownEvent>(OnPointerDown);
                _btnEmbark.RegisterCallback<PointerUpEvent>(OnPointerUp);
                _btnEmbark.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            }

            ResetProgressVisual();
            _isInitialized = true;
        }

        private void HandleEmbarkTriggered()
        {
            _isEmbarking = true;
        }

        // =============================================================================
        // POINTER EVENT HANDLERS
        // =============================================================================

        private void OnPointerDown(PointerDownEvent evt)
        {
            _isMouseHolding = true;
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            _isMouseHolding = false;
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            _isMouseHolding = false;
        }

        // =============================================================================
        // UPDATE -- CORE HOLD LOGIC
        // =============================================================================

        private void Update()
        {
            if (!_isInitialized || _isEmbarking) return;

            // Check both mouse hold AND gamepad hold
            bool mouseHold = _isMouseHolding;
            bool gamepadHold = InputManager.HasInstance && InputManager.Instance.GetAction(InputManager.GameAction.Confirm);
            bool wantsHold = mouseHold || gamepadHold;

            if (wantsHold)
            {
                _holdProgress += Time.deltaTime / kHoldDuration;

                // Lock navigation during hold
                if (_focusManager != null)
                {
                    _focusManager.SetHoldLock(true);
                }

                if (_holdProgress >= 1f)
                {
                    _holdProgress = 1f;
                    _isEmbarking = true;

                    if (_focusManager != null)
                    {
                        _focusManager.SetHoldLock(false);
                    }

                    UpdateProgressVisual(_holdProgress);
                    PlayEmbarkComplete();
                    CharSelectEvents.RaiseEmbarkTriggered();
                    return;
                }

                UpdateProgressVisual(_holdProgress);
                CharSelectEvents.RaiseEmbarkHoldProgress(_holdProgress);
            }
            else if (_holdProgress > 0f)
            {
                // Released early -- reset
                _holdProgress = 0f;

                if (_focusManager != null)
                {
                    _focusManager.SetHoldLock(false);
                }

                ResetProgressVisual();
                CharSelectEvents.RaiseEmbarkHoldProgress(0f);
            }
        }

        // =============================================================================
        // PROGRESS VISUAL
        // =============================================================================

        /// <summary>
        /// Updates the progress ring visual during hold. Uses scale and opacity
        /// to provide visual feedback of hold progress.
        /// </summary>
        private void UpdateProgressVisual(float progress)
        {
            if (_progressRing == null) return;

            if (progress > 0f && !_progressRing.ClassListContains(kHoldActiveClass))
            {
                _progressRing.AddToClassList(kHoldActiveClass);
            }

            // Scale from 0.8 to 1.0 as progress fills
            float scaleVal = 0.8f + 0.2f * progress;
            _progressRing.style.scale = new Scale(new Vector2(scaleVal, scaleVal));

            // Opacity from 0.3 to 1.0 as progress fills
            _progressRing.style.opacity = 0.3f + 0.7f * progress;
        }

        /// <summary>
        /// Resets the progress ring visual to its initial hidden state.
        /// </summary>
        private void ResetProgressVisual()
        {
            if (_progressRing == null) return;

            _progressRing.RemoveFromClassList(kHoldActiveClass);
            _progressRing.style.scale = new Scale(Vector2.one * 0.8f);
            _progressRing.style.opacity = 0f;
        }

        // =============================================================================
        // AUDIO -- PLACEHOLDER TONE GENERATION
        // =============================================================================

        /// <summary>
        /// Generates a simple sine wave AudioClip for placeholder SFX.
        /// Includes a linear fade-out to prevent click artifacts.
        /// </summary>
        private static AudioClip GeneratePlaceholderTone(float frequency, float duration, float volume)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            var clip = AudioClip.Create("placeholder_tone", sampleCount, 1, sampleRate, false);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - t / duration;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * envelope;
            }

            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generates a rising-pitch tone AudioClip for the embark hold drone.
        /// Frequency sweeps linearly from startFreq to endFreq over the duration.
        /// </summary>
        private static AudioClip GenerateRisingTone(float startFreq, float endFreq, float duration, float volume)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            var clip = AudioClip.Create("rising_tone", sampleCount, 1, sampleRate, false);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(startFreq, endFreq, progress);
                float envelope = Mathf.Lerp(0.5f, 1f, progress); // Crescendo
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * volume * envelope;
            }

            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Plays the embark completion sound.</summary>
        private void PlayEmbarkComplete()
        {
            if (_sfxSource != null && _embarkCompleteClip != null)
            {
                _sfxSource.PlayOneShot(_embarkCompleteClip);
            }
        }
    }
}
