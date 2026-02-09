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
    /// Main coordinator for the BG3-inspired character select screen.
    /// Loads hero data from GameDatabase, manages selection state,
    /// coordinates sub-controllers, and handles input/navigation.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CharacterSelectController : MonoBehaviour
    {
        private const string kChevronLeft = "\u2039";
        private const string kChevronRight = "\u203A";

        // =============================================================================
        // CONSTANTS
        // =============================================================================

        private const string kMainMenuScene = "MainMenu";
        private const string kGameScene = "Overworld";
        private const int kMaxStats = 20; // Max D&D stat value for bar display
        private const int kMaxAbilities = 5;
        private const string kSelectedHeroPref = "SelectedHero";
        private const string kStarterTownLocation = "StarterTown";
        private static readonly string[] kStatKeys = { "str", "dex", "con", "int", "wis", "cha" };
        private static readonly Color[] kStatFillBaseColors =
        {
            new Color(0.86f, 0.36f, 0.28f, 1f), // STR
            new Color(0.34f, 0.76f, 0.45f, 1f), // DEX
            new Color(0.80f, 0.64f, 0.30f, 1f), // CON
            new Color(0.30f, 0.62f, 0.92f, 1f), // INT
            new Color(0.31f, 0.78f, 0.78f, 1f), // WIS
            new Color(0.86f, 0.45f, 0.66f, 1f)  // CHA
        };
        private static readonly string[] kFallbackAbilityIds = { "attack_basic", "defend" };
        private const string kMissingAbilityDescription = "Signature technique data syncing.";

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
        private Label _starterStatHp;
        private Label _starterStatAtk;
        private Label _starterStatDef;
        private Label _starterStatSpd;

        // Monster info
        private VisualElement _monsterThumbnail;
        private Label _monsterName;
        private Label _monsterBrand;

        // Stats panel
        private VisualElement _statsPanel;
        private readonly VisualElement[] _statFills = new VisualElement[6];
        private readonly VisualElement[] _statRows = new VisualElement[6];
        private readonly Label[] _statValues = new Label[6];
        private readonly VisualElement[] _abilityRows = new VisualElement[kMaxAbilities];
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
        private Button _btnPrevHero;
        private Button _btnNextHero;
        private Button _btnRotateModelLeft;
        private Button _btnRotateModelRight;
        private Button _btnPrevHeroFallback;
        private Button _btnNextHeroFallback;
        private Button _btnRotateModelLeftFallback;
        private Button _btnRotateModelRightFallback;
        private Label _heroIndexIndicator;

        // Overlays
        private VisualElement _screenFade;
        private VisualElement _vfxOverlay;
        private VisualElement _heroStageRender;
        private VisualElement _infoPanel;
        private VisualElement _carouselStrip;
        private VisualElement _heroCycleHud;

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
        private bool _eventHandlersBound;
        private bool _inputSubscribed;
        private float _nextNavVisualRefreshTime;
        private Coroutine _switchCoroutine;
        private Coroutine _statsCoroutine;
        private Coroutine _quoteCoroutine;
        private Camera _cachedCamera;
        private bool _runtimeFallbackUIBuilt;
        private bool _readabilityFallbackEnabled;

        // Sub-controllers (added in later phases)
        private Coroutine _initCoroutine;
        private HeroStageController _heroStage;
        private EnvironmentController _environment;
        private VeilTearTransition _veilTear;
        private HeroVFXController _heroVFX;
        private Action<HeroData> _subControllerHandler;

        // =============================================================================
        // CACHED ALLOCATIONS
        // =============================================================================

        private static readonly WaitForSeconds kWait01 = new WaitForSeconds(0.1f);
        private static readonly WaitForSeconds kWait02 = new WaitForSeconds(0.2f);
        private static readonly WaitForSeconds kWait03 = new WaitForSeconds(0.3f);
        private static readonly WaitForSeconds kWait05 = new WaitForSeconds(0.5f);
        private static readonly WaitForSeconds kWait10 = new WaitForSeconds(1.0f);

        // Pre-baked number strings to avoid int.ToString() allocations
        private static readonly string[] kNumberStrings = new string[21];

        static CharacterSelectController()
        {
            for (int i = 0; i <= 20; i++)
                kNumberStrings[i] = i.ToString();
        }

        // Cached text elements for readability fallback (populated once after UI init)
        private List<TextElement> _cachedTextElements;
        private List<Label> _cachedLabels;

        // Reusable buffer for GetDisplayStats to avoid allocation per call
        private readonly int[] _displayStatsBuf = new int[6];

        // Event handler cache for proper unregistration (sized dynamically to match carousel slots)
        private EventCallback<ClickEvent>[] _slotClickHandlers;
        private static Font _fallbackUIFont;

        // Static cache for FormatSkillName to avoid repeated string allocations
        private static readonly Dictionary<string, string> _formattedSkillNames = new();

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
            EnsureCoreManagers();
            _initCoroutine = StartCoroutine(InitializeWhenReady());
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            if (_initCoroutine != null) StopCoroutine(_initCoroutine);
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
            float waitSeconds = 0f;
            const float maxWaitSeconds = 15f;
            while (GameDatabase.Instance == null || !GameDatabase.Instance.IsLoaded)
            {
                waitSeconds += Time.unscaledDeltaTime;
                if (waitSeconds >= maxWaitSeconds)
                {
                    Debug.LogError("[CharacterSelect] Timed out waiting for GameDatabase to load.");
                    yield break;
                }

                yield return null;
            }

            if (GameDatabase.Instance.LoadFailed)
            {
                Debug.LogError($"[CharacterSelect] GameDatabase reported load failure: {GameDatabase.Instance.LastLoadError}");
                yield break;
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
            _statsVisible = true;
            _statsPanel?.RemoveFromClassList("hidden");
            _statsPanel?.AddToClassList("visible");
            EnsureCriticalUIVisible();
            StartCoroutine(VisibilityFailsafe());

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
            QueryUIElements();
            _cachedTextElements = _root.Query<TextElement>().ToList();
            _cachedLabels = _root.Query<Label>().ToList();

            if (NeedsRuntimeFallbackUI())
            {
                Debug.LogWarning("[CharacterSelect] Critical UI nodes missing from CharacterSelect UXML. Building runtime fallback UI.");
                BuildRuntimeFallbackUI();
                QueryUIElements(); // Re-query after fallback UI is built
                _cachedTextElements = _root.Query<TextElement>().ToList();
                _cachedLabels = _root.Query<Label>().ToList();
                _readabilityFallbackEnabled = true;
            }

            ApplyStaticTextDefaults();
            EnsureTextReadability();
        }

        private void QueryUIElements()
        {
            if (_root == null) return;

            // Info panel
            _heroName = _root.Q<Label>("hero-name");
            _heroTitle = _root.Q<Label>("hero-title");
            _heroQuote = _root.Q<Label>("hero-quote");
            _heroPath = _root.Q<Label>("hero-path");
            _heroRole = _root.Q<Label>("hero-role");
            _heroResource = _root.Q<Label>("hero-resource");
            _starterStatHp = _root.Q<Label>("starter-stat-hp");
            _starterStatAtk = _root.Q<Label>("starter-stat-atk");
            _starterStatDef = _root.Q<Label>("starter-stat-def");
            _starterStatSpd = _root.Q<Label>("starter-stat-spd");

            // Monster info
            _monsterThumbnail = _root.Q("monster-thumbnail");
            _monsterName = _root.Q<Label>("monster-name");
            _monsterBrand = _root.Q<Label>("monster-brand");

            // Stats panel
            _statsPanel = _root.Q("stats-panel");
            for (int i = 0; i < kStatKeys.Length; i++)
            {
                _statRows[i] = _root.Q($"stat-row-{kStatKeys[i]}");
                _statFills[i] = _root.Q($"stat-fill-{kStatKeys[i]}");
                _statValues[i] = _root.Q<Label>($"stat-val-{kStatKeys[i]}");
            }
            for (int i = 0; i < kMaxAbilities; i++)
            {
                _abilityRows[i] = _root.Q($"ability-{i}");
                _abilityNames[i] = _root.Q<Label>($"ability-name-{i}");
                _abilityDescs[i] = _root.Q<Label>($"ability-desc-{i}");
            }

            // Carousel — discover slots dynamically
            _carouselTrack = _root.Q("carousel-track");
            _carouselSlots.Clear();
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
            _btnPrevHero = _root.Q<Button>("btn-prev-hero");
            _btnNextHero = _root.Q<Button>("btn-next-hero");
            _btnRotateModelLeft = _root.Q<Button>("btn-rotate-model-left");
            _btnRotateModelRight = _root.Q<Button>("btn-rotate-model-right");
            _btnPrevHeroFallback = _root.Q<Button>("btn-prev-hero-fallback");
            _btnNextHeroFallback = _root.Q<Button>("btn-next-hero-fallback");
            _btnRotateModelLeftFallback = _root.Q<Button>("btn-rotate-model-left-fallback");
            _btnRotateModelRightFallback = _root.Q<Button>("btn-rotate-model-right-fallback");
            _heroIndexIndicator = _root.Q<Label>("hero-index-indicator");
            EnsureRuntimeControlsVisible();
            SetButtonLabel(_btnPrevHero, "PREVIOUS");
            SetButtonLabel(_btnNextHero, "NEXT");
            if (_btnPrevHero != null) _btnPrevHero.tooltip = "Previous Hero (A / Left Arrow)";
            if (_btnNextHero != null) _btnNextHero.tooltip = "Next Hero (D / Right Arrow)";
            EnforceAbilityReadability();

            // Overlays
            _screenFade = _root.Q("screen-fade");
            _vfxOverlay = _root.Q("vfx-overlay");
            _heroStageRender = _root.Q("hero-stage-render");
            if (_vfxOverlay != null) _vfxOverlay.pickingMode = PickingMode.Ignore;
            if (_heroStageRender != null) _heroStageRender.pickingMode = PickingMode.Position;
            _envLayerColor = _root.Q("env-layer-color");
            _infoPanel = _root.Q("info-panel");
            _carouselStrip = _root.Q("carousel-strip");
            _heroCycleHud = _root.Q("hero-cycle-hud");
        }

        private bool NeedsRuntimeFallbackUI()
        {
            if (_root == null || _runtimeFallbackUIBuilt) return false;

            return _heroStageRender == null
                || _vfxOverlay == null
                || _heroName == null
                || _statsPanel == null
                || _btnEmbark == null
                || _btnBack == null
                || _carouselTrack == null
                || _carouselSlots.Count == 0;
        }

        private void BuildRuntimeFallbackUI()
        {
            if (_root == null) return;

            _runtimeFallbackUIBuilt = true;
            _root.style.flexGrow = 1f;
            _root.style.width = new StyleLength(new Length(100f, LengthUnit.Percent));
            _root.style.height = new StyleLength(new Length(100f, LengthUnit.Percent));

            var environmentContainer = EnsureElement(_root, "environment-container");
            environmentContainer.style.position = Position.Absolute;
            environmentContainer.style.left = 0f;
            environmentContainer.style.top = 0f;
            environmentContainer.style.right = 0f;
            environmentContainer.style.bottom = 0f;
            var envBase = EnsureElement(environmentContainer, "env-layer-base");
            var envColor = EnsureElement(environmentContainer, "env-layer-color");
            var envVignette = EnsureElement(environmentContainer, "env-vignette");
            envBase.style.position = Position.Absolute;
            envBase.style.left = 0f;
            envBase.style.top = 0f;
            envBase.style.right = 0f;
            envBase.style.bottom = 0f;
            envBase.style.backgroundColor = new Color(0.04f, 0.03f, 0.06f, 1f);
            envColor.style.position = Position.Absolute;
            envColor.style.left = 0f;
            envColor.style.top = 0f;
            envColor.style.right = 0f;
            envColor.style.bottom = 0f;
            envVignette.style.position = Position.Absolute;
            envVignette.style.left = 0f;
            envVignette.style.top = 0f;
            envVignette.style.right = 0f;
            envVignette.style.bottom = 0f;

            var heroStage = EnsureElement(_root, "hero-stage");
            heroStage.style.position = Position.Absolute;
            heroStage.style.left = Length.Percent(25f);
            heroStage.style.top = Length.Percent(11f);
            heroStage.style.width = Length.Percent(50f);
            heroStage.style.height = Length.Percent(66f);
            heroStage.style.overflow = Overflow.Visible;

            var heroStageRender = EnsureElement(heroStage, "hero-stage-render");
            heroStageRender.style.width = new StyleLength(new Length(100f, LengthUnit.Percent));
            heroStageRender.style.height = new StyleLength(new Length(100f, LengthUnit.Percent));
            var vfxOverlay = EnsureElement(heroStage, "vfx-overlay");
            vfxOverlay.style.position = Position.Absolute;
            vfxOverlay.style.left = 0f;
            vfxOverlay.style.top = 0f;
            vfxOverlay.style.right = 0f;
            vfxOverlay.style.bottom = 0f;
            vfxOverlay.pickingMode = PickingMode.Ignore;

            var infoPanel = EnsureElement(_root, "info-panel");
            ApplyFallbackPanelStyle(infoPanel, false);
            EnsureLabel(infoPanel, "hero-name", "VEX");
            EnsureLabel(infoPanel, "hero-title", "THE WARDEN");
            EnsureLabel(infoPanel, "hero-quote", "");
            EnsureLabel(infoPanel, "hero-path", "IRONBOUND");
            EnsureLabel(infoPanel, "hero-role", "TANK");
            EnsureLabel(infoPanel, "hero-resource", "GUARD");
            EnsureElement(infoPanel, "monster-thumbnail");
            EnsureLabel(infoPanel, "monster-name", "Skitter-Teeth");
            EnsureLabel(infoPanel, "monster-brand", "IRON");

            var statsPanel = EnsureElement(_root, "stats-panel");
            ApplyFallbackPanelStyle(statsPanel, true);

            for (int i = 0; i < kStatKeys.Length; i++)
            {
                string key = kStatKeys[i];
                var row = EnsureElement(statsPanel, $"stat-row-{key}");
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 8f;

                var track = EnsureElement(row, $"stat-track-{key}");
                track.style.flexGrow = 1f;
                track.style.height = 6f;
                track.style.marginLeft = 8f;
                track.style.marginRight = 8f;
                track.style.backgroundColor = new Color(1f, 1f, 1f, 0.18f);

                var fill = EnsureElement(track, $"stat-fill-{key}");
                fill.style.height = new StyleLength(new Length(100f, LengthUnit.Percent));
                fill.style.width = Length.Percent(50f);
                fill.style.backgroundColor = new Color(0.88f, 0.55f, 0.32f, 0.95f);

                EnsureLabel(row, $"stat-val-{key}", "10");
            }

            for (int i = 0; i < kMaxAbilities; i++)
            {
                var abilityRow = EnsureElement(statsPanel, $"ability-{i}");
                abilityRow.style.marginBottom = 6f;
                abilityRow.style.paddingLeft = 8f;
                abilityRow.style.paddingRight = 8f;
                abilityRow.style.paddingTop = 6f;
                abilityRow.style.paddingBottom = 6f;
                abilityRow.style.backgroundColor = new Color(1f, 1f, 1f, 0.08f);
                EnsureLabel(abilityRow, $"ability-name-{i}", "");
                EnsureLabel(abilityRow, $"ability-desc-{i}", "");
            }

            var carouselStrip = EnsureElement(_root, "carousel-strip");
            carouselStrip.style.position = Position.Absolute;
            carouselStrip.style.left = 0f;
            carouselStrip.style.right = 0f;
            carouselStrip.style.bottom = 24f;
            carouselStrip.style.height = 160f;
            carouselStrip.style.flexDirection = FlexDirection.Row;
            carouselStrip.style.justifyContent = Justify.Center;
            carouselStrip.style.alignItems = Align.Center;

            var carouselTrack = EnsureElement(carouselStrip, "carousel-track");
            carouselTrack.style.flexDirection = FlexDirection.Row;
            carouselTrack.style.alignItems = Align.Center;
            for (int i = 0; i < 4; i++)
            {
                var slot = EnsureElement(carouselTrack, $"carousel-slot-{i}");
                slot.style.width = 112f;
                slot.style.height = 130f;
                slot.style.marginLeft = 6f;
                slot.style.marginRight = 6f;
                slot.style.backgroundColor = new Color(0.09f, 0.08f, 0.12f, 0.85f);
                slot.style.alignItems = Align.Center;
                slot.style.justifyContent = Justify.Center;
                slot.style.borderTopWidth = 1f;
                slot.style.borderBottomWidth = 1f;
                slot.style.borderLeftWidth = 1f;
                slot.style.borderRightWidth = 1f;
                slot.style.borderTopColor = new Color(1f, 1f, 1f, 0.15f);
                slot.style.borderBottomColor = new Color(1f, 1f, 1f, 0.15f);
                slot.style.borderLeftColor = new Color(1f, 1f, 1f, 0.15f);
                slot.style.borderRightColor = new Color(1f, 1f, 1f, 0.15f);
                EnsureLabel(slot, $"slot-name-{i}", $"HERO {i + 1}");
            }

            var cycleHud = EnsureElement(_root, "hero-cycle-hud");
            cycleHud.style.position = Position.Absolute;
            cycleHud.style.left = Length.Percent(50f);
            cycleHud.style.bottom = 236f;
            cycleHud.style.width = 320f;
            cycleHud.style.height = 36f;
            cycleHud.style.marginLeft = -160f;
            cycleHud.style.flexDirection = FlexDirection.Row;
            cycleHud.style.justifyContent = Justify.Center;
            cycleHud.style.alignItems = Align.Center;

            // Nav arrows are standalone root-level elements (not children of cycleHud)
            var prev = EnsureButton(_root, "btn-prev-hero");
            prev.AddToClassList("vb-hero-nav-arrow");
            prev.AddToClassList("vb-hero-nav-prev");
            SetButtonLabel(prev, "PREVIOUS");

            EnsureLabel(cycleHud, "hero-index-indicator", "HERO 1 / 4");

            var next = EnsureButton(_root, "btn-next-hero");
            next.AddToClassList("vb-hero-nav-arrow");
            next.AddToClassList("vb-hero-nav-next");
            SetButtonLabel(next, "NEXT");

            var embark = EnsureButton(_root, "btn-embark");
            embark.style.position = Position.Absolute;
            embark.style.left = Length.Percent(50f);
            embark.style.bottom = 152f;
            embark.style.width = 320f;
            embark.style.height = 64f;
            embark.style.marginLeft = -160f;
            embark.style.backgroundColor = new Color(0.1f, 0.08f, 0.12f, 0.98f);
            embark.style.borderTopWidth = 1f;
            embark.style.borderBottomWidth = 1f;
            embark.style.borderLeftWidth = 1f;
            embark.style.borderRightWidth = 1f;
            embark.style.borderTopColor = new Color(1f, 1f, 1f, 0.2f);
            embark.style.borderBottomColor = new Color(1f, 1f, 1f, 0.2f);
            embark.style.borderLeftColor = new Color(1f, 1f, 1f, 0.2f);
            embark.style.borderRightColor = new Color(1f, 1f, 1f, 0.2f);
            EnsureLabel(embark, "embark-label", "EMBARK AS VEX");

            var back = EnsureButton(_root, "btn-back");
            back.text = "BACK";
            back.style.position = Position.Absolute;
            back.style.left = 40f;
            back.style.top = 36f;

            var neutral = EnsureButton(_root, "btn-neutral-toggle");
            neutral.text = "NEUTRAL BG";
            neutral.style.position = Position.Absolute;
            neutral.style.right = 40f;
            neutral.style.top = 36f;
            neutral.style.display = DisplayStyle.None;

            var screenFade = EnsureElement(_root, "screen-fade");
            screenFade.style.position = Position.Absolute;
            screenFade.style.left = 0f;
            screenFade.style.top = 0f;
            screenFade.style.right = 0f;
            screenFade.style.bottom = 0f;
            screenFade.style.backgroundColor = Color.black;
            screenFade.style.opacity = 0f;
            screenFade.pickingMode = PickingMode.Ignore;
        }

        private static VisualElement EnsureElement(VisualElement parent, string name)
        {
            var element = parent.Q<VisualElement>(name);
            if (element == null)
            {
                element = new VisualElement { name = name };
                parent.Add(element);
            }
            return element;
        }

        private static Label EnsureLabel(VisualElement parent, string name, string text)
        {
            var label = parent.Q<Label>(name);
            if (label == null)
            {
                label = new Label { name = name };
                parent.Add(label);
            }
            label.text = text ?? string.Empty;
            return label;
        }

        private static Button EnsureButton(VisualElement parent, string name)
        {
            var button = parent.Q<Button>(name);
            if (button == null)
            {
                button = new Button { name = name };
                parent.Add(button);
            }
            return button;
        }

        private static void ApplyFallbackPanelStyle(VisualElement panel, bool right)
        {
            panel.style.position = Position.Absolute;
            panel.style.top = 130f;
            if (right)
            {
                panel.style.right = 40f;
                panel.style.left = StyleKeyword.Null;
            }
            else
            {
                panel.style.left = 40f;
                panel.style.right = StyleKeyword.Null;
            }

            panel.style.width = 300f;
            panel.style.paddingTop = 16f;
            panel.style.paddingBottom = 16f;
            panel.style.paddingLeft = 16f;
            panel.style.paddingRight = 16f;
            panel.style.backgroundColor = new Color(0.06f, 0.06f, 0.09f, 0.92f);
            panel.style.borderTopWidth = 1f;
            panel.style.borderBottomWidth = 1f;
            panel.style.borderLeftWidth = 1f;
            panel.style.borderRightWidth = 1f;
            panel.style.borderTopColor = new Color(1f, 1f, 1f, 0.22f);
            panel.style.borderBottomColor = new Color(1f, 1f, 1f, 0.22f);
            panel.style.borderLeftColor = new Color(1f, 1f, 1f, 0.22f);
            panel.style.borderRightColor = new Color(1f, 1f, 1f, 0.22f);
        }

        private void ApplyReadabilityFallbackStyles()
        {
            if (_root == null || !_readabilityFallbackEnabled) return;

            Color textColor = new Color(0.94f, 0.9f, 0.84f, 1f);
            _root.style.color = textColor;

            ApplyPanelReadability(_infoPanel);
            ApplyPanelReadability(_statsPanel);
            ApplyPanelReadability(_carouselStrip);
            ApplyPanelReadability(_heroCycleHud);
            ApplyButtonReadability(_btnEmbark);
            ApplyButtonReadability(_btnBack);
            ApplyButtonReadability(_btnPrevHero);
            ApplyButtonReadability(_btnNextHero);

            var labels = _cachedLabels ?? _root.Query<Label>().ToList();
            foreach (var label in labels)
            {
                Color c = label.resolvedStyle.color;
                if (c.a < 0.25f || (c.r + c.g + c.b) < 0.45f)
                {
                    label.style.color = textColor;
                }
            }
        }

        private static void ApplyPanelReadability(VisualElement panel)
        {
            if (panel == null) return;
            if (panel.resolvedStyle.backgroundColor.a > 0.05f) return;

            panel.style.backgroundColor = new Color(0.06f, 0.06f, 0.09f, 0.9f);
            panel.style.borderTopWidth = 1f;
            panel.style.borderBottomWidth = 1f;
            panel.style.borderLeftWidth = 1f;
            panel.style.borderRightWidth = 1f;
            panel.style.borderTopColor = new Color(1f, 1f, 1f, 0.16f);
            panel.style.borderBottomColor = new Color(1f, 1f, 1f, 0.16f);
            panel.style.borderLeftColor = new Color(1f, 1f, 1f, 0.16f);
            panel.style.borderRightColor = new Color(1f, 1f, 1f, 0.16f);
        }

        private static void ApplyButtonReadability(Button button)
        {
            if (button == null) return;
            if (button.resolvedStyle.backgroundColor.a > 0.05f) return;

            button.style.backgroundColor = new Color(0.08f, 0.08f, 0.11f, 0.95f);
            button.style.borderTopWidth = 1f;
            button.style.borderBottomWidth = 1f;
            button.style.borderLeftWidth = 1f;
            button.style.borderRightWidth = 1f;
            button.style.borderTopColor = new Color(1f, 1f, 1f, 0.2f);
            button.style.borderBottomColor = new Color(1f, 1f, 1f, 0.2f);
            button.style.borderLeftColor = new Color(1f, 1f, 1f, 0.2f);
            button.style.borderRightColor = new Color(1f, 1f, 1f, 0.2f);
        }

        private static void EnsureHeroCycleButtonText(Button button, string fallbackText, string tooltip)
        {
            if (button == null) return;

            button.tooltip = tooltip;

            var arrowLabel = button.Q<Label>(className: "vb-hero-cycle-arrow") ?? button.Q<Label>();
            if (arrowLabel != null)
            {
                if (string.IsNullOrWhiteSpace(arrowLabel.text))
                {
                    arrowLabel.text = fallbackText;
                }
            }
            // Hard fallback: always set button.text so arrows remain visible even if nested labels fail.
            button.text = fallbackText;
            PrepareButtonForText(button);
        }

        private static void SetButtonLabel(Button button, string text)
        {
            if (button == null) return;

            var label = button.Q<Label>(className: "vb-hero-cycle-arrow")
                        ?? button.Q<Label>(className: "vb-model-rotate-arrow")
                        ?? button.Q<Label>(className: "vb-nav-label")
                        ?? button.Q<Label>();
            if (label == null)
            {
                label = new Label();
                label.AddToClassList("vb-nav-label");
                button.Add(label);
            }

            label.enableRichText = false;
            label.text = text ?? string.Empty;
            button.text = string.Empty;
        }

        private void EnsureRuntimeControlsVisible()
        {
            if (_root == null) return;

            // Hero cycle HUD: indicator pill only (nav arrows are now standalone elements in UXML)
            var cycleHud = _heroCycleHud ?? EnsureElement(_root, "hero-cycle-hud");
            cycleHud.style.display = DisplayStyle.Flex;
            cycleHud.style.opacity = 1f;

            // Nav arrows are standalone root-level elements; only ensure references are set.
            _btnPrevHero ??= _root.Q<Button>("btn-prev-hero");
            _btnNextHero ??= _root.Q<Button>("btn-next-hero");
            _heroIndexIndicator ??= EnsureLabel(cycleHud, "hero-index-indicator", "HERO 1 / 4");
            EnsureButtonParent(_btnPrevHero, _root);
            EnsureButtonParent(_btnNextHero, _root);

            // Ensure nav arrows are visible (USS handles all positioning/sizing)
            if (_btnPrevHero != null)
            {
                _btnPrevHero.style.display = DisplayStyle.Flex;
                _btnPrevHero.style.opacity = 1f;
                _btnPrevHero.tooltip = "Previous hero";
            }
            if (_btnNextHero != null)
            {
                _btnNextHero.style.display = DisplayStyle.Flex;
                _btnNextHero.style.opacity = 1f;
                _btnNextHero.tooltip = "Next hero";
            }
            ApplyHeroNavigationLayout();

            var carousel = _carouselStrip ?? EnsureElement(_root, "carousel-strip");
            carousel.style.display = DisplayStyle.Flex;
            carousel.style.opacity = 1f;

            if (_btnEmbark != null)
            {
                _btnEmbark.style.display = DisplayStyle.Flex;
                _btnEmbark.style.opacity = 1f;
            }

            // Model rotation controls: USS handles styling; just ensure visible
            var stage = _root.Q<VisualElement>("hero-stage") ?? _heroStageRender?.parent;
            if (stage != null)
            {
                _btnRotateModelLeft ??= stage.Q<Button>("btn-rotate-model-left");
                _btnRotateModelRight ??= stage.Q<Button>("btn-rotate-model-right");
                EnsureButtonParent(_btnRotateModelLeft, _root);
                EnsureButtonParent(_btnRotateModelRight, _root);
                if (_btnRotateModelLeft != null)
                {
                    _btnRotateModelLeft.style.display = DisplayStyle.Flex;
                    _btnRotateModelLeft.style.opacity = 1f;
                    _btnRotateModelLeft.BringToFront();
                }
                if (_btnRotateModelRight != null)
                {
                    _btnRotateModelRight.style.display = DisplayStyle.Flex;
                    _btnRotateModelRight.style.opacity = 1f;
                    _btnRotateModelRight.BringToFront();
                }

                ApplyRotationButtonLayout(stage);
            }

            _btnNeutralToggle ??= EnsureButton(_root, "btn-neutral-toggle");
            _btnNeutralToggle.RemoveFromClassList("hidden");
            _btnNeutralToggle.style.display = DisplayStyle.Flex;
            _btnNeutralToggle.style.opacity = 1f;
            ApplyButtonReadability(_btnNeutralToggle);
            _btnNeutralToggle.BringToFront();
            RefreshNeutralToggleButtonText();

            // Keep runtime fallback nav controls hidden unless explicitly needed.
            if (_btnPrevHeroFallback != null) _btnPrevHeroFallback.style.display = DisplayStyle.None;
            if (_btnNextHeroFallback != null) _btnNextHeroFallback.style.display = DisplayStyle.None;
            if (_btnRotateModelLeftFallback != null) _btnRotateModelLeftFallback.style.display = DisplayStyle.None;
            if (_btnRotateModelRightFallback != null) _btnRotateModelRightFallback.style.display = DisplayStyle.None;
        }

        private static void EnsureButtonParent(Button button, VisualElement targetParent)
        {
            if (button == null || targetParent == null) return;
            if (button.parent == targetParent) return;

            button.RemoveFromHierarchy();
            targetParent.Add(button);
        }

        private void ApplyHeroNavigationLayout()
        {
            float stageLeft;
            float stageTop;
            float stageWidth;
            float stageHeight;
            bool hasStageRect = TryGetHeroStageRect(out stageLeft, out stageTop, out stageWidth, out stageHeight);
            float centerX = hasStageRect ? (stageLeft + (stageWidth * 0.5f)) : (_root.resolvedStyle.width * 0.5f);
            float navTop = hasStageRect ? (stageTop + stageHeight + 14f) : (_root.resolvedStyle.height - 214f);

            if (_btnPrevHero != null)
            {
                _btnPrevHero.style.position = Position.Absolute;
                _btnPrevHero.style.left = centerX - 170f;
                _btnPrevHero.style.top = navTop;
                _btnPrevHero.style.bottom = StyleKeyword.Null;
                _btnPrevHero.style.width = 156f;
                _btnPrevHero.style.height = 46f;
                _btnPrevHero.style.marginLeft = 0f;
                _btnPrevHero.style.zIndex = 1200;
                _btnPrevHero.style.display = DisplayStyle.Flex;
                _btnPrevHero.style.opacity = 1f;
                _btnPrevHero.BringToFront();
            }

            if (_btnNextHero != null)
            {
                _btnNextHero.style.position = Position.Absolute;
                _btnNextHero.style.left = centerX + 14f;
                _btnNextHero.style.top = navTop;
                _btnNextHero.style.bottom = StyleKeyword.Null;
                _btnNextHero.style.width = 156f;
                _btnNextHero.style.height = 46f;
                _btnNextHero.style.marginLeft = 0f;
                _btnNextHero.style.zIndex = 1200;
                _btnNextHero.style.display = DisplayStyle.Flex;
                _btnNextHero.style.opacity = 1f;
                _btnNextHero.BringToFront();
            }

            if (_heroCycleHud != null)
            {
                _heroCycleHud.style.position = Position.Absolute;
                _heroCycleHud.style.left = centerX - 140f;
                _heroCycleHud.style.top = navTop - 40f;
                _heroCycleHud.style.bottom = StyleKeyword.Null;
                _heroCycleHud.style.width = 280f;
                _heroCycleHud.style.height = 32f;
                _heroCycleHud.style.marginLeft = 0f;
                _heroCycleHud.style.zIndex = 1190;
                _heroCycleHud.style.display = DisplayStyle.Flex;
                _heroCycleHud.style.opacity = 1f;
                _heroCycleHud.BringToFront();
            }
        }

        private void ApplyRotationButtonLayout(VisualElement stage)
        {
            if (_root == null) return;

            float stageLeft;
            float stageTop;
            float stageWidth;
            float stageHeight;
            bool hasStageRect = TryGetHeroStageRect(out stageLeft, out stageTop, out stageWidth, out stageHeight);

            float buttonTop = hasStageRect
                ? stageTop + (stageHeight * 0.5f) - 20f
                : (_root.resolvedStyle.height * 0.5f) - 20f;
            float leftX = hasStageRect
                ? stageLeft + 8f
                : (_root.resolvedStyle.width * 0.25f);
            float rightX = hasStageRect
                ? stageLeft + stageWidth - 48f
                : (_root.resolvedStyle.width * 0.75f) - 40f;

            if (_btnRotateModelLeft != null)
            {
                _btnRotateModelLeft.style.position = Position.Absolute;
                _btnRotateModelLeft.style.left = leftX;
                _btnRotateModelLeft.style.right = StyleKeyword.Null;
                _btnRotateModelLeft.style.top = buttonTop;
                _btnRotateModelLeft.style.bottom = StyleKeyword.Null;
                _btnRotateModelLeft.style.marginTop = 0f;
                _btnRotateModelLeft.style.width = 40f;
                _btnRotateModelLeft.style.height = 40f;
                _btnRotateModelLeft.style.display = DisplayStyle.Flex;
                _btnRotateModelLeft.style.opacity = 1f;
                _btnRotateModelLeft.style.zIndex = 1210;
                _btnRotateModelLeft.BringToFront();
            }

            if (_btnRotateModelRight != null)
            {
                _btnRotateModelRight.style.position = Position.Absolute;
                _btnRotateModelRight.style.left = rightX;
                _btnRotateModelRight.style.right = StyleKeyword.Null;
                _btnRotateModelRight.style.top = buttonTop;
                _btnRotateModelRight.style.bottom = StyleKeyword.Null;
                _btnRotateModelRight.style.marginTop = 0f;
                _btnRotateModelRight.style.width = 40f;
                _btnRotateModelRight.style.height = 40f;
                _btnRotateModelRight.style.display = DisplayStyle.Flex;
                _btnRotateModelRight.style.opacity = 1f;
                _btnRotateModelRight.style.zIndex = 1210;
                _btnRotateModelRight.BringToFront();
            }
        }

        private bool TryGetHeroStageRect(out float left, out float top, out float width, out float height)
        {
            left = 0f;
            top = 0f;
            width = 0f;
            height = 0f;

            var stage = _root?.Q<VisualElement>("hero-stage") ?? _heroStageRender?.parent;
            if (stage == null)
            {
                return false;
            }

            var rs = stage.resolvedStyle;
            if (rs.width < 10f || rs.height < 10f)
            {
                return false;
            }

            left = rs.left;
            top = rs.top;
            width = rs.width;
            height = rs.height;
            return true;
        }

        private void EnsureFallbackNavigationOverlay(VisualElement stage)
        {
            if (_root == null) return;

            var navOverlay = EnsureElement(_root, "hero-nav-overlay");
            navOverlay.style.position = Position.Absolute;
            navOverlay.style.left = 0f;
            navOverlay.style.right = 0f;
            navOverlay.style.top = 0f;
            navOverlay.style.bottom = 0f;
            navOverlay.style.display = DisplayStyle.Flex;
            navOverlay.pickingMode = PickingMode.Ignore;
            navOverlay.BringToFront();

            _btnPrevHeroFallback ??= EnsureButton(navOverlay, "btn-prev-hero-fallback");
            _btnNextHeroFallback ??= EnsureButton(navOverlay, "btn-next-hero-fallback");
            _btnRotateModelLeftFallback ??= EnsureButton(navOverlay, "btn-rotate-model-left-fallback");
            _btnRotateModelRightFallback ??= EnsureButton(navOverlay, "btn-rotate-model-right-fallback");

            ApplyFallbackNavButtonStyle(_btnPrevHeroFallback, "PREV HERO", 36f, null, 236f, null, 180f);
            ApplyFallbackNavButtonStyle(_btnNextHeroFallback, "NEXT HERO", null, 36f, 236f, null, 180f);
            ApplyFallbackNavButtonStyle(_btnRotateModelLeftFallback, "ROTATE L", 40f, null, null, 50f, 150f, -40f);
            ApplyFallbackNavButtonStyle(_btnRotateModelRightFallback, "ROTATE R", null, 40f, null, 50f, 150f, -40f);
        }

        private static void ApplyFallbackNavButtonStyle(
            Button button,
            string text,
            float? left = null,
            float? right = null,
            float? bottom = null,
            float? topPercent = null,
            float? width = null,
            float? marginTop = null)
        {
            if (button == null) return;

            button.text = text;
            button.style.position = Position.Absolute;
            button.style.left = left.HasValue ? left.Value : StyleKeyword.Null;
            button.style.right = right.HasValue ? right.Value : StyleKeyword.Null;
            button.style.bottom = bottom.HasValue ? bottom.Value : StyleKeyword.Null;
            button.style.top = topPercent.HasValue ? Length.Percent(topPercent.Value) : StyleKeyword.Null;
            button.style.marginTop = marginTop.HasValue ? marginTop.Value : 0f;
            button.style.width = width ?? 170f;
            button.style.height = 48f;
            button.style.display = DisplayStyle.Flex;
            button.style.opacity = 1f;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.fontSize = 14f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.color = new Color(0.97f, 0.93f, 0.86f, 1f);
            button.style.backgroundColor = new Color(0.07f, 0.07f, 0.11f, 0.9f);
            button.style.borderTopWidth = 1f;
            button.style.borderBottomWidth = 1f;
            button.style.borderLeftWidth = 1f;
            button.style.borderRightWidth = 1f;
            button.style.borderTopColor = new Color(1f, 1f, 1f, 0.28f);
            button.style.borderBottomColor = new Color(1f, 1f, 1f, 0.28f);
            button.style.borderLeftColor = new Color(1f, 1f, 1f, 0.28f);
            button.style.borderRightColor = new Color(1f, 1f, 1f, 0.28f);
            button.style.borderTopLeftRadius = 5f;
            button.style.borderTopRightRadius = 5f;
            button.style.borderBottomLeftRadius = 5f;
            button.style.borderBottomRightRadius = 5f;
        }

        private static void ApplyArrowButtonStyle(Button button, string arrowText)
        {
            if (button == null) return;

            button.text = arrowText == "<" ? "PREV" : "NEXT";
            PrepareButtonForText(button);
            button.style.display = DisplayStyle.Flex;
            button.style.opacity = 1f;
            button.style.width = 112f;
            button.style.height = 42f;
            button.style.marginLeft = 10f;
            button.style.marginRight = 10f;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.fontSize = 16f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.color = new Color(0.95f, 0.9f, 0.84f, 1f);
            button.style.backgroundColor = new Color(0.09f, 0.08f, 0.13f, 0.95f);
            button.style.borderTopWidth = 1f;
            button.style.borderBottomWidth = 1f;
            button.style.borderLeftWidth = 1f;
            button.style.borderRightWidth = 1f;
            button.style.borderTopColor = new Color(1f, 1f, 1f, 0.25f);
            button.style.borderBottomColor = new Color(1f, 1f, 1f, 0.25f);
            button.style.borderLeftColor = new Color(1f, 1f, 1f, 0.25f);
            button.style.borderRightColor = new Color(1f, 1f, 1f, 0.25f);
            button.tooltip = arrowText == "<" ? "Previous hero" : "Next hero";
            EnsureChevronIcon(button, arrowText != "<", "vb-button-chevron");
        }

        private static void ApplyRoundRotateButtonStyle(Button button, string arrowText, bool leftSide)
        {
            if (button == null) return;

            button.text = leftSide ? "L" : "R";
            PrepareButtonForText(button);
            button.style.position = Position.Absolute;
            button.style.top = Length.Percent(50f);
            button.style.marginTop = -18f;
            button.style.left = leftSide ? 16f : StyleKeyword.Null;
            button.style.right = leftSide ? StyleKeyword.Null : 16f;
            button.style.width = 56f;
            button.style.height = 56f;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.fontSize = 24f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.color = new Color(0.95f, 0.9f, 0.84f, 1f);
            button.style.backgroundColor = new Color(0.08f, 0.07f, 0.11f, 0.9f);
            button.style.borderTopWidth = 1f;
            button.style.borderBottomWidth = 1f;
            button.style.borderLeftWidth = 1f;
            button.style.borderRightWidth = 1f;
            button.style.borderTopColor = new Color(1f, 1f, 1f, 0.25f);
            button.style.borderBottomColor = new Color(1f, 1f, 1f, 0.25f);
            button.style.borderLeftColor = new Color(1f, 1f, 1f, 0.25f);
            button.style.borderRightColor = new Color(1f, 1f, 1f, 0.25f);
            button.style.borderTopLeftRadius = 28f;
            button.style.borderTopRightRadius = 28f;
            button.style.borderBottomLeftRadius = 28f;
            button.style.borderBottomRightRadius = 28f;
            button.style.display = DisplayStyle.Flex;
            button.style.opacity = 1f;
            button.tooltip = leftSide ? "Rotate model left" : "Rotate model right";
            EnsureChevronIcon(button, !leftSide, "vb-rotate-chevron");
        }

        private static void PrepareButtonForText(Button button)
        {
            if (button == null) return;

            var label = button.Q<Label>();
            if (label != null)
            {
                label.enableRichText = false;
                label.style.color = new Color(0.95f, 0.9f, 0.84f, 1f);
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.fontSize = 15f;
            }
        }

        private static void EnsureArrowLabelStyle(Button button, string preferredClass, int fontSize)
        {
            if (button == null) return;

            var arrowLabel = button.Q<Label>(className: preferredClass) ?? button.Q<Label>();
            if (arrowLabel == null)
            {
                arrowLabel = new Label();
                arrowLabel.AddToClassList(preferredClass);
                button.Add(arrowLabel);
            }
            else
            {
                arrowLabel.AddToClassList(preferredClass);
            }

            arrowLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            arrowLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            arrowLabel.style.fontSize = fontSize;
            arrowLabel.style.color = new Color(0.98f, 0.94f, 0.86f, 1f);
            arrowLabel.style.opacity = 1f;
            arrowLabel.enableRichText = false;
            arrowLabel.BringToFront();
        }

        private static void EnsureChevronIcon(Button button, bool rightFacing, string className)
        {
            if (button == null) return;

            var icon = button.Q<VisualElement>(className: className);
            if (icon == null)
            {
                icon = new VisualElement();
                icon.AddToClassList(className);
                button.Add(icon);
            }

            icon.pickingMode = PickingMode.Ignore;
            icon.style.position = Position.Absolute;
            icon.style.left = Length.Percent(50f);
            icon.style.top = Length.Percent(50f);
            icon.style.marginLeft = -7f;
            icon.style.marginTop = -7f;
            icon.style.width = 14f;
            icon.style.height = 14f;
            icon.style.borderTopWidth = 2f;
            icon.style.borderRightWidth = rightFacing ? 2f : 0f;
            icon.style.borderBottomWidth = 0f;
            icon.style.borderLeftWidth = rightFacing ? 0f : 2f;
            icon.style.borderTopColor = new Color(0.98f, 0.94f, 0.86f, 1f);
            icon.style.borderRightColor = new Color(0.98f, 0.94f, 0.86f, 1f);
            icon.style.borderBottomColor = StyleKeyword.Null;
            icon.style.borderLeftColor = new Color(0.98f, 0.94f, 0.86f, 1f);
            icon.style.rotate = new Rotate(new Angle(rightFacing ? 45f : -45f));
            icon.style.left = Length.Percent(50f);
            icon.style.top = Length.Percent(50f);
            icon.style.marginLeft = rightFacing ? 28f : -42f;
            icon.style.marginTop = -7f;
            icon.BringToFront();
        }

        private void TryEnableReadabilityFallback()
        {
            if (_readabilityFallbackEnabled || _runtimeFallbackUIBuilt) return;
            if (_heroName == null) return;

            Color heroNameColor = _heroName.resolvedStyle.color;
            bool darkOrInvisibleText = heroNameColor.a < 0.2f || (heroNameColor.r + heroNameColor.g + heroNameColor.b) < 0.45f;

            bool panelLooksUnstyled = _infoPanel != null && _infoPanel.resolvedStyle.backgroundColor.a < 0.05f;

            if (darkOrInvisibleText || panelLooksUnstyled)
            {
                _readabilityFallbackEnabled = true;
                Debug.LogWarning("[CharacterSelect] Readability fallback enabled (detected unstyled/low-contrast UI).");
            }
        }

        private void SetupEventHandlers()
        {
            if (_eventHandlersBound) return;

            _btnBack?.RegisterCallback<ClickEvent>(OnBackClicked);
            _btnEmbark?.RegisterCallback<ClickEvent>(OnEmbarkClicked);
            _btnNeutralToggle?.RegisterCallback<ClickEvent>(OnNeutralToggleClicked);
            _btnPrevHero?.RegisterCallback<ClickEvent>(OnPrevHeroClicked);
            _btnNextHero?.RegisterCallback<ClickEvent>(OnNextHeroClicked);
            _btnRotateModelLeft?.RegisterCallback<ClickEvent>(OnRotateModelLeftClicked);
            _btnRotateModelRight?.RegisterCallback<ClickEvent>(OnRotateModelRightClicked);
            _btnPrevHeroFallback?.RegisterCallback<ClickEvent>(OnPrevHeroClicked);
            _btnNextHeroFallback?.RegisterCallback<ClickEvent>(OnNextHeroClicked);
            _btnRotateModelLeftFallback?.RegisterCallback<ClickEvent>(OnRotateModelLeftClicked);
            _btnRotateModelRightFallback?.RegisterCallback<ClickEvent>(OnRotateModelRightClicked);

            // Carousel slot click handlers (cached for unregistration)
            for (int i = 0; i < _carouselSlots.Count; i++)
            {
                int index = i; // Capture for closure
                _slotClickHandlers[i] = evt => SelectHeroByIndex(index);
                _carouselSlots[i].RegisterCallback(_slotClickHandlers[i]);
            }

            // Tab key for stats panel toggle
            _root?.RegisterCallback<KeyDownEvent>(OnKeyDown);
            _eventHandlersBound = true;
        }

        private void CleanupEventHandlers()
        {
            if (!_eventHandlersBound) return;

            _btnBack?.UnregisterCallback<ClickEvent>(OnBackClicked);
            _btnEmbark?.UnregisterCallback<ClickEvent>(OnEmbarkClicked);
            _btnNeutralToggle?.UnregisterCallback<ClickEvent>(OnNeutralToggleClicked);
            _btnPrevHero?.UnregisterCallback<ClickEvent>(OnPrevHeroClicked);
            _btnNextHero?.UnregisterCallback<ClickEvent>(OnNextHeroClicked);
            _btnRotateModelLeft?.UnregisterCallback<ClickEvent>(OnRotateModelLeftClicked);
            _btnRotateModelRight?.UnregisterCallback<ClickEvent>(OnRotateModelRightClicked);
            _btnPrevHeroFallback?.UnregisterCallback<ClickEvent>(OnPrevHeroClicked);
            _btnNextHeroFallback?.UnregisterCallback<ClickEvent>(OnNextHeroClicked);
            _btnRotateModelLeftFallback?.UnregisterCallback<ClickEvent>(OnRotateModelLeftClicked);
            _btnRotateModelRightFallback?.UnregisterCallback<ClickEvent>(OnRotateModelRightClicked);

            if (_slotClickHandlers != null)
            {
                for (int i = 0; i < _carouselSlots.Count && i < _slotClickHandlers.Length; i++)
                {
                    if (_slotClickHandlers[i] != null)
                        _carouselSlots[i]?.UnregisterCallback(_slotClickHandlers[i]);
                }
            }

            _root?.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            _eventHandlersBound = false;
        }

        private void InitializeSubControllers()
        {
            // Add sub-controller components (runtime-added; [SerializeField] fields use code defaults)
            _heroStage = GetComponent<HeroStageController>() ?? gameObject.AddComponent<HeroStageController>();
            _heroStage.Initialize(_heroStageRender);

            _environment = GetComponent<EnvironmentController>() ?? gameObject.AddComponent<EnvironmentController>();
            _environment.Initialize(_root);

            _veilTear = GetComponent<VeilTearTransition>() ?? gameObject.AddComponent<VeilTearTransition>();
            _veilTear.Initialize(_root);

            _heroVFX = GetComponent<HeroVFXController>() ?? gameObject.AddComponent<HeroVFXController>();
            _heroVFX.Initialize(_vfxOverlay);

            // Wire OnHeroChanged to sub-controllers (cached for unsubscription)
            _subControllerHandler = hero =>
            {
                _heroStage?.ShowHero(hero);
                _environment?.SetHeroEnvironment(hero);
                _heroVFX?.SetHero(hero);
            };
            OnHeroChanged += _subControllerHandler;

            RefreshNeutralToggleButtonText();
        }

        // =============================================================================
        // INPUT
        // =============================================================================

        private void SubscribeInput()
        {
            if (_inputSubscribed) return;

            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnActionTriggered += OnInputAction;
                _inputSubscribed = true;
            }
        }

        private void UnsubscribeInput()
        {
            if (!_inputSubscribed) return;

            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnActionTriggered -= OnInputAction;
            }

            _inputSubscribed = false;
        }

        private void LateUpdate()
        {
            if (_isDestroyed) return;
            if (!_inputSubscribed) SubscribeInput();

            if (Time.unscaledTime >= _nextNavVisualRefreshTime)
            {
                EnsureNavigationButtonVisuals();
                _nextNavVisualRefreshTime = Time.unscaledTime + 0.33f;
            }
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
            if (evt.target is TextField) return;

            switch (evt.keyCode)
            {
                case KeyCode.Tab:
                    ToggleStatsPanel();
                    evt.StopPropagation();
                    break;
                case KeyCode.LeftArrow:
                case KeyCode.A:
                    SelectPreviousHero();
                    evt.StopPropagation();
                    break;
                case KeyCode.RightArrow:
                case KeyCode.D:
                    SelectNextHero();
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
            _isAnimating = true;
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
            try
            {
                if (_heroes == null || toIndex < 0 || toIndex >= _heroes.Count)
                {
                    yield break;
                }

                var hero = _heroes[toIndex];
                if (hero == null)
                {
                    yield break;
                }

                PlaySFX(_switchSFX, 0.5f);

                // Cancel quote animation if running
                if (_quoteCoroutine != null) StopCoroutine(_quoteCoroutine);
                if (_heroQuote != null) _heroQuote.style.opacity = 0;

                // Trigger veil tear transition (if available)
                Color heroColor = hero.color_palette != null ? hero.color_palette.ToColor() : Color.white;
                _veilTear?.Play(heroColor);

                // Brief pause for transition flash
                yield return kWait02;

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
                yield return kWait03;
                if (!_isDestroyed)
                {
                    _quoteCoroutine = StartCoroutine(FadeInQuote());
                }

                yield return kWait02;
            }
            finally
            {
                _switchCoroutine = null;
                _isAnimating = false;
            }
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
            ApplyStarterStats(hero);

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
            EnforceAbilityReadability();
            EnsureTextReadability();
        }

        private void ApplyMonsterData(string monsterId)
        {
            if (string.IsNullOrEmpty(monsterId))
            {
                ClearMonsterData();
                return;
            }

            var monster = GameDatabase.Instance?.GetMonster(monsterId);
            if (monster == null)
            {
                ClearMonsterData();
                return;
            }

            if (_monsterName != null) _monsterName.text = monster.display_name ?? "";
            if (_monsterBrand != null) _monsterBrand.text = monster.GetPrimaryBrand().ToString();
        }

        private void ClearMonsterData()
        {
            if (_monsterName != null) _monsterName.text = "Unknown";
            if (_monsterBrand != null) _monsterBrand.text = "N/A";
        }

        private void ApplyStarterStats(HeroData hero)
        {
            if (hero == null) return;

            if (_starterStatHp != null) _starterStatHp.text = hero.base_hp.ToString();
            if (_starterStatAtk != null) _starterStatAtk.text = hero.base_attack.ToString();
            if (_starterStatDef != null) _starterStatDef.text = hero.base_defense.ToString();
            if (_starterStatSpd != null) _starterStatSpd.text = hero.base_speed.ToString();
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
            int[] stats = GetDisplayStats(hero);
            ApplyStatsTheme(hero, stats);

            for (int i = 0; i < 6; i++)
            {
                int val = Mathf.Clamp(stats[i], 0, kMaxStats);
                float pct = val / (float)kMaxStats * 100f;

                if (_statFills[i] != null)
                    _statFills[i].style.width = new StyleLength(new Length(pct, LengthUnit.Percent));
                if (_statValues[i] != null)
                    _statValues[i].text = val <= 20 ? kNumberStrings[val] : val.ToString();
            }
        }

        private int[] GetDisplayStats(HeroData hero)
        {
            if (hero?.base_stats != null)
            {
                _displayStatsBuf[0] = hero.base_stats.strength;
                _displayStatsBuf[1] = hero.base_stats.dexterity;
                _displayStatsBuf[2] = hero.base_stats.constitution;
                _displayStatsBuf[3] = hero.base_stats.intelligence;
                _displayStatsBuf[4] = hero.base_stats.wisdom;
                _displayStatsBuf[5] = hero.base_stats.charisma;
                return _displayStatsBuf;
            }

            // Fallback mapping if base_stats is absent in source data.
            _displayStatsBuf[0] = Mathf.Clamp(8 + Mathf.RoundToInt(((hero?.base_attack ?? 10) - 10) * 0.35f), 1, kMaxStats);
            _displayStatsBuf[1] = Mathf.Clamp(8 + Mathf.RoundToInt(((hero?.base_speed ?? 10) - 10) * 0.50f), 1, kMaxStats);
            _displayStatsBuf[2] = Mathf.Clamp(8 + Mathf.RoundToInt(((hero?.base_defense ?? 10) - 10) * 0.35f), 1, kMaxStats);
            _displayStatsBuf[3] = Mathf.Clamp(8 + Mathf.RoundToInt(((hero?.base_magic ?? 10) - 10) * 0.40f), 1, kMaxStats);
            _displayStatsBuf[4] = Mathf.Clamp(8 + Mathf.RoundToInt(((hero?.base_resistance ?? 10) - 10) * 0.40f), 1, kMaxStats);
            _displayStatsBuf[5] = Mathf.Clamp(8 + Mathf.RoundToInt(((hero?.base_luck ?? 10) - 10) * 0.45f), 1, kMaxStats);
            return _displayStatsBuf;
        }

        private void ApplyStatsTheme(HeroData hero, int[] stats)
        {
            Color heroAccent = hero?.color_palette != null ? hero.color_palette.ToColor() : new Color(0.95f, 0.5f, 0.2f);

            int highestIndex = 0;
            int highestValue = int.MinValue;
            for (int i = 0; i < stats.Length; i++)
            {
                if (stats[i] > highestValue)
                {
                    highestValue = stats[i];
                    highestIndex = i;
                }
            }

            for (int i = 0; i < 6; i++)
            {
                if (_statRows[i] != null)
                {
                    _statRows[i].RemoveFromClassList("vb-stat-row-primary");
                    if (i == highestIndex)
                    {
                        _statRows[i].AddToClassList("vb-stat-row-primary");
                    }
                }

                if (_statFills[i] != null)
                {
                    Color fillColor = Color.Lerp(kStatFillBaseColors[i], heroAccent, 0.18f);
                    if (i == highestIndex)
                    {
                        fillColor = Color.Lerp(fillColor, Color.white, 0.22f);
                    }
                    fillColor.a = 0.95f;
                    _statFills[i].style.backgroundColor = fillColor;
                }
            }
        }

        private void ApplyAbilities(HeroData hero)
        {
            for (int i = 0; i < kMaxAbilities; i++)
            {
                string abilityName = string.Empty;
                string abilityDesc = string.Empty;
                bool hasAbility = false;

                string skillId = (hero?.innate_skills != null && i < hero.innate_skills.Length)
                    ? hero.innate_skills[i]
                    : null;

                SkillData skill = !string.IsNullOrWhiteSpace(skillId)
                    ? GameDatabase.Instance?.GetSkill(skillId)
                    : null;

                if (skill != null)
                {
                    hasAbility = true;
                    abilityName = FormatSkillName(skill.display_name);
                    abilityDesc = skill.description ?? string.Empty;
                }
                else if (!string.IsNullOrWhiteSpace(skillId))
                {
                    hasAbility = true;
                    abilityName = FormatSkillName(skillId);
                    abilityDesc = kMissingAbilityDescription;
                }
                else if (i < kFallbackAbilityIds.Length)
                {
                    SkillData fallbackSkill = GameDatabase.Instance?.GetSkill(kFallbackAbilityIds[i]);
                    hasAbility = true;
                    if (fallbackSkill != null)
                    {
                        abilityName = FormatSkillName(fallbackSkill.display_name);
                        abilityDesc = fallbackSkill.description ?? string.Empty;
                    }
                    else
                    {
                        abilityName = i == 0 ? "Attack" : "Defend";
                        abilityDesc = i == 0
                            ? "A basic physical attack."
                            : "Brace for incoming attacks this turn.";
                    }
                }
                else
                {
                    hasAbility = false;
                }

                if (_abilityRows[i] != null)
                {
                    _abilityRows[i].style.display = hasAbility ? DisplayStyle.Flex : DisplayStyle.None;
                }
                if (_abilityNames[i] != null) _abilityNames[i].text = abilityName;
                if (_abilityDescs[i] != null) _abilityDescs[i].text = abilityDesc;
            }
        }

        private void EnforceAbilityReadability()
        {
            for (int i = 0; i < kMaxAbilities; i++)
            {
                if (_abilityRows[i] != null)
                {
                    _abilityRows[i].style.backgroundColor = new Color(0.1f, 0.1f, 0.14f, 0.96f);
                    _abilityRows[i].style.borderTopColor = new Color(1f, 1f, 1f, 0.24f);
                    _abilityRows[i].style.borderBottomColor = new Color(1f, 1f, 1f, 0.24f);
                    _abilityRows[i].style.borderLeftColor = new Color(1f, 1f, 1f, 0.24f);
                    _abilityRows[i].style.borderRightColor = new Color(1f, 1f, 1f, 0.24f);
                    _abilityRows[i].style.paddingTop = 10f;
                    _abilityRows[i].style.paddingBottom = 10f;
                }

                if (_abilityNames[i] != null)
                {
                    _abilityNames[i].style.fontSize = 14f;
                    _abilityNames[i].style.unityFontStyleAndWeight = FontStyle.Bold;
                    _abilityNames[i].style.color = new Color(0.98f, 0.95f, 0.88f, 1f);
                }

                if (_abilityDescs[i] != null)
                {
                    _abilityDescs[i].style.fontSize = 12f;
                    _abilityDescs[i].style.color = new Color(0.92f, 0.9f, 0.84f, 0.98f);
                    _abilityDescs[i].style.unityTextAlign = TextAnchor.UpperLeft;
                }
            }
        }

        // =============================================================================
        // CAROUSEL
        // =============================================================================

        private void UpdateCarouselSelection(int activeIndex)
        {
            if (_carouselSlots == null || _carouselSlots.Count == 0)
            {
                UpdateHeroNavigationIndicator();
                return;
            }

            int clampedIndex = Mathf.Clamp(activeIndex, 0, _carouselSlots.Count - 1);

            for (int i = 0; i < _carouselSlots.Count; i++)
            {
                if (i == clampedIndex)
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

            UpdateHeroNavigationIndicator();
        }

        private void UpdateHeroNavigationIndicator()
        {
            if (_heroIndexIndicator == null || _heroes == null || _heroes.Count == 0) return;
            int heroNum = _currentIndex + 1;
            int heroCount = _heroes.Count;
            string numStr = heroNum <= 20 ? kNumberStrings[heroNum] : heroNum.ToString();
            string countStr = heroCount <= 20 ? kNumberStrings[heroCount] : heroCount.ToString();
            _heroIndexIndicator.text = string.Concat("HERO ", numStr, " / ", countStr);
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
            if (hero == null) yield break;

            int[] stats = GetDisplayStats(hero);
            ApplyStatsTheme(hero, stats);

            float duration = 0.6f;
            float elapsed = 0f;

            // Reset all fills to 0
            for (int i = 0; i < 6; i++)
            {
                if (_statFills[i] != null)
                    _statFills[i].style.width = new StyleLength(new Length(0, LengthUnit.Percent));
                if (_statValues[i] != null)
                    _statValues[i].text = kNumberStrings[0];
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
                        _statValues[i].text = displayVal <= 20 ? kNumberStrings[displayVal] : displayVal.ToString();
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
        private void OnNeutralToggleClicked(ClickEvent evt)
        {
            _environment?.ToggleNeutral();
            RefreshNeutralToggleButtonText();
        }
        private void OnPrevHeroClicked(ClickEvent evt) => SelectPreviousHero();
        private void OnNextHeroClicked(ClickEvent evt) => SelectNextHero();
        private void OnRotateModelLeftClicked(ClickEvent evt) => _heroStage?.RotateByStep(-1f);
        private void OnRotateModelRightClicked(ClickEvent evt) => _heroStage?.RotateByStep(1f);

        private void OnBack()
        {
            if (_isAnimating || _isDestroyed) return;
            _isAnimating = true;
            _btnBack?.SetEnabled(false);
            _btnEmbark?.SetEnabled(false);
            UnsubscribeInput();

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
            PlayerPrefs.Save();

            StartCoroutine(StartNewGameFlow(hero));
        }

        private IEnumerator StartNewGameFlow(HeroData hero)
        {
            yield return StartCoroutine(CreateOrRotateNewGameSave(hero));

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

        private IEnumerator CreateOrRotateNewGameSave(HeroData hero)
        {
            if (!SaveManager.HasInstance || hero == null)
            {
                yield break;
            }

            var saveManager = SaveManager.Instance;
            var slotTask = saveManager.GetBestNewGameSlotAsync();
            while (!slotTask.IsCompleted)
            {
                yield return null;
            }

            if (slotTask.IsFaulted || slotTask.IsCanceled)
            {
                Debug.LogWarning("[CharacterSelect] Failed to resolve save slot for new game.");
                yield break;
            }

            int slot = slotTask.Result;
            string heroName = string.IsNullOrEmpty(hero.display_name) ? hero.hero_id : hero.display_name;

            var createTask = saveManager.CreateNewSaveAsync(slot, hero.hero_id, heroName, hero.GetPrimaryPath());
            while (!createTask.IsCompleted)
            {
                yield return null;
            }

            if (createTask.IsFaulted || createTask.IsCanceled || !createTask.Result)
            {
                Debug.LogWarning($"[CharacterSelect] Failed to create new save in slot {slot}.");
                yield break;
            }

            saveManager.SetCurrentLocation(kStarterTownLocation);
            var saveTask = saveManager.SaveAsync(slot);
            while (!saveTask.IsCompleted)
            {
                yield return null;
            }
        }

        private IEnumerator FadeAndNavigate(string sceneName)
        {
            _screenFade?.RemoveFromClassList("hidden");
            _screenFade?.AddToClassList("active");

            yield return kWait10;

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

            yield return kWait02;
            if (_isDestroyed) yield break;

            // Fade screen in
            _screenFade?.RemoveFromClassList("active");

            yield return kWait03;
            if (_isDestroyed) yield break;

            // Stagger in UI elements
            var anim = UIAnimationController.Instance;
            if (anim != null)
            {
                if (_infoPanel != null) anim.FadeSlideIn(_infoPanel, UIAnimationController.SlideDirection.Left, duration: 0.4f);

                yield return kWait02;
                if (_isDestroyed) yield break;

                if (_carouselStrip != null) anim.FadeSlideIn(_carouselStrip, UIAnimationController.SlideDirection.Down, duration: 0.4f);

                yield return kWait01;
                if (_isDestroyed) yield break;

                if (_btnEmbark != null) anim.FadeIn(_btnEmbark, 0.3f);
            }
            else
            {
                EnsureCriticalUIVisible();
            }

            // Quote fade-in
            yield return kWait03;
            if (!_isDestroyed)
            {
                _quoteCoroutine = StartCoroutine(FadeInQuote());
            }
        }

        private IEnumerator VisibilityFailsafe()
        {
            yield return null;
            if (_isDestroyed) yield break;
            EnsureCriticalUIVisible();
            TryEnableReadabilityFallback();
            ApplyReadabilityFallbackStyles();

            yield return kWait03;
            if (_isDestroyed) yield break;
            EnsureCriticalUIVisible();
            TryEnableReadabilityFallback();
            ApplyReadabilityFallbackStyles();

            yield return kWait10;
            if (_isDestroyed) yield break;
            EnsureCriticalUIVisible();
            TryEnableReadabilityFallback();
            ApplyReadabilityFallbackStyles();
        }

        private void EnsureCriticalUIVisible()
        {
            ForceVisible(_infoPanel);
            ForceVisible(_statsPanel);
            ForceVisible(_carouselStrip);
            ForceVisible(_heroCycleHud);
            ForceVisible(_btnNeutralToggle);
            ForceVisible(_btnPrevHero);
            ForceVisible(_btnNextHero);
            ForceVisible(_btnRotateModelLeft);
            ForceVisible(_btnRotateModelRight);
            ForceVisible(_btnEmbark);

            var currentHero = CurrentHero;
            if (currentHero != null && _heroName != null && string.IsNullOrWhiteSpace(_heroName.text))
            {
                ApplyHeroData(currentHero);
                UpdateHeroNavigationIndicator();
            }
            else
            {
                ApplyStaticTextDefaults();
            }

            EnsureTextReadability();

            _screenFade?.RemoveFromClassList("active");
            if (_readabilityFallbackEnabled)
            {
                ApplyReadabilityFallbackStyles();
            }

            RefreshNeutralToggleButtonText();
        }

        private void RefreshNeutralToggleButtonText()
        {
            if (_btnNeutralToggle == null) return;

            bool neutralEnabled = _environment != null && _environment.IsNeutralMode;
            SetButtonLabel(_btnNeutralToggle, neutralEnabled ? "NEUTRAL BG: ON" : "NEUTRAL BG: OFF");
            _btnNeutralToggle.tooltip = neutralEnabled ? "Switch to hero color tint." : "Switch to neutral background.";
        }

        private void EnsureNavigationButtonVisuals()
        {
            // Re-assert layout to survive runtime style collisions.
            ApplyHeroNavigationLayout();
            var stage = _root?.Q<VisualElement>("hero-stage") ?? _heroStageRender?.parent;
            ApplyRotationButtonLayout(stage);

            if (_btnPrevHero != null)
            {
                _btnPrevHero.style.display = DisplayStyle.Flex;
                _btnPrevHero.style.opacity = 1f;
            }
            if (_btnNextHero != null)
            {
                _btnNextHero.style.display = DisplayStyle.Flex;
                _btnNextHero.style.opacity = 1f;
            }
            if (_btnRotateModelLeft != null)
            {
                _btnRotateModelLeft.style.display = DisplayStyle.Flex;
                _btnRotateModelLeft.style.opacity = 1f;
            }
            if (_btnRotateModelRight != null)
            {
                _btnRotateModelRight.style.display = DisplayStyle.Flex;
                _btnRotateModelRight.style.opacity = 1f;
            }
        }

        private static void ForceVisible(VisualElement element)
        {
            if (element == null) return;
            element.RemoveFromClassList("hidden");
            element.AddToClassList("visible");
            element.style.display = DisplayStyle.Flex;
            element.style.opacity = 1f;
            element.style.translate = new Translate(0f, 0f);
        }

        private void EnsureTextReadability()
        {
            if (_root == null) return;

            if (_fallbackUIFont == null)
            {
                _fallbackUIFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            Color fallbackTextColor = new Color(0.94f, 0.9f, 0.84f, 1f);
            var textElements = _cachedTextElements ?? _root.Query<TextElement>().ToList();
            foreach (var textElement in textElements)
            {
                if (textElement == null) continue;

                textElement.style.display = DisplayStyle.Flex;
                textElement.style.opacity = 1f;
                textElement.style.visibility = Visibility.Visible;
                textElement.style.color = fallbackTextColor;
                if (_fallbackUIFont != null)
                {
                    textElement.style.unityFont = _fallbackUIFont;
                }
                if (textElement.resolvedStyle.fontSize < 9f)
                {
                    textElement.style.fontSize = 12f;
                }
            }
        }

        private void ApplyStaticTextDefaults()
        {
            if (_heroName != null && string.IsNullOrWhiteSpace(_heroName.text)) _heroName.text = "VEX";
            if (_heroTitle != null && string.IsNullOrWhiteSpace(_heroTitle.text)) _heroTitle.text = "THE WARDEN";
            if (_heroPath != null && string.IsNullOrWhiteSpace(_heroPath.text)) _heroPath.text = "IRONBOUND";
            if (_heroRole != null && string.IsNullOrWhiteSpace(_heroRole.text)) _heroRole.text = "TANK";
            if (_heroResource != null && string.IsNullOrWhiteSpace(_heroResource.text)) _heroResource.text = "GUARD";
            if (_monsterName != null && string.IsNullOrWhiteSpace(_monsterName.text)) _monsterName.text = "Skitter-Teeth";
            if (_monsterBrand != null && string.IsNullOrWhiteSpace(_monsterBrand.text)) _monsterBrand.text = "IRON";
            if (_starterStatHp != null && string.IsNullOrWhiteSpace(_starterStatHp.text)) _starterStatHp.text = "68";
            if (_starterStatAtk != null && string.IsNullOrWhiteSpace(_starterStatAtk.text)) _starterStatAtk.text = "10";
            if (_starterStatDef != null && string.IsNullOrWhiteSpace(_starterStatDef.text)) _starterStatDef.text = "20";
            if (_starterStatSpd != null && string.IsNullOrWhiteSpace(_starterStatSpd.text)) _starterStatSpd.text = "5";
            if (_embarkLabel != null && string.IsNullOrWhiteSpace(_embarkLabel.text)) _embarkLabel.text = "EMBARK AS VEX";
            if (_heroIndexIndicator != null && string.IsNullOrWhiteSpace(_heroIndexIndicator.text))
            {
                _heroIndexIndicator.text = "HERO 1 / 4";
            }

            for (int i = 0; i < _statValues.Length; i++)
            {
                if (_statValues[i] != null && string.IsNullOrWhiteSpace(_statValues[i].text))
                {
                    _statValues[i].text = "10";
                }
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
            if (string.IsNullOrEmpty(rawName)) return string.Empty;
            if (_formattedSkillNames.TryGetValue(rawName, out var cached)) return cached;
            // Convert "shadow_strike" -> "Shadow Strike"
            var formatted = System.Globalization.CultureInfo.InvariantCulture.TextInfo
                .ToTitleCase(rawName.Replace("_", " "));
            _formattedSkillNames[rawName] = formatted;
            return formatted;
        }

        private void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;

            float effectiveVolume = volume;
            if (SettingsManager.HasInstance)
            {
                var settings = SettingsManager.Instance.Settings;
                if (settings != null)
                {
                    if (settings.MuteAll) return;
                    effectiveVolume *= Mathf.Clamp01(settings.MasterVolume) * Mathf.Clamp01(settings.SFXVolume);
                }
            }

            if (effectiveVolume <= 0.001f) return;

            if (_cachedCamera == null) _cachedCamera = Camera.main;
            if (_cachedCamera != null)
                AudioSource.PlayClipAtPoint(clip, _cachedCamera.transform.position, effectiveVolume);
        }

        private void EnsureCoreManagers()
        {
            EnsureSingleton<GameManager>("[GameManager]");
            EnsureSingleton<GameDatabase>("[GameDatabase]");
            EnsureSingleton<InputManager>("[InputManager]");
            EnsureSingleton<SaveManager>("[SaveManager]");
            EnsureSingleton<AutoSaveManager>("[AutoSaveManager]");
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
