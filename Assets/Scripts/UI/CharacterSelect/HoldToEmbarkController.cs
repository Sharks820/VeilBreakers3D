using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;
using VeilBreakers.Core;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// AAA hold-to-confirm embark button with flowing energy animation.
    /// Click-and-hold fills a luminous energy sweep across the button over 2.5s.
    /// Visual layers: fill bar, leading edge pulse, energy sweep, border glow, text brighten, completion flash.
    /// </summary>
    public class HoldToEmbarkController : MonoBehaviour
    {
        // =============================================================================
        // CONSTANTS
        // =============================================================================

        private const float kHoldDuration = 2.5f;
        private const string kBtnEmbark = "btn-embark";
        private const string kEmbarkProgressRing = "embark-progress-ring";

        // =============================================================================
        // SERIALIZED FIELDS
        // =============================================================================

        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private CharSelectFocusManager _focusManager;

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
        private bool _holdStarted; // True once hold has begun (for audio)

        // UI Elements
        private VisualElement _btnEmbark;
        private VisualElement _progressTrack;
        private VisualElement _fillBar;
        private VisualElement _fillEdge;
        private VisualElement _energySweep;
        private VisualElement _borderGlow;
        private VisualElement _completionFlash;
        private VisualElement _embarkGlow;
        private Label _embarkText;
        private Label _embarkSubtitle;

        // Tweens
        private Tween _breathingTween;
        private Tween _edgePulseTween;
        private Tween _sweepTween;

        // UNITY-23: Cache last displayed percentage to avoid .text assignment every frame
        private int _lastDisplayedPct = -1;

        // Original colors (for reset)
        private Color _origTextColor;

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

            _sfxSource = GetComponent<AudioSource>();
            if (_sfxSource == null) _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;

            _holdDroneClip = GenerateRisingTone(180f, 520f, kHoldDuration, 0.15f);
            _embarkCompleteClip = GenerateCompletionChime();
        }

        private void OnDisable()
        {
            CharSelectEvents.OnScreenReady -= HandleScreenReady;
            CharSelectEvents.OnEmbarkTriggered -= HandleEmbarkTriggered;

            if (_btnEmbark != null)
            {
                _btnEmbark.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
                _btnEmbark.UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
                _btnEmbark.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave, TrickleDown.TrickleDown);
                _btnEmbark.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
                _btnEmbark.UnregisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
            }

            _breathingTween.Stop();
            _edgePulseTween.Stop();
            _sweepTween.Stop();

            if (_holdDroneClip != null) { Destroy(_holdDroneClip); _holdDroneClip = null; }
            if (_embarkCompleteClip != null) { Destroy(_embarkCompleteClip); _embarkCompleteClip = null; }
            _sfxSource = null;

            _isInitialized = false;
            _isEmbarking = false;
            _isMouseHolding = false;
            _holdStarted = false;
            _holdProgress = 0f;
            _btnEmbark = null;
            _fillBar = null;
        }

        // =============================================================================
        // INITIALIZATION — Build AAA visual layers
        // =============================================================================

        private void HandleScreenReady()
        {
            if (_uiDocument == null) return;
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            _btnEmbark = root.Q<VisualElement>(kBtnEmbark);
            if (_btnEmbark == null)
            {
                Debug.LogError("[HoldToEmbark] btn-embark not found in UXML!");
                return;
            }

            _embarkText = _btnEmbark.Q<Label>("embark-text");
            _embarkSubtitle = _btnEmbark.Q<Label>("embark-subtitle");
            _embarkGlow = _btnEmbark.Q<VisualElement>("embark-glow");

            // Cache original colors
            _origTextColor = _embarkText != null
                ? new Color(15f/255f, 10f/255f, 5f/255f, 0.95f)
                : Color.black;

            // Remove any old progress elements
            var oldTrack = _btnEmbark.Q<VisualElement>("embark-progress-track");
            if (oldTrack != null) _btnEmbark.Remove(oldTrack);
            var oldRing = _btnEmbark.Q<VisualElement>(kEmbarkProgressRing);
            if (oldRing != null && oldRing.parent != null) oldRing.parent.Remove(oldRing);

            BuildVisualLayers();
            RegisterPointerEvents();
            ResetAllVisuals();

            _isInitialized = true;
            StartBreathingGlow();

            Debug.Log("[HoldToEmbark] AAA embark button initialized");
        }

        private void BuildVisualLayers()
        {
            // Layer 1: Progress track (clips the fill bar)
            _progressTrack = new VisualElement();
            _progressTrack.name = "embark-progress-track";
            _progressTrack.pickingMode = PickingMode.Ignore;
            _progressTrack.style.position = Position.Absolute;
            _progressTrack.style.left = 0;
            _progressTrack.style.right = 0;
            _progressTrack.style.top = 0;
            _progressTrack.style.bottom = 0;
            _progressTrack.style.overflow = Overflow.Hidden;
            _progressTrack.style.borderTopLeftRadius = 6f;
            _progressTrack.style.borderTopRightRadius = 6f;
            _progressTrack.style.borderBottomLeftRadius = 6f;
            _progressTrack.style.borderBottomRightRadius = 6f;
            _btnEmbark.Insert(0, _progressTrack);

            // Layer 2: Fill bar — energy that flows left to right
            _fillBar = new VisualElement();
            _fillBar.name = kEmbarkProgressRing;
            _fillBar.pickingMode = PickingMode.Ignore;
            _fillBar.style.position = Position.Absolute;
            _fillBar.style.left = 0;
            _fillBar.style.top = 0;
            _fillBar.style.bottom = 0;
            _fillBar.style.width = new Length(0, LengthUnit.Percent);
            _fillBar.usageHints = UsageHints.DynamicTransform;
            _progressTrack.Add(_fillBar);

            // Layer 3: Leading edge — bright pulsing line at the fill frontier
            _fillEdge = new VisualElement();
            _fillEdge.name = "embark-fill-edge";
            _fillEdge.pickingMode = PickingMode.Ignore;
            _fillEdge.style.position = Position.Absolute;
            _fillEdge.style.right = 0;
            _fillEdge.style.top = 0;
            _fillEdge.style.bottom = 0;
            _fillEdge.style.width = 4;
            _fillEdge.style.opacity = 0f;
            _fillBar.Add(_fillEdge);

            // Layer 4: Energy sweep — luminous highlight that slides across repeatedly
            _energySweep = new VisualElement();
            _energySweep.name = "embark-energy-sweep";
            _energySweep.pickingMode = PickingMode.Ignore;
            _energySweep.style.position = Position.Absolute;
            _energySweep.style.top = 0;
            _energySweep.style.bottom = 0;
            _energySweep.style.width = new Length(20, LengthUnit.Percent);
            _energySweep.style.left = new Length(-20, LengthUnit.Percent);
            _energySweep.style.opacity = 0f;
            _progressTrack.Add(_energySweep);

            // Layer 5: Border glow overlay (covers full button, only border visible)
            _borderGlow = new VisualElement();
            _borderGlow.name = "embark-border-glow";
            _borderGlow.pickingMode = PickingMode.Ignore;
            _borderGlow.style.position = Position.Absolute;
            _borderGlow.style.left = -2;
            _borderGlow.style.right = -2;
            _borderGlow.style.top = -2;
            _borderGlow.style.bottom = -2;
            _borderGlow.style.borderTopWidth = 2;
            _borderGlow.style.borderBottomWidth = 2;
            _borderGlow.style.borderLeftWidth = 2;
            _borderGlow.style.borderRightWidth = 2;
            _borderGlow.style.borderTopLeftRadius = 8;
            _borderGlow.style.borderTopRightRadius = 8;
            _borderGlow.style.borderBottomLeftRadius = 8;
            _borderGlow.style.borderBottomRightRadius = 8;
            _borderGlow.style.opacity = 0f;
            _btnEmbark.Add(_borderGlow);

            // Layer 6: Completion flash (full white overlay)
            _completionFlash = new VisualElement();
            _completionFlash.name = "embark-completion-flash";
            _completionFlash.pickingMode = PickingMode.Ignore;
            _completionFlash.style.position = Position.Absolute;
            _completionFlash.style.left = 0;
            _completionFlash.style.right = 0;
            _completionFlash.style.top = 0;
            _completionFlash.style.bottom = 0;
            _completionFlash.style.backgroundColor = new Color(1f, 0.95f, 0.8f, 0f);
            _completionFlash.style.borderTopLeftRadius = 6;
            _completionFlash.style.borderTopRightRadius = 6;
            _completionFlash.style.borderBottomLeftRadius = 6;
            _completionFlash.style.borderBottomRightRadius = 6;
            _completionFlash.style.opacity = 0f;
            _btnEmbark.Add(_completionFlash);
        }

        private void RegisterPointerEvents()
        {
            _btnEmbark.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            _btnEmbark.UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            _btnEmbark.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave, TrickleDown.TrickleDown);
            _btnEmbark.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            _btnEmbark.UnregisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);

            _btnEmbark.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            _btnEmbark.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            _btnEmbark.RegisterCallback<PointerLeaveEvent>(OnPointerLeave, TrickleDown.TrickleDown);
            _btnEmbark.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            _btnEmbark.RegisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
        }

        // =============================================================================
        // BREATHING GLOW (idle state)
        // =============================================================================

        private void StartBreathingGlow()
        {
            if (_embarkGlow == null) return;
            _breathingTween.Stop();
            _embarkGlow.style.opacity = 0.2f;
            _breathingTween = Tween.Custom(_embarkGlow, 0.2f, 0.5f, 1.25f, // VB-IGNORE PERF-02 -- PrimeTween callback, not hot path
                onValueChange: (glow, val) => { glow.style.opacity = val; },
                ease: Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo);
        }

        private void StopBreathingGlow()
        {
            _breathingTween.Stop();
            if (_embarkGlow != null) _embarkGlow.style.opacity = 0.7f;
        }

        private void HandleEmbarkTriggered()
        {
            _isEmbarking = true;
        }

        // =============================================================================
        // POINTER EVENT HANDLERS
        // =============================================================================

        private void OnPointerDown(PointerDownEvent evt) => _isMouseHolding = true;
        private void OnPointerUp(PointerUpEvent evt) => _isMouseHolding = false;
        private void OnPointerLeave(PointerLeaveEvent evt) => _isMouseHolding = false;
        private void OnMouseDown(MouseDownEvent evt) => _isMouseHolding = true;
        private void OnMouseUp(MouseUpEvent evt) => _isMouseHolding = false;

        // =============================================================================
        // UPDATE — CORE HOLD LOGIC + VISUAL DRIVE
        // =============================================================================

        private void Update()
        {
            if (!_isInitialized || _isEmbarking) return;

            bool mouseHold = _isMouseHolding;
            bool gamepadHold = InputManager.HasInstance
                && InputManager.Instance.GetAction(InputManager.GameAction.Confirm)
                && _focusManager != null && _focusManager.CurrentZoneIndex == 2;
            bool wantsHold = mouseHold || gamepadHold;

            if (wantsHold)
            {
                if (!_holdStarted)
                {
                    _holdStarted = true;
                    OnHoldBegin();
                }

                _holdProgress += Time.deltaTime / kHoldDuration;

                if (_focusManager != null) _focusManager.SetHoldLock(true);

                if (_holdProgress >= 1f)
                {
                    _holdProgress = 1f;
                    _isEmbarking = true;
                    if (_focusManager != null) _focusManager.SetHoldLock(false);

                    UpdateHoldVisuals(_holdProgress);
                    OnHoldComplete();
                    CharSelectEvents.RaiseEmbarkTriggered();
                    return;
                }

                UpdateHoldVisuals(_holdProgress);
                CharSelectEvents.RaiseEmbarkHoldProgress(_holdProgress);
            }
            else if (_holdProgress > 0f)
            {
                // Released early — snap back
                _holdStarted = false;
                _holdProgress = 0f;
                if (_focusManager != null) _focusManager.SetHoldLock(false);

                OnHoldCancelled();
                CharSelectEvents.RaiseEmbarkHoldProgress(0f);
            }
        }

        // =============================================================================
        // HOLD LIFECYCLE EVENTS
        // =============================================================================

        private void OnHoldBegin()
        {
            StopBreathingGlow();

            // Start rising drone audio
            if (_sfxSource != null && _holdDroneClip != null)
            {
                _sfxSource.clip = _holdDroneClip;
                _sfxSource.loop = false;
                _sfxSource.Play();
            }

            // Start energy sweep animation
            StartEnergySweep();

            // Show the fill edge
            if (_fillEdge != null)
            {
                _fillEdge.style.opacity = 1f;
                _edgePulseTween.Stop();
                _edgePulseTween = Tween.Custom(_fillEdge, 0.6f, 1f, 0.3f, // VB-IGNORE PERF-02 -- PrimeTween callback
                    onValueChange: (edge, val) => { edge.style.opacity = val; },
                    ease: Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo);
            }

            // Scale button slightly on hold start
            if (_btnEmbark != null)
            {
                Tween.Custom(_btnEmbark, 1f, 0.97f, 0.15f, // VB-IGNORE PERF-02 -- PrimeTween callback
                    onValueChange: (btn, val) => { btn.style.scale = new Scale(new Vector2(val, val)); },
                    ease: Ease.OutCubic);
            }
        }

        private void OnHoldCancelled()
        {
            // Stop audio
            if (_sfxSource != null) _sfxSource.Stop();

            // Kill sweep + edge tweens
            _edgePulseTween.Stop();
            _sweepTween.Stop();

            // Reset all visuals smoothly
            ResetAllVisuals();

            // Scale back
            if (_btnEmbark != null)
            {
                Tween.Custom(_btnEmbark, 0.97f, 1f, 0.3f, // VB-IGNORE PERF-02 -- PrimeTween callback
                    onValueChange: (btn, val) => { btn.style.scale = new Scale(new Vector2(val, val)); },
                    ease: Ease.OutBack);
            }

            // Restart idle breathing
            StartBreathingGlow();
        }

        private void OnHoldComplete()
        {
            // Stop ongoing tweens
            _edgePulseTween.Stop();
            _sweepTween.Stop();
            if (_sfxSource != null) _sfxSource.Stop();

            // Play completion chime
            if (_sfxSource != null && _embarkCompleteClip != null)
            {
                _sfxSource.PlayOneShot(_embarkCompleteClip);
            }

            // Completion flash: bright white burst that fades
            if (_completionFlash != null)
            {
                _completionFlash.style.backgroundColor = new Color(1f, 0.97f, 0.9f, 0.85f);
                _completionFlash.style.opacity = 1f;
                Tween.Custom(_completionFlash, 1f, 0f, 0.6f, // VB-IGNORE PERF-02 -- PrimeTween callback
                    onValueChange: (flash, val) => { flash.style.opacity = val; },
                    ease: Ease.OutExpo);
            }

            // Scale pop
            if (_btnEmbark != null)
            {
                Tween.Custom(_btnEmbark, 1f, 1.06f, 0.12f, // VB-IGNORE PERF-02 -- PrimeTween callback
                    onValueChange: (btn, val) => { btn.style.scale = new Scale(new Vector2(val, val)); },
                    ease: Ease.OutCubic);
            }
        }

        // =============================================================================
        // VISUAL UPDATE (called every frame during hold)
        // =============================================================================

        private void UpdateHoldVisuals(float progress)
        {
            float p = Mathf.Clamp01(progress);

            // --- Fill bar: width 0% → 100% with dynamic color ---
            if (_fillBar != null)
            {
                _fillBar.style.width = new Length(p * 100f, LengthUnit.Percent);

                // Color: starts dark warm, ends bright fiery
                float r = Mathf.Lerp(0.85f, 1f, p);
                float g = Mathf.Lerp(0.45f, 0.75f, p);
                float b = Mathf.Lerp(0.12f, 0.25f, p);
                float a = Mathf.Lerp(0.25f, 0.55f, p);
                _fillBar.style.backgroundColor = new Color(r, g, b, a);
            }

            // --- Leading edge: bright white-gold, width grows ---
            if (_fillEdge != null)
            {
                float edgeWidth = Mathf.Lerp(3f, 6f, p);
                _fillEdge.style.width = edgeWidth;
                float ea = Mathf.Lerp(0.6f, 1f, p);
                _fillEdge.style.backgroundColor = new Color(1f, 0.95f, 0.8f, ea);
            }

            // --- Border glow: intensifies with progress ---
            if (_borderGlow != null)
            {
                float bo = Mathf.Lerp(0f, 0.8f, p);
                _borderGlow.style.opacity = bo;
                Color bc = Color.Lerp(
                    new Color(1f, 0.65f, 0.2f, 0.4f),
                    new Color(1f, 0.85f, 0.4f, 1f),
                    p);
                _borderGlow.style.borderTopColor = bc;
                _borderGlow.style.borderBottomColor = bc;
                _borderGlow.style.borderLeftColor = bc;
                _borderGlow.style.borderRightColor = bc;
            }

            // --- Energy sweep: opacity and speed increase ---
            if (_energySweep != null)
            {
                float so = Mathf.Lerp(0.15f, 0.5f, p);
                _energySweep.style.opacity = so;

                Color sc = Color.Lerp(
                    new Color(1f, 0.8f, 0.4f, 0.3f),
                    new Color(1f, 0.95f, 0.7f, 0.6f),
                    p);
                _energySweep.style.backgroundColor = sc;
            }

            // --- Text: brightens as progress increases ---
            if (_embarkText != null)
            {
                Color tc = Color.Lerp(
                    _origTextColor,
                    new Color(1f, 0.98f, 0.9f, 1f),
                    p * 0.5f);
                _embarkText.style.color = tc;
            }

            // --- Subtitle: shows "HOLD..." text (UNITY-23: only update when value changes) ---
            if (_embarkSubtitle != null)
            {
                int pctInt = (int)(p * 100f);
                if (pctInt != _lastDisplayedPct)
                {
                    _lastDisplayedPct = pctInt;
                    if (pctInt < 100)
                        _embarkSubtitle.text = $"HOLD  {pctInt}%";
                    else
                        _embarkSubtitle.text = "CONFIRMED";
                }
            }

            // --- Button scale: slight pulse at key thresholds ---
            if (_btnEmbark != null && p > 0.5f)
            {
                float pulse = 0.97f + Mathf.Sin(p * Mathf.PI * 6f) * 0.008f;
                _btnEmbark.style.scale = new Scale(new Vector2(pulse, pulse));
            }
        }

        // =============================================================================
        // ENERGY SWEEP ANIMATION
        // =============================================================================

        private void StartEnergySweep()
        {
            if (_energySweep == null) return;
            _sweepTween.Stop();

            // Continuous sweep: slides a bright highlight from left (-20%) to right (100%)
            // Speed increases as hold progresses (handled by cycling duration)
            _energySweep.style.opacity = 0.2f;
            RunSweepCycle();
        }

        private void RunSweepCycle()
        {
            if (_energySweep == null || _isEmbarking || !_holdStarted) return;

            // Sweep gets faster as hold progresses
            float duration = Mathf.Lerp(1.2f, 0.4f, _holdProgress);

            _sweepTween = Tween.Custom(_energySweep, -20f, 100f, duration,
                onValueChange: (sweep, val) =>
                {
                    sweep.style.left = new Length(val, LengthUnit.Percent);
                },
                ease: Ease.InOutQuad);

            // Chain next cycle after this one
            _sweepTween.OnComplete(() => RunSweepCycle());
        }

        // =============================================================================
        // RESET
        // =============================================================================

        private void ResetAllVisuals()
        {
            if (_fillBar != null)
            {
                _fillBar.style.width = new Length(0, LengthUnit.Percent);
                _fillBar.style.backgroundColor = new Color(0.85f, 0.45f, 0.12f, 0.25f);
            }

            if (_fillEdge != null)
            {
                _fillEdge.style.opacity = 0f;
                _fillEdge.style.width = 3;
            }

            if (_energySweep != null)
            {
                _energySweep.style.opacity = 0f;
                _energySweep.style.left = new Length(-20, LengthUnit.Percent);
            }

            if (_borderGlow != null)
            {
                _borderGlow.style.opacity = 0f;
            }

            if (_completionFlash != null)
            {
                _completionFlash.style.opacity = 0f;
            }

            if (_embarkText != null)
            {
                _embarkText.style.color = _origTextColor;
            }

            if (_embarkSubtitle != null)
            {
                _embarkSubtitle.text = "HOLD TO CONFIRM"; // VB-IGNORE UNITY-23 -- reset path (not Update), called once on cancel
                _lastDisplayedPct = -1;
            }

            if (_btnEmbark != null)
            {
                _btnEmbark.style.scale = new Scale(Vector2.one);
            }
        }

        // =============================================================================
        // AUDIO — PROCEDURAL TONE GENERATION
        // =============================================================================

        private static AudioClip GenerateRisingTone(float startFreq, float endFreq, float duration, float volume)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            var clip = AudioClip.Create("embark_rising_tone", sampleCount, 1, sampleRate, false);
            float[] samples = new float[sampleCount];
            float phase = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(startFreq, endFreq, progress * progress); // Exponential rise
                float envelope = Mathf.Lerp(0.3f, 1f, progress); // Crescendo
                // Add subtle harmonic for depth
                float fundamental = Mathf.Sin(2f * Mathf.PI * phase);
                float harmonic = Mathf.Sin(2f * Mathf.PI * phase * 1.5f) * 0.3f;
                samples[i] = (fundamental + harmonic) * volume * envelope;
                phase += freq / sampleRate;
            }

            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateCompletionChime()
        {
            int sampleRate = 44100;
            float duration = 0.5f;
            int sampleCount = (int)(sampleRate * duration);
            var clip = AudioClip.Create("embark_complete_chime", sampleCount, 1, sampleRate, false);
            float[] samples = new float[sampleCount];

            // Two-note ascending chime (C5 → E5)
            float freq1 = 523f; // C5
            float freq2 = 659f; // E5
            float volume = 0.35f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float envelope = Mathf.Max(0f, 1f - progress * 2f); // Fast decay
                float freq = progress < 0.3f ? freq1 : freq2;
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * volume * envelope;
            }

            clip.SetData(samples, 0);
            return clip;
        }
    }
}
