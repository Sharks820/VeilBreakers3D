using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;

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
        [SerializeField] private float _veilPulseSpeed = 2f;
        [SerializeField] private float _veilPulseScale = 1.15f;

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
        private List<VisualElement> _veilPulses = new List<VisualElement>();
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

            // Query veil pulse elements
            _veilPulses.Clear();
            for (int i = 1; i <= 3; i++)
            {
                var pulse = _root.Q<VisualElement>($"veil-pulse-{i}");
                if (pulse != null)
                    _veilPulses.Add(pulse);
            }

            // Set version
            if (_versionLabel != null)
            {
                _versionLabel.text = $"v{Application.version}";
            }

            // Bind button events
            BindEvents();

            // Start animations
            PlayEntranceAnimation();
            StartVeilPulseAnimation();

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
            // Fade in title
            if (_gameTitle != null)
            {
                _gameTitle.style.opacity = 0;
                StartCoroutine(FadeInElement(_gameTitle, _titleFadeInDuration, 0f));
            }

            // Fade in tagline with delay
            if (_tagline != null)
            {
                _tagline.style.opacity = 0;
                StartCoroutine(FadeInElement(_tagline, 1f, 0.5f));
            }

            // Stagger button animations
            if (_buttonContainer != null)
            {
                _buttonContainer.style.opacity = 0;
                StartCoroutine(FadeInElement(_buttonContainer, 0.8f, 1f));
                StartCoroutine(SlideInElement(_buttonContainer, 30, 0.8f, 1f));
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

        private IEnumerator SlideInElement(VisualElement element, float fromY, float duration, float delay)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);

            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = EaseOutCubic(t);
                float y = Mathf.Lerp(fromY, 0, t);
                element.style.translate = new Translate(0, y);
                yield return null;
            }
            element.style.translate = new Translate(0, 0);
        }

        private void StartVeilPulseAnimation()
        {
            if (_animationCoroutine != null)
                StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(VeilPulseLoop());
        }

        private IEnumerator VeilPulseLoop()
        {
            float[] phases = { 0f, 0.33f, 0.66f };
            float[] baseOpacities = { 0.06f, 0.05f, 0.04f };

            while (true)
            {
                for (int i = 0; i < _veilPulses.Count; i++)
                {
                    if (_veilPulses[i] == null) continue;

                    float phase = phases[i];
                    float t = (Time.time * _veilPulseSpeed + phase * Mathf.PI * 2) % (Mathf.PI * 2);

                    // Pulse scale
                    float scale = 1f + (Mathf.Sin(t) * 0.5f + 0.5f) * (_veilPulseScale - 1f);
                    _veilPulses[i].style.scale = new Scale(new Vector2(scale, scale));

                    // Pulse opacity
                    float opacity = baseOpacities[i] * (0.5f + Mathf.Sin(t) * 0.5f + 0.5f);
                    _veilPulses[i].style.opacity = opacity;

                    // Subtle position drift
                    float driftX = Mathf.Sin(t * 0.5f) * 20f;
                    float driftY = Mathf.Cos(t * 0.3f) * 15f;
                    _veilPulses[i].style.translate = new Translate(driftX, driftY);
                }

                yield return null;
            }
        }

        private float EaseOutCubic(float t)
        {
            return 1 - Mathf.Pow(1 - t, 3);
        }

        private void OnDisable()
        {
            UnbindEvents();
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
        }
    }
}
