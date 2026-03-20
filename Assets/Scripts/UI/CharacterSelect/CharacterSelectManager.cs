using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.Data;
using VeilBreakers.Managers;
using VeilBreakers.UI.Controls;
using VeilBreakers.UI.Core;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Orchestrator for the character select screen.
    /// Manages hero navigation, tab switching, async embark flow with timeout/error toast,
    /// skeleton loading states, and scene lifecycle.
    /// Delegates UI population to focused sub-controllers via CharSelectEvents.
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
        private const float kSaveTimeout = 10f;

        private const int kTabOverview = 0;
        private const int kTabAbilities = 1;
        private const int kTabLore = 2;

        private const string kBtnPrev = "btn-prev";
        private const string kBtnNext = "btn-next";
        private const string kBtnBack = "btn-back";
        private const string kBtnEmbark = "btn-embark";
        private const string kEmbarkText = "embark-text";
        private const string kEmbarkGlow = "embark-glow";
        private const string kEmbarkHexBg = "embark-hex-bg";
        private const string kInfoPanelContainer = "info-panel-container";
        private const string kTabBtnOverview = "tab-btn-overview";
        private const string kTabBtnAbilities = "tab-btn-abilities";
        private const string kTabBtnLore = "tab-btn-lore";
        private const string kTabOverviewContent = "tab-overview-content";
        private const string kTabAbilitiesContent = "tab-abilities-content";
        private const string kTabLoreContent = "tab-lore-content";
        private const string kEmbarkProgressRing = "embark-progress-ring";
        private const string kToastContainer = "toast-container";
        private const string kToastMessage = "toast-message";
        private const string kBtnToastRetry = "btn-toast-retry";
        private const string kBtnToastBack = "btn-toast-back";
        private const string kSkeletonOverlay = "skeleton-overlay";
        private const string kEmbarkSubtitle = "embark-subtitle";

        // =============================================================================
        // SERIALIZED FIELDS
        // =============================================================================

        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private HeroDisplayConfig[] _heroConfigs;
        [SerializeField] private HeroThemeTransitioner _themeTransitioner;
        [SerializeField] private EmbarkCinematicController _embarkCinematic;

        // =============================================================================
        // STATE
        // =============================================================================

        private List<HeroData> _heroList;
        private int _currentIndex;
        private bool _isTransitioning;
        private bool _isEmbarking;
        private bool _isInitialized;
        private int _currentTab = kTabOverview;
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
        private Button _tabBtnOverview;
        private Button _tabBtnAbilities;
        private Button _tabBtnLore;
        private VisualElement _tabOverviewContent;
        private VisualElement _tabAbilitiesContent;
        private VisualElement _tabLoreContent;
        private VisualElement _embarkProgressRing;
        private VisualElement _toastContainer;
        private Label _toastMessage;
        private Button _btnToastRetry;
        private Button _btnToastBack;
        private VisualElement _skeletonOverlay;
        private Label _embarkSubtitle;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>Current hero index in the hero list.</summary>
        public int CurrentIndex => _currentIndex;
        /// <summary>Currently selected hero data, or null if no hero selected.</summary>
        public HeroData CurrentHero => _heroList != null && _currentIndex >= 0 && _currentIndex < _heroList.Count
            ? _heroList[_currentIndex] : null;
        /// <summary>Display config for the currently selected hero.</summary>
        public HeroDisplayConfig CurrentConfig => _heroConfigs != null && _currentIndex >= 0 && _currentIndex < _heroConfigs.Length
            ? _heroConfigs[_currentIndex] : null;
        /// <summary>Total number of heroes available.</summary>
        public int HeroCount => _heroList?.Count ?? 0;
        /// <summary>True if a hero switch transition is in progress.</summary>
        public bool IsTransitioning => _isTransitioning;

        // =============================================================================
        // LIFECYCLE
        // =============================================================================

        private void OnEnable()
        {
            EnsureCriticalManagers();
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            CharSelectEvents.OnNavigationRequested += NavigateToHero;
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

        private void OnDestroy() { CharSelectEvents.ClearAll(); }

        private void LateUpdate()
        {
            if (_themeTransitioner != null)
            {
                _themeTransitioner.UpdateParallax();
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (scene == gameObject.scene) CharSelectEvents.ClearAll();
        }

        /// <summary>
        /// Creates critical singleton managers if they don't already exist.
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
            ShowSkeletonLoading();
            float timeout = 10f;
            float elapsed = 0f;
            while (GameDatabase.Instance == null || !GameDatabase.Instance.IsReady)
            {
                elapsed += Time.deltaTime;
                if (elapsed > timeout)
                {
                    Debug.LogError("[CharSelectManager] Timed out waiting for GameDatabase after 10s.");
                    CharSelectEvents.RaiseErrorOccurred("GameDatabase initialization timed out. Please restart.");
                    yield break;
                }
                yield return null;
            }
            LoadHeroData();
            if (_heroList == null || _heroList.Count == 0)
            {
                HideSkeletonLoading();
                CharSelectEvents.RaiseErrorOccurred("No heroes found. Please restart.");
                yield break;
            }
            HideSkeletonLoading();
            CacheUIReferences();
            SetAnimationHints();
            InitVisualSystems();
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
            _heroList.Sort((a, b) => string.Compare(a.hero_id, b.hero_id, StringComparison.Ordinal));
            if (_heroConfigs == null || _heroConfigs.Length == 0)
            {
                Debug.LogWarning("[CharSelectManager] No HeroDisplayConfigs assigned. Using defaults.");
                _heroConfigs = new HeroDisplayConfig[_heroList.Count];
            }
            ReorderConfigsToMatchHeroes();
        }

        private void ReorderConfigsToMatchHeroes()
        {
            var ordered = new HeroDisplayConfig[_heroList.Count];
            for (int i = 0; i < _heroList.Count; i++)
                ordered[i] = FindConfigForHero(_heroList[i].hero_id);
            _heroConfigs = ordered;
        }

        private HeroDisplayConfig FindConfigForHero(string heroId)
        {
            if (_heroConfigs == null) return null;
            for (int i = 0; i < _heroConfigs.Length; i++)
                if (_heroConfigs[i] != null && _heroConfigs[i].heroId == heroId) return _heroConfigs[i];
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
            _tabBtnOverview = _root.Q<Button>(kTabBtnOverview);
            _tabBtnAbilities = _root.Q<Button>(kTabBtnAbilities);
            _tabBtnLore = _root.Q<Button>(kTabBtnLore);
            _tabOverviewContent = _root.Q<VisualElement>(kTabOverviewContent);
            _tabAbilitiesContent = _root.Q<VisualElement>(kTabAbilitiesContent);
            _tabLoreContent = _root.Q<VisualElement>(kTabLoreContent);
            _embarkProgressRing = _root.Q<VisualElement>(kEmbarkProgressRing);
            _toastContainer = _root.Q<VisualElement>(kToastContainer);
            _toastMessage = _root.Q<Label>(kToastMessage);
            _btnToastRetry = _root.Q<Button>(kBtnToastRetry);
            _btnToastBack = _root.Q<Button>(kBtnToastBack);
            _skeletonOverlay = _root.Q<VisualElement>(kSkeletonOverlay);
            _embarkSubtitle = _root.Q<Label>(kEmbarkSubtitle);

            Debug.Assert(_btnPrev != null, $"[CharSelectManager] Element '{kBtnPrev}' not found in UXML");
            Debug.Assert(_btnNext != null, $"[CharSelectManager] Element '{kBtnNext}' not found in UXML");
            Debug.Assert(_btnBack != null, $"[CharSelectManager] Element '{kBtnBack}' not found in UXML");
            Debug.Assert(_btnEmbark != null, $"[CharSelectManager] Element '{kBtnEmbark}' not found in UXML");
            Debug.Assert(_infoPanelContainer != null, $"[CharSelectManager] Element '{kInfoPanelContainer}' not found in UXML");
            Debug.Assert(_tabBtnOverview != null, $"[CharSelectManager] Element '{kTabBtnOverview}' not found in UXML");
            Debug.Assert(_tabBtnAbilities != null, $"[CharSelectManager] Element '{kTabBtnAbilities}' not found in UXML");
            Debug.Assert(_tabBtnLore != null, $"[CharSelectManager] Element '{kTabBtnLore}' not found in UXML");
            Debug.Assert(_tabOverviewContent != null, $"[CharSelectManager] Element '{kTabOverviewContent}' not found in UXML");
            Debug.Assert(_tabAbilitiesContent != null, $"[CharSelectManager] Element '{kTabAbilitiesContent}' not found in UXML");
            Debug.Assert(_tabLoreContent != null, $"[CharSelectManager] Element '{kTabLoreContent}' not found in UXML");
            Debug.Assert(_embarkProgressRing != null, $"[CharSelectManager] Element '{kEmbarkProgressRing}' not found in UXML");
            Debug.Assert(_toastContainer != null, $"[CharSelectManager] Element '{kToastContainer}' not found in UXML");
            Debug.Assert(_toastMessage != null, $"[CharSelectManager] Element '{kToastMessage}' not found in UXML");
            Debug.Assert(_btnToastRetry != null, $"[CharSelectManager] Element '{kBtnToastRetry}' not found in UXML");
            Debug.Assert(_btnToastBack != null, $"[CharSelectManager] Element '{kBtnToastBack}' not found in UXML");
            Debug.Assert(_skeletonOverlay != null, $"[CharSelectManager] Element '{kSkeletonOverlay}' not found in UXML");
        }

        /// <summary>
        /// Initializes HeroThemeTransitioner, veil-pulse-flash element, and embark button breathing.
        /// Called after CacheUIReferences when all UI elements are cached.
        /// </summary>
        private void InitVisualSystems()
        {
            // Initialize HeroThemeTransitioner with root VisualElement
            if (_themeTransitioner != null && _root != null)
            {
                _themeTransitioner.Init(_root);
            }

            // Create veil-pulse-flash element for hero switch animation
            if (_root != null)
            {
                var veilPulseFlash = new VisualElement();
                veilPulseFlash.name = "veil-pulse-flash";
                veilPulseFlash.style.position = Position.Absolute;
                veilPulseFlash.style.width = Length.Percent(100);
                veilPulseFlash.style.height = Length.Percent(100);
                veilPulseFlash.style.opacity = 0f;
                veilPulseFlash.pickingMode = PickingMode.Ignore;
                veilPulseFlash.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
                _root.Add(veilPulseFlash);
            }

            // Initialize embark cinematic with UI references
            if (_embarkCinematic != null && _root != null)
            {
                var heroStage = _root.Q<VisualElement>("hero-stage");
                var carousel = _root.Q<VisualElement>("carousel-strip")?.parent
                               ?? _root.Q<VisualElement>("carousel-container");
                _embarkCinematic.Init(_root, heroStage, _infoPanelContainer, carousel, _themeTransitioner);
            }

            // Embark button breathing glow (VISUAL-08)
            if (_btnEmbark != null)
            {
                ButtonVFXHelper.AddBreathing(_btnEmbark, 0.012f, 2500f);
            }
        }

        private void BindUI()
        {
            _btnPrev?.RegisterCallback<ClickEvent>(OnPrevClicked);
            _btnNext?.RegisterCallback<ClickEvent>(OnNextClicked);
            _btnBack?.RegisterCallback<ClickEvent>(OnBackClicked);
            _btnEmbark?.RegisterCallback<ClickEvent>(OnEmbarkClicked);
            _tabBtnOverview?.RegisterCallback<ClickEvent>(OnTabOverviewClicked);
            _tabBtnAbilities?.RegisterCallback<ClickEvent>(OnTabAbilitiesClicked);
            _tabBtnLore?.RegisterCallback<ClickEvent>(OnTabLoreClicked);
            _btnToastRetry?.RegisterCallback<ClickEvent>(OnToastRetryClicked);
            _btnToastBack?.RegisterCallback<ClickEvent>(OnToastBackClicked);
            _root?.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
            _root?.RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
            _root?.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);
        }

        private void UnbindUI()
        {
            _btnPrev?.UnregisterCallback<ClickEvent>(OnPrevClicked);
            _btnNext?.UnregisterCallback<ClickEvent>(OnNextClicked);
            _btnBack?.UnregisterCallback<ClickEvent>(OnBackClicked);
            _btnEmbark?.UnregisterCallback<ClickEvent>(OnEmbarkClicked);
            _tabBtnOverview?.UnregisterCallback<ClickEvent>(OnTabOverviewClicked);
            _tabBtnAbilities?.UnregisterCallback<ClickEvent>(OnTabAbilitiesClicked);
            _tabBtnLore?.UnregisterCallback<ClickEvent>(OnTabLoreClicked);
            _btnToastRetry?.UnregisterCallback<ClickEvent>(OnToastRetryClicked);
            _btnToastBack?.UnregisterCallback<ClickEvent>(OnToastBackClicked);
            _root?.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove);
            _root?.UnregisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
            _root?.UnregisterCallback<NavigationCancelEvent>(OnNavigationCancel);
        }

        // =============================================================================
        // INITIAL STATE
        // =============================================================================

        private void ApplyInitialState()
        {
            _currentIndex = 0;
            _isTransitioning = false;
            SwitchTab(kTabOverview);
            DismissToast();
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

        /// <summary>Navigates to the hero at the specified index with transition guard.</summary>
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
            StartCoroutine(EndTransitionAfterDelay());
        }

        private IEnumerator EndTransitionAfterDelay()
        {
            yield return kTransitionWait;
            _isTransitioning = false;
        }

        /// <summary>Navigates to the previous hero (wraps around).</summary>
        public void NavigatePrev()
        {
            if (_heroList == null || _heroList.Count == 0) return;
            int newIndex = _currentIndex - 1;
            if (newIndex < 0) newIndex = _heroList.Count - 1;
            NavigateToHero(newIndex);
        }

        /// <summary>Navigates to the next hero (wraps around).</summary>
        public void NavigateNext()
        {
            if (_heroList == null || _heroList.Count == 0) return;
            NavigateToHero((_currentIndex + 1) % _heroList.Count);
        }

        // =============================================================================
        // THEME MANAGEMENT
        // =============================================================================

        private void ApplyThemeClass(string heroId)
        {
            if (_root == null) return;
            if (!string.IsNullOrEmpty(_currentThemeClass)) _root.RemoveFromClassList(_currentThemeClass);
            _currentThemeClass = $"theme-{heroId}";
            _root.AddToClassList(_currentThemeClass);
        }

        // =============================================================================
        // TAB MANAGEMENT
        // =============================================================================

        private void SwitchTab(int tabIndex)
        {
            if (tabIndex == _currentTab && tabIndex != kTabOverview) return;
            _currentTab = tabIndex;
            SetTabButtonActive(_tabBtnOverview, tabIndex == kTabOverview);
            SetTabButtonActive(_tabBtnAbilities, tabIndex == kTabAbilities);
            SetTabButtonActive(_tabBtnLore, tabIndex == kTabLore);
            SetTabContentActive(_tabOverviewContent, tabIndex == kTabOverview);
            SetTabContentActive(_tabAbilitiesContent, tabIndex == kTabAbilities);
            SetTabContentActive(_tabLoreContent, tabIndex == kTabLore);
            CharSelectEvents.RaiseTabChanged(tabIndex);
        }

        private static void SetTabButtonActive(Button btn, bool active)
        {
            if (btn == null) return;
            if (active) btn.AddToClassList("tab-btn-active");
            else btn.RemoveFromClassList("tab-btn-active");
        }

        private static void SetTabContentActive(VisualElement content, bool active)
        {
            if (content == null) return;
            if (active) content.AddToClassList("tab-active");
            else content.RemoveFromClassList("tab-active");
        }

        // =============================================================================
        // LOADING STATE
        // =============================================================================

        private void ShowSkeletonLoading()
        {
            if (_skeletonOverlay == null && _uiDocument != null)
                _skeletonOverlay = _uiDocument.rootVisualElement?.Q<VisualElement>(kSkeletonOverlay);
            _skeletonOverlay?.RemoveFromClassList("hidden");
            CharSelectEvents.RaiseLoadingStarted();
        }

        private void HideSkeletonLoading()
        {
            if (_skeletonOverlay == null && _uiDocument != null)
                _skeletonOverlay = _uiDocument.rootVisualElement?.Q<VisualElement>(kSkeletonOverlay);
            _skeletonOverlay?.AddToClassList("hidden");
            CharSelectEvents.RaiseLoadingComplete();
        }

        // =============================================================================
        // EMBARK FLOW (async/await with timeout)
        // =============================================================================

        private void UpdateEmbarkText()
        {
            var hero = CurrentHero;
            if (hero == null) return;
            string name = string.IsNullOrEmpty(hero.display_name) ? (hero.hero_id?.ToUpper() ?? "UNKNOWN") : hero.display_name.ToUpper();
            if (_embarkText != null) _embarkText.text = $"EMBARK AS {name}";
            _embarkGlow?.AddToClassList("breathing");
        }

        /// <summary>
        /// Initiates the async embark sequence with save creation and scene transition.
        /// Uses Awaitable with destroyCancellationToken for lifecycle safety.
        /// </summary>
        private async void TriggerEmbark()
        {
            if (_isEmbarking) return;
            var hero = CurrentHero;
            if (hero == null) return;
            _isEmbarking = true;
            // NOTE: Do NOT call RaiseEmbarkTriggered() here — TriggerEmbark IS the handler
            // for OnEmbarkTriggered (subscribed in OnEnable). Raising it would re-invoke this method.
            CharSelectEvents.RaiseScreenExiting();
            try
            {
                await ExecuteEmbarkAsync(hero);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[CharSelectManager] Embark cancelled (destroyed or timed out).");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CharSelectManager] Embark failed: {ex.Message}");
                ShowErrorToast($"Embark failed: {ex.Message}");
                _isEmbarking = false;
            }
        }

        private async Awaitable ExecuteEmbarkAsync(HeroData hero)
        {
            // Play embark cinematic before save/load (if available)
            if (_embarkCinematic != null && _themeTransitioner != null)
            {
                var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

                void OnComplete()
                {
                    _embarkCinematic.OnCinematicComplete -= OnComplete;
                    tcs.TrySetResult(true);
                }
                _embarkCinematic.OnCinematicComplete += OnComplete;

                var theme = _themeTransitioner.GetCurrentTheme();
                if (theme != null)
                {
                    string cinematicName = string.IsNullOrEmpty(hero.display_name) ? hero.hero_id?.ToUpper() ?? "UNKNOWN" : hero.display_name.ToUpper();
                    string heroTitle = hero.title ?? "";
                    _embarkCinematic.PlayEmbarkCinematic(theme, cinematicName, heroTitle);

                    // Await cinematic completion (~1.2s)
                    await tcs.Task;
                    // Re-sync to main thread (await Task can resume on thread pool)
                    await Awaitable.MainThreadAsync();
                }
                else
                {
                    _embarkCinematic.OnCinematicComplete -= OnComplete;
                }
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(kSaveTimeout));
            var token = cts.Token;
            if (!SaveManager.HasInstance)
            {
                ShowErrorToast("Save system unavailable. Please restart.");
                _isEmbarking = false;
                return;
            }
            var saveManager = SaveManager.Instance;
            int slot = await saveManager.GetBestNewGameSlotAsync();
            token.ThrowIfCancellationRequested();
            string heroName = string.IsNullOrEmpty(hero.display_name) ? hero.hero_id : hero.display_name;
            bool created = await saveManager.CreateNewSaveAsync(slot, hero.hero_id, heroName, hero.GetPrimaryPath());
            token.ThrowIfCancellationRequested();
            if (!created)
            {
                ShowErrorToast("Failed to create save file. Please try again.");
                _isEmbarking = false;
                return;
            }
            saveManager.SetCurrentLocation(kStarterTownLocation);
            await saveManager.SaveAsync(slot);
            token.ThrowIfCancellationRequested();
            if (ScreenTransition.HasInstance)
                ScreenTransition.Instance.Transition(() => SceneManager.LoadScene(kGameScene));
            else
                SceneManager.LoadScene(kGameScene);
        }

        // =============================================================================
        // TOAST NOTIFICATION
        // =============================================================================

        private void ShowErrorToast(string message)
        {
            CharSelectUIUtils.SetLabel(_toastMessage, message);
            _toastContainer?.RemoveFromClassList("toast-hidden");
            _toastContainer?.AddToClassList("toast-visible");
            CharSelectEvents.RaiseErrorOccurred(message);
        }

        private void DismissToast()
        {
            _toastContainer?.RemoveFromClassList("toast-visible");
            _toastContainer?.AddToClassList("toast-hidden");
        }

        // =============================================================================
        // UI EVENT HANDLERS
        // =============================================================================

        private void OnPrevClicked(ClickEvent evt) => NavigatePrev();
        private void OnNextClicked(ClickEvent evt) => NavigateNext();
        private void OnBackClicked(ClickEvent evt) => NavigateBack();
        private void OnEmbarkClicked(ClickEvent evt) => TriggerEmbark();

        private void NavigateBack()
        {
            CharSelectEvents.RaiseScreenExiting();
            if (ScreenTransition.HasInstance)
                ScreenTransition.Instance.Transition(() => SceneManager.LoadScene(kMainMenuScene));
            else
                SceneManager.LoadScene(kMainMenuScene);
        }

        private void OnTabOverviewClicked(ClickEvent evt) => SwitchTab(kTabOverview);
        private void OnTabAbilitiesClicked(ClickEvent evt) => SwitchTab(kTabAbilities);
        private void OnTabLoreClicked(ClickEvent evt) => SwitchTab(kTabLore);
        private void OnToastRetryClicked(ClickEvent evt) { DismissToast(); _isEmbarking = false; TriggerEmbark(); }
        private void OnToastBackClicked(ClickEvent evt) { DismissToast(); NavigateBack(); }

        // =============================================================================
        // NAVIGATION EVENTS (KEYBOARD / GAMEPAD)
        // =============================================================================

        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            switch (evt.direction)
            {
                case NavigationMoveEvent.Direction.Left:
                    NavigatePrev();
                    evt.StopPropagation();
                    break;
                case NavigationMoveEvent.Direction.Right:
                    NavigateNext();
                    evt.StopPropagation();
                    break;
                // Up/Down reserved for Plan 03 CharSelectFocusManager zone navigation
            }
        }

        private void OnNavigationSubmit(NavigationSubmitEvent evt) { TriggerEmbark(); evt.StopPropagation(); }
        private void OnNavigationCancel(NavigationCancelEvent evt) { NavigateBack(); evt.StopPropagation(); }

        // =============================================================================
        // ANIMATION HINTS
        // =============================================================================

        /// <summary>
        /// Sets UsageHints.DynamicTransform on all VisualElements that have USS
        /// transitions or hover scale/translate effects. Called once during init.
        /// </summary>
        private void SetAnimationHints()
        {
            SetHint(_btnBack);
            SetHint(_btnEmbark);
            var embarkHexBg = _root.Q<VisualElement>(kEmbarkHexBg);
            Debug.Assert(embarkHexBg != null, $"[CharSelectManager] Missing element: {kEmbarkHexBg}");
            SetHint(embarkHexBg);
            SetHint(_embarkGlow);
            SetHint(_btnPrev);
            SetHint(_btnNext);
            SetHint(_embarkText);
            SetHint(_infoPanelContainer);
            SetHint(_tabOverviewContent);
            SetHint(_tabAbilitiesContent);
            SetHint(_tabLoreContent);
            SetHint(_embarkProgressRing);
            SetHint(_toastContainer);
            _root.Query<VisualElement>(className: "ability-slot").ForEach(SetHint);
            _root.Query<VisualElement>(className: "hero-card").ForEach(SetHint);
            _root.Query<VisualElement>(className: "nav-arrow").ForEach(SetHint);
            _root.Query<VisualElement>(className: "veil-panel").ForEach(SetHint);
            _root.Query<VisualElement>(className: "fog-particles").ForEach(SetHint);
        }

        private static void SetHint(VisualElement el)
        {
            if (el != null) el.usageHints |= UsageHints.DynamicTransform;
        }
    }
}
