using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.Data;

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
        private bool _isInitialized;
        private VisualElement _root;

        // =============================================================================
        // CACHED UI REFERENCES
        // =============================================================================

        private Button _btnPrev;
        private Button _btnNext;
        private Button _btnBack;
        private Button _btnEmbark;
        private Button _btnConfirm;
        private Button _btnCancel;
        private VisualElement _confirmOverlay;
        private Label _embarkText;
        private Label _confirmDescription;
        private VisualElement _embarkGlow;

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
            StartCoroutine(InitializeWhenReady());
        }

        private void OnDisable()
        {
            UnbindUI();
            CharSelectEvents.ClearAll();
        }

        private IEnumerator InitializeWhenReady()
        {
            // Wait for GameDatabase (guard null for quit-time re-enable)
            while (GameDatabase.Instance == null || !GameDatabase.Instance.IsReady)
            {
                yield return null;
            }

            LoadHeroData();

            // Bail if no hero data -- downstream controllers can't function
            if (_heroList == null || _heroList.Count == 0)
            {
                yield break;
            }

            CacheUIReferences();
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
            _root = _uiDocument.rootVisualElement;

            _btnPrev = _root.Q<Button>("btn-prev");
            _btnNext = _root.Q<Button>("btn-next");
            _btnBack = _root.Q<Button>("btn-back");
            _btnEmbark = _root.Q<Button>("btn-embark");
            _btnConfirm = _root.Q<Button>("btn-confirm");
            _btnCancel = _root.Q<Button>("btn-cancel");
            _confirmOverlay = _root.Q<VisualElement>("confirm-overlay");
            _embarkText = _root.Q<Label>("embark-text");
            _confirmDescription = _root.Q<Label>("confirm-description");
            _embarkGlow = _root.Q<VisualElement>("embark-glow");
        }

        private void BindUI()
        {
            _btnPrev?.RegisterCallback<ClickEvent>(OnPrevClicked);
            _btnNext?.RegisterCallback<ClickEvent>(OnNextClicked);
            _btnBack?.RegisterCallback<ClickEvent>(OnBackClicked);
            _btnEmbark?.RegisterCallback<ClickEvent>(OnEmbarkClicked);
            _btnConfirm?.RegisterCallback<ClickEvent>(OnConfirmClicked);
            _btnCancel?.RegisterCallback<ClickEvent>(OnCancelClicked);

            // Keyboard / gamepad navigation
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
            _btnConfirm?.UnregisterCallback<ClickEvent>(OnConfirmClicked);
            _btnCancel?.UnregisterCallback<ClickEvent>(OnCancelClicked);

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
            _confirmOverlay?.AddToClassList("hidden");

            if (_heroList != null && _heroList.Count > 0)
            {
                ApplyThemeClass(_heroList[0].hero_id);
                CharSelectEvents.RaiseHeroChanged(0, _heroList[0], CurrentConfig);
                CharSelectEvents.RaiseHeroDataLoaded(_heroList[0]);
                CharSelectEvents.RaiseHeroSelected();
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
            int prevIndex = _currentIndex;
            _currentIndex = index;

            ApplyThemeClass(_heroList[_currentIndex].hero_id);
            CharSelectEvents.RaiseHeroChanged(_currentIndex, _heroList[_currentIndex], CurrentConfig);
            CharSelectEvents.RaiseHeroDataLoaded(_heroList[_currentIndex]);
            CharSelectEvents.RaiseHeroSelected();
            UpdateEmbarkText();

            // Transition completes after USS animations finish
            StartCoroutine(EndTransitionAfterDelay(0.8f));
        }

        private IEnumerator EndTransitionAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
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

        private static readonly string[] kThemeClasses = { "theme-vex", "theme-seraphina", "theme-orion", "theme-nyx" };

        private void ApplyThemeClass(string heroId)
        {
            if (_root == null) return;

            // Remove all theme classes
            for (int i = 0; i < kThemeClasses.Length; i++)
            {
                _root.RemoveFromClassList(kThemeClasses[i]);
            }

            // Add new theme class
            string themeClass = $"theme-{heroId}";
            _root.AddToClassList(themeClass);
        }

        // =============================================================================
        // EMBARK FLOW
        // =============================================================================

        private void UpdateEmbarkText()
        {
            var hero = CurrentHero;
            if (hero == null) return;

            string name = string.IsNullOrEmpty(hero.display_name) ? hero.hero_id.ToUpper() : hero.display_name.ToUpper();
            if (_embarkText != null) _embarkText.text = $"EMBARK AS {name}";
            if (_confirmDescription != null)
            {
                string title = string.IsNullOrEmpty(hero.title) ? "" : $", {hero.title.ToUpper()}";
                _confirmDescription.text = $"You will begin your journey as {name}{title}";
            }

            // Start breathing glow
            _embarkGlow?.AddToClassList("breathing");
        }

        private void ShowConfirmPopup()
        {
            _confirmOverlay?.RemoveFromClassList("hidden");
            CharSelectEvents.RaiseEmbarkRequested();
        }

        private void HideConfirmPopup()
        {
            _confirmOverlay?.AddToClassList("hidden");
            CharSelectEvents.RaiseEmbarkCancelled();
        }

        private void ExecuteEmbark()
        {
            var hero = CurrentHero;
            if (hero == null) return;

            CharSelectEvents.RaiseEmbarkConfirmed();
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

        private IEnumerator CreateOrRotateNewGameSave(HeroData hero)
        {
            if (!SaveManager.HasInstance || hero == null)
            {
                yield break;
            }

            var saveManager = SaveManager.Instance;
            var slotTask = saveManager.GetBestNewGameSlotAsync();
            while (!slotTask.IsCompleted) yield return null;

            if (slotTask.IsFaulted || slotTask.IsCanceled)
            {
                Debug.LogWarning("[CharSelectManager] Failed to resolve save slot.");
                yield break;
            }

            int slot = slotTask.Result;
            string heroName = string.IsNullOrEmpty(hero.display_name) ? hero.hero_id : hero.display_name;

            var createTask = saveManager.CreateNewSaveAsync(slot, hero.hero_id, heroName, hero.GetPrimaryPath());
            while (!createTask.IsCompleted) yield return null;

            if (createTask.IsFaulted || createTask.IsCanceled || !createTask.Result)
            {
                Debug.LogWarning($"[CharSelectManager] Failed to create save in slot {slot}.");
                yield break;
            }

            saveManager.SetCurrentLocation(kStarterTownLocation);
            var saveTask = saveManager.SaveAsync(slot);
            while (!saveTask.IsCompleted) yield return null;
        }

        // =============================================================================
        // UI EVENT HANDLERS
        // =============================================================================

        private void OnPrevClicked(ClickEvent evt) => NavigatePrev();
        private void OnNextClicked(ClickEvent evt) => NavigateNext();

        private void OnBackClicked(ClickEvent evt)
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

        private void OnEmbarkClicked(ClickEvent evt) => ShowConfirmPopup();
        private void OnConfirmClicked(ClickEvent evt) => ExecuteEmbark();
        private void OnCancelClicked(ClickEvent evt) => HideConfirmPopup();

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
            }
        }

        private void OnNavigationSubmit(NavigationSubmitEvent evt)
        {
            if (_confirmOverlay != null && !_confirmOverlay.ClassListContains("hidden"))
            {
                ExecuteEmbark();
            }
            else
            {
                ShowConfirmPopup();
            }
            evt.StopPropagation();
        }

        private void OnNavigationCancel(NavigationCancelEvent evt)
        {
            if (_confirmOverlay != null && !_confirmOverlay.ClassListContains("hidden"))
            {
                HideConfirmPopup();
            }
            else
            {
                OnBackClicked(null);
            }
            evt.StopPropagation();
        }
    }
}
