using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using VeilBreakers.Core;
using VeilBreakers.Managers;
using VeilBreakers.UI.Controls;
using VeilBreakers.UI.Core;

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
        // CACHED TRANSITION LISTS
        // =============================================================================

        private static readonly List<StylePropertyName> kScatterProperties = new()
        {
            new StylePropertyName("opacity"),
            new StylePropertyName("translate")
        };
        private static readonly List<TimeValue> kScatterDurations = new()
        {
            new TimeValue(0.25f, TimeUnit.Second),
            new TimeValue(0.25f, TimeUnit.Second)
        };
        private static readonly List<EasingFunction> kScatterEasing = new()
        {
            new EasingFunction(EasingMode.EaseIn),
            new EasingFunction(EasingMode.EaseIn)
        };

        private static readonly List<StylePropertyName> kTitleExitProperties = new()
        {
            new StylePropertyName("opacity"),
            new StylePropertyName("scale")
        };
        private static readonly List<TimeValue> kTitleExitDurations = new()
        {
            new TimeValue(0.3f, TimeUnit.Second),
            new TimeValue(0.3f, TimeUnit.Second)
        };

        private static readonly List<StylePropertyName> kFlashProperties = new()
        {
            new StylePropertyName("background-color")
        };
        private static readonly List<TimeValue> kFlashInDurations = new()
        {
            new TimeValue(0.15f, TimeUnit.Second)
        };
        private static readonly List<TimeValue> kFlashOutDurations = new()
        {
            new TimeValue(0.4f, TimeUnit.Second)
        };

        private static readonly List<StylePropertyName> kRootFadeProperties = new()
        {
            new StylePropertyName("opacity")
        };
        private static readonly List<TimeValue> kRootFadeDurations = new()
        {
            new TimeValue(0.35f, TimeUnit.Second)
        };

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
        private bool _hasValidSave;
        private bool _initialized;
        private TitleScreenVFX _titleVfx; // For logo aura hover response
        private bool _eventsBound;
        private int _continueSlot = SaveManager.kNoneSlot;

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
            if (!_initialized)
            {
                InitializeUI();
            }

            if (_initialized)
            {
                BindEvents();
                StartCoroutine(RefreshContinueButton());
            }

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

            // Stop all coroutines (entrance animations, RefreshContinueButton, etc.)
            StopAllCoroutines();
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
            _cachedButtons = _buttonContainer != null ? _buttonContainer.Query<Button>().ToList() : new List<Button>();

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

            _initialized = true;
            ErrorLogger.UI("MainMenu initialized");
        }

        private void BindEvents()
        {
            if (_eventsBound) return;
            _eventsBound = true;

            _btnContinue?.RegisterCallback<ClickEvent>(OnContinueButtonClicked);
            _btnNewGame?.RegisterCallback<ClickEvent>(OnNewGameButtonClicked);
            _btnSettings?.RegisterCallback<ClickEvent>(OnSettingsButtonClicked);
            _btnCredits?.RegisterCallback<ClickEvent>(OnCreditsButtonClicked);
            _btnExit?.RegisterCallback<ClickEvent>(OnExitButtonClicked);
        }

        private void UnbindEvents()
        {
            if (!_eventsBound) return;
            _eventsBound = false;

            _btnContinue?.UnregisterCallback<ClickEvent>(OnContinueButtonClicked);
            _btnNewGame?.UnregisterCallback<ClickEvent>(OnNewGameButtonClicked);
            _btnSettings?.UnregisterCallback<ClickEvent>(OnSettingsButtonClicked);
            _btnCredits?.UnregisterCallback<ClickEvent>(OnCreditsButtonClicked);
            _btnExit?.UnregisterCallback<ClickEvent>(OnExitButtonClicked);
        }

        // =============================================================================
        // SAVE FILE CHECK
        // =============================================================================

        private IEnumerator RefreshContinueButton()
        {
            bool saveExists = false;
            bool corrupted = false;
            int continueSlot = SaveManager.kNoneSlot;

            if (SaveManager.HasInstance)
            {
                var slotTask = SaveManager.Instance.GetMostRecentSlotAsync(includeAutoSlots: true);
                while (!slotTask.IsCompleted)
                {
                    yield return null;
                }

                if (slotTask.IsFaulted)
                {
                    Debug.LogError($"[MainMenu] Failed to get save slot: {slotTask.Exception?.InnerException?.Message}");
                    yield break;
                }

                continueSlot = slotTask.Result;
                saveExists = continueSlot != SaveManager.kNoneSlot;

                if (saveExists)
                {
                    var metaTask = SaveManager.Instance.GetSlotMetadataAsync(continueSlot);
                    while (!metaTask.IsCompleted)
                    {
                        yield return null;
                    }

                    if (metaTask.IsFaulted)
                    {
                        Debug.LogError($"[MainMenu] Failed to get slot metadata: {metaTask.Exception?.InnerException?.Message}");
                        yield break;
                    }

                    var meta = metaTask.Result;
                    corrupted = meta?.isCorrupted == true;
                    if (corrupted)
                    {
                        saveExists = false;
                        continueSlot = SaveManager.kNoneSlot;
                    }
                }
            }
            else
            {
                // Fallback for early initialization before SaveManager is created.
                continueSlot = FindMostRecentSlotFromDiskFallback();
                saveExists = continueSlot != SaveManager.kNoneSlot;
            }

            SetContinueVisible(saveExists);
            _hasValidSave = saveExists;
            _continueSlot = continueSlot;

            if (corrupted)
            {
                Debug.LogWarning($"[MainMenuController] Save slot {continueSlot} appears corrupted; hiding Continue.");
            }
        }

        private void SetContinueVisible(bool visible)
        {
            if (_btnContinue != null)
            {
                _btnContinue.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
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

        // OnKeyDown removed - Escape handling is done via InputManager.GameAction.Cancel in OnActionTriggered

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

            StartCoroutine(TransitionToScene(_characterSelectScene));
        }

        private IEnumerator TransitionToScene(string sceneName)
        {
            if (_root == null)
            {
                SceneManager.LoadScene(sceneName);
                yield break;
            }

            // Phase 1: Buttons scatter outward (0.25s)
            if (_buttonContainer != null)
            {
                var buttons = _cachedButtons; // Use cached list (no allocation)
                for (int i = 0; i < buttons.Count; i++)
                {
                    var btn = buttons[i];
                    btn.style.transitionProperty = kScatterProperties;
                    btn.style.transitionDuration = kScatterDurations;
                    btn.style.transitionTimingFunction = kScatterEasing;
                    // Alternate buttons slide left/right
                    float slideX = (i % 2 == 0) ? -80f : 80f;
                    btn.style.translate = new Translate(slideX, 0);
                    btn.style.opacity = 0f;
                }
            }

            // Phase 1b: Title scales up and fades
            if (_titleSection != null)
            {
                _titleSection.style.transitionProperty = kTitleExitProperties;
                _titleSection.style.transitionDuration = kTitleExitDurations;
                _titleSection.style.opacity = 0f;
                _titleSection.style.scale = new Scale(new Vector2(1.1f, 1.1f));
            }

            yield return new WaitForSecondsRealtime(0.25f);

            // Phase 2: Flash white overlay (0.15s in, 0.2s out)
            var flash = new VisualElement();
            flash.name = "transition-flash";
            flash.pickingMode = PickingMode.Ignore;
            flash.style.position = Position.Absolute;
            flash.style.left = 0;
            flash.style.top = 0;
            flash.style.right = 0;
            flash.style.bottom = 0;
            flash.style.backgroundColor = new Color(1f, 0.85f, 0.6f, 0f);
            _root.Add(flash);

            // Flash in
            flash.style.transitionProperty = kFlashProperties;
            flash.style.transitionDuration = kFlashInDurations;
            flash.schedule.Execute(() =>
            {
                flash.style.backgroundColor = new Color(1f, 0.9f, 0.7f, 0.6f);
            });

            yield return new WaitForSecondsRealtime(0.15f);

            // Phase 3: Flash fades to black (0.4s)
            flash.style.transitionDuration = kFlashOutDurations;
            flash.style.backgroundColor = new Color(0f, 0f, 0f, 1f);

            // Also fade entire root content behind the flash
            _root.style.transitionProperty = new List<StylePropertyName> { new("opacity") };
            _root.style.transitionDuration = new List<TimeValue> { new(0.35f, TimeUnit.Second) };

            yield return new WaitForSecondsRealtime(0.45f);

            // Load scene
            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            while (asyncOp != null && !asyncOp.isDone)
            {
                yield return null;
            }
        }

        private void LoadGame()
        {
            StartCoroutine(LoadAndEnterGame());
        }

        private void ShowSettings()
        {
            // Settings panel is opened via MainMenuBootstrap.HandleSettingsClicked()
            // which subscribes to the OnSettingsClicked event fired above.
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
        /// Attempts to load the most recent valid save slot via SaveManager, then transitions to the game scene.
        /// Falls back to direct scene load if SaveManager is unavailable or load fails.
        /// </summary>
        private IEnumerator LoadAndEnterGame()
        {
            SetInteractable(false);
            int slotToLoad = _continueSlot;

            if (SaveManager.HasInstance)
            {
                if (slotToLoad == SaveManager.kNoneSlot)
                {
                    var slotTask = SaveManager.Instance.GetMostRecentSlotAsync(includeAutoSlots: true);
                    while (!slotTask.IsCompleted)
                    {
                        yield return null;
                    }

                    if (slotTask.IsFaulted)
                    {
                        Debug.LogError($"[MainMenu] Failed to get save slot during load: {slotTask.Exception?.InnerException?.Message}");
                        SetInteractable(true);
                        yield break;
                    }
                    slotToLoad = slotTask.Result;
                }

                if (slotToLoad != SaveManager.kNoneSlot)
                {
                    var loadTask = SaveManager.Instance.LoadAsync(slotToLoad);
                    while (!loadTask.IsCompleted)
                    {
                        yield return null;
                    }

                    if (loadTask.IsFaulted)
                    {
                        Debug.LogError($"[MainMenu] Failed to load save: {loadTask.Exception?.InnerException?.Message}");
                        SetInteractable(true);
                        yield break;
                    }

                    if (!loadTask.Result)
                    {
                        Debug.LogWarning($"[MainMenuController] Failed to load slot {slotToLoad}. Proceeding to scene load.");
                    }
                }
                else
                {
                    Debug.LogWarning("[MainMenuController] Continue requested but no valid save slot was found.");
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
            if (_initialized)
            {
                return;
            }

            _root = root;

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
            _cachedButtons = _buttonContainer != null ? _buttonContainer.Query<Button>().ToList() : new List<Button>();

            // Initialize hover callback dictionaries (needed by PlayEntranceAnimation)
            _hoverEnterCallbacks = new Dictionary<Button, EventCallback<MouseEnterEvent>>();
            _hoverLeaveCallbacks = new Dictionary<Button, EventCallback<MouseLeaveEvent>>();

            // Set version
            if (_versionLabel != null)
            {
                _versionLabel.text = $"v{Application.version}";
            }

            // Bind button events
            BindEvents();

            // Cache TitleScreenVFX for logo hover response
            _titleVfx = FindAnyObjectByType<TitleScreenVFX>();

            // Play entrance animations and apply button VFX
            PlayEntranceAnimation();

            // Check for save
            StartCoroutine(RefreshContinueButton());

            _initialized = true;
            ErrorLogger.UI("MainMenu initialized via external root");
        }

        private int FindMostRecentSlotFromDiskFallback()
        {
            string savesDir = Path.Combine(Application.persistentDataPath, "saves");
            if (!Directory.Exists(savesDir))
            {
                return SaveManager.kNoneSlot;
            }

            string[] candidatePaths =
            {
                Path.Combine(savesDir, "slot_0.sav"),
                Path.Combine(savesDir, "slot_1.sav"),
                Path.Combine(savesDir, "slot_2.sav"),
                Path.Combine(savesDir, "auto.sav"),
                Path.Combine(savesDir, "auto_checkpoint.sav")
            };

            int[] candidateSlots =
            {
                0,
                1,
                2,
                SaveManager.kAutoSlot,
                SaveManager.kAutoSlotCheckpoint
            };

            DateTime bestWriteTime = DateTime.MinValue;
            int bestSlot = SaveManager.kNoneSlot;

            for (int i = 0; i < candidatePaths.Length; i++)
            {
                string path = candidatePaths[i];
                if (!File.Exists(path))
                {
                    continue;
                }

                DateTime writeTime = File.GetLastWriteTimeUtc(path);
                if (writeTime > bestWriteTime)
                {
                    bestWriteTime = writeTime;
                    bestSlot = candidateSlots[i];
                }
            }

            return bestSlot;
        }

        /// <summary>
        /// Get the button container for animation purposes.
        /// </summary>
        public VisualElement GetButtonContainer() => _buttonContainer;

        /// <summary>
        /// Get the title element for animation purposes.
        /// </summary>
        public Label GetTitleElement() => _gameTitle;

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

                    float delay = 1.2f + (i * _buttonStaggerDelay); // Stagger each button
                    StartCoroutine(FadeInElement(button, 0.5f, delay));
                    StartCoroutine(SlideInElement(button, -50, 0, 0.6f, delay, EaseType.BackOut));
                    StartCoroutine(ScaleInElement(button, 0.8f, 1f, 0.6f, delay, EaseType.BackOut));

                    // Add hover color/scale effects
                    AddButtonHoverEffects(button);

                    // Apply AAA VFX (ripple, glow overlay, press effect)
                    ButtonVFXHelper.ApplyEffects(button, ButtonVFXOptions.Silent);

                    // Click burst for impact feedback on all buttons
                    ButtonVFXHelper.AddClickBurst(button);

                    // Gamepad/keyboard focus choreography
                    ButtonVFXHelper.AddFocusEffect(button);
                }
            }

            // Idle breathing on containers for organic feel
            if (_titleSection != null)
                ButtonVFXHelper.AddBreathing(_titleSection, 0.006f, 4000f);
            if (_buttonContainer != null)
                ButtonVFXHelper.AddBreathing(_buttonContainer, 0.004f, 3500f);

            var logoContainer = _root?.Q<VisualElement>("logo-container");
            if (logoContainer != null)
                ButtonVFXHelper.AddBreathing(logoContainer, 0.008f, 3000f);
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
            // NOTE: Skip color changes for buttons using art skins (vb-btn-sheet class) - MoltenButtonVFX handles those

            bool isPrimary = button.name == "btn-new-game" || button.name == "btn-continue";

            // Define colors programmatically (bypasses USS specificity issues)
            // Primary buttons: Subtle warm tones, no harsh borders
            Color primaryBaseColor = new Color(120f / 255f, 50f / 255f, 20f / 255f, 1f);
            Color primaryHoverColor = new Color(150f / 255f, 65f / 255f, 25f / 255f, 1f);
            Color primaryBorderBase = new Color(90f / 255f, 45f / 255f, 25f / 255f, 0.6f);
            Color primaryBorderHover = new Color(140f / 255f, 70f / 255f, 35f / 255f, 0.8f);

            // Secondary buttons: Dark with minimal border visibility
            Color secondaryBaseColor = new Color(45f / 255f, 38f / 255f, 35f / 255f, 1f);
            Color secondaryHoverColor = new Color(65f / 255f, 52f / 255f, 42f / 255f, 1f);
            Color secondaryBorderBase = new Color(70f / 255f, 55f / 255f, 45f / 255f, 0.4f);
            Color secondaryBorderHover = new Color(100f / 255f, 70f / 255f, 50f / 255f, 0.6f);

            // Only set initial base colors if NOT using art skins
            // (art skin buttons will have vb-btn-sheet class added later by MoltenButtonVFX)
            // We delay the initial color setting since the class isn't present yet at startup

            // Create and store hover enter callback (for proper unregistration)
            EventCallback<MouseEnterEvent> enterCallback = evt =>
            {
                // Skip entirely for art skin buttons - MoltenButtonVFX handles hover
                if (button.ClassListContains("vb-btn-sheet"))
                {
                    return;
                }

                Color hoverColor = isPrimary ? primaryHoverColor : secondaryHoverColor;
                Color hoverBorder = isPrimary ? primaryBorderHover : secondaryBorderHover;
                SetButtonColors(button, hoverColor, hoverBorder);
                button.style.scale = new Scale(new Vector2(1.05f, 1.05f));
                button.AddToClassList("vb-button-hover-glow");

                // Hover light propagation to adjacent buttons
                PropagateHoverLight(button, true);

                // Notify logo VFX of button hover
                if (_titleVfx != null)
                {
                    _titleVfx.OnButtonHovered(true, isPrimary);
                }
            };

            // Create and store hover leave callback (for proper unregistration)
            EventCallback<MouseLeaveEvent> leaveCallback = evt =>
            {
                // Skip entirely for art skin buttons - MoltenButtonVFX handles hover
                if (button.ClassListContains("vb-btn-sheet"))
                {
                    return;
                }

                Color restoreColor = isPrimary ? primaryBaseColor : secondaryBaseColor;
                Color restoreBorder = isPrimary ? primaryBorderBase : secondaryBorderBase;
                SetButtonColors(button, restoreColor, restoreBorder);
                button.style.scale = new Scale(Vector2.one);
                button.RemoveFromClassList("vb-button-hover-glow");

                // Clear neighbor hover light
                PropagateHoverLight(button, false);

                // Notify logo VFX of button unhover
                if (_titleVfx != null)
                {
                    _titleVfx.OnButtonHovered(false, isPrimary);
                }
            };

            // Register callbacks
            button.RegisterCallback(enterCallback);
            button.RegisterCallback(leaveCallback);

            // Store for unregistration in OnDisable (prevents memory leaks)
            _hoverEnterCallbacks[button] = enterCallback;
            _hoverLeaveCallbacks[button] = leaveCallback;
        }

        /// <summary>
        /// Propagates a subtle brightness lift to adjacent buttons when one is hovered.
        /// Uses opacity instead of border colors for a softer, non-intrusive effect.
        /// </summary>
        private void PropagateHoverLight(Button hoveredButton, bool isEntering)
        {
            if (_cachedButtons == null || _cachedButtons.Count < 2) return;

            int hoveredIndex = _cachedButtons.IndexOf(hoveredButton);
            if (hoveredIndex < 0) return;

            for (int offset = -1; offset <= 1; offset += 2)
            {
                int neighborIndex = hoveredIndex + offset;
                if (neighborIndex < 0 || neighborIndex >= _cachedButtons.Count) continue;

                var neighbor = _cachedButtons[neighborIndex];
                if (neighbor == null || neighbor.ClassListContains("vb-btn-sheet")) continue;

                // Subtle opacity lift instead of border color change
                neighbor.style.opacity = isEntering ? 0.85f : 1f;
            }
        }

        /// <summary>
        /// Helper to set button background and border colors.
        /// </summary>
        private void SetButtonColors(Button button, Color bgColor, Color borderColor)
        {
            button.style.backgroundColor = bgColor;
            // Don't set border colors programmatically - let USS handle borders
            // Setting them caused visible orange lines on hover
            button.style.borderTopColor = Color.clear;
            button.style.borderBottomColor = Color.clear;
            button.style.borderLeftColor = Color.clear;
            button.style.borderRightColor = Color.clear;
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
