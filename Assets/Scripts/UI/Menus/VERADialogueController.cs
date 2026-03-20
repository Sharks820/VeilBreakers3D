using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Audio;
using VeilBreakers.Core;
using VeilBreakers.UI.Core;

namespace VeilBreakers.UI.Menus
{
    /// <summary>
    /// Controller for VERA's dialogue UI.
    /// Handles text display, typewriter effect, choices, and glitch visuals.
    /// Integrates with VERAVoiceController for audio and integrity state.
    /// </summary>
    public class VERADialogueController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("VERA Portrait")]
        [SerializeField] private Sprite _veraPortraitNormal;
        [SerializeField] private Sprite _veraPortraitGlitched;

        [Header("Typewriter Settings")]
        [SerializeField] private float _baseTypeSpeed = 0.03f;
        [SerializeField] private float _punctuationPause = 0.15f;
        [SerializeField] private float _glitchedTypeSpeed = 0.015f;

        [Header("Glitch Settings")]
        [SerializeField] private float _glitchFlickerRate = 0.1f;
        [SerializeField] private int _glitchCharSwapCount = 3;

        // =============================================================================
        // RUNTIME STATE
        // =============================================================================

        private VisualElement _root;
        private bool _isDisplayingText = false;
        private bool _isWaitingForInput = false;
        private string _fullText = "";
        private readonly StringBuilder _displayedTextBuilder = new StringBuilder(256);
        private int _currentCharIndex = 0;
        private float _currentVeilIntegrity = 100f;
        private bool _isGlitched = false;
        private Coroutine _typewriterCoroutine;
        private Coroutine _glitchCoroutine;

        private DialogueData _currentDialogue;
        private int _currentLineIndex = 0;

        // Glitch characters for text corruption
        private static readonly char[] GlitchChars = { '!', '@', '#', '$', '%', '^', '&', '*', '/', '\\', '|', '~', '`' };

        // Cached WaitForSeconds to avoid per-character allocation in typewriter coroutine
        private WaitForSeconds _waitGlitchChar;
        private WaitForSeconds _waitPunctuation;
        private WaitForSeconds _waitComma;
        private WaitForSeconds _waitTypeSpeed;
        private WaitForSeconds _waitGlitchedTypeSpeed;
        private WaitForSeconds _waitGlitchPulse;
        private VERAVoiceController _cachedVoiceController; // Cached for safe unsubscribe

        // =============================================================================
        // UI ELEMENTS
        // =============================================================================

        private VisualElement _dialogueContainer;
        private VisualElement _glitchOverlay;
        private VisualElement _portraitFrame;
        private VisualElement _portraitGlow;
        private VisualElement _veraPortrait;
        private VisualElement _portraitScanlines;
        private Label _speakerName;
        private VisualElement _integrityIndicator;
        private Label _dialogueText;
        private VisualElement _continueIndicator;
        private VisualElement _choicesContainer;
        private VisualElement _choicesList;
        private VisualElement _integrityBarFill;
        private Label _integrityPercent;
        
        // Cached Colors for performance
        private static readonly Color IntegrityColorNormal = new Color(0.47f, 0.78f, 0.59f);
        private static readonly Color IntegrityColorWarning = new Color(0.86f, 0.71f, 0.24f);
        private static readonly Color IntegrityColorCritical = new Color(0.78f, 0.24f, 0.31f);
        private static readonly Color IntegrityColorCorrupted = new Color(0.63f, 0.24f, 0.47f);

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action<string> OnDialogueStarted;
        public event Action OnDialogueEnded;
        public event Action<int> OnChoiceSelected;
        public event Action<string> OnLineDisplayed;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            // Cache WaitForSeconds to avoid per-character GC allocations in typewriter
            _waitGlitchChar = new WaitForSeconds(0.03f);
            _waitPunctuation = new WaitForSeconds(_punctuationPause);
            _waitComma = new WaitForSeconds(_punctuationPause * 0.5f);
            _waitTypeSpeed = new WaitForSeconds(_baseTypeSpeed);
            _waitGlitchedTypeSpeed = new WaitForSeconds(_glitchedTypeSpeed);
            _waitGlitchPulse = new WaitForSeconds(0.1f);
        }

        private void OnEnable()
        {
            InitializeUI();
            SubscribeToVERA();
        }

        private void OnDisable()
        {
            if (_dialogueContainer != null)
            {
                _dialogueContainer.UnregisterCallback<ClickEvent>(OnDialogueBoxClicked);
            }
            UnsubscribeFromVERA();
            StopAllCoroutines();
        }

        private void Update()
        {
            if (!InputManager.HasInstance) return;

            // Check for input to advance dialogue
            if (_isWaitingForInput && (InputManager.Instance.GetMouseButtonDown(0) || InputManager.Instance.GetActionDown(InputManager.GameAction.DialogueAdvance)))
            {
                AdvanceDialogue();
            }
            // Skip typewriter effect (else prevents same-frame double-fire after CompleteCurrentLine sets _isWaitingForInput)
            else if (_isDisplayingText && (InputManager.Instance.GetMouseButtonDown(0) || InputManager.Instance.GetActionDown(InputManager.GameAction.DialogueAdvance)))
            {
                CompleteCurrentLine();
            }
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void InitializeUI()
        {
            if (_uiDocument == null)
            {
                ErrorLogger.Error("VERADialogueController: UIDocument is null!");
                return;
            }

            _root = _uiDocument.rootVisualElement;

            // Query elements
            _dialogueContainer = _root.Q<VisualElement>("dialogue-container");
            _glitchOverlay = _root.Q<VisualElement>("glitch-overlay");
            _portraitFrame = _root.Q<VisualElement>("portrait-frame");
            _portraitGlow = _root.Q<VisualElement>("portrait-glow");
            _veraPortrait = _root.Q<VisualElement>("vera-portrait");
            _portraitScanlines = _root.Q<VisualElement>("portrait-scanlines");
            _speakerName = _root.Q<Label>("speaker-name");
            _integrityIndicator = _root.Q<VisualElement>("integrity-indicator");
            _dialogueText = _root.Q<Label>("dialogue-text");
            _continueIndicator = _root.Q<VisualElement>("continue-indicator");
            _choicesContainer = _root.Q<VisualElement>("choices-container");
            _choicesList = _root.Q<VisualElement>("choices-list");
            _integrityBarFill = _root.Q<VisualElement>("integrity-bar-fill");
            _integrityPercent = _root.Q<Label>("integrity-percent");

            // Set portrait if available
            if (_veraPortraitNormal != null && _veraPortrait != null)
            {
                _veraPortrait.style.backgroundImage = new StyleBackground(_veraPortraitNormal);
            }

            // Register click handler for dialogue box
            _dialogueContainer?.RegisterCallback<ClickEvent>(OnDialogueBoxClicked);

            // Hide by default
            Hide();

            ErrorLogger.UI("VERADialogue initialized");
        }

        private void SubscribeToVERA()
        {
            if (VERAVoiceController.Instance != null)
            {
                _cachedVoiceController = VERAVoiceController.Instance;
                _cachedVoiceController.OnVeilIntegrityChanged += OnVeilIntegrityChanged;
                _cachedVoiceController.OnGlitchTriggered += OnGlitchTriggered;

                // Get initial integrity
                _currentVeilIntegrity = _cachedVoiceController.VeilIntegrity;
                UpdateIntegrityDisplay();
            }
        }

        private void UnsubscribeFromVERA()
        {
            // Use cached reference to safely unsubscribe even if singleton was destroyed
            if (_cachedVoiceController != null)
            {
                _cachedVoiceController.OnVeilIntegrityChanged -= OnVeilIntegrityChanged;
                _cachedVoiceController.OnGlitchTriggered -= OnGlitchTriggered;
                _cachedVoiceController = null;
            }
        }

        // =============================================================================
        // VEIL INTEGRITY
        // =============================================================================

        private void OnVeilIntegrityChanged(float newIntegrity)
        {
            _currentVeilIntegrity = newIntegrity;
            UpdateIntegrityDisplay();
            UpdateGlitchState();
        }

        private void UpdateIntegrityDisplay()
        {
            if (_integrityBarFill != null)
            {
                _integrityBarFill.style.width = Length.Percent(_currentVeilIntegrity);

                // Update color based on integrity
                _integrityBarFill.RemoveFromClassList("integrity-bar-fill-warning");
                _integrityBarFill.RemoveFromClassList("integrity-bar-fill-critical");
                _integrityBarFill.RemoveFromClassList("integrity-bar-fill-corrupted");

                if (_currentVeilIntegrity <= 25)
                {
                    _integrityBarFill.AddToClassList("integrity-bar-fill-corrupted");
                }
                else if (_currentVeilIntegrity <= 50)
                {
                    _integrityBarFill.AddToClassList("integrity-bar-fill-critical");
                }
                else if (_currentVeilIntegrity <= 75)
                {
                    _integrityBarFill.AddToClassList("integrity-bar-fill-warning");
                }
            }

            if (_integrityPercent != null)
            {
                _integrityPercent.text = $"{Mathf.RoundToInt(_currentVeilIntegrity)}%";

                // Update text color
                if (_currentVeilIntegrity <= 25)
                {
                    _integrityPercent.style.color = IntegrityColorCorrupted;
                }
                else if (_currentVeilIntegrity <= 50)
                {
                    _integrityPercent.style.color = IntegrityColorCritical;
                }
                else if (_currentVeilIntegrity <= 75)
                {
                    _integrityPercent.style.color = IntegrityColorWarning;
                }
                else
                {
                    _integrityPercent.style.color = IntegrityColorNormal;
                }
            }

            // Update integrity indicator pip
            UpdateIntegrityPip();
        }

        private void UpdateIntegrityPip()
        {
            if (_integrityIndicator == null) return;

            _integrityIndicator.RemoveFromClassList("integrity-pip-warning");
            _integrityIndicator.RemoveFromClassList("integrity-pip-critical");
            _integrityIndicator.RemoveFromClassList("integrity-pip-corrupted");

            if (_currentVeilIntegrity <= 25)
            {
                _integrityIndicator.AddToClassList("integrity-pip-corrupted");
            }
            else if (_currentVeilIntegrity <= 50)
            {
                _integrityIndicator.AddToClassList("integrity-pip-critical");
            }
            else if (_currentVeilIntegrity <= 75)
            {
                _integrityIndicator.AddToClassList("integrity-pip-warning");
            }
        }

        private void UpdateGlitchState()
        {
            bool shouldGlitch = _currentVeilIntegrity <= 50;

            if (shouldGlitch != _isGlitched)
            {
                _isGlitched = shouldGlitch;
                ApplyGlitchVisuals();
            }
        }

        private void ApplyGlitchVisuals()
        {
            if (_isGlitched)
            {
                _dialogueContainer?.AddToClassList("dialogue-box-glitched");
                _portraitFrame?.AddToClassList("vera-portrait-frame-glitched");
                _portraitGlow?.AddToClassList("vera-portrait-glow-corrupted");
                _speakerName?.AddToClassList("vera-name-glitched");
                _dialogueText?.AddToClassList("dialogue-text-glitched");

                if (_portraitScanlines != null)
                {
                    _portraitScanlines.style.display = DisplayStyle.Flex;
                }

                if (_veraPortraitGlitched != null && _veraPortrait != null)
                {
                    _veraPortrait.style.backgroundImage = new StyleBackground(_veraPortraitGlitched);
                }

                // Start glitch coroutine
                if (_glitchCoroutine == null)
                {
                    _glitchCoroutine = StartCoroutine(GlitchFlickerCoroutine());
                }
            }
            else
            {
                _dialogueContainer?.RemoveFromClassList("dialogue-box-glitched");
                _portraitFrame?.RemoveFromClassList("vera-portrait-frame-glitched");
                _portraitGlow?.RemoveFromClassList("vera-portrait-glow-corrupted");
                _speakerName?.RemoveFromClassList("vera-name-glitched");
                _dialogueText?.RemoveFromClassList("dialogue-text-glitched");

                if (_portraitScanlines != null)
                {
                    _portraitScanlines.style.display = DisplayStyle.None;
                }

                if (_veraPortraitNormal != null && _veraPortrait != null)
                {
                    _veraPortrait.style.backgroundImage = new StyleBackground(_veraPortraitNormal);
                }

                // Stop glitch coroutine
                if (_glitchCoroutine != null)
                {
                    StopCoroutine(_glitchCoroutine);
                    _glitchCoroutine = null;
                }

                if (_glitchOverlay != null)
                {
                    _glitchOverlay.style.display = DisplayStyle.None;
                }
            }
        }

        private void OnGlitchTriggered()
        {
            // Brief visual glitch
            StartCoroutine(TriggerGlitchPulse());
        }

        private IEnumerator TriggerGlitchPulse()
        {
            if (_glitchOverlay != null)
            {
                _glitchOverlay.style.display = DisplayStyle.Flex;
            }

            yield return _waitGlitchPulse;

            if (_glitchOverlay != null && !_isGlitched)
            {
                _glitchOverlay.style.display = DisplayStyle.None;
            }
        }

        private IEnumerator GlitchFlickerCoroutine()
        {
            var waitFlickerRate = new WaitForSeconds(_glitchFlickerRate);
            while (_isGlitched)
            {
                // Random flicker
                if (UnityEngine.Random.value < 0.3f)
                {
                    if (_glitchOverlay != null)
                    {
                        _glitchOverlay.style.display = DisplayStyle.Flex;
                    }

                    // Manual timer for random-duration wait to avoid per-iteration allocation
                    float wait = UnityEngine.Random.Range(0.05f, 0.15f);
                    float elapsed = 0f;
                    while (elapsed < wait) { elapsed += Time.deltaTime; yield return null; }

                    if (_glitchOverlay != null)
                    {
                        _glitchOverlay.style.display = DisplayStyle.None;
                    }
                }

                yield return waitFlickerRate;
            }
        }

        // =============================================================================
        // DIALOGUE DISPLAY
        // =============================================================================

        public void ShowDialogue(DialogueData dialogue)
        {
            _currentDialogue = dialogue;
            _currentLineIndex = 0;

            Show();
            OnDialogueStarted?.Invoke(dialogue.DialogueId);
            DisplayCurrentLine();
        }

        public void ShowLine(string text, bool hasChoices = false)
        {
            Show();
            DisplayLine(text, hasChoices);
        }

        private void DisplayCurrentLine()
        {
            if (_currentDialogue == null || _currentLineIndex >= _currentDialogue.Lines.Count)
            {
                EndDialogue();
                return;
            }

            var line = _currentDialogue.Lines[_currentLineIndex];
            bool hasChoices = _currentLineIndex == _currentDialogue.Lines.Count - 1 &&
                             _currentDialogue.Choices != null &&
                             _currentDialogue.Choices.Count > 0;

            DisplayLine(line, hasChoices);
        }

        private void DisplayLine(string text, bool hasChoices)
        {
            _fullText = text;
            _displayedTextBuilder.Clear();
            _currentCharIndex = 0;
            _isWaitingForInput = false;

            if (_dialogueText != null)
            {
                _dialogueText.text = "";
            }

            // Hide continue indicator and choices
            if (_continueIndicator != null)
            {
                _continueIndicator.style.display = DisplayStyle.None;
            }
            if (_choicesContainer != null)
            {
                _choicesContainer.style.display = DisplayStyle.None;
            }

            // Start typewriter
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }
            _typewriterCoroutine = StartCoroutine(TypewriterCoroutine(hasChoices));

            OnLineDisplayed?.Invoke(text);
        }

        private IEnumerator TypewriterCoroutine(bool showChoicesAfter)
        {
            _isDisplayingText = true;

            float typeSpeed = _isGlitched ? _glitchedTypeSpeed : _baseTypeSpeed;

            while (_currentCharIndex < _fullText.Length)
            {
                char c = _fullText[_currentCharIndex];

                // Maybe glitch the character
                if (_isGlitched && UnityEngine.Random.value < 0.1f)
                {
                    // Show glitch char briefly
                    char glitchChar = GlitchChars[UnityEngine.Random.Range(0, GlitchChars.Length)];
                    _displayedTextBuilder.Append(glitchChar);
                    UpdateDisplayedText();

                    yield return _waitGlitchChar;

                    // Replace with correct char (defensive check for empty builder)
                    if (_displayedTextBuilder.Length > 0)
                    {
                        _displayedTextBuilder.Length--;  // Remove last char
                        _displayedTextBuilder.Append(c);
                    }
                    else
                    {
                        _displayedTextBuilder.Append(c);
                    }
                }
                else
                {
                    _displayedTextBuilder.Append(c);
                }

                _currentCharIndex++;
                UpdateDisplayedText();

                // Pause for punctuation
                if (c == '.' || c == '!' || c == '?')
                {
                    yield return _waitPunctuation;
                }
                else if (c == ',')
                {
                    yield return _waitComma;
                }
                else
                {
                    yield return _isGlitched ? _waitGlitchedTypeSpeed : _waitTypeSpeed;
                }
            }

            _isDisplayingText = false;

            // Show continue indicator or choices
            if (showChoicesAfter && _currentDialogue?.Choices != null)
            {
                ShowChoices();
            }
            else
            {
                if (_continueIndicator != null)
                {
                    _continueIndicator.style.display = DisplayStyle.Flex;
                }
                _isWaitingForInput = true;
            }
        }

        private void UpdateDisplayedText()
        {
            if (_dialogueText != null)
            {
                _dialogueText.text = _displayedTextBuilder.ToString();
            }
        }

        private void CompleteCurrentLine()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            _displayedTextBuilder.Clear();
            _displayedTextBuilder.Append(_fullText);
            _currentCharIndex = _fullText.Length;
            _isDisplayingText = false;

            UpdateDisplayedText();

            // Show continue indicator or choices
            bool hasChoices = _currentDialogue != null &&
                             _currentLineIndex == _currentDialogue.Lines.Count - 1 &&
                             _currentDialogue.Choices != null &&
                             _currentDialogue.Choices.Count > 0;

            if (hasChoices)
            {
                ShowChoices();
            }
            else
            {
                if (_continueIndicator != null)
                {
                    _continueIndicator.style.display = DisplayStyle.Flex;
                }
                _isWaitingForInput = true;
            }
        }

        private void AdvanceDialogue()
        {
            _isWaitingForInput = false;

            if (_continueIndicator != null)
            {
                _continueIndicator.style.display = DisplayStyle.None;
            }

            _currentLineIndex++;
            DisplayCurrentLine();
        }

        private void EndDialogue()
        {
            Hide();
            OnDialogueEnded?.Invoke();
        }

        // =============================================================================
        // CHOICES
        // =============================================================================

        private void ShowChoices()
        {
            if (_choicesContainer == null || _choicesList == null) return;
            if (_currentDialogue?.Choices == null || _currentDialogue.Choices.Count == 0) return;

            _choicesList.Clear();

            for (int i = 0; i < _currentDialogue.Choices.Count; i++)
            {
                var choice = _currentDialogue.Choices[i];
                var choiceButton = CreateChoiceButton(choice, i);
                _choicesList.Add(choiceButton);
            }

            _choicesContainer.style.display = DisplayStyle.Flex;
        }

        private VisualElement CreateChoiceButton(DialogueChoice choice, int index)
        {
            var button = new VisualElement();
            button.AddToClassList("dialogue-choice");

            if (choice.HasConsequence)
            {
                button.AddToClassList("dialogue-choice-consequence");
            }
            if (choice.IsDangerous)
            {
                button.AddToClassList("dialogue-choice-dangerous");
            }

            var text = new Label(choice.Text);
            text.AddToClassList("dialogue-choice-text");
            button.Add(text);

            int capturedIndex = index;
            button.RegisterCallback<ClickEvent>(e => OnChoiceClicked(capturedIndex));

            return button;
        }

        private void OnChoiceClicked(int index)
        {
            PlaySelectSound();
            OnChoiceSelected?.Invoke(index);

            // Handle choice result
            if (_currentDialogue?.Choices != null && index < _currentDialogue.Choices.Count)
            {
                var choice = _currentDialogue.Choices[index];

                // Apply integrity change if any
                if (choice.IntegrityChange != 0 && VERAVoiceController.Instance != null)
                {
                    // VERAVoiceController.Instance.ModifyIntegrity(choice.IntegrityChange);
                }

                // If choice leads to another dialogue, show it
                if (!string.IsNullOrEmpty(choice.NextDialogueId))
                {
                    // TODO: Load next dialogue
                    EndDialogue();
                }
                else
                {
                    EndDialogue();
                }
            }
            else
            {
                EndDialogue();
            }
        }

        // =============================================================================
        // INPUT
        // =============================================================================

        private void OnDialogueBoxClicked(ClickEvent evt)
        {
            if (_isDisplayingText)
            {
                CompleteCurrentLine();
            }
            else if (_isWaitingForInput)
            {
                AdvanceDialogue();
            }
        }

        // =============================================================================
        // AUDIO
        // =============================================================================

        private void PlaySelectSound()
        {
            // TODO: Integrate with AudioManager
        }

        // =============================================================================
        // VISIBILITY
        // =============================================================================

        public void Show()
        {
            gameObject.SetActive(true);
            if (_root != null)
            {
                _root.style.display = DisplayStyle.Flex;
            }

            // Update integrity display
            UpdateIntegrityDisplay();
            UpdateGlitchState();
        }

        public void Hide()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            if (_root != null)
            {
                _root.style.display = DisplayStyle.None;
            }
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Initialize with external root element.
        /// </summary>
        public void Initialize(VisualElement root)
        {
            _root = root;

            // Re-query all elements
            _dialogueContainer = _root.Q<VisualElement>("dialogue-container");
            _glitchOverlay = _root.Q<VisualElement>("glitch-overlay");
            _portraitFrame = _root.Q<VisualElement>("portrait-frame");
            _portraitGlow = _root.Q<VisualElement>("portrait-glow");
            _veraPortrait = _root.Q<VisualElement>("vera-portrait");
            _portraitScanlines = _root.Q<VisualElement>("portrait-scanlines");
            _speakerName = _root.Q<Label>("speaker-name");
            _integrityIndicator = _root.Q<VisualElement>("integrity-indicator");
            _dialogueText = _root.Q<Label>("dialogue-text");
            _continueIndicator = _root.Q<VisualElement>("continue-indicator");
            _choicesContainer = _root.Q<VisualElement>("choices-container");
            _choicesList = _root.Q<VisualElement>("choices-list");
            _integrityBarFill = _root.Q<VisualElement>("integrity-bar-fill");
            _integrityPercent = _root.Q<Label>("integrity-percent");

            _dialogueContainer?.RegisterCallback<ClickEvent>(OnDialogueBoxClicked);
            SubscribeToVERA();

            ErrorLogger.UI("VERADialogue initialized via external root");
        }

        /// <summary>
        /// Set VERA's current Veil Integrity (0-100).
        /// </summary>
        public void SetVeilIntegrity(float integrity)
        {
            _currentVeilIntegrity = Mathf.Clamp(integrity, 0f, 100f);
            UpdateIntegrityDisplay();
            UpdateGlitchState();
        }

        /// <summary>
        /// Force glitch state on/off (for testing).
        /// </summary>
        public void ForceGlitchState(bool glitched)
        {
            _isGlitched = glitched;
            ApplyGlitchVisuals();
        }

        /// <summary>
        /// Check if dialogue is currently active.
        /// </summary>
        public bool IsActive => gameObject.activeInHierarchy && _root?.style.display == DisplayStyle.Flex;
    }

    // =============================================================================
    // DIALOGUE DATA STRUCTURES
    // =============================================================================

    /// <summary>
    /// Data for a complete dialogue sequence.
    /// </summary>
    [Serializable]
    public class DialogueData
    {
        public string DialogueId;
        public string Speaker;
        public List<string> Lines = new List<string>();
        public List<DialogueChoice> Choices;
    }

    /// <summary>
    /// Data for a dialogue choice.
    /// </summary>
    [Serializable]
    public class DialogueChoice
    {
        public string Text;
        public string NextDialogueId;
        public int IntegrityChange;
        public bool HasConsequence;
        public bool IsDangerous;
    }
}
