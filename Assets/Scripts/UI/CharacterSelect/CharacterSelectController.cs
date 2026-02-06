using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.Data;
using VeilBreakers.UI.Core;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Main coordinator for the BG3-inspired character select screen.
    /// Loads hero data from GameDatabase, manages selection state,
    /// coordinates sub-controllers, and handles input/navigation.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CharacterSelectController : MonoBehaviour
    {
        // =============================================================================
        // CONSTANTS
        // =============================================================================

        private const string kMainMenuScene = "TitleScreen";
        private const string kGameScene = "MainGame";
        private const int kMaxStats = 20; // Max D&D stat value for bar display
        private const int kMaxAbilities = 3;
        private const string kSelectedHeroPref = "SelectedHero";

        // =============================================================================
        // SERIALIZED FIELDS
        // =============================================================================

        [Header("Audio")]
        [SerializeField] private AudioClip _switchSFX;
        [SerializeField] private AudioClip _confirmSFX;
        [SerializeField] private AudioClip _cancelSFX;

        // =============================================================================
        // CACHED UI ELEMENTS
        // =============================================================================

        private VisualElement _root;

        // Info panel
        private Label _heroName;
        private Label _heroTitle;
        private Label _heroQuote;
        private Label _heroPath;
        private Label _heroRole;
        private Label _heroResource;

        // Monster info
        private VisualElement _monsterThumbnail;
        private Label _monsterName;
        private Label _monsterBrand;

        // Stats panel
        private VisualElement _statsPanel;
        private readonly VisualElement[] _statFills = new VisualElement[6];
        private readonly Label[] _statValues = new Label[6];
        private readonly Label[] _abilityNames = new Label[kMaxAbilities];
        private readonly Label[] _abilityDescs = new Label[kMaxAbilities];

        // Carousel
        private VisualElement _carouselTrack;
        private readonly List<VisualElement> _carouselSlots = new List<VisualElement>(4);

        // Buttons
        private Button _btnEmbark;
        private Label _embarkLabel;
        private Button _btnBack;
        private Button _btnNeutralToggle;

        // Overlays
        private VisualElement _screenFade;
        private VisualElement _vfxOverlay;
        private VisualElement _heroStageRender;

        // Environment
        private VisualElement _envLayerColor;

        // =============================================================================
        // STATE
        // =============================================================================

        private List<HeroData> _heroes;
        private int _currentIndex;
        private bool _isAnimating;
        private bool _isDestroyed;
        private bool _statsVisible;
        private Coroutine _switchCoroutine;
        private Coroutine _statsCoroutine;
        private Coroutine _quoteCoroutine;
        private Camera _cachedCamera;

        // Sub-controllers (added in later phases)
        private HeroStageController _heroStage;
        private EnvironmentController _environment;
        private VeilTearTransition _veilTear;
        private HeroVFXController _heroVFX;
        private Action<HeroData> _subControllerHandler;

        // =============================================================================
        // CACHED ALLOCATIONS
        // =============================================================================

        private static readonly WaitForSeconds Wait01 = new WaitForSeconds(0.1f);
        private static readonly WaitForSeconds Wait02 = new WaitForSeconds(0.2f);
        private static readonly WaitForSeconds Wait03 = new WaitForSeconds(0.3f);
        private static readonly WaitForSeconds Wait05 = new WaitForSeconds(0.5f);
        private static readonly WaitForSeconds Wait10 = new WaitForSeconds(1.0f);

        // Pre-baked number strings to avoid int.ToString() allocations
        private static readonly string[] NumberStrings = new string[21];

        static CharacterSelectController()
        {
            for (int i = 0; i <= 20; i++)
                NumberStrings[i] = i.ToString();
        }

        // Event handler cache for proper unregistration (sized dynamically to match carousel slots)
        private EventCallback<ClickEvent>[] _slotClickHandlers;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public HeroData CurrentHero => _heroes != null && _currentIndex >= 0 && _currentIndex < _heroes.Count
            ? _heroes[_currentIndex] : null;

        public int CurrentIndex => _currentIndex;
        public bool IsAnimating => _isAnimating;

        /// <summary>
        /// Fired when the selected hero changes. Passes the new HeroData.
        /// Sub-controllers subscribe to this.
        /// </summary>
        public event Action<HeroData> OnHeroChanged;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Start()
        {
            StartCoroutine(InitializeWhenReady());
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            if (_subControllerHandler != null) OnHeroChanged -= _subControllerHandler;
            UnsubscribeInput();
            CleanupEventHandlers();
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private IEnumerator InitializeWhenReady()
        {
            // Wait for GameDatabase to finish loading
            while (GameDatabase.Instance == null || !GameDatabase.Instance.IsLoaded)
            {
                yield return null;
            }

            _heroes = GameDatabase.Instance.GetAllHeroes();
            if (_heroes == null || _heroes.Count == 0)
            {
                Debug.LogError("[CharacterSelect] No heroes loaded from GameDatabase!");
                yield break;
            }

            // Sort by canonical order: vex, seraphina, orion, nyx
            _heroes.Sort((a, b) => GetHeroSortOrder(a.hero_id).CompareTo(GetHeroSortOrder(b.hero_id)));

            CacheUIElements();
            SetupEventHandlers();
            SubscribeInput();
            InitializeSubControllers();

            // Check for previously selected hero
            string lastSelected = PlayerPrefs.GetString(kSelectedHeroPref, "");
            _currentIndex = 0;
            if (!string.IsNullOrEmpty(lastSelected))
            {
                for (int i = 0; i < _heroes.Count; i++)
                {
                    if (_heroes[i].hero_id == lastSelected)
                    {
                        _currentIndex = i;
                        break;
                    }
                }
            }

            // Apply initial hero (no transition)
            var initialHero = _heroes[_currentIndex];
            ApplyHeroData(initialHero);
            UpdateCarouselSelection(_currentIndex);

            // Trigger sub-controllers for initial hero
            OnHeroChanged?.Invoke(initialHero);

            // Entrance animation
            yield return StartCoroutine(EntranceSequence());
        }

        private void CacheUIElements()
        {
            var doc = GetComponent<UIDocument>();
            _root = doc.rootVisualElement;

            // Info panel
            _heroName = _root.Q<Label>("hero-name");
            _heroTitle = _root.Q<Label>("hero-title");
            _heroQuote = _root.Q<Label>("hero-quote");
            _heroPath = _root.Q<Label>("hero-path");
            _heroRole = _root.Q<Label>("hero-role");
            _heroResource = _root.Q<Label>("hero-resource");

            // Monster info
            _monsterThumbnail = _root.Q("monster-thumbnail");
            _monsterName = _root.Q<Label>("monster-name");
            _monsterBrand = _root.Q<Label>("monster-brand");

            // Stats panel
            _statsPanel = _root.Q("stats-panel");
            string[] statKeys = { "str", "dex", "con", "int", "wis", "cha" };
            for (int i = 0; i < statKeys.Length; i++)
            {
                _statFills[i] = _root.Q($"stat-fill-{statKeys[i]}");
                _statValues[i] = _root.Q<Label>($"stat-val-{statKeys[i]}");
            }
            for (int i = 0; i < kMaxAbilities; i++)
            {
                _abilityNames[i] = _root.Q<Label>($"ability-name-{i}");
                _abilityDescs[i] = _root.Q<Label>($"ability-desc-{i}");
            }

            // Carousel — discover slots dynamically
            _carouselTrack = _root.Q("carousel-track");
            for (int i = 0; ; i++)
            {
                var slot = _root.Q($"carousel-slot-{i}");
                if (slot == null) break;
                _carouselSlots.Add(slot);
            }
            _slotClickHandlers = new EventCallback<ClickEvent>[_carouselSlots.Count];

            // Buttons
            _btnEmbark = _root.Q<Button>("btn-embark");
            _embarkLabel = _root.Q<Label>("embark-label");
            _btnBack = _root.Q<Button>("btn-back");
            _btnNeutralToggle = _root.Q<Button>("btn-neutral-toggle");

            // Overlays
            _screenFade = _root.Q("screen-fade");
            _vfxOverlay = _root.Q("vfx-overlay");
            _heroStageRender = _root.Q("hero-stage-render");
            _envLayerColor = _root.Q("env-layer-color");
        }

        private void SetupEventHandlers()
        {
            _btnBack?.RegisterCallback<ClickEvent>(OnBackClicked);
            _btnEmbark?.RegisterCallback<ClickEvent>(OnEmbarkClicked);
            _btnNeutralToggle?.RegisterCallback<ClickEvent>(OnNeutralToggleClicked);

            // Carousel slot click handlers (cached for unregistration)
            for (int i = 0; i < _carouselSlots.Count; i++)
            {
                int index = i; // Capture for closure
                _slotClickHandlers[i] = evt => SelectHeroByIndex(index);
                _carouselSlots[i].RegisterCallback(_slotClickHandlers[i]);
            }

            // Tab key for stats panel toggle
            _root?.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void CleanupEventHandlers()
        {
            if (_root == null) return;

            try
            {
                _btnBack?.UnregisterCallback<ClickEvent>(OnBackClicked);
                _btnEmbark?.UnregisterCallback<ClickEvent>(OnEmbarkClicked);
                _btnNeutralToggle?.UnregisterCallback<ClickEvent>(OnNeutralToggleClicked);

                if (_slotClickHandlers != null)
                {
                    for (int i = 0; i < _carouselSlots.Count && i < _slotClickHandlers.Length; i++)
                    {
                        if (_slotClickHandlers[i] != null)
                            _carouselSlots[i].UnregisterCallback(_slotClickHandlers[i]);
                    }
                }

                _root.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CharacterSelect] Cleanup error: {ex.Message}");
            }
        }

        private void InitializeSubControllers()
        {
            // Add sub-controller components
            _heroStage = gameObject.AddComponent<HeroStageController>();
            _heroStage.Initialize(_heroStageRender);

            _environment = gameObject.AddComponent<EnvironmentController>();
            _environment.Initialize(_root);

            _veilTear = gameObject.AddComponent<VeilTearTransition>();
            _veilTear.Initialize(_root);

            _heroVFX = gameObject.AddComponent<HeroVFXController>();
            _heroVFX.Initialize(_vfxOverlay);

            // Wire OnHeroChanged to sub-controllers (cached for unsubscription)
            _subControllerHandler = hero =>
            {
                _heroStage?.ShowHero(hero);
                _environment?.SetHeroEnvironment(hero);
                _heroVFX?.SetHero(hero);
            };
            OnHeroChanged += _subControllerHandler;
        }

        // =============================================================================
        // INPUT
        // =============================================================================

        private void SubscribeInput()
        {
            if (InputManager.Instance != null)
                InputManager.Instance.OnActionTriggered += OnInputAction;
        }

        private void UnsubscribeInput()
        {
            if (InputManager.Instance != null)
                InputManager.Instance.OnActionTriggered -= OnInputAction;
        }

        private void OnInputAction(InputManager.GameAction action)
        {
            if (_isAnimating || _isDestroyed) return;

            switch (action)
            {
                case InputManager.GameAction.MoveLeft:
                    SelectPreviousHero();
                    break;
                case InputManager.GameAction.MoveRight:
                    SelectNextHero();
                    break;
                case InputManager.GameAction.Confirm:
                    OnEmbark();
                    break;
                case InputManager.GameAction.Cancel:
                    OnBack();
                    break;
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (_isAnimating || _isDestroyed) return;

            switch (evt.keyCode)
            {
                case KeyCode.Tab:
                    ToggleStatsPanel();
                    evt.StopPropagation();
                    break;
            }
        }

        // =============================================================================
        // HERO SELECTION
        // =============================================================================

        public void SelectHeroByIndex(int index)
        {
            if (_isAnimating || _isDestroyed) return;
            if (_heroes == null || index < 0 || index >= _heroes.Count) return;
            if (index == _currentIndex) return;

            int prevIndex = _currentIndex;
            _currentIndex = index;

            // Cancel any in-progress switch
            if (_switchCoroutine != null) StopCoroutine(_switchCoroutine);
            _switchCoroutine = StartCoroutine(HeroSwitchSequence(prevIndex, _currentIndex));
        }

        private void SelectPreviousHero()
        {
            if (_heroes == null || _heroes.Count == 0) return;
            int newIndex = (_currentIndex - 1 + _heroes.Count) % _heroes.Count;
            SelectHeroByIndex(newIndex);
        }

        private void SelectNextHero()
        {
            if (_heroes == null || _heroes.Count == 0) return;
            int newIndex = (_currentIndex + 1) % _heroes.Count;
            SelectHeroByIndex(newIndex);
        }

        // =============================================================================
        // HERO SWITCH SEQUENCE
        // =============================================================================

        private IEnumerator HeroSwitchSequence(int fromIndex, int toIndex)
        {
            _isAnimating = true;
            var hero = _heroes[toIndex];

            PlaySFX(_switchSFX, 0.5f);

            // Cancel quote animation if running
            if (_quoteCoroutine != null) StopCoroutine(_quoteCoroutine);
            if (_heroQuote != null) _heroQuote.style.opacity = 0;

            // Trigger veil tear transition (if available)
            Color heroColor = hero.color_palette != null ? hero.color_palette.ToColor() : Color.white;
            _veilTear?.Play(heroColor);

            // Brief pause for transition flash
            yield return Wait02;

            if (_isDestroyed) yield break;

            // Update all data
            ApplyHeroData(hero);
            UpdateCarouselSelection(toIndex);

            // Notify sub-controllers
            OnHeroChanged?.Invoke(hero);

            // Animate stats if panel is visible
            if (_statsVisible)
            {
                if (_statsCoroutine != null) StopCoroutine(_statsCoroutine);
                _statsCoroutine = StartCoroutine(AnimateStats(hero));
            }

            // Delayed quote fade-in
            yield return Wait03;
            if (!_isDestroyed)
            {
                _quoteCoroutine = StartCoroutine(FadeInQuote());
            }

            yield return Wait02;
            _isAnimating = false;
        }

        // =============================================================================
        // DATA APPLICATION
        // =============================================================================

        private void ApplyHeroData(HeroData hero)
        {
            if (hero == null) return;

            // Identity
            if (_heroName != null) _heroName.text = hero.display_name?.ToUpper() ?? "";
            if (_heroTitle != null) _heroTitle.text = hero.title?.ToUpper() ?? "";
            if (_heroQuote != null)
            {
                _heroQuote.text = hero.quote ?? "";
                _heroQuote.style.opacity = 0; // Will fade in
            }

            // Details
            if (_heroPath != null) _heroPath.text = hero.GetPrimaryPath().ToString();
            if (_heroRole != null) _heroRole.text = hero.role?.ToUpper() ?? "";
            if (_heroResource != null) _heroResource.text = hero.resource_type?.ToUpper() ?? "";

            // Monster info
            ApplyMonsterData(hero.starter_monster_id);

            // Embark button
            if (_embarkLabel != null) _embarkLabel.text = $"EMBARK AS {hero.display_name?.ToUpper()}";

            // Environment color
            ApplyHeroColor(hero);

            // Stats (immediate, no animation)
            ApplyStatsImmediate(hero);

            // Abilities
            ApplyAbilities(hero);
        }

        private void ApplyMonsterData(string monsterId)
        {
            if (string.IsNullOrEmpty(monsterId)) return;

            var monster = GameDatabase.Instance?.GetMonster(monsterId);
            if (monster == null) return;

            if (_monsterName != null) _monsterName.text = monster.display_name ?? "";
            if (_monsterBrand != null) _monsterBrand.text = monster.GetPrimaryBrand().ToString();
        }

        private void ApplyHeroColor(HeroData hero)
        {
            Color color = hero.color_palette != null ? hero.color_palette.ToColor() : Color.white;

            // Tint the environment color overlay
            if (_envLayerColor != null)
            {
                _envLayerColor.style.backgroundColor = new Color(color.r, color.g, color.b, 0.15f);
            }
        }

        private void ApplyStatsImmediate(HeroData hero)
        {
            if (hero.base_stats == null) return;

            int[] stats = {
                hero.base_stats.strength,
                hero.base_stats.dexterity,
                hero.base_stats.constitution,
                hero.base_stats.intelligence,
                hero.base_stats.wisdom,
                hero.base_stats.charisma
            };

            for (int i = 0; i < 6; i++)
            {
                int val = Mathf.Clamp(stats[i], 0, kMaxStats);
                float pct = val / (float)kMaxStats * 100f;

                if (_statFills[i] != null)
                    _statFills[i].style.width = new StyleLength(new Length(pct, LengthUnit.Percent));
                if (_statValues[i] != null)
                    _statValues[i].text = val <= 20 ? NumberStrings[val] : val.ToString();
            }
        }

        private void ApplyAbilities(HeroData hero)
        {
            var skills = GameDatabase.Instance?.GetHeroInnateSkills(hero);
            for (int i = 0; i < kMaxAbilities; i++)
            {
                if (skills != null && i < skills.Count && skills[i] != null)
                {
                    if (_abilityNames[i] != null) _abilityNames[i].text = FormatSkillName(skills[i].display_name);
                    if (_abilityDescs[i] != null) _abilityDescs[i].text = skills[i].description ?? "";
                }
                else
                {
                    if (_abilityNames[i] != null) _abilityNames[i].text = "";
                    if (_abilityDescs[i] != null) _abilityDescs[i].text = "";
                }
            }
        }

        // =============================================================================
        // CAROUSEL
        // =============================================================================

        private void UpdateCarouselSelection(int activeIndex)
        {
            if (activeIndex < 0 || activeIndex >= _carouselSlots.Count) return;

            for (int i = 0; i < _carouselSlots.Count; i++)
            {
                if (i == activeIndex)
                {
                    _carouselSlots[i].AddToClassList("selected");
                    // Punch animation
                    var anim = UIAnimationController.Instance;
                    if (anim != null) anim.PunchScale(_carouselSlots[i], 1.08f, 0.25f);
                }
                else
                {
                    _carouselSlots[i].RemoveFromClassList("selected");
                }
            }
        }

        // =============================================================================
        // STATS PANEL
        // =============================================================================

        private void ToggleStatsPanel()
        {
            _statsVisible = !_statsVisible;

            if (_statsPanel == null) return;

            if (_statsVisible)
            {
                _statsPanel.RemoveFromClassList("hidden");
                _statsPanel.AddToClassList("visible");
                var hero = CurrentHero;
                if (hero != null)
                {
                    if (_statsCoroutine != null) StopCoroutine(_statsCoroutine);
                    _statsCoroutine = StartCoroutine(AnimateStats(hero));
                }
            }
            else
            {
                _statsPanel.RemoveFromClassList("visible");
                _statsPanel.AddToClassList("hidden");
            }
        }

        private IEnumerator AnimateStats(HeroData hero)
        {
            if (hero?.base_stats == null) yield break;

            int[] stats = {
                hero.base_stats.strength,
                hero.base_stats.dexterity,
                hero.base_stats.constitution,
                hero.base_stats.intelligence,
                hero.base_stats.wisdom,
                hero.base_stats.charisma
            };

            float duration = 0.6f;
            float elapsed = 0f;

            // Reset all fills to 0
            for (int i = 0; i < 6; i++)
            {
                if (_statFills[i] != null)
                    _statFills[i].style.width = new StyleLength(new Length(0, LengthUnit.Percent));
                if (_statValues[i] != null)
                    _statValues[i].text = NumberStrings[0];
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutBack(t);

                for (int i = 0; i < 6; i++)
                {
                    int val = Mathf.Clamp(stats[i], 0, kMaxStats);
                    float pct = val * eased / kMaxStats * 100f;
                    int displayVal = Mathf.RoundToInt(val * Mathf.Clamp01(t));

                    if (_statFills[i] != null)
                        _statFills[i].style.width = new StyleLength(new Length(pct, LengthUnit.Percent));
                    if (_statValues[i] != null)
                        _statValues[i].text = displayVal <= 20 ? NumberStrings[displayVal] : displayVal.ToString();
                }

                yield return null;
            }

            // Snap to final values
            ApplyStatsImmediate(hero);
        }

        // =============================================================================
        // NAVIGATION
        // =============================================================================

        private void OnBackClicked(ClickEvent evt) => OnBack();
        private void OnEmbarkClicked(ClickEvent evt) => OnEmbark();
        private void OnNeutralToggleClicked(ClickEvent evt) => _environment?.ToggleNeutral();

        private void OnBack()
        {
            if (_isAnimating || _isDestroyed) return;

            PlaySFX(_cancelSFX, 0.5f);

            if (ScreenTransition.Instance != null)
            {
                ScreenTransition.Instance.Transition(
                    () => SceneManager.LoadScene(kMainMenuScene)
                );
            }
            else
            {
                StartCoroutine(FadeAndNavigate(kMainMenuScene));
            }
        }

        private void OnEmbark()
        {
            if (_isAnimating || _isDestroyed) return;

            var hero = CurrentHero;
            if (hero == null) return;

            _isAnimating = true;
            PlaySFX(_confirmSFX, 0.7f);

            // Save selection
            GameManager.Instance?.SelectHero(hero.hero_id);
            PlayerPrefs.SetString(kSelectedHeroPref, hero.hero_id);

            // Navigate to game
            if (ScreenTransition.Instance != null)
            {
                ScreenTransition.Instance.Transition(
                    () => SceneManager.LoadScene(kGameScene)
                );
            }
            else
            {
                StartCoroutine(FadeAndNavigate(kGameScene));
            }
        }

        private IEnumerator FadeAndNavigate(string sceneName)
        {
            _screenFade?.RemoveFromClassList("hidden");
            _screenFade?.AddToClassList("active");

            yield return Wait10;

            if (!_isDestroyed && !string.IsNullOrEmpty(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        // =============================================================================
        // ANIMATIONS
        // =============================================================================

        private IEnumerator EntranceSequence()
        {
            // Start with screen fade active
            _screenFade?.AddToClassList("active");

            yield return Wait02;
            if (_isDestroyed) yield break;

            // Fade screen in
            _screenFade?.RemoveFromClassList("active");

            yield return Wait03;
            if (_isDestroyed) yield break;

            // Stagger in UI elements
            var anim = UIAnimationController.Instance;
            if (anim != null)
            {
                var infoPanel = _root?.Q("info-panel");
                if (infoPanel != null) anim.FadeSlideIn(infoPanel, UIAnimationController.SlideDirection.Left, duration: 0.4f);

                yield return Wait02;
                if (_isDestroyed) yield break;

                var carousel = _root?.Q("carousel-strip");
                if (carousel != null) anim.FadeSlideIn(carousel, UIAnimationController.SlideDirection.Down, duration: 0.4f);

                yield return Wait01;
                if (_isDestroyed) yield break;

                if (_btnEmbark != null) anim.FadeIn(_btnEmbark, 0.3f);
            }

            // Quote fade-in
            yield return Wait03;
            if (!_isDestroyed)
            {
                _quoteCoroutine = StartCoroutine(FadeInQuote());
            }
        }

        private IEnumerator FadeInQuote()
        {
            if (_heroQuote == null) yield break;

            float duration = 0.8f;
            float elapsed = 0f;
            _heroQuote.style.opacity = 0;

            while (elapsed < duration)
            {
                if (_isDestroyed) yield break;
                elapsed += Time.unscaledDeltaTime;
                _heroQuote.style.opacity = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            _heroQuote.style.opacity = 1;
        }

        // =============================================================================
        // UTILITY
        // =============================================================================

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private static int GetHeroSortOrder(string heroId)
        {
            if (string.Equals(heroId, "vex", StringComparison.OrdinalIgnoreCase)) return 0;
            if (string.Equals(heroId, "seraphina", StringComparison.OrdinalIgnoreCase)) return 1;
            if (string.Equals(heroId, "orion", StringComparison.OrdinalIgnoreCase)) return 2;
            if (string.Equals(heroId, "nyx", StringComparison.OrdinalIgnoreCase)) return 3;
            return 99;
        }

        private static string FormatSkillName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return "";
            // Convert "shadow_strike" -> "Shadow Strike"
            return System.Globalization.CultureInfo.InvariantCulture.TextInfo
                .ToTitleCase(rawName.Replace("_", " "));
        }

        private void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            if (_cachedCamera == null) _cachedCamera = Camera.main;
            if (_cachedCamera != null)
                AudioSource.PlayClipAtPoint(clip, _cachedCamera.transform.position, volume);
        }
    }
}
