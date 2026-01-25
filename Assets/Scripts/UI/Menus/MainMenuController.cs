using System;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;

namespace VeilBreakers.UI.Menus
{
    /// <summary>
    /// Controller for the main menu UI.
    /// Handles New Game, Continue, Settings, and Exit functionality.
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

        private void OnDisable()
        {
            UnbindEvents();
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

            // Set version
            if (_versionLabel != null)
            {
                _versionLabel.text = $"v{Application.version}";
            }

            // Bind button events
            BindEvents();

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
    }
}
