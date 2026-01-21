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
        private VisualElement _detailsPanel;

        // Header elements
        private Label _heroName;
        private Label _heroTitle;

        // Brand elements
        private VisualElement _primaryBrand;
        private VisualElement _secondaryBrand;

        // Stat elements
        private VisualElement _statHealthFill;
        private VisualElement _statAttackFill;
        private VisualElement _statDefenseFill;
        private VisualElement _statSpeedFill;
        private Label _statHealthValue;
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
            _detailsPanel = _root.Q<VisualElement>("details-panel");

            // Query header
            _heroName = _root.Q<Label>("hero-name");
            _heroTitle = _root.Q<Label>("hero-title");

            // Query brands
            _primaryBrand = _root.Q<VisualElement>("primary-brand");
            _secondaryBrand = _root.Q<VisualElement>("secondary-brand");

            // Query stats
            _statHealthFill = _root.Q<VisualElement>("stat-health-fill");
            _statAttackFill = _root.Q<VisualElement>("stat-attack-fill");
            _statDefenseFill = _root.Q<VisualElement>("stat-defense-fill");
            _statSpeedFill = _root.Q<VisualElement>("stat-speed-fill");
            _statHealthValue = _root.Q<Label>("stat-health-value");
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
            var card = new VisualElement();
            card.name = $"hero-card-{index}";
            card.AddToClassList("hero-card");
            card.AddToClassList("vb-card");
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;
            card.style.padding = 12;
            card.style.marginBottom = 8;
            card.style.cursor = StyleKeyword.Initial;

            // Portrait
            var portrait = new VisualElement();
            portrait.AddToClassList("hero-card-portrait");
            portrait.style.width = 50;
            portrait.style.height = 50;
            portrait.style.backgroundColor = new Color(25f/255f, 20f/255f, 35f/255f);
            portrait.style.borderRadius = new StyleLength(4);
            portrait.style.marginRight = 12;

            // Try to find portrait from mapping
            var mapping = GetHeroMapping(hero.hero_id);
            if (mapping?.portrait != null)
            {
                portrait.style.backgroundImage = new StyleBackground(mapping.portrait);
            }

            card.Add(portrait);

            // Info container
            var info = new VisualElement();
            info.style.flexGrow = 1;

            // Name
            var nameLabel = new Label(hero.display_name);
            nameLabel.AddToClassList("hero-card-name");
            nameLabel.style.color = new Color(235f/255f, 225f/255f, 215f/255f);
            nameLabel.style.fontSize = 16;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            info.Add(nameLabel);

            // Brands
            var brandsRow = new VisualElement();
            brandsRow.style.flexDirection = FlexDirection.Row;
            brandsRow.style.marginTop = 4;

            var primaryBrand = hero.GetPrimaryBrand();
            var primaryIndicator = CreateBrandIndicator(primaryBrand);
            brandsRow.Add(primaryIndicator);

            info.Add(brandsRow);
            card.Add(info);

            // Selection indicator
            var selectIndicator = new VisualElement();
            selectIndicator.name = "select-indicator";
            selectIndicator.style.width = 4;
            selectIndicator.style.height = StyleKeyword.Auto;
            selectIndicator.style.position = Position.Absolute;
            selectIndicator.style.left = 0;
            selectIndicator.style.top = 0;
            selectIndicator.style.bottom = 0;
            selectIndicator.style.backgroundColor = new Color(120f/255f, 60f/255f, 160f/255f);
            selectIndicator.style.opacity = 0;
            card.Add(selectIndicator);

            // Click handler
            int capturedIndex = index;
            card.RegisterCallback<ClickEvent>(evt => OnHeroCardClicked(capturedIndex));

            // Hover effects
            card.RegisterCallback<MouseEnterEvent>(evt =>
            {
                if (_selectedHeroIndex != capturedIndex)
                {
                    card.style.backgroundColor = new Color(35f/255f, 28f/255f, 45f/255f);
                }
            });

            card.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                if (_selectedHeroIndex != capturedIndex)
                {
                    card.style.backgroundColor = new Color(25f/255f, 20f/255f, 35f/255f);
                }
            });

            return card;
        }

        private VisualElement CreateBrandIndicator(Brand brand)
        {
            var indicator = new VisualElement();
            indicator.style.width = 12;
            indicator.style.height = 12;
            indicator.style.borderRadius = new StyleLength(6);
            indicator.style.backgroundColor = ThemeManager.Instance.GetBrandColor(brand);
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

            // Update selection visuals
            UpdateSelectionVisuals(index);

            // Update selected hero
            _selectedHeroIndex = index;
            _selectedHero = _availableHeroes[index];

            // Update details panel
            UpdateDetailsPanel(_selectedHero);

            // Update preview
            UpdatePreviewModel(_selectedHero);

            // Enable select button
            _btnSelect?.SetEnabled(true);

            // Play selection sound
            // AudioManager.Instance?.PlaySFX("UI_Select");
        }

        private void UpdateSelectionVisuals(int newIndex)
        {
            // Deselect previous
            if (_selectedHeroIndex >= 0 && _selectedHeroIndex < _heroCards.Count)
            {
                var prevCard = _heroCards[_selectedHeroIndex];
                prevCard.style.backgroundColor = new Color(25f/255f, 20f/255f, 35f/255f);
                prevCard.style.borderColor = new Color(60f/255f, 50f/255f, 70f/255f);

                var prevIndicator = prevCard.Q("select-indicator");
                if (prevIndicator != null)
                    prevIndicator.style.opacity = 0;
            }

            // Select new
            if (newIndex >= 0 && newIndex < _heroCards.Count)
            {
                var newCard = _heroCards[newIndex];
                newCard.style.backgroundColor = new Color(40f/255f, 32f/255f, 55f/255f);
                newCard.style.borderColor = new Color(120f/255f, 60f/255f, 160f/255f);

                var newIndicator = newCard.Q("select-indicator");
                if (newIndicator != null)
                    newIndicator.style.opacity = 1;

                // Animate selection
                UIAnimationController.Instance?.PunchScale(newCard, 1.02f, 0.15f);
            }
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

            // Update brands
            UpdateBrandDisplay(hero);

            // Update stats
            UpdateStatBars(hero);

            // Update abilities
            UpdateAbilityDisplay(hero);

            // Hide placeholder, show model
            if (_placeholder != null) _placeholder.style.display = DisplayStyle.None;
            if (_modelDisplay != null) _modelDisplay.style.display = DisplayStyle.Flex;

            // Animate details panel
            UIAnimationController.Instance?.FadeIn(_detailsPanel, 0.3f);
        }

        private void UpdateBrandDisplay(HeroData hero)
        {
            var primaryBrand = hero.GetPrimaryBrand();

            if (_primaryBrand != null)
            {
                var icon = _primaryBrand.Q(className: "brand-icon");
                var name = _primaryBrand.Q<Label>(className: "brand-name");

                if (icon != null)
                    icon.style.backgroundColor = ThemeManager.Instance.GetBrandColor(primaryBrand);
                if (name != null)
                    name.text = primaryBrand.ToString();
            }

            // Hide secondary brand display since HeroData only has primary
            if (_secondaryBrand != null)
            {
                _secondaryBrand.style.display = DisplayStyle.None;
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
