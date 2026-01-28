using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.UI.Effects;

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

        // VFX is handled by MainMenuVFXController (pure UI Toolkit, auto-added in InitializeVFXSystem)

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
            CheckForSaveFile();
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

            // Set version
            if (_versionLabel != null)
            {
                _versionLabel.text = $"v{Application.version}";
            }

            // Bind button events
            BindEvents();

            // Initialize new VFX system (replaces old particle effects)
            InitializeVFXSystem();

            // Start animations
            PlayEntranceAnimation();

            ErrorLogger.UI("MainMenu initialized with new VFX system");
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

        private void InitializeVFXSystem()
        {
            // VFX disabled - awaiting new VFX instructions from user
            ErrorLogger.UI("VFX disabled - awaiting new design");
        }

        // =============================================================================
        // SAVE FILE CHECK
        // =============================================================================

        private void CheckForSaveFile()
        {
            // Check if a save file exists to show/hide Continue button
            bool saveExists = SaveFileExists();

            if (_btnContinue != null)
            {
                _btnContinue.style.display = saveExists
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        private bool SaveFileExists()
        {
            // TODO: Integrate with SaveManager when available
            string savePath = System.IO.Path.Combine(
                Application.persistentDataPath,
                "Saves",
                "save_0.vbs"
            );
            return System.IO.File.Exists(savePath);
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
            // ESC to exit
            if (evt.keyCode == KeyCode.Escape)
            {
                QuitGame();
            }
            // Enter to start new game (if no save) or continue (if save exists)
            else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                if (SaveFileExists())
                {
                    LoadGame();
                }
                else
                {
                    StartNewGame();
                }
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
            // TODO: Integrate with SaveManager
            // SaveManager.Instance?.LoadGame(0);

            // For now, just load the game scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(_gameScene);
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
            CheckForSaveFile();
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
            CheckForSaveFile();

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

                    // Add hover glow effect
                    AddButtonHoverEffects(button);
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

        // NOTE: Old VeilPulseLoop removed - VFX now handled by MenuPulseController
        // and MainMenuVFXSetup which create SpriteRenderer-based effects

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
            // Subtle glow pulse on hover - lightweight and performant
            button.RegisterCallback<MouseEnterEvent>(evt =>
            {
                button.AddToClassList("vb-button-hover-glow");
            });

            button.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                button.RemoveFromClassList("vb-button-hover-glow");
            });
        }

        private void OnDisable()
        {
            UnbindEvents();
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }

            // VFX cleanup handled automatically by MainMenuVFXController
        }
    }
}
