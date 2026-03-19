using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.Data;
using VeilBreakers.Managers;
using VeilBreakers.UI.Core;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Orchestrator for the character select screen.
    /// Wires sub-controllers, manages hero navigation, and handles scene lifecycle.
    /// Does NOT directly manipulate UI -- delegates to focused controllers.
    /// </summary>
    public class CharacterSelectManager : MonoBehaviour
    {
        // =============================================================================
        // CONSTANTS
        // =============================================================================

        private const string kGameScene = "Overworld";
        private const string kMainMenuScene = "MainMenu";
        private const string kStarterTownLocation = "StarterTown";
        private const string kConfigPath = "CharacterSelect/HeroDisplayConfigs/";

        // UI element name constants (prevent silent null from typos)
        private const string kBtnPrev = "btn-prev";
        private const string kBtnNext = "btn-next";
        private const string kBtnBack = "btn-back";
        private const string kBtnEmbark = "btn-embark";
        private const string kEmbarkText = "embark-text";
        private const string kEmbarkGlow = "embark-glow";
        private const string kInfoPanelContainer = "info-panel-container";
        private const string kEmbarkHexBg = "embark-hex-bg";

        // =============================================================================
        // SERIALIZED FIELDS
        // =============================================================================

        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private HeroDisplayConfig[] _heroConfigs;

        // =============================================================================
        // STATE
        // =============================================================================

        private List<HeroData> _heroList;
        private int _currentIndex;
        private bool _isTransitioning;
        private bool _isEmbarking;
        private bool _isInitialized;
        private static readonly WaitForSeconds kTransitionWait = new WaitForSeconds(0.15f);
        private VisualElement _root;
        private string _currentThemeClass;

        // =============================================================================
        // CACHED UI REFERENCES
        // =============================================================================

        private Button _btnPrev;
        private Button _btnNext;
        private Button _btnBack;
        private Button _btnEmbark;
        private Label _embarkText;
        private VisualElement _embarkGlow;
        private VisualElement _infoPanelContainer;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public int CurrentIndex => _currentIndex;
        public HeroData CurrentHero => _heroList != null && _currentIndex >= 0 && _currentIndex < _heroList.Count
            ? _heroList[_currentIndex] : null;
        public HeroDisplayConfig CurrentConfig => _heroConfigs != null && _currentIndex >= 0 && _currentIndex < _heroConfigs.Length
            ? _heroConfigs[_currentIndex] : null;
        public int HeroCount => _heroList?.Count ?? 0;
        public bool IsTransitioning => _isTransitioning;

        // =============================================================================
        // LIFECYCLE
        // =============================================================================

        private void OnEnable()
        {
            // Ensure critical singletons exist (handles direct scene entry without Bootstrap)
            EnsureCriticalManagers();

            // Safety net: clear events on scene unload to prevent memory leaks
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            // Listen for navigation requests from sub-controllers (e.g. CarouselController)
            CharSelectEvents.OnNavigationRequested += NavigateToHero;

            // Listen for embark trigger from HoldToEmbarkController
            CharSelectEvents.OnEmbarkTriggered += TriggerEmbark;

            StartCoroutine(InitializeWhenReady());
        }

        private void OnDisable()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            CharSelectEvents.OnNavigationRequested -= NavigateToHero;
            CharSelectEvents.OnEmbarkTriggered -= TriggerEmbark;
            StopAllCoroutines();
            _isTransitioning = false;
            _isEmbarking = false;
            _isInitialized = false;
            UnbindUI();
        }

        private void OnDestroy()
        {
            CharSelectEvents.ClearAll();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            // H2 fix: only clear events when OUR scene unloads, not any scene
            if (scene == gameObject.scene)
            {
                CharSelectEvents.ClearAll();
            }
        }

        /// <summary>
        /// Creates critical singleton managers if they don't already exist.
        /// Handles the case where this scene is entered directly (skipping Bootstrap/MainMenu).
        /// </summary>
        private static void EnsureCriticalManagers()
        {
            EnsureSingleton<GameManager>("[GameManager]");
            EnsureSingleton<GameDatabase>("[GameDatabase]");
            EnsureSingleton<SaveManager>("[SaveManager]");
        }

        private static void EnsureSingleton<T>(string objectName) where T : SingletonMonoBehaviour<T>
        {
            if (!SingletonMonoBehaviour<T>.HasInstance)
            {
                var go = new GameObject(objectName);
                go.AddComponent<T>();
            }
        }

        private IEnumerator InitializeWhenReady()
        {
            // Wait for GameDatabase (guard null for quit-time re-enable)
            float timeout = 10f;
            float elapsed = 0f;
            while (GameDatabase.Instance == null || !GameDatabase.Instance.IsReady)
            {
                elapsed += Time.deltaTime;
                if (elapsed > timeout)
                {
                    Debug.LogError("[CharSelectManager] Timed out waiting for GameDatabase after 10s.");
                    yield break;
                }
                yield return null;
            }

            LoadHeroData();

            // Bail if no hero data -- downstream controllers can't function
            if (_heroList == null || _heroList.Count == 0)
            {
                yield break;
            }

            CacheUIReferences();
            SetAnimationHints();
            BindUI();
            ApplyInitialState();

            _isInitialized = true;
            CharSelectEvents.RaiseScreenReady();
        }

        // =============================================================================
        // DATA LOADING
        // =============================================================================

        private void LoadHeroData()
        {
            _heroList = GameDatabase.Instance.GetAllHeroes();

            if (_heroList == null || _heroList.Count == 0)
            {
                Debug.LogError("[CharSelectManager] No heroes found in GameDatabase!");
                return;
            }

            // Sort by hero_id for consistent ordering
            _heroList.Sort((a, b) => string.Compare(a.hero_id, b.hero_id, StringComparison.Ordinal));

            // Validate configs match hero count
            if (_heroConfigs == null || _heroConfigs.Length == 0)
            {
                Debug.LogWarning("[CharSelectManager] No HeroDisplayConfigs assigned. Using defaults.");
                _heroConfigs = new HeroDisplayConfig[_heroList.Count];
            }

            // Reorder configs to match hero list order
            ReorderConfigsToMatchHeroes();
        }

        private void ReorderConfigsToMatchHeroes()
        {
            var ordered = new HeroDisplayConfig[_heroList.Count];
            for (int i = 0; i < _heroList.Count; i++)
            {
                ordered[i] = FindConfigForHero(_heroList[i].hero_id);
            }
            _heroConfigs = ordered;
        }

        private HeroDisplayConfig FindConfigForHero(string heroId)
        {
            if (_heroConfigs == null) return null;

            for (int i = 0; i < _heroConfigs.Length; i++)
            {
                if (_heroConfigs[i] != null && _heroConfigs[i].heroId == heroId)
                {
                    return _heroConfigs[i];
                }
            }

            Debug.LogWarning($"[CharSelectManager] No config found for hero '{heroId}'");
            return null;
        }

        // =============================================================================
        // UI BINDING
        // =============================================================================

        private void CacheUIReferences()
        {
            if (_uiDocument == null) { Debug.LogError("[CharacterSelectManager] UIDocument not assigned!"); return; }
            _root = _uiDocument.rootVisualElement;

            _btnPrev = _root.Q<Button>(kBtnPrev);
            _btnNext = _root.Q<Button>(kBtnNext);
            _btnBack = _root.Q<Button>(kBtnBack);
            _btnEmbark = _root.Q<Button>(kBtnEmbark);
            _embarkText = _root.Q<Label>(kEmbarkText);
            _embarkGlow = _root.Q<VisualElement>(kEmbarkGlow);
            _infoPanelContainer = _root.Q<VisualElement>(kInfoPanelContainer);

            // Assert critical elements exist -- surfaces typos immediately instead of silent null
            Debug.Assert(_btnPrev != null, $"[CharSelectManager] Element '{kBtnPrev}' not found in UXML");
            Debug.Assert(_btnNext != null, $"[CharSelectManager] Element '{kBtnNext}' not found in UXML");
            Debug.Assert(_btnBack != null, $"[CharSelectManager] Element '{kBtnBack}' not found in UXML");
            Debug.Assert(_btnEmbark != null, $"[CharSelectManager] Element '{kBtnEmbark}' not found in UXML");
            Debug.Assert(_infoPanelContainer != null, $"[CharSelectManager] Element '{kInfoPanelContainer}' not found in UXML");
        }

        private void BindUI()
        {
            _btnPrev?.RegisterCallback<ClickEvent>(OnPrevClicked);
            _btnNext?.RegisterCallback<ClickEvent>(OnNextClicked);
            _btnBack?.RegisterCallback<ClickEvent>(OnBackClicked);

            // NOTE: btn-embark click is handled by HoldToEmbarkController (hold-to-confirm)
            // Zone navigation is handled by CharSelectFocusManager
            // Only cancel (back) navigation remains here
            _root?.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);
        }

        private void UnbindUI()
        {
            _btnPrev?.UnregisterCallback<ClickEvent>(OnPrevClicked);
            _btnNext?.UnregisterCallback<ClickEvent>(OnNextClicked);
            _btnBack?.UnregisterCallback<ClickEvent>(OnBackClicked);

            _root?.UnregisterCallback<NavigationCancelEvent>(OnNavigationCancel);
        }

        // =============================================================================
        // INITIAL STATE
        // =============================================================================

        private void ApplyInitialState()
        {
            _currentIndex = 0;
            _isTransitioning = false;

            if (_heroList != null && _heroList.Count > 0)
            {
                ApplyThemeClass(_heroList[0].hero_id);
                CharSelectEvents.RaiseHeroChanged(0, _heroList[0], CurrentConfig);
                UpdateEmbarkText();
            }
        }

        // =============================================================================
        // HERO NAVIGATION
        // =============================================================================

        public void NavigateToHero(int index)
        {
            if (_isTransitioning || _heroList == null || _heroList.Count == 0) return;

            index = Mathf.Clamp(index, 0, _heroList.Count - 1);
            if (index == _currentIndex) return;

            _isTransitioning = true;
            _currentIndex = index;

            ApplyThemeClass(_heroList[_currentIndex].hero_id);
            CharSelectEvents.RaiseHeroChanged(_currentIndex, _heroList[_currentIndex], CurrentConfig);
            UpdateEmbarkText();

            // Transition completes after USS animations finish
            StartCoroutine(EndTransitionAfterDelay());
        }

        private IEnumerator EndTransitionAfterDelay()
        {
            yield return kTransitionWait;
            _isTransitioning = false;
        }

        public void NavigatePrev()
        {
            if (_heroList == null || _heroList.Count == 0) return;
            int newIndex = _currentIndex - 1;
            if (newIndex < 0) newIndex = _heroList.Count - 1; // Wrap around
            NavigateToHero(newIndex);
        }

        public void NavigateNext()
        {
            if (_heroList == null || _heroList.Count == 0) return;
            int newIndex = (_currentIndex + 1) % _heroList.Count;
            NavigateToHero(newIndex);
        }

        // =============================================================================
        // THEME MANAGEMENT
        // =============================================================================

        private void ApplyThemeClass(string heroId)
        {
            if (_root == null) return;

            // H7 fix: track current theme class instead of hardcoding hero list
            if (!string.IsNullOrEmpty(_currentThemeClass))
            {
                _root.RemoveFromClassList(_currentThemeClass);
            }

            _currentThemeClass = $"theme-{heroId}";
            _root.AddToClassList(_currentThemeClass);
        }

        // =============================================================================
        // EMBARK FLOW
        // =============================================================================

        private void UpdateEmbarkText()
        {
            var hero = CurrentHero;
            if (hero == null) return;

            string name = string.IsNullOrEmpty(hero.display_name) ? (hero.hero_id?.ToUpper() ?? "UNKNOWN") : hero.display_name.ToUpper();
            if (_embarkText != null) _embarkText.text = $"EMBARK AS {name}";

            // Start breathing glow
            _embarkGlow?.AddToClassList("breathing");
        }

        /// <summary>
        /// Called when embark hold completes (via OnEmbarkTriggered event).
        /// Begins the async embark sequence (save creation + scene transition).
        /// </summary>
        private void TriggerEmbark()
        {
            // Guard against double re-entry
            if (_isEmbarking) return;

            var hero = CurrentHero;
            if (hero == null) return;

            _isEmbarking = true;

            CharSelectEvents.RaiseScreenExiting();

            StartCoroutine(EmbarkSequence(hero));
        }

        private IEnumerator EmbarkSequence(HeroData hero)
        {
            // Create save file
            yield return StartCoroutine(CreateOrRotateNewGameSave(hero));

            // Transition to gameplay
            if (ScreenTransition.HasInstance)
            {
                ScreenTransition.Instance.Transition(() => SceneManager.LoadScene(kGameScene));
            }
            else
            {
                SceneManager.LoadScene(kGameScene);
            }
        }

        private const float kSaveTaskTimeout = 10f;

        private IEnumerator CreateOrRotateNewGameSave(HeroData hero)
        {
            if (!SaveManager.HasInstance || hero == null)
            {
                yield break;
            }

            var saveManager = SaveManager.Instance;
            float elapsed;

            // H5 fix: add timeouts to all async task polling loops
            var slotTask = saveManager.GetBestNewGameSlotAsync();
            elapsed = 0f;
            while (!slotTask.IsCompleted)
            {
                elapsed += Time.unscaledDeltaTime;
                if (elapsed > kSaveTaskTimeout)
                {
                    Debug.LogError("[CharSelectManager] GetBestNewGameSlotAsync timed out.");
                    yield break;
                }
                yield return null;
            }

            if (slotTask.IsFaulted || slotTask.IsCanceled)
            {
                Debug.LogWarning("[CharSelectManager] Failed to resolve save slot.");
                yield break;
            }

            int slot = slotTask.Result;
            string heroName = string.IsNullOrEmpty(hero.display_name) ? hero.hero_id : hero.display_name;

            var createTask = saveManager.CreateNewSaveAsync(slot, hero.hero_id, heroName, hero.GetPrimaryPath());
            elapsed = 0f;
            while (!createTask.IsCompleted)
            {
                elapsed += Time.unscaledDeltaTime;
                if (elapsed > kSaveTaskTimeout)
                {
                    Debug.LogError("[CharSelectManager] CreateNewSaveAsync timed out.");
                    yield break;
                }
                yield return null;
            }

            if (createTask.IsFaulted || createTask.IsCanceled || !createTask.Result)
            {
                Debug.LogWarning($"[CharSelectManager] Failed to create save in slot {slot}.");
                yield break;
            }

            saveManager.SetCurrentLocation(kStarterTownLocation);
            var saveTask = saveManager.SaveAsync(slot);
            elapsed = 0f;
            while (!saveTask.IsCompleted)
            {
                elapsed += Time.unscaledDeltaTime;
                if (elapsed > kSaveTaskTimeout)
                {
                    Debug.LogError("[CharSelectManager] SaveAsync timed out.");
                    yield break;
                }
                yield return null;
            }
        }

        // =============================================================================
        // UI EVENT HANDLERS
        // =============================================================================

        private void OnPrevClicked(ClickEvent evt) => NavigatePrev();
        private void OnNextClicked(ClickEvent evt) => NavigateNext();

        private void OnBackClicked(ClickEvent evt) => NavigateBack();

        private void NavigateBack()
        {
            CharSelectEvents.RaiseScreenExiting();
            if (ScreenTransition.HasInstance)
            {
                ScreenTransition.Instance.Transition(() => SceneManager.LoadScene(kMainMenuScene));
            }
            else
            {
                SceneManager.LoadScene(kMainMenuScene);
            }
        }

        // =============================================================================
        // NAVIGATION EVENTS (KEYBOARD / GAMEPAD)
        // =============================================================================

        private void OnNavigationCancel(NavigationCancelEvent evt)
        {
            NavigateBack();
            evt.StopPropagation();
        }

        // =================================================================
        // ANIMATION HINTS
        // =================================================================

        /// <summary>
        /// Sets UsageHints.DynamicTransform on all VisualElements that have USS
        /// transitions or hover scale/translate effects. Called once during init,
        /// after CacheUIReferences(). Uses |= to preserve existing flags (e.g. DynamicColor).
        /// </summary>
        private void SetAnimationHints()
        {
            // Named elements
            SetHint(_infoPanelContainer);
            SetHint(_btnBack);
            SetHint(_btnEmbark);

            var embarkHexBg = _root.Q<VisualElement>(kEmbarkHexBg);
            Debug.Assert(embarkHexBg != null, $"[CharSelectManager] Missing element: {kEmbarkHexBg}");
            SetHint(embarkHexBg);

            SetHint(_embarkGlow);
            SetHint(_btnPrev);
            SetHint(_btnNext);
            SetHint(_embarkText);

            // Class-based queries (multiple elements)
            _root.Query<VisualElement>(className: "ability-slot").ForEach(SetHint);
            _root.Query<VisualElement>(className: "hero-card").ForEach(SetHint);
            _root.Query<VisualElement>(className: "nav-arrow").ForEach(SetHint);
            _root.Query<VisualElement>(className: "veil-panel").ForEach(SetHint);
            _root.Query<VisualElement>(className: "fog-particles").ForEach(SetHint);
            _root.Query<VisualElement>(className: "tab-btn").ForEach(SetHint);
            _root.Query<VisualElement>(className: "toast-container").ForEach(SetHint);
        }

        private static void SetHint(VisualElement el)
        {
            if (el != null) el.usageHints |= UsageHints.DynamicTransform;
        }
    }
}
