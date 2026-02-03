using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using VeilBreakers.Core;
using VeilBreakers.Managers;
using VeilBreakers.UI.Controls;

namespace VeilBreakers.UI.Menus
{
    /// <summary>
    /// Controller for the main menu UI.
    /// Handles New Game, Continue, Settings, and Exit functionality.
    /// Features animated title screen with pulsing veil effects.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Scenes")]
        [SerializeField] private string _characterSelectScene = "CharacterSelect";
        [SerializeField] private string _gameScene = "Overworld";

        [Header("Animation Settings")]
        [SerializeField] private float _titleFadeInDuration = 1.5f;
        [SerializeField] private float _buttonStaggerDelay = 0.1f;

        // VFX will be implemented in a future update

        // =============================================================================
        // UI ELEMENTS
        // =============================================================================

        private VisualElement _root;
        private Button _btnContinue;
        private Button _btnNewGame;
        private Button _btnSettings;
        private Button _btnCredits;
        private Button _btnExit;
        private Label _versionLabel;
        private Label _gameTitle;
        private Label _tagline;
        private VisualElement _titleSection;
        private VisualElement _buttonContainer;
        private List<Button> _cachedButtons; // Cache for entrance animation (avoid ToList allocation)
        private Coroutine _animationCoroutine;
        private bool _hasValidSave;

        // Hover callback storage for proper unregistration (prevents memory leaks)
        private Dictionary<Button, EventCallback<MouseEnterEvent>> _hoverEnterCallbacks;
        private Dictionary<Button, EventCallback<MouseLeaveEvent>> _hoverLeaveCallbacks;

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action OnNewGameClicked;
        public event Action OnContinueClicked;
        public event Action OnSettingsClicked;
        public event Action OnCreditsClicked;
        public event Action OnExitClicked;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }
        }

        private void OnEnable()
        {
            InitializeUI();
            StartCoroutine(RefreshContinueButton());

            // Subscribe to InputManager for universal navigation
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnActionTriggered += OnActionTriggered;
            }
        }

        private void OnDisable()
        {
            UnbindEvents();
            UnbindHoverCallbacks();

            // Unsubscribe from InputManager
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnActionTriggered -= OnActionTriggered;
            }

            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
        }

        private void OnActionTriggered(InputManager.GameAction action)
        {
            if (!gameObject.activeInHierarchy || _root?.style.display == DisplayStyle.None) return;

            switch (action)
            {
                case InputManager.GameAction.Cancel:
                    OnExitButtonClicked(null);
                    break;
                case InputManager.GameAction.Confirm:
                    // Primary action trigger logic could be added here
                    break;
            }
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void InitializeUI()
        {
            if (_uiDocument == null)
            {
                ErrorLogger.Error("MainMenuController: UIDocument is null!");
                return;
            }

            _root = _uiDocument.rootVisualElement;

            // Query UI elements
            _btnContinue = _root.Q<Button>("btn-continue");
            _btnNewGame = _root.Q<Button>("btn-new-game");
            _btnSettings = _root.Q<Button>("btn-settings");
            _btnCredits = _root.Q<Button>("btn-credits");
            _btnExit = _root.Q<Button>("btn-exit");
            _versionLabel = _root.Q<Label>("version-label");
            _gameTitle = _root.Q<Label>("game-title");
            _tagline = _root.Q<Label>("tagline");
            _titleSection = _root.Q<VisualElement>("title-section");
            _buttonContainer = _root.Q<VisualElement>("button-container");

            // Cache buttons for entrance animation (avoid ToList allocation)
            _cachedButtons = _buttonContainer.Query<Button>().ToList();

            // Initialize hover callback dictionaries for proper cleanup
            _hoverEnterCallbacks = new Dictionary<Button, EventCallback<MouseEnterEvent>>();
            _hoverLeaveCallbacks = new Dictionary<Button, EventCallback<MouseLeaveEvent>>();

            // Set version
            if (_versionLabel != null)
            {
                _versionLabel.text = $"v{Application.version}";
            }

            // Bind button events
            BindEvents();

            // Start animations
            PlayEntranceAnimation();

            ErrorLogger.UI("MainMenu initialized");
        }

        private void BindEvents()
        {
            _btnContinue?.RegisterCallback<ClickEvent>(OnContinueButtonClicked);
            _btnNewGame?.RegisterCallback<ClickEvent>(OnNewGameButtonClicked);
            _btnSettings?.RegisterCallback<ClickEvent>(OnSettingsButtonClicked);
            _btnCredits?.RegisterCallback<ClickEvent>(OnCreditsButtonClicked);
            _btnExit?.RegisterCallback<ClickEvent>(OnExitButtonClicked);

            // Keyboard navigation
            _root?.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void UnbindEvents()
        {
            _btnContinue?.UnregisterCallback<ClickEvent>(OnContinueButtonClicked);
            _btnNewGame?.UnregisterCallback<ClickEvent>(OnNewGameButtonClicked);
            _btnSettings?.UnregisterCallback<ClickEvent>(OnSettingsButtonClicked);
            _btnCredits?.UnregisterCallback<ClickEvent>(OnCreditsButtonClicked);
            _btnExit?.UnregisterCallback<ClickEvent>(OnExitButtonClicked);
            _root?.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        // =============================================================================
        // SAVE FILE CHECK
        // =============================================================================

        private IEnumerator RefreshContinueButton()
        {
            bool saveExists = false;
            bool corrupted = false;

            if (SaveManager.HasInstance)
            {
                var metaTask = SaveManager.Instance.GetSlotMetadataAsync(0);
                while (!metaTask.IsCompleted)
                {
                    yield return null;
                }

                var meta = metaTask.Result;
                saveExists = meta?.hasData == true && meta.isCorrupted == false;
                corrupted = meta?.isCorrupted == true;
            }
            else
            {
                // Fallback for early initialization before SaveManager is created
                string savePath = System.IO.Path.Combine(
                    Application.persistentDataPath,
                    "saves",
                    "slot_0.sav"
                );
                saveExists = System.IO.File.Exists(savePath);
            }

            SetContinueVisible(saveExists);
            _hasValidSave = saveExists;

            if (corrupted)
            {
                Debug.LogWarning("[MainMenuController] Save slot 0 appears corrupted; hiding Continue.");
            }
        }

        /// <summary>
        /// Triggers a check for existing save files and updates the Continue button visibility.
        /// </summary>
        private void CheckForSaveFile()
        {
            StartCoroutine(RefreshContinueButton());
        }

        private void SetContinueVisible(bool visible)
        {
            if (_btnContinue != null)
            {
                _btnContinue.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private bool SaveFileExists()
        {
            return _hasValidSave;
        }

        // =============================================================================
        // BUTTON HANDLERS
        // =============================================================================

        private void OnContinueButtonClicked(ClickEvent evt)
        {
            ErrorLogger.UI("Continue clicked");
            PlayButtonSound();

            OnContinueClicked?.Invoke();

            // Load the most recent save
            LoadGame();
        }

        private void OnNewGameButtonClicked(ClickEvent evt)
        {
            ErrorLogger.UI("New Game clicked");
            PlayButtonSound();

            OnNewGameClicked?.Invoke();

            // Go to character select or start new game
            StartNewGame();
        }

        private void OnSettingsButtonClicked(ClickEvent evt)
        {
            ErrorLogger.UI("Settings clicked");
            PlayButtonSound();

            OnSettingsClicked?.Invoke();

            // Show settings panel
            ShowSettings();
        }

        private void OnCreditsButtonClicked(ClickEvent evt)
        {
            ErrorLogger.UI("Credits clicked");
            PlayButtonSound();

            OnCreditsClicked?.Invoke();

            // Show credits
            ShowCredits();
        }

        private void OnExitButtonClicked(ClickEvent evt)
        {
            ErrorLogger.UI("Exit clicked");
            PlayButtonSound();

            OnExitClicked?.Invoke();

            // Quit application
            QuitGame();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            // Primary navigation handled by OnActionTriggered.
            // Keeping KeyDown only for specific accessibility fallbacks if needed.
            if (evt.keyCode == KeyCode.Escape)
            {
                OnExitButtonClicked(null);
            }
        }

        // =============================================================================
        // GAME FLOW
        // =============================================================================

        private void StartNewGame()
        {
            // Reset game state
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetGame();
            }

            // Load character select scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(_characterSelectScene);
        }

        private void LoadGame()
        {
            StartCoroutine(LoadAndEnterGame());
        }

        private void ShowSettings()
        {
            // TODO: Show settings panel overlay
            ErrorLogger.UI("Settings panel not yet implemented");
        }

        private void ShowCredits()
        {
            // TODO: Show credits panel overlay
            ErrorLogger.UI("Credits panel not yet implemented");
        }

        private void QuitGame()
        {
            ErrorLogger.UI("Quitting game...");

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        /// <summary>
        /// Attempts to load save slot 0 via SaveManager, then transitions to the game scene.
        /// Falls back to direct scene load if SaveManager is unavailable or load fails.
        /// </summary>
        private IEnumerator LoadAndEnterGame()
        {
            SetInteractable(false);
            bool loaded = false;

            if (SaveManager.HasInstance)
            {
                var loadTask = SaveManager.Instance.LoadAsync(0);
                while (!loadTask.IsCompleted)
                {
                    yield return null;
                }

                loaded = loadTask.Result;
                if (!loaded)
                {
                    Debug.LogWarning("[MainMenuController] Failed to load slot 0. Proceeding to scene load.");
                }
            }

            SceneManager.LoadScene(_gameScene);
        }

        // =============================================================================
        // AUDIO
        // =============================================================================

        private void PlayButtonSound()
        {
            // TODO: Integrate with AudioManager
            // AudioManager.Instance?.PlaySFX("UI_Click");
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Show the main menu.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            StartCoroutine(RefreshContinueButton());
        }

        /// <summary>
        /// Hide the main menu.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Enable or disable menu interaction.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            _btnContinue?.SetEnabled(interactable);
            _btnNewGame?.SetEnabled(interactable);
            _btnSettings?.SetEnabled(interactable);
            _btnCredits?.SetEnabled(interactable);
            _btnExit?.SetEnabled(interactable);
        }

        /// <summary>
        /// Initialize with an external root element (called by MainMenuBootstrap).
        /// </summary>
        public void Initialize(VisualElement root)
        {
            _root = root;

            // Query UI elements
            _btnContinue = _root.Q<Button>("btn-continue");
            _btnNewGame = _root.Q<Button>("btn-new-game");
            _btnSettings = _root.Q<Button>("btn-settings");
            _btnCredits = _root.Q<Button>("btn-credits");
            _btnExit = _root.Q<Button>("btn-exit");
            _versionLabel = _root.Q<Label>("version-label");

            // Set version
            if (_versionLabel != null)
            {
                _versionLabel.text = $"v{Application.version}";
            }

            // Bind button events
            BindEvents();

            // Check for save
            StartCoroutine(RefreshContinueButton());

            ErrorLogger.UI("MainMenu initialized via external root");
        }

        /// <summary>
        /// Get the button container for animation purposes.
        /// </summary>
        public VisualElement GetButtonContainer()
        {
            return _root?.Q<VisualElement>("button-container");
        }

        /// <summary>
        /// Get the title element for animation purposes.
        /// </summary>
        public Label GetTitleElement()
        {
            return _root?.Q<Label>("game-title");
        }

        // =============================================================================
        // ANIMATIONS
        // =============================================================================

        private void PlayEntranceAnimation()
        {
            // DRAMATIC title entrance with scale + fade
            if (_gameTitle != null)
            {
                _gameTitle.style.opacity = 0;
                _gameTitle.style.scale = new Scale(Vector2.one * 0.5f);
                StartCoroutine(FadeInElement(_gameTitle, _titleFadeInDuration, 0f));
                StartCoroutine(ScaleInElement(_gameTitle, 0.5f, 1f, _titleFadeInDuration, 0f, EaseType.ElasticOut));
            }

            // Tagline with bounce
            if (_tagline != null)
            {
                _tagline.style.opacity = 0;
                _tagline.style.translate = new Translate(0, -30);
                StartCoroutine(FadeInElement(_tagline, 0.8f, 0.6f));
                StartCoroutine(SlideInElement(_tagline, 0, -30, 0.8f, 0.6f, EaseType.BounceOut));
            }

            // Individual button stagger with BOUNCE
            if (_buttonContainer != null)
            {
                var buttons = _cachedButtons; // Use cached list (no allocation)
                for (int i = 0; i < buttons.Count; i++)
                {
                    var button = buttons[i];
                    button.style.opacity = 0;
                    button.style.translate = new Translate(-50, 0);
                    button.style.scale = new Scale(Vector2.one * 0.8f);

                    float delay = 1.2f + (i * 0.15f); // Stagger each button
                    StartCoroutine(FadeInElement(button, 0.5f, delay));
                    StartCoroutine(SlideInElement(button, -50, 0, 0.6f, delay, EaseType.BackOut));
                    StartCoroutine(ScaleInElement(button, 0.8f, 1f, 0.6f, delay, EaseType.BackOut));

                    // Add hover color/scale effects
                    AddButtonHoverEffects(button);

                    // Apply AAA VFX (ripple, glow overlay, press effect)
                    ButtonVFXHelper.ApplyEffects(button, ButtonVFXOptions.Silent);

                    // Add shimmer to primary buttons for extra polish
                    bool isPrimary = button.name == "btn-new-game" || button.name == "btn-continue";
                    if (isPrimary)
                    {
                        ButtonVFXHelper.AddShimmer(button, 4f);
                        ButtonVFXHelper.AddPulseBorder(button, new Color(1f, 0.5f, 0.2f, 0.8f));
                    }
                }
            }
        }

        private IEnumerator FadeInElement(VisualElement element, float duration, float delay)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);

            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = EaseOutCubic(t);
                element.style.opacity = t;
                yield return null;
            }
            element.style.opacity = 1;
        }

        private IEnumerator SlideInElement(VisualElement element, float fromX, float fromY, float duration, float delay, EaseType easeType = EaseType.EaseOut)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);

            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = ApplyEasing(t, easeType);
                float x = Mathf.Lerp(fromX, 0, t);
                float y = Mathf.Lerp(fromY, 0, t);
                element.style.translate = new Translate(x, y);
                yield return null;
            }
            element.style.translate = new Translate(0, 0);
        }

        private IEnumerator ScaleInElement(VisualElement element, float fromScale, float toScale, float duration, float delay, EaseType easeType = EaseType.EaseOut)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);

            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = ApplyEasing(t, easeType);
                float scale = Mathf.Lerp(fromScale, toScale, t);
                element.style.scale = new Scale(new Vector2(scale, scale));
                yield return null;
            }
            element.style.scale = new Scale(new Vector2(toScale, toScale));
        }

        // =============================================================================
        // EASING FUNCTIONS
        // =============================================================================

        private enum EaseType
        {
            EaseOut,
            ElasticOut,
            BounceOut,
            BackOut
        }

        private float ApplyEasing(float t, EaseType easeType)
        {
            switch (easeType)
            {
                case EaseType.ElasticOut:
                    return EaseElasticOut(t);
                case EaseType.BounceOut:
                    return EaseBounceOut(t);
                case EaseType.BackOut:
                    return EaseBackOut(t);
                case EaseType.EaseOut:
                default:
                    return EaseOutCubic(t);
            }
        }

        private float EaseOutCubic(float t)
        {
            return 1 - Mathf.Pow(1 - t, 3);
        }

        private float EaseElasticOut(float t)
        {
            if (t == 0 || t == 1) return t;
            float p = 0.3f;
            return Mathf.Pow(2, -10 * t) * Mathf.Sin((t - p / 4) * (2 * Mathf.PI) / p) + 1;
        }

        private float EaseBounceOut(float t)
        {
            if (t < (1 / 2.75f))
            {
                return 7.5625f * t * t;
            }
            else if (t < (2 / 2.75f))
            {
                t -= (1.5f / 2.75f);
                return 7.5625f * t * t + 0.75f;
            }
            else if (t < (2.5f / 2.75f))
            {
                t -= (2.25f / 2.75f);
                return 7.5625f * t * t + 0.9375f;
            }
            else
            {
                t -= (2.625f / 2.75f);
                return 7.5625f * t * t + 0.984375f;
            }
        }

        private float EaseBackOut(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
        }

        private void AddButtonHoverEffects(Button button)
        {
            if (button == null) return;

            // PROGRAMMATIC HOVER COLORS - bypasses USS specificity issues with Unity's built-in Button theme
            // USS hover states get overridden by Unity's default Button:hover, so we set colors directly via C#

            bool isPrimary = button.name == "btn-new-game" || button.name == "btn-continue";

            // Define colors programmatically (matches USS molten orange theme)
            // Primary buttons: Molten orange
            Color primaryBaseColor = new Color(180f / 255f, 80f / 255f, 20f / 255f, 1f);
            Color primaryHoverColor = new Color(210f / 255f, 100f / 255f, 30f / 255f, 1f);
            Color primaryBorderBase = new Color(255f / 255f, 140f / 255f, 50f / 255f, 1f);
            Color primaryBorderHover = new Color(255f / 255f, 180f / 255f, 80f / 255f, 1f);

            // Secondary buttons: Dark gray with warm amber hover
            Color secondaryBaseColor = new Color(60f / 255f, 50f / 255f, 45f / 255f, 1f);
            Color secondaryHoverColor = new Color(90f / 255f, 70f / 255f, 50f / 255f, 1f);
            Color secondaryBorderBase = new Color(180f / 255f, 90f / 255f, 40f / 255f, 1f);
            Color secondaryBorderHover = new Color(200f / 255f, 120f / 255f, 60f / 255f, 1f);

            // Set initial base colors
            Color baseColor = isPrimary ? primaryBaseColor : secondaryBaseColor;
            Color baseBorder = isPrimary ? primaryBorderBase : secondaryBorderBase;
            SetButtonColors(button, baseColor, baseBorder);

            // Create and store hover enter callback (for proper unregistration)
            EventCallback<MouseEnterEvent> enterCallback = evt =>
            {
                Color hoverColor = isPrimary ? primaryHoverColor : secondaryHoverColor;
                Color hoverBorder = isPrimary ? primaryBorderHover : secondaryBorderHover;
                SetButtonColors(button, hoverColor, hoverBorder);
                button.style.scale = new Scale(new Vector2(1.05f, 1.05f));
                button.AddToClassList("vb-button-hover-glow");
            };

            // Create and store hover leave callback (for proper unregistration)
            EventCallback<MouseLeaveEvent> leaveCallback = evt =>
            {
                Color restoreColor = isPrimary ? primaryBaseColor : secondaryBaseColor;
                Color restoreBorder = isPrimary ? primaryBorderBase : secondaryBorderBase;
                SetButtonColors(button, restoreColor, restoreBorder);
                button.style.scale = new Scale(Vector2.one);
                button.RemoveFromClassList("vb-button-hover-glow");
            };

            // Register callbacks
            button.RegisterCallback(enterCallback);
            button.RegisterCallback(leaveCallback);

            // Store for unregistration in OnDisable (prevents memory leaks)
            _hoverEnterCallbacks[button] = enterCallback;
            _hoverLeaveCallbacks[button] = leaveCallback;
        }

        /// <summary>
        /// Helper to set button background and border colors.
        /// </summary>
        private void SetButtonColors(Button button, Color bgColor, Color borderColor)
        {
            button.style.backgroundColor = bgColor;
            button.style.borderTopColor = borderColor;
            button.style.borderBottomColor = borderColor;
            button.style.borderLeftColor = borderColor;
            button.style.borderRightColor = borderColor;
        }

        /// <summary>
        /// Unregister all hover callbacks to prevent memory leaks.
        /// </summary>
        private void UnbindHoverCallbacks()
        {
            if (_hoverEnterCallbacks != null)
            {
                foreach (var kvp in _hoverEnterCallbacks)
                {
                    kvp.Key?.UnregisterCallback(kvp.Value);
                }
                _hoverEnterCallbacks.Clear();
            }

            if (_hoverLeaveCallbacks != null)
            {
                foreach (var kvp in _hoverLeaveCallbacks)
                {
                    kvp.Key?.UnregisterCallback(kvp.Value);
                }
                _hoverLeaveCallbacks.Clear();
            }
        }
    }
}
