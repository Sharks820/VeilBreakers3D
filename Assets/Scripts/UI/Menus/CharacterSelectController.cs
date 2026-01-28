using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.Data;
using VeilBreakers.UI.Core;

namespace VeilBreakers.UI.Menus
{
    /// <summary>
    /// Controller for the Character Select screen.
    /// Manages hero list, selection, and preview functionality.
    /// Uses GameDatabase to load hero data from JSON.
    /// </summary>
    public class CharacterSelectController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Hero Model Prefabs (keyed by hero_id)")]
        [SerializeField] private List<HeroModelMapping> _heroModelMappings;

        [Serializable]
        public class HeroModelMapping
        {
            public string heroId;
            public GameObject modelPrefab;
            public Sprite portrait;
        }

        // Non-allocating event handler for hero cards
        private class HeroCardEventHandler
        {
            private readonly CharacterSelectController _controller;
            private readonly int _heroIndex;
            private readonly VisualElement _card;
            private readonly Color _heroColor;
            private readonly Color _heroColorGlow;

            public HeroCardEventHandler(CharacterSelectController controller, int index, VisualElement card, Color heroColor, Color heroColorGlow)
            {
                _controller = controller;
                _heroIndex = index;
                _card = card;
                _heroColor = heroColor;
                _heroColorGlow = heroColorGlow;
            }

            public void OnClick(ClickEvent evt) => _controller.OnHeroCardClicked(_heroIndex);

            public void OnMouseEnter(MouseEnterEvent evt)
            {
                if (_controller._selectedHeroIndex != _heroIndex)
                {
                    _controller.ApplyHeroHover(_card, _heroColor, _heroColorGlow, 0.22f);
                    UIAnimationController.Instance?.FadeSlideIn(_card, UIAnimationController.SlideDirection.Right, 4f, 0.12f);
                }
            }

            public void OnMouseLeave(MouseLeaveEvent evt)
            {
                if (_controller._selectedHeroIndex != _heroIndex)
                {
                    _controller.ResetHeroCardVisual(_card, _heroColor);
                }
            }
        }

        // Runtime hero list from GameDatabase
        private List<HeroData> _availableHeroes;

        [Header("3D Preview")]
        [SerializeField] private Transform _previewModelParent;
        [SerializeField] private Camera _previewCamera;
        [SerializeField] private RenderTexture _previewRenderTexture;

        [Header("Scenes")]
        [SerializeField] private string _mainMenuScene = "MainMenu";
        [SerializeField] private string _gameScene = "TestArena";

        // =============================================================================
        // UI ELEMENTS
        // =============================================================================

        private VisualElement _root;
        private VisualElement _heroList;
        private VisualElement _modelViewport;
        private VisualElement _modelDisplay;
        private VisualElement _placeholder;
        private VisualElement _previewRing;
        private Label _previewIcon;
        private VisualElement _detailsPanel;

        // Header elements
        private Label _heroName;
        private Label _heroTitle;

        // Brand elements
        private VisualElement _primaryBrand;
        private VisualElement _secondaryBrand;

        // Path elements
        private Label _pathName;
        private VisualElement _pathBadge;

        // Brand affinity elements
        private VisualElement _brandIcon1;
        private Label _brandName1;
        private VisualElement _brandIcon2;
        private Label _brandName2;

        // Monster elements
        private Label _monsterName;
        private Label _monsterType;
        private Label _monsterAbilityName;
        private Label _monsterAbilityDesc;
        private VisualElement _monsterBrandIcons;
        private VisualElement _monsterSectionBar;
        private Label _monsterSectionLabel;
        
        // Orb elements
        private VisualElement _heroBrandOrb;
        private VisualElement _heroPathOrb;
        private VisualElement _monsterBrandOrb1;
        private VisualElement _monsterBrandOrb2;

        // Innate ability elements
        private Label _innateAbilityName;
        private Label _innateAbilityDesc;

        // Stat elements
        private VisualElement _statHealthFill;
        private VisualElement _statMpFill;
        private VisualElement _statAttackFill;
        private VisualElement _statDefenseFill;
        private VisualElement _statSpeedFill;
        private Label _statHealthValue;
        private Label _statMpValue;
        private Label _statAttackValue;
        private Label _statDefenseValue;
        private Label _statSpeedValue;

        // Ability elements
        private List<VisualElement> _abilityCards = new();

        // Buttons
        private Button _btnBack;
        private Button _btnSelect;

        // =============================================================================
        // STATE
        // =============================================================================

        private int _selectedHeroIndex = -1;
        private HeroData _selectedHero;
        private GameObject _currentPreviewModel;
        private List<VisualElement> _heroCards = new();
        private List<HeroCardEventHandler> _heroCardHandlers = new(); // Cache event handlers (no allocation)

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action OnBackClicked;
        public event Action<HeroData> OnHeroSelected;

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
            PopulateHeroList();
            PlayEntranceAnimation();
        }

        private void OnDisable()
        {
            UnbindEvents();
            ClearPreviewModel();
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void InitializeUI()
        {
            if (_uiDocument == null)
            {
                ErrorLogger.Error("CharacterSelectController: UIDocument is null!");
                return;
            }

            _root = _uiDocument.rootVisualElement;

            // Query main containers
            _heroList = _root.Q<VisualElement>("hero-list");
            _modelViewport = _root.Q<VisualElement>("model-viewport");
            _modelDisplay = _root.Q<VisualElement>("model-display");
            _placeholder = _root.Q<VisualElement>("placeholder");
            _previewRing = _root.Q<VisualElement>("preview-ring");
            _previewIcon = _root.Q<Label>("preview-icon");
            _detailsPanel = _root.Q<VisualElement>("details-panel");

            // Query header
            _heroName = _root.Q<Label>("hero-name");
            _heroTitle = _root.Q<Label>("hero-title");

            // Query brands
            _primaryBrand = _root.Q<VisualElement>("primary-brand");
            _secondaryBrand = _root.Q<VisualElement>("secondary-brand");

            // Query path elements
            _pathName = _root.Q<Label>("path-name");
            _pathBadge = _root.Q<VisualElement>("path-badge");

            // Query brand affinity elements
            _brandIcon1 = _root.Q<VisualElement>("brand-icon-1");
            _brandName1 = _root.Q<Label>("brand-name-1");
            _brandIcon2 = _root.Q<VisualElement>("brand-icon-2");
            _brandName2 = _root.Q<Label>("brand-name-2");

            // Query monster elements
            _monsterName = _root.Q<Label>("monster-name");
            _monsterType = _root.Q<Label>("monster-type");
            _monsterAbilityName = _root.Q<Label>("monster-ability-name");
            _monsterAbilityDesc = _root.Q<Label>("monster-ability-desc");
            _monsterSectionBar = _root.Q<VisualElement>("monster-section-bar");
            _monsterSectionLabel = _root.Q<Label>("monster-section-label");
            
            // Query orb elements
            _heroBrandOrb = _root.Q<VisualElement>("hero-brand-orb");
            _heroPathOrb = _root.Q<VisualElement>("hero-path-orb");
            _monsterBrandOrb1 = _root.Q<VisualElement>("monster-brand-orb-1");
            _monsterBrandOrb2 = _root.Q<VisualElement>("monster-brand-orb-2");

            // Query innate ability elements
            _innateAbilityName = _root.Q<Label>("innate-ability-name");
            _innateAbilityDesc = _root.Q<Label>("innate-ability-desc");

            // Query stats
            _statHealthFill = _root.Q<VisualElement>("stat-health-fill");
            _statMpFill = _root.Q<VisualElement>("stat-mp-fill");
            _statAttackFill = _root.Q<VisualElement>("stat-attack-fill");
            _statDefenseFill = _root.Q<VisualElement>("stat-defense-fill");
            _statSpeedFill = _root.Q<VisualElement>("stat-speed-fill");
            _statHealthValue = _root.Q<Label>("stat-health-value");
            _statMpValue = _root.Q<Label>("stat-mp-value");
            _statAttackValue = _root.Q<Label>("stat-attack-value");
            _statDefenseValue = _root.Q<Label>("stat-defense-value");
            _statSpeedValue = _root.Q<Label>("stat-speed-value");

            // Query abilities
            _abilityCards.Add(_root.Q<VisualElement>("ability-1"));
            _abilityCards.Add(_root.Q<VisualElement>("ability-2"));
            _abilityCards.Add(_root.Q<VisualElement>("ability-3"));

            // Query buttons
            _btnBack = _root.Q<Button>("btn-back");
            _btnSelect = _root.Q<Button>("btn-select");

            // Initial state
            _btnSelect?.SetEnabled(false);

            // Bind events
            BindEvents();

            // Setup preview render texture if available
            if (_previewRenderTexture != null && _modelDisplay != null)
            {
                _modelDisplay.style.backgroundImage = new StyleBackground(
                    Background.FromRenderTexture(_previewRenderTexture)
                );
            }

            ErrorLogger.UI("CharacterSelect initialized");
        }

        private void BindEvents()
        {
            _btnBack?.RegisterCallback<ClickEvent>(OnBackButtonClicked);
            _btnSelect?.RegisterCallback<ClickEvent>(OnSelectButtonClicked);

            // Keyboard navigation
            _root?.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void UnbindEvents()
        {
            _btnBack?.UnregisterCallback<ClickEvent>(OnBackButtonClicked);
            _btnSelect?.UnregisterCallback<ClickEvent>(OnSelectButtonClicked);
            _root?.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        // =============================================================================
        // HERO LIST
        // =============================================================================

        private void PopulateHeroList()
        {
            if (_heroList == null) return;

            _heroList.Clear();
            _heroCards.Clear();
            _heroCardHandlers.Clear(); // Clear event handlers (avoid memory leaks)

            // Load heroes from GameDatabase
            _availableHeroes = new List<HeroData>();
            if (GameDatabase.Instance != null)
            {
                var allHeroes = GameDatabase.Instance.GetAllHeroes();
                if (allHeroes != null)
                {
                    _availableHeroes.AddRange(allHeroes);
                }
            }

            if (_availableHeroes == null || _availableHeroes.Count == 0)
            {
                // Show placeholder if no heroes
                var noHeroLabel = new Label("No heroes available");
                noHeroLabel.AddToClassList("vb-text-secondary");
                _heroList.Add(noHeroLabel);
                return;
            }

            for (int i = 0; i < _availableHeroes.Count; i++)
            {
                var hero = _availableHeroes[i];
                var heroCard = CreateHeroCard(hero, i);
                _heroList.Add(heroCard);
                _heroCards.Add(heroCard);
            }
        }

        private VisualElement CreateHeroCard(HeroData hero, int index)
        {
            var heroColor = GetHeroColor(hero);
            var heroColorDark = GetHeroColorDark(hero);
            var heroColorGlow = GetHeroColorGlow(hero);

            var card = new VisualElement();
            card.name = $"hero-card-{index}";
            card.AddToClassList("hero-card");
            // Apply hero-specific color class for CSS theming
            card.AddToClassList($"hero-{hero.hero_id.ToLower()}");
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;
            card.style.paddingTop = 12;
            card.style.paddingBottom = 12;
            card.style.paddingLeft = 12;
            card.style.paddingRight = 12;
            card.style.marginBottom = 8;
            card.style.backgroundColor = new Color(25f/255f, 20f/255f, 28f/255f);
            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1;
            card.style.borderRightWidth = 1;
            var baseBorder = new Color(heroColor.r * 0.35f + 0.15f, heroColor.g * 0.35f + 0.12f, heroColor.b * 0.35f + 0.15f, 1f);
            card.style.borderTopColor = baseBorder;
            card.style.borderBottomColor = baseBorder;
            card.style.borderLeftColor = baseBorder;
            card.style.borderRightColor = baseBorder;
            // Persist color on the card for resets
            card.userData = heroColor;
            card.style.borderTopLeftRadius = 6;
            card.style.borderTopRightRadius = 6;
            card.style.borderBottomLeftRadius = 6;
            card.style.borderBottomRightRadius = 6;

            // Portrait - use explicit sizing
            var portrait = new VisualElement();
            portrait.name = "portrait";
            portrait.style.width = 48;
            portrait.style.minWidth = 48;
            portrait.style.height = 48;
            portrait.style.minHeight = 48;
            portrait.style.flexShrink = 0;
            portrait.style.backgroundColor = new Color(35f/255f, 28f/255f, 35f/255f);
            portrait.style.borderTopLeftRadius = 6;
            portrait.style.borderTopRightRadius = 6;
            portrait.style.borderBottomRightRadius = 6;
            portrait.style.borderBottomLeftRadius = 6;
            portrait.style.marginRight = 12;
            portrait.style.borderTopColor = heroColor;
            portrait.style.borderBottomColor = heroColor;
            portrait.style.borderLeftColor = heroColor;
            portrait.style.borderRightColor = heroColor;
            portrait.style.borderTopWidth = 1;
            portrait.style.borderBottomWidth = 1;
            portrait.style.borderLeftWidth = 1;
            portrait.style.borderRightWidth = 1;

            // Try to find portrait from mapping
            var mapping = GetHeroMapping(hero.hero_id);
            if (mapping?.portrait != null)
            {
                portrait.style.backgroundImage = new StyleBackground(mapping.portrait);
                portrait.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            }

            card.Add(portrait);

            // Info container
            var info = new VisualElement();
            info.style.flexGrow = 1;
            info.style.flexShrink = 1;
            info.style.overflow = Overflow.Hidden;

            // Name
            var nameLabel = new Label(hero.display_name);
            nameLabel.style.color = new Color(235f/255f, 225f/255f, 215f/255f);
            nameLabel.style.fontSize = 15;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.marginBottom = 2;
            info.Add(nameLabel);

            // Brand and Path affinity row
            var affinityRow = new VisualElement();
            affinityRow.style.flexDirection = FlexDirection.Row;
            affinityRow.style.alignItems = Align.Center;
            affinityRow.style.marginBottom = 4;

            // Brand name
            var primaryBrand = hero.GetPrimaryBrand();
            var brandLabel = new Label(primaryBrand.ToString());
            brandLabel.style.color = new Color(heroColor.r * 0.9f + 0.1f, heroColor.g * 0.9f + 0.1f, heroColor.b * 0.9f + 0.1f, 1f);
            brandLabel.style.fontSize = 9;
            brandLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            brandLabel.style.letterSpacing = 1;
            affinityRow.Add(brandLabel);

            // Separator
            var separator = new Label("•");
            separator.style.color = new Color(100f/255f, 90f/255f, 85f/255f);
            separator.style.fontSize = 9;
            separator.style.marginLeft = 4;
            separator.style.marginRight = 4;
            affinityRow.Add(separator);

            // Path name
            var primaryPath = hero.GetPrimaryPath();
            var pathLabel = new Label(primaryPath.ToString());
            pathLabel.style.color = new Color(140f/255f, 130f/255f, 120f/255f);
            pathLabel.style.fontSize = 9;
            pathLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            pathLabel.style.letterSpacing = 1;
            affinityRow.Add(pathLabel);

            info.Add(affinityRow);

            // Brands row (visual indicators)
            var brandsRow = new VisualElement();
            brandsRow.style.flexDirection = FlexDirection.Row;
            brandsRow.style.alignItems = Align.Center;

            var primaryIndicator = CreateBrandIndicator(primaryBrand, heroColor);
            brandsRow.Add(primaryIndicator);

            info.Add(brandsRow);
            card.Add(info);

            // Selection indicator (left bar)
            var selectIndicator = new VisualElement();
            selectIndicator.name = "select-indicator";
            selectIndicator.style.width = 4;
            selectIndicator.style.position = Position.Absolute;
            selectIndicator.style.left = 0;
            selectIndicator.style.top = 0;
            selectIndicator.style.bottom = 0;
            selectIndicator.style.backgroundColor = heroColor;
            selectIndicator.style.borderTopLeftRadius = 6;
            selectIndicator.style.borderBottomLeftRadius = 6;
            selectIndicator.style.opacity = 0;
            card.Add(selectIndicator);

            // Create reusable event handler (no lambda allocation)
            var handler = new HeroCardEventHandler(this, index, card, heroColor, heroColorGlow);
            _heroCardHandlers.Add(handler);

            // Register callbacks using handler methods (no lambda allocation)
            card.RegisterCallback<ClickEvent>(handler.OnClick);
            card.RegisterCallback<MouseEnterEvent>(handler.OnMouseEnter);
            card.RegisterCallback<MouseLeaveEvent>(handler.OnMouseLeave);

            return card;
        }

        private void ApplyHeroHover(VisualElement card, Color heroColor, Color heroGlow, float tintStrength)
        {
            card.style.backgroundColor = new Color(
                Mathf.Clamp01(heroColor.r * tintStrength + 0.10f),
                Mathf.Clamp01(heroColor.g * tintStrength + 0.08f),
                Mathf.Clamp01(heroColor.b * tintStrength + 0.10f),
                1f);
            card.style.borderTopColor = heroGlow;
            card.style.borderRightColor = heroGlow;
            card.style.borderBottomColor = heroGlow;
            card.style.borderLeftColor = heroGlow;

            var indicator = card.Q("select-indicator");
            if (indicator != null)
            {
                indicator.style.opacity = 0.4f;
                indicator.style.backgroundColor = heroGlow;
            }
        }

        private void ResetHeroCardVisual(VisualElement card, Color heroColor)
        {
            card.style.backgroundColor = new Color(25f/255f, 20f/255f, 28f/255f);
            var borderColor = new Color(heroColor.r * 0.35f + 0.15f, heroColor.g * 0.35f + 0.12f, heroColor.b * 0.35f + 0.15f, 1f);
            card.style.borderTopColor = borderColor;
            card.style.borderRightColor = borderColor;
            card.style.borderBottomColor = borderColor;
            card.style.borderLeftColor = borderColor;

            var indicator = card.Q("select-indicator");
            if (indicator != null && (_selectedHeroIndex == -1 || card.name != $"hero-card-{_selectedHeroIndex}"))
                indicator.style.opacity = 0;
        }

        private VisualElement CreateBrandIndicator(Brand brand, Color? heroColor = null)
        {
            var indicator = new VisualElement();
            indicator.style.width = 12;
            indicator.style.height = 12;
            indicator.style.borderTopLeftRadius = new StyleLength(6);
            indicator.style.borderTopRightRadius = new StyleLength(6);
            indicator.style.borderBottomRightRadius = new StyleLength(6);
            indicator.style.borderBottomLeftRadius = new StyleLength(6);
            
            // Use hero color if provided, otherwise use brand color
            indicator.style.backgroundColor = heroColor ?? ThemeManager.Instance.GetBrandColor(brand);
            return indicator;
        }

        private HeroModelMapping GetHeroMapping(string heroId)
        {
            if (_heroModelMappings == null) return null;
            return _heroModelMappings.Find(m => m.heroId == heroId);
        }

        // =============================================================================
        // SELECTION
        // =============================================================================

        private void OnHeroCardClicked(int index)
        {
            if (index < 0 || index >= _availableHeroes.Count) return;

            // Get the hero for color theming
            var hero = _availableHeroes[index];

            // Update selection visuals with hero-specific colors
            UpdateSelectionVisuals(index, GetHeroColor(hero));

            // Update selected hero
            _selectedHeroIndex = index;
            _selectedHero = hero;

            // Update details panel
            UpdateDetailsPanel(_selectedHero);

            // Apply hero theme colors to the entire UI
            ApplyHeroThemeColors(_selectedHero);

            // Update preview
            UpdatePreviewModel(_selectedHero);

            // Enable select button
            _btnSelect?.SetEnabled(true);

            // Play selection sound
            // AudioManager.Instance?.PlaySFX("UI_Select");
        }

        private Color GetHeroColor(HeroData hero)
        {
            // Use the hero's color palette from JSON
            return new Color(
                hero.color_palette.r,
                hero.color_palette.g,
                hero.color_palette.b,
                hero.color_palette.a
            );
        }

        private Color GetHeroColorDark(HeroData hero)
        {
            var c = GetHeroColor(hero);
            return new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, 1f);
        }

        private Color GetHeroColorGlow(HeroData hero)
        {
            var c = GetHeroColor(hero);
            return new Color(
                Mathf.Min(1f, c.r * 1.3f),
                Mathf.Min(1f, c.g * 1.3f),
                Mathf.Min(1f, c.b * 1.3f),
                1f
            );
        }

        private void UpdateSelectionVisuals(int newIndex, Color heroColor)
        {
            // Deselect previous
            if (_selectedHeroIndex >= 0 && _selectedHeroIndex < _heroCards.Count)
            {
                var prevCard = _heroCards[_selectedHeroIndex];
                var prevHeroColor = GetHeroColor(_availableHeroes[_selectedHeroIndex]);
                ResetHeroCardVisual(prevCard, prevHeroColor);
            }

            // Select new with hero-specific color
            if (newIndex >= 0 && newIndex < _heroCards.Count)
            {
                var newCard = _heroCards[newIndex];
                // Background tinted with hero color
                newCard.style.backgroundColor = new Color(
                    heroColor.r * 0.2f + 0.08f,
                    heroColor.g * 0.2f + 0.06f,
                    heroColor.b * 0.2f + 0.08f
                );
                // Border in hero color
                newCard.style.borderTopColor = heroColor;
                newCard.style.borderRightColor = heroColor;
                newCard.style.borderBottomColor = heroColor;
                newCard.style.borderLeftColor = heroColor;

                // Selection indicator in hero color
                var newIndicator = newCard.Q("select-indicator");
                if (newIndicator != null)
                {
                    newIndicator.style.opacity = 1;
                    newIndicator.style.backgroundColor = heroColor;
                }

                // Animate selection
                UIAnimationController.Instance?.PunchScale(newCard, 1.02f, 0.15f);
            }
        }

        private void ApplyHeroThemeColors(HeroData hero)
        {
            var heroColor = GetHeroColor(hero);
            var heroColorDark = GetHeroColorDark(hero);
            var heroColorGlow = GetHeroColorGlow(hero);

            // Apply to details panel border
            if (_detailsPanel != null)
            {
                _detailsPanel.style.borderTopColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.5f);
                _detailsPanel.style.borderRightColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.5f);
                _detailsPanel.style.borderBottomColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.5f);
                _detailsPanel.style.borderLeftColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.5f);
            }

            // Apply to model viewport border
            if (_modelViewport != null)
            {
                _modelViewport.style.borderTopColor = heroColor;
                _modelViewport.style.borderRightColor = heroColor;
                _modelViewport.style.borderBottomColor = heroColor;
                _modelViewport.style.borderLeftColor = heroColor;
                _modelViewport.style.backgroundColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.15f);
            }

            // Apply to center preview ring/icon
            if (_previewRing != null)
            {
                _previewRing.style.borderTopColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.5f);
                _previewRing.style.borderRightColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.5f);
                _previewRing.style.borderBottomColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.5f);
                _previewRing.style.borderLeftColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.5f);
            }
            if (_previewIcon != null)
            {
                _previewIcon.style.color = new Color(heroColor.r, heroColor.g, heroColor.b, 0.6f);
            }

            // Apply to hero name
            if (_heroName != null)
            {
                _heroName.style.color = heroColorGlow;
            }

            // Apply to select button
            if (_btnSelect != null)
            {
                _btnSelect.style.backgroundColor = heroColorDark;
                _btnSelect.style.borderTopColor = heroColor;
                _btnSelect.style.borderRightColor = heroColor;
                _btnSelect.style.borderBottomColor = heroColor;
                _btnSelect.style.borderLeftColor = heroColor;
            }

            // Apply to signature monster section
            var monsterSection = _root?.Q<VisualElement>("monster-section");
            if (monsterSection != null)
            {
                monsterSection.style.borderTopColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.4f);
                monsterSection.style.borderRightColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.4f);
                monsterSection.style.borderBottomColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.4f);
                monsterSection.style.borderLeftColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.4f);
            }

            // Apply to monster icon frame
            var monsterIconFrame = _root?.Q<VisualElement>("monster-icon-frame");
            if (monsterIconFrame != null)
            {
                monsterIconFrame.style.borderTopColor = heroColor;
                monsterIconFrame.style.borderRightColor = heroColor;
                monsterIconFrame.style.borderBottomColor = heroColor;
                monsterIconFrame.style.borderLeftColor = heroColor;
            }

            // Apply to path badge - full color treatment
            if (_pathBadge != null)
            {
                _pathBadge.style.borderTopColor = heroColor;
                _pathBadge.style.borderRightColor = heroColor;
                _pathBadge.style.borderBottomColor = heroColor;
                _pathBadge.style.borderLeftColor = heroColor;
                _pathBadge.style.backgroundColor = new Color(heroColor.r * 0.2f, heroColor.g * 0.2f, heroColor.b * 0.2f, 0.8f);

                // Update the path indicator dot
                var pathIndicator = _pathBadge.Q<VisualElement>();
                if (pathIndicator != null)
                    pathIndicator.style.backgroundColor = heroColor;
            }

            // Apply to hero list panel border
            var heroListPanel = _root?.Q<VisualElement>("hero-list-panel");
            if (heroListPanel != null)
            {
                heroListPanel.style.borderTopColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.5f);
                heroListPanel.style.borderRightColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.5f);
                heroListPanel.style.borderBottomColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.5f);
                heroListPanel.style.borderLeftColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.5f);
            }

            // Apply to signature monster card
            var signatureMonsterCard = _root?.Q<VisualElement>("signature-monster-card");
            if (signatureMonsterCard != null)
            {
                signatureMonsterCard.style.borderTopColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.5f);
                signatureMonsterCard.style.borderRightColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.5f);
                signatureMonsterCard.style.borderBottomColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.5f);
                signatureMonsterCard.style.borderLeftColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.5f);
            }

            // Apply to header border
            var header = _root?.Q<VisualElement>("header");
            if (header != null)
            {
                header.style.borderBottomColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.4f);
            }

            // Apply to footer border
            var footer = _root?.Q<VisualElement>("footer");
            if (footer != null)
            {
                footer.style.borderTopColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.4f);
            }

            // Apply to hero title (below name)
            if (_heroTitle != null)
            {
                _heroTitle.style.color = heroColor;
            }

            // Apply to brand icons
            if (_brandIcon1 != null)
                _brandIcon1.style.backgroundColor = heroColor;

            // NOTE: Stat bar colors are now defined in UXML with individual colors:
            // Health=#DC6464, MP=#64B4DC, Attack=#E6A064, Defense=#82AAC8, Speed=#78C88C
        }

        // =============================================================================
        // DETAILS PANEL
        // =============================================================================

        private void UpdateDetailsPanel(HeroData hero)
        {
            if (hero == null) return;

            // Update name and title
            if (_heroName != null) _heroName.text = hero.display_name;
            if (_heroTitle != null) _heroTitle.text = hero.title ?? "";

            // Update path display
            UpdatePathDisplay(hero);

            // Update brands
            UpdateBrandDisplay(hero);

            // Update stats
            UpdateStatBars(hero);
            
            // Update orb colors
            UpdateOrbColors(hero);

            // Update signature monster
            UpdateMonsterDisplay(hero);

            // Update innate abilities
            UpdateInateAbilityDisplay(hero);

            // Hide placeholder, show model
            if (_placeholder != null) _placeholder.style.display = DisplayStyle.None;
            if (_modelDisplay != null) _modelDisplay.style.display = DisplayStyle.Flex;

            // Animate details panel
            UIAnimationController.Instance?.FadeIn(_detailsPanel, 0.3f);
        }

        private void UpdatePathDisplay(HeroData hero)
        {
            var path = hero.GetPrimaryPath();

            if (_pathName != null)
            {
                _pathName.text = path.ToString();
                // Also color the path name text with hero color
                var heroColor = GetHeroColor(hero);
                _pathName.style.color = new Color(
                    Mathf.Min(1f, heroColor.r * 1.2f + 0.2f),
                    Mathf.Min(1f, heroColor.g * 1.2f + 0.2f),
                    Mathf.Min(1f, heroColor.b * 1.2f + 0.2f),
                    1f
                );
            }

            // Path badge is now updated in ApplyHeroThemeColors using hero color
        }

        private void UpdateBrandDisplay(HeroData hero)
        {
            var primaryBrand = hero.GetPrimaryBrand();
            var brandColor = ThemeManager.Instance.GetBrandColor(primaryBrand);

            // Update the new brand affinity display
            if (_brandIcon1 != null)
                _brandIcon1.style.backgroundColor = brandColor;
            if (_brandName1 != null)
                _brandName1.text = primaryBrand.ToString();

            // Show path in second badge for clarity
            var primaryPath = hero.GetPrimaryPath();
            if (_brandIcon2 != null)
                _brandIcon2.style.display = DisplayStyle.Flex;
            if (_brandName2 != null)
            {
                _brandName2.style.display = DisplayStyle.Flex;
                _brandName2.text = primaryPath.ToString();
            }

            // Also update the old primary brand element if it exists
            if (_primaryBrand != null)
            {
                var icon = _primaryBrand.Q(className: "brand-icon");
                var name = _primaryBrand.Q<Label>(className: "brand-name");

                if (icon != null)
                    icon.style.backgroundColor = brandColor;
                if (name != null)
                    name.text = primaryBrand.ToString();
            }

            // Hide secondary brand display since HeroData only has primary
            if (_secondaryBrand != null)
            {
                _secondaryBrand.style.display = DisplayStyle.None;
            }
        }

        private void UpdateMonsterDisplay(HeroData hero)
        {
            // Update monster section colors to match hero theme
            if (hero != null)
            {
                Color heroColor = new Color(
                    hero.color_palette.r,
                    hero.color_palette.g,
                    hero.color_palette.b,
                    1f
                );
                
                if (_monsterSectionBar != null)
                    _monsterSectionBar.style.backgroundColor = heroColor;
                if (_monsterSectionLabel != null)
                    _monsterSectionLabel.style.color = heroColor;
            }
            
            // Get signature monster from recommended_monsters
            if (hero.recommended_monsters != null && hero.recommended_monsters.Length > 0)
            {
                string signatureMonsterId = hero.recommended_monsters[0];

                // Try to get monster data from GameDatabase
                MonsterData monsterData = null;
                if (GameDatabase.Instance != null)
                {
                    monsterData = GameDatabase.Instance.GetMonster(signatureMonsterId);
                }

                if (monsterData != null)
                {
                    if (_monsterName != null)
                        _monsterName.text = monsterData.display_name;
                    if (_monsterType != null)
                    {
                        var brand = (Brand)monsterData.brand;
                        _monsterType.text = $"{brand} - Tier {monsterData.tier}";
                    }

                    // Update monster skills if available
                    if (monsterData.innate_skills != null && monsterData.innate_skills.Length > 0)
                    {
                        if (_monsterAbilityName != null)
                            _monsterAbilityName.text = FormatMonsterId(monsterData.innate_skills[0]);
                        if (_monsterAbilityDesc != null)
                            _monsterAbilityDesc.text = monsterData.description ?? "Signature monster ability";
                    }
                }
                else
                {
                    // Fallback - just display the ID cleaned up
                    if (_monsterName != null)
                        _monsterName.text = FormatMonsterId(signatureMonsterId);
                    if (_monsterType != null)
                        _monsterType.text = "Signature Monster";
                }
            }
            else
            {
                if (_monsterName != null)
                    _monsterName.text = "No Monster";
                if (_monsterType != null)
                    _monsterType.text = "---";
            }
        }

        private string FormatMonsterId(string id)
        {
            // Convert "shadow_hound" to "Shadow Hound"
            if (string.IsNullOrEmpty(id)) return "Unknown";
            var words = id.Split('_');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
            }
            return string.Join(" ", words);
        }

        private void UpdateInateAbilityDisplay(HeroData hero)
        {
            if (hero.innate_skills != null && hero.innate_skills.Length > 0)
            {
                if (_innateAbilityName != null)
                    _innateAbilityName.text = hero.innate_skills[0];
                if (_innateAbilityDesc != null)
                    _innateAbilityDesc.text = hero.combat_description ?? "Hero innate ability";
            }
            else
            {
                if (_innateAbilityName != null)
                    _innateAbilityName.text = "None";
                if (_innateAbilityDesc != null)
                    _innateAbilityDesc.text = "";
            }
        }

        private void UpdateStatBars(HeroData hero)
        {
            const float maxStat = 150f; // For percentage calculation

            // Health
            if (_statHealthFill != null && _statHealthValue != null)
            {
                float healthPercent = Mathf.Clamp01(hero.base_hp / maxStat);
                _statHealthFill.style.width = Length.Percent(healthPercent * 100);
                _statHealthValue.text = hero.base_hp.ToString();
            }

            // MP (NEW)
            if (_statMpFill != null && _statMpValue != null)
            {
                float mpPercent = Mathf.Clamp01(hero.base_mp / maxStat);
                _statMpFill.style.width = Length.Percent(mpPercent * 100);
                _statMpValue.text = hero.base_mp.ToString();
            }

            // Attack
            if (_statAttackFill != null && _statAttackValue != null)
            {
                float attackPercent = Mathf.Clamp01(hero.base_attack / maxStat);
                _statAttackFill.style.width = Length.Percent(attackPercent * 100);
                _statAttackValue.text = hero.base_attack.ToString();
            }

            // Defense
            if (_statDefenseFill != null && _statDefenseValue != null)
            {
                float defensePercent = Mathf.Clamp01(hero.base_defense / maxStat);
                _statDefenseFill.style.width = Length.Percent(defensePercent * 100);
                _statDefenseValue.text = hero.base_defense.ToString();
            }

            // Speed
            if (_statSpeedFill != null && _statSpeedValue != null)
            {
                float speedPercent = Mathf.Clamp01(hero.base_speed / maxStat);
                _statSpeedFill.style.width = Length.Percent(speedPercent * 100);
                _statSpeedValue.text = hero.base_speed.ToString();
            }
        }

        private void UpdateOrbColors(HeroData hero)
        {
            if (hero == null) return;

            // Get hero color from palette
            Color heroColor = new Color(
                hero.color_palette.r,
                hero.color_palette.g,
                hero.color_palette.b,
                1f
            );

            // Set hero brand orb to hero's color
            if (_heroBrandOrb != null)
            {
                _heroBrandOrb.style.backgroundColor = heroColor;
            }

            // Set hero path orb to a darker shade of hero color
            if (_heroPathOrb != null)
            {
                Color pathColor = new Color(
                    heroColor.r * 0.6f,
                    heroColor.g * 0.6f,
                    heroColor.b * 0.6f,
                    1f
                );
                _heroPathOrb.style.backgroundColor = pathColor;
            }

            // Set monster brand orbs based on recommended monster's brands
            if (hero.recommended_monsters != null && hero.recommended_monsters.Length > 0)
            {
                string monsterId = hero.recommended_monsters[0];
                MonsterData monsterData = GameDatabase.Instance?.GetMonster(monsterId);

                if (monsterData != null)
                {
                    // Monster brand orb 1 - primary brand color
                    if (_monsterBrandOrb1 != null)
                    {
                        Color brandColor1 = ThemeManager.Instance.GetBrandColor(monsterData.GetPrimaryBrand());
                        _monsterBrandOrb1.style.backgroundColor = brandColor1;
                    }

                    // Monster brand orb 2 - secondary brand color (darker)
                    if (_monsterBrandOrb2 != null)
                    {
                        Color brandColor2 = ThemeManager.Instance.GetBrandColor(monsterData.GetPrimaryBrand());
                        Color darkerBrand = new Color(
                            brandColor2.r * 0.5f,
                            brandColor2.g * 0.5f,
                            brandColor2.b * 0.5f,
                            1f
                        );
                        _monsterBrandOrb2.style.backgroundColor = darkerBrand;
                    }
                }
            }
        }

        private void UpdateAbilityDisplay(HeroData hero)
        {
            // Show innate skills from hero data
            for (int i = 0; i < _abilityCards.Count; i++)
            {
                if (_abilityCards[i] == null) continue;

                var nameLabel = _abilityCards[i].Q<Label>(className: "ability-name");
                var descLabel = _abilityCards[i].Q<Label>(className: "ability-desc");

                if (hero.innate_skills != null && i < hero.innate_skills.Length)
                {
                    _abilityCards[i].style.display = DisplayStyle.Flex;
                    if (nameLabel != null) nameLabel.text = hero.innate_skills[i];
                    if (descLabel != null) descLabel.text = ""; // Skill descriptions from SkillData if available
                }
                else
                {
                    _abilityCards[i].style.display = DisplayStyle.None;
                }
            }
        }

        // =============================================================================
        // 3D PREVIEW
        // =============================================================================

        private void UpdatePreviewModel(HeroData hero)
        {
            ClearPreviewModel();

            var mapping = GetHeroMapping(hero.hero_id);
            if (mapping?.modelPrefab != null && _previewModelParent != null)
            {
                _currentPreviewModel = Instantiate(mapping.modelPrefab, _previewModelParent);
                _currentPreviewModel.transform.localPosition = Vector3.zero;
                _currentPreviewModel.transform.localRotation = Quaternion.identity;

                // Setup preview layer
                SetLayerRecursively(_currentPreviewModel, LayerMask.NameToLayer("UI"));
            }
        }

        private void ClearPreviewModel()
        {
            if (_currentPreviewModel != null)
            {
                Destroy(_currentPreviewModel);
                _currentPreviewModel = null;
            }
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        // =============================================================================
        // BUTTON HANDLERS
        // =============================================================================

        private void OnBackButtonClicked(ClickEvent evt)
        {
            OnBackClicked?.Invoke();
            UnityEngine.SceneManagement.SceneManager.LoadScene(_mainMenuScene);
        }

        private void OnSelectButtonClicked(ClickEvent evt)
        {
            if (_selectedHero == null) return;

            ErrorLogger.UI($"Hero selected: {_selectedHero.display_name}");
            OnHeroSelected?.Invoke(_selectedHero);

            // Store selection in GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SelectHero(_selectedHero.hero_id);
            }

            // Transition to game
            UnityEngine.SceneManagement.SceneManager.LoadScene(_gameScene);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                OnBackButtonClicked(null);
            }
            else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                if (_selectedHero != null)
                {
                    OnSelectButtonClicked(null);
                }
            }
            else if (evt.keyCode == KeyCode.UpArrow || evt.keyCode == KeyCode.W)
            {
                SelectPreviousHero();
            }
            else if (evt.keyCode == KeyCode.DownArrow || evt.keyCode == KeyCode.S)
            {
                SelectNextHero();
            }
        }

        private void SelectPreviousHero()
        {
            if (_availableHeroes == null || _availableHeroes.Count == 0) return;

            int newIndex = _selectedHeroIndex <= 0 ? _availableHeroes.Count - 1 : _selectedHeroIndex - 1;
            OnHeroCardClicked(newIndex);
        }

        private void SelectNextHero()
        {
            if (_availableHeroes == null || _availableHeroes.Count == 0) return;

            int newIndex = (_selectedHeroIndex + 1) % _availableHeroes.Count;
            OnHeroCardClicked(newIndex);
        }

        // =============================================================================
        // ANIMATION
        // =============================================================================

        private void PlayEntranceAnimation()
        {
            var animator = UIAnimationController.Instance;
            if (animator == null) return;

            // Stagger hero cards
            if (_heroCards.Count > 0)
            {
                animator.StaggeredFadeIn(_heroCards, 0.05f, 0.3f);
            }
        }
    }
}
