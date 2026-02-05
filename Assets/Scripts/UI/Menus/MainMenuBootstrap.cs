using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.UI.Core;
// Migrated to use UIAssets for centralized asset references

namespace VeilBreakers.UI.Menus
{
    /// <summary>
    /// Bootstrap script for the MainMenu scene.
    /// Initializes UI Document, controllers, and entrance animations.
    /// Attach this to a GameObject with a UIDocument component.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuBootstrap : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("UI Assets")]
        [SerializeField] private VisualTreeAsset _mainMenuTemplate;
        [SerializeField] private VisualTreeAsset _settingsPanelTemplate;
        [SerializeField] private StyleSheet _themeStylesheet;
        [SerializeField] private StyleSheet _componentStylesheet;

        [Header("Animation Settings")]
        [SerializeField] private float _titleAnimDelay = 0.3f;
        [SerializeField] private float _buttonStaggerDelay = 0.1f;
        [SerializeField] private float _fadeInDuration = 0.5f;

        [Header("Backdrop")]
        [SerializeField, Range(0f, 1f)] private float _rootBackdropAlpha = 0.2f;
        [SerializeField] private Color _rootBackdropColor = new Color(0.03f, 0.02f, 0.05f, 1f);

        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip _menuMusic;
        [SerializeField] private AudioClip _buttonHoverSound;
        [SerializeField] private AudioClip _buttonClickSound;

        // =============================================================================
        // PRIVATE FIELDS
        // =============================================================================

        private UIDocument _uiDocument;
        private VisualElement _root;
        private MainMenuController _mainMenuController;
        private SettingsPanelController _settingsController;

        private VisualElement _settingsOverlay;
        private bool _settingsOpen;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            Time.timeScale = 1f;

            if (_uiDocument != null && _uiDocument.panelSettings == null)
            {
                // Use UIAssets for centralized asset loading
                var uiAssets = UIAssets.Instance;
                if (uiAssets != null && uiAssets.DefaultPanelSettings != null)
                {
                    _uiDocument.panelSettings = uiAssets.DefaultPanelSettings;
                }
            }
        }

        private void Start()
        {
            InitializeUI();
            PlayEntranceAnimation();
            SetupEventHandlers();

            // Play menu music if assigned
            if (_menuMusic != null && AudioSource.FindFirstObjectByType<AudioSource>() != null)
            {
                // TODO: Hook into AudioManager when implemented
            }
        }

        private void OnDestroy()
        {
            CleanupEventHandlers();
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void InitializeUI()
        {
            _root = _uiDocument.rootVisualElement;

            // CRITICAL: Ensure root fills the entire viewport
            _root.style.flexGrow = 1;
            _root.style.width = Length.Percent(100);
            _root.style.height = Length.Percent(100);

            // Apply stylesheets
            if (_themeStylesheet != null)
                _root.styleSheets.Add(_themeStylesheet);
            if (_componentStylesheet != null)
                _root.styleSheets.Add(_componentStylesheet);

            // Clear and setup main menu
            _root.Clear();

            // DEFENSIVE: Add full-screen black background at root level to prevent any color bleed
            var rootBackground = new VisualElement();
            rootBackground.name = "root-background";
            rootBackground.style.position = Position.Absolute;
            rootBackground.style.left = 0;
            rootBackground.style.top = 0;
            rootBackground.style.right = 0;
            rootBackground.style.bottom = 0;
            Color backdrop = _rootBackdropColor;
            backdrop.a = Mathf.Clamp01(_rootBackdropAlpha);
            rootBackground.style.backgroundColor = new StyleColor(backdrop); // Allow VFX to show through
            rootBackground.pickingMode = PickingMode.Ignore;
            _root.Add(rootBackground);

            // Create main container
            var mainContainer = new VisualElement();
            mainContainer.name = "main-container";
            mainContainer.style.flexGrow = 1;
            _root.Add(mainContainer);

            // Instantiate main menu template
            if (_mainMenuTemplate != null)
            {
                var menuContent = _mainMenuTemplate.Instantiate();
                menuContent.style.flexGrow = 1;
                mainContainer.Add(menuContent);

                // Initialize controller
                _mainMenuController = gameObject.AddComponent<MainMenuController>();
                _mainMenuController.Initialize(_root);

                // Setup button hover effects (C# fallback since USS transitions can be flaky)
                SetupButtonHoverEffects(menuContent);
            }
            else
            {
                Debug.LogError("MainMenuBootstrap: MainMenu template not assigned!");
                CreateFallbackMenu(mainContainer);
            }

            // Create settings overlay (hidden by default)
            CreateSettingsOverlay();

            // REMOVED: mainContainer.style.opacity = 0 was hiding content permanently
            // Animation will handle fade-in if UIAnimationController is working
            // Failsafe: ensure content is visible
            mainContainer.style.opacity = 1;
        }

        private void CreateFallbackMenu(VisualElement container)
        {
            // Minimal fallback if no template assigned
            container.AddToClassList("vb-root");
            container.style.justifyContent = Justify.Center;
            container.style.alignItems = Align.Center;

            var title = new Label("VEILBREAKERS");
            title.AddToClassList("vb-header");
            container.Add(title);

            var errorMsg = new Label("Menu template not assigned. Check MainMenuBootstrap inspector.");
            errorMsg.AddToClassList("vb-text-secondary");
            errorMsg.style.marginTop = 20;
            container.Add(errorMsg);
        }

        private void CreateSettingsOverlay()
        {
            _settingsOverlay = new VisualElement();
            _settingsOverlay.name = "settings-overlay";
            _settingsOverlay.style.position = Position.Absolute;
            _settingsOverlay.style.left = 0;
            _settingsOverlay.style.top = 0;
            _settingsOverlay.style.right = 0;
            _settingsOverlay.style.bottom = 0;
            _settingsOverlay.style.display = DisplayStyle.None;
            _settingsOverlay.style.opacity = 0;

            if (_settingsPanelTemplate != null)
            {
                var settingsContent = _settingsPanelTemplate.Instantiate();
                settingsContent.style.flexGrow = 1;
                _settingsOverlay.Add(settingsContent);

                // Initialize settings controller
                _settingsController = gameObject.AddComponent<SettingsPanelController>();
                _settingsController.Initialize(_settingsOverlay);
            }

            _root.Add(_settingsOverlay);
        }

        // =============================================================================
        // BUTTON HOVER EFFECTS
        // =============================================================================

        private void SetupButtonHoverEffects(VisualElement container)
        {
            var buttons = container.Query<Button>().ToList();
            foreach (var btn in buttons)
            {
                var originalBg = btn.resolvedStyle.backgroundColor;
                var originalBorder = btn.resolvedStyle.borderTopColor;
                var originalScale = btn.resolvedStyle.scale;

                bool isPrimary = btn.ClassListContains("vb-menu-btn-primary") ||
                                 btn.name == "btn-new-game" || btn.name == "btn-continue";

                btn.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    // Skip entirely for art skin buttons - MoltenButtonVFX handles hover
                    if (btn.ClassListContains("vb-btn-sheet"))
                    {
                        return;
                    }

                    btn.style.scale = new Scale(new Vector2(1.05f, 1.05f));
                    if (isPrimary)
                    {
                        // MOLTEN ORANGE hover: rgb(210, 100, 30)
                        btn.style.backgroundColor = new StyleColor(new Color(0.82f, 0.39f, 0.12f, 1f));
                        btn.style.borderTopColor = new StyleColor(new Color(1.0f, 0.71f, 0.31f, 1f));
                        btn.style.borderBottomColor = new StyleColor(new Color(1.0f, 0.71f, 0.31f, 1f));
                        btn.style.borderLeftColor = new StyleColor(new Color(1.0f, 0.71f, 0.31f, 1f));
                        btn.style.borderRightColor = new StyleColor(new Color(1.0f, 0.71f, 0.31f, 1f));
                    }
                    else
                    {
                        // WARM AMBER hover: rgb(55, 35, 20)
                        btn.style.backgroundColor = new StyleColor(new Color(0.22f, 0.14f, 0.08f, 0.98f));
                        btn.style.borderTopColor = new StyleColor(new Color(0.71f, 0.51f, 0.27f, 1f));
                        btn.style.borderBottomColor = new StyleColor(new Color(0.71f, 0.51f, 0.27f, 1f));
                        btn.style.borderLeftColor = new StyleColor(new Color(0.71f, 0.51f, 0.27f, 1f));
                        btn.style.borderRightColor = new StyleColor(new Color(0.71f, 0.51f, 0.27f, 1f));
                        btn.style.color = new StyleColor(new Color(1.0f, 0.96f, 0.86f, 1f));
                    }
                });

                btn.RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    // Skip entirely for art skin buttons - MoltenButtonVFX handles hover
                    if (btn.ClassListContains("vb-btn-sheet"))
                    {
                        return;
                    }

                    btn.style.scale = new Scale(Vector2.one);
                    if (isPrimary)
                    {
                        // MOLTEN ORANGE base: rgb(180, 80, 20)
                        btn.style.backgroundColor = new StyleColor(new Color(0.71f, 0.31f, 0.08f, 1f));
                        btn.style.borderTopColor = new StyleColor(new Color(1.0f, 0.55f, 0.20f, 1f));
                        btn.style.borderBottomColor = new StyleColor(new Color(1.0f, 0.55f, 0.20f, 1f));
                        btn.style.borderLeftColor = new StyleColor(new Color(1.0f, 0.55f, 0.20f, 1f));
                        btn.style.borderRightColor = new StyleColor(new Color(1.0f, 0.55f, 0.20f, 1f));
                    }
                    else
                    {
                        // WARM DARK base: rgb(25, 18, 12)
                        btn.style.backgroundColor = new StyleColor(new Color(0.10f, 0.07f, 0.05f, 0.95f));
                        btn.style.borderTopColor = new StyleColor(new Color(0.39f, 0.27f, 0.16f, 1f));
                        btn.style.borderBottomColor = new StyleColor(new Color(0.39f, 0.27f, 0.16f, 1f));
                        btn.style.borderLeftColor = new StyleColor(new Color(0.39f, 0.27f, 0.16f, 1f));
                        btn.style.borderRightColor = new StyleColor(new Color(0.39f, 0.27f, 0.16f, 1f));
                        btn.style.color = new StyleColor(new Color(0.78f, 0.71f, 0.63f, 1f));
                    }
                });

                btn.RegisterCallback<MouseDownEvent>(evt =>
                {
                    btn.style.scale = new Scale(new Vector2(0.95f, 0.95f));
                });

                btn.RegisterCallback<MouseUpEvent>(evt =>
                {
                    btn.style.scale = new Scale(new Vector2(1.05f, 1.05f));
                });
            }
        }

        // =============================================================================
        // ANIMATION
        // =============================================================================

        private void PlayEntranceAnimation()
        {
            StartCoroutine(EntranceAnimationSequence());
            // FAILSAFE: Ensure all content is visible after animations should complete
            StartCoroutine(EnsureVisibilityFailsafe());
        }

        private IEnumerator EnsureVisibilityFailsafe()
        {
            // Wait for all animations to complete (generous timeout)
            yield return new WaitForSecondsRealtime(3f);

            // Force all critical elements to be visible
            var mainContainer = _root.Q<VisualElement>("main-container");
            if (mainContainer != null) mainContainer.style.opacity = 1;

            var title = _root.Q<Label>("game-title");
            if (title != null) title.style.opacity = 1;

            var buttonContainer = _root.Q<VisualElement>("button-container");
            if (buttonContainer != null)
            {
                var buttons = buttonContainer.Query<Button>().ToList();
                foreach (var btn in buttons)
                {
                    btn.style.opacity = 1;
                    btn.style.translate = new Translate(0, 0);
                }
            }
        }

        private IEnumerator EntranceAnimationSequence()
        {
            var mainContainer = _root.Q<VisualElement>("main-container");
            if (mainContainer == null) yield break;

            // Wait a brief moment before starting
            yield return new WaitForSecondsRealtime(0.1f);

            // Fade in the main container
            var animator = UIAnimationController.Instance;
            animator.FadeIn(mainContainer, _fadeInDuration);

            yield return new WaitForSecondsRealtime(_titleAnimDelay);

            // Find and animate title
            var title = _root.Q<Label>(className: "vb-header");
            if (title == null)
                title = _root.Q<Label>("game-title");

            if (title != null)
            {
                title.style.opacity = 0;
                title.style.scale = new Scale(new Vector2(0.9f, 0.9f));
                animator.FadeScaleIn(title, 0.9f, 0.6f);
            }

            yield return new WaitForSecondsRealtime(0.3f);

            // Find and stagger animate menu buttons
            var buttonContainer = _root.Q<VisualElement>("button-container");
            if (buttonContainer != null)
            {
                var buttons = buttonContainer.Query<Button>().ToList();
                for (int i = 0; i < buttons.Count; i++)
                {
                    var btn = buttons[i];
                    btn.style.opacity = 0;
                    btn.style.translate = new Translate(0, 20);

                    // Delay each button
                    StartCoroutine(AnimateButtonDelayed(btn, i * _buttonStaggerDelay));
                }
            }
        }

        private IEnumerator AnimateButtonDelayed(Button button, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            UIAnimationController.Instance.FadeSlideIn(button,
                UIAnimationController.SlideDirection.Up, 20, 0.4f);
        }

        // =============================================================================
        // EVENT HANDLERS
        // =============================================================================

        private void SetupEventHandlers()
        {
            if (_mainMenuController != null)
            {
                _mainMenuController.OnSettingsClicked += HandleSettingsClicked;
                _mainMenuController.OnNewGameClicked += HandleNewGameClicked;
                _mainMenuController.OnContinueClicked += HandleContinueClicked;
                _mainMenuController.OnExitClicked += HandleExitClicked;
            }

            if (_settingsController != null)
            {
                _settingsController.OnClose += HandleSettingsClosed;
            }

            // Setup button sounds
            SetupButtonSounds();
        }

        private void CleanupEventHandlers()
        {
            if (_mainMenuController != null)
            {
                _mainMenuController.OnSettingsClicked -= HandleSettingsClicked;
                _mainMenuController.OnNewGameClicked -= HandleNewGameClicked;
                _mainMenuController.OnContinueClicked -= HandleContinueClicked;
                _mainMenuController.OnExitClicked -= HandleExitClicked;
            }

            if (_settingsController != null)
            {
                _settingsController.OnClose -= HandleSettingsClosed;
            }
        }

        private void SetupButtonSounds()
        {
            // Add hover/click sounds to all buttons
            var allButtons = _root.Query<Button>().ToList();
            foreach (var button in allButtons)
            {
                button.RegisterCallback<MouseEnterEvent>(evt => PlaySound(_buttonHoverSound));
                button.RegisterCallback<ClickEvent>(evt => PlaySound(_buttonClickSound));
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null)
            {
                // TODO: Use AudioManager when implemented
                // For now, play at camera position
                AudioSource.PlayClipAtPoint(clip, Camera.main?.transform.position ?? Vector3.zero, 0.5f);
            }
        }

        // =============================================================================
        // MENU ACTIONS
        // =============================================================================

        private void HandleNewGameClicked()
        {
            Debug.Log("Starting new game...");
            StartCoroutine(TransitionToScene("CharacterSelect"));
        }

        private void HandleContinueClicked()
        {
            Debug.Log("Continuing game...");
            // TODO: Load last save and transition to appropriate scene
            StartCoroutine(TransitionToScene("Overworld"));
        }

        private void HandleSettingsClicked()
        {
            OpenSettings();
        }

        private void HandleSettingsClosed()
        {
            CloseSettings();
        }

        private void HandleExitClicked()
        {
            Debug.Log("Exiting game...");
            StartCoroutine(ExitSequence());
        }

        // =============================================================================
        // SETTINGS PANEL
        // =============================================================================

        private void OpenSettings()
        {
            if (_settingsOpen) return;
            _settingsOpen = true;

            _settingsOverlay.style.display = DisplayStyle.Flex;
            UIAnimationController.Instance.FadeIn(_settingsOverlay, 0.3f);

            // Also animate the settings panel
            var settingsPanel = _settingsOverlay.Q<VisualElement>("settings-panel");
            if (settingsPanel != null)
            {
                settingsPanel.style.opacity = 0;
                settingsPanel.style.scale = new Scale(new Vector2(0.95f, 0.95f));
                UIAnimationController.Instance.FadeScaleIn(settingsPanel, 0.95f, 0.3f);
            }
        }

        private void CloseSettings()
        {
            if (!_settingsOpen) return;
            _settingsOpen = false;

            var settingsPanel = _settingsOverlay.Q<VisualElement>("settings-panel");
            if (settingsPanel != null)
            {
                UIAnimationController.Instance.FadeScaleOut(settingsPanel, 1.02f, 0.2f, () =>
                {
                    UIAnimationController.Instance.FadeOut(_settingsOverlay, 0.2f, () =>
                    {
                        _settingsOverlay.style.display = DisplayStyle.None;
                    });
                });
            }
            else
            {
                UIAnimationController.Instance.FadeOut(_settingsOverlay, 0.2f, () =>
                {
                    _settingsOverlay.style.display = DisplayStyle.None;
                });
            }
        }

        // =============================================================================
        // SCENE TRANSITIONS
        // =============================================================================

        private IEnumerator TransitionToScene(string sceneName)
        {
            // Fade out UI
            var mainContainer = _root.Q<VisualElement>("main-container");
            if (mainContainer != null)
            {
                UIAnimationController.Instance.FadeOut(mainContainer, 0.5f);
            }

            yield return new WaitForSecondsRealtime(0.6f);

            // Load scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }

        private IEnumerator ExitSequence()
        {
            // Fade out everything
            UIAnimationController.Instance.FadeOut(_root, 0.5f);

            yield return new WaitForSecondsRealtime(0.6f);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
