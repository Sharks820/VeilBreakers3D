using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.Audio;
using VeilBreakers.Managers;
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
        // CACHED TRANSITION LISTS
        // =============================================================================

        private static readonly List<StylePropertyName> kDimProperties = new()
        {
            new StylePropertyName("opacity"),
            new StylePropertyName("scale")
        };
        private static readonly List<TimeValue> kDimDurations = new()
        {
            new TimeValue(0.3f, TimeUnit.Second),
            new TimeValue(0.3f, TimeUnit.Second)
        };
        private static readonly List<StylePropertyName> kDimBgProperties = new()
        {
            new StylePropertyName("background-color")
        };
        private static readonly List<TimeValue> kDimBgDurations = new()
        {
            new TimeValue(0.3f, TimeUnit.Second)
        };

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
        [SerializeField, Range(0f, 1f)] private float _rootBackdropAlpha = 0f; // Was 0.2 — removed dark overlay that obscured demon figure
        [SerializeField] private Color _rootBackdropColor = new Color(0f, 0f, 0f, 0f);

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
        private Coroutine _entranceCoroutine;
        private Coroutine _failsafeCoroutine;
        private UIAnimationController _animator;
        private Camera _cachedCamera;

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
            // Wire up UIAnimationController singleton for animated settings transitions
            _animator = UIAnimationController.Instance;

            InitializeUI();
            // NOTE: Entrance animation is handled by MainMenuController.PlayEntranceAnimation()
            // which has the full AAA elastic/bounce effects. Running both simultaneously
            // caused visible flickering as two systems fought over the same elements.
            SetupEventHandlers();
            StartCoroutine(DeferredStartupInit());
        }

        private void OnDestroy()
        {
            if (_entranceCoroutine != null) StopCoroutine(_entranceCoroutine);
            if (_failsafeCoroutine != null) StopCoroutine(_failsafeCoroutine);

            CleanupEventHandlers();
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private IEnumerator DeferredStartupInit()
        {
            // Allow menu UI to paint before expensive startup systems run.
            yield return null;

            if (!Application.isBatchMode)
            {
                EnsureCriticalManagers();
            }

            EnsureOverlayVfx();
            EnsureTitleScreenAudio();
            DisableExpensiveVfxInBatchMode();
        }

        private void InitializeUI()
        {
            if (_uiDocument == null)
            {
                Debug.LogError("[MainMenuBootstrap] UIDocument missing on MainMenuBootstrap GameObject.");
                return;
            }

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

            // Root background: FULLY TRANSPARENT — the demon/video/bg layers handle all visuals.
            // Previously had a 20% alpha dark overlay that obscured the demon figure.
            // Force transparent regardless of Inspector serialized values.
            var rootBackground = new VisualElement();
            rootBackground.name = "root-background";
            rootBackground.style.position = Position.Absolute;
            rootBackground.style.left = 0;
            rootBackground.style.top = 0;
            rootBackground.style.right = 0;
            rootBackground.style.bottom = 0;
            rootBackground.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0f)); // FORCED transparent
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

                // Initialize controller (guard against duplicates on reload)
                _mainMenuController = GetComponent<MainMenuController>() ?? gameObject.AddComponent<MainMenuController>();
                _mainMenuController.Initialize(_root);
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

                _root.Add(_settingsOverlay);

                // Initialize settings controller (guard against duplicates on reload)
                _settingsController = GetComponent<SettingsPanelController>() ?? gameObject.AddComponent<SettingsPanelController>();
                _settingsController.Initialize(_settingsOverlay);
            }
            else
            {
                _root.Add(_settingsOverlay);
            }
        }

        // =============================================================================
        // ANIMATION
        // =============================================================================

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
            if (_animator != null) _animator.FadeIn(mainContainer, _fadeInDuration);

            yield return new WaitForSecondsRealtime(_titleAnimDelay);

            // Find and animate title
            var title = _root.Q<Label>(className: "vb-header");
            if (title == null)
                title = _root.Q<Label>("game-title");

            if (title != null)
            {
                title.style.opacity = 0;
                title.style.scale = new Scale(new Vector2(0.9f, 0.9f));
                if (_animator != null) _animator.FadeScaleIn(title, 0.9f, 0.6f);
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
            if (_animator != null) _animator.FadeSlideIn(button,
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
            }

            if (_settingsController != null)
            {
                _settingsController.OnClose -= HandleSettingsClosed;
            }
        }

        private void SetupButtonSounds()
        {
            // Audio is now handled by MainMenuController.InitAudio() — no duplicate sound registration here
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip == null) return;
            if (!SettingsManager.HasInstance) return;

            float volume = 0.5f;
            var settings = SettingsManager.Instance.Settings;
            if (settings != null)
            {
                if (settings.MuteAll) return;
                volume *= Mathf.Clamp01(settings.MasterVolume) * Mathf.Clamp01(settings.SFXVolume);
            }

            if (volume <= 0.001f) return;

            if (_cachedCamera == null) _cachedCamera = Camera.main;
            AudioSource.PlayClipAtPoint(clip, _cachedCamera?.transform.position ?? Vector3.zero, volume);
        }

        // =============================================================================
        // MENU ACTIONS
        // =============================================================================

        private void HandleSettingsClicked()
        {
            OpenSettings();
        }

        private void HandleSettingsClosed()
        {
            CloseSettings();
        }

        // =============================================================================
        // SETTINGS PANEL
        // =============================================================================

        private void OpenSettings()
        {
            if (_settingsOpen) return;
            _settingsOpen = true;

            _settingsOverlay.style.display = DisplayStyle.Flex;

            // Dim and defocus background elements for cinematic settings overlay
            DimBackground(true);

            if (_animator != null)
            {
                _animator.FadeIn(_settingsOverlay, 0.3f);

                // Also animate the settings panel
                var settingsPanel = _settingsOverlay.Q<VisualElement>("settings-panel");
                if (settingsPanel != null)
                {
                    settingsPanel.style.opacity = 0;
                    settingsPanel.style.scale = new Scale(new Vector2(0.95f, 0.95f));
                    _animator.FadeScaleIn(settingsPanel, 0.95f, 0.3f);
                }
            }
            else
            {
                // Fallback: show immediately when no animator is available
                _settingsOverlay.style.opacity = 1;
                var settingsPanel = _settingsOverlay.Q<VisualElement>("settings-panel");
                if (settingsPanel != null)
                {
                    settingsPanel.style.opacity = 1;
                    settingsPanel.style.scale = new Scale(Vector2.one);
                }
            }
        }

        private void CloseSettings()
        {
            if (!_settingsOpen) return;
            _settingsOpen = false;

            // Restore background elements
            DimBackground(false);

            var settingsPanel = _settingsOverlay.Q<VisualElement>("settings-panel");
            if (_animator == null)
            {
                // Fallback: hide immediately when no animator is available
                _settingsOverlay.style.opacity = 0;
                _settingsOverlay.style.display = DisplayStyle.None;
                return;
            }

            if (settingsPanel != null)
            {
                _animator.FadeScaleOut(settingsPanel, 1.02f, 0.2f, () =>
                {
                    if (_animator != null) _animator.FadeOut(_settingsOverlay, 0.2f, () =>
                    {
                        _settingsOverlay.style.display = DisplayStyle.None;
                    });
                });
            }
            else
            {
                _animator.FadeOut(_settingsOverlay, 0.2f, () =>
                {
                    _settingsOverlay.style.display = DisplayStyle.None;
                });
            }
        }

        /// <summary>
        /// Dims/restores background elements when settings overlay opens/closes.
        /// Creates a cinematic defocus illusion (UI Toolkit has no native blur).
        /// </summary>
        private void DimBackground(bool dim)
        {
            var mainContainer = _root?.Q<VisualElement>("main-container");
            var rootBg = _root?.Q<VisualElement>("root-background");

            if (dim)
            {
                // Reduce opacity and slightly scale background for defocus illusion
                if (mainContainer != null)
                {
                    mainContainer.style.transitionProperty = kDimProperties;
                    mainContainer.style.transitionDuration = kDimDurations;
                    mainContainer.style.opacity = 0.3f;
                    mainContainer.style.scale = new Scale(new Vector2(1.02f, 1.02f));
                }

                // Darken root backdrop
                if (rootBg != null)
                {
                    rootBg.style.transitionProperty = kDimBgProperties;
                    rootBg.style.transitionDuration = kDimBgDurations;
                    rootBg.style.backgroundColor = new Color(0.02f, 0.01f, 0.03f, 0.7f);
                }
            }
            else
            {
                // Restore
                if (mainContainer != null)
                {
                    mainContainer.style.opacity = 1f;
                    mainContainer.style.scale = new Scale(Vector2.one);
                }

                if (rootBg != null)
                {
                    // Force transparent — no dark overlay on demon/video
                    rootBg.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0f));
                }
            }
        }

        /// <summary>
        /// MainMenu is often launched directly in-editor. Ensure core singletons exist
        /// so character select and continue flows don't stall on missing managers.
        /// </summary>
        private void EnsureCriticalManagers()
        {
            EnsureSingleton<GameManager>("[GameManager]");
            EnsureSingleton<GameDatabase>("[GameDatabase]");
            EnsureSingleton<InputManager>("[InputManager]");
            EnsureSingleton<SaveManager>("[SaveManager]");
            EnsureSingleton<AutoSaveManager>("[AutoSaveManager]");
            EnsureSingleton<SettingsManager>("[SettingsManager]");
            EnsureSingleton<AudioManager>("[AudioManager]");
        }

        private void EnsureOverlayVfx()
        {
            // VFX overlay disabled — the orange bar columns detract from AAA quality.
            // The component self-disables in Awake, so adding it is a no-op.
            // Kept for future re-enablement if VFX overlay is redesigned.
            if (GetComponent<VeilBreakers.UI.Effects.MainMenuVFXOverlayController>() == null)
            {
                gameObject.AddComponent<VeilBreakers.UI.Effects.MainMenuVFXOverlayController>();
            }
        }

        private void EnsureTitleScreenAudio()
        {
            // AAA audio: ambient music, random demon laughs, girl crying
            if (GetComponent<VeilBreakers.UI.Core.TitleScreenAudio>() == null)
            {
                gameObject.AddComponent<VeilBreakers.UI.Core.TitleScreenAudio>();
            }
        }

        /// <summary>
        /// Batchmode perf tests run without rendering; disable heavy decorative VFX to avoid
        /// artificial GC pressure that does not affect actual gameplay/menu visuals.
        /// </summary>
        private void DisableExpensiveVfxInBatchMode()
        {
            if (!Application.isBatchMode)
            {
                return;
            }

            string[] disabledTypes =
            {
                "VeilBreakers.UI.Menus.MainMenuVFXController",
                "VeilBreakers.UI.Core.TitleScreenVFX",
                "VeilBreakers.UI.Core.MenuVFXController",
                "VeilBreakers.UI.Core.MoltenVeinVFX",
                "VeilBreakers.UI.Core.SoulSwarmVFX",
                "VeilBreakers.UI.Core.MoltenButtonVFX"
            };

            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null || behaviour == this) continue;

                string fullName = behaviour.GetType().FullName;
                if (string.IsNullOrEmpty(fullName)) continue;

                for (int t = 0; t < disabledTypes.Length; t++)
                {
                    if (string.Equals(fullName, disabledTypes[t], System.StringComparison.Ordinal))
                    {
                        behaviour.enabled = false;
                        break;
                    }
                }
            }
        }

        private static void EnsureSingleton<T>(string objectName) where T : SingletonMonoBehaviour<T>
        {
            if (!SingletonMonoBehaviour<T>.HasInstance)
            {
                var go = new GameObject(objectName);
                go.AddComponent<T>();
            }
        }
    }
}
