using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.UI.Controls;
using VeilBreakers.Data;
using VeilBreakers.UI.Core;

namespace VeilBreakers.UI.Menus
{
    /// <summary>
    /// Controller for the Monster Collection UI.
    /// Displays captured monsters with filtering, sorting, and detail views.
    /// </summary>
    public class MonsterCollectionController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Monster Portraits (keyed by monster_id)")]
        [SerializeField] private List<MonsterPortraitMapping> _portraitMappings;

        [Serializable]
        public class MonsterPortraitMapping
        {
            public string monsterId;
            public Sprite portrait;
        }

        // =============================================================================
        // RUNTIME STATE
        // =============================================================================

        private VisualElement _root;
        private List<CapturedMonster> _capturedMonsters = new List<CapturedMonster>();
        private readonly List<CapturedMonster> _filteredMonsters = new List<CapturedMonster>();
        private CapturedMonster _selectedMonster;
        private int _selectedIndex = -1;

        // Filters
        private Brand? _filterBrand = null;
        private Rarity? _filterRarity = null;
        private string _searchQuery = "";
        private SortMode _currentSortMode = SortMode.Level;

        // GC-free search: pre-cached lowercased display names
        private readonly Dictionary<CapturedMonster, string> _lowercaseNameCache = new Dictionary<CapturedMonster, string>();

        // Cached sort comparisons to avoid delegate allocation per frame
        private static readonly Comparison<CapturedMonster> _sortByName =
            (a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
        private static readonly Comparison<CapturedMonster> _sortByLevelThenName =
            (a, b) =>
            {
                int cmp = b.Level.CompareTo(a.Level);
                return cmp != 0 ? cmp : string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
            };
        private static readonly Comparison<CapturedMonster> _sortByRarityThenLevel =
            (a, b) =>
            {
                int cmp = b.Data.rarity.CompareTo(a.Data.rarity);
                return cmp != 0 ? cmp : b.Level.CompareTo(a.Level);
            };
        private static readonly Comparison<CapturedMonster> _sortByBrandThenName =
            (a, b) =>
            {
                int cmp = a.Data.brand.CompareTo(b.Data.brand);
                return cmp != 0 ? cmp : string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
            };
        private static readonly Comparison<CapturedMonster> _sortByCorruptionThenName =
            (a, b) =>
            {
                int cmp = b.Corruption.CompareTo(a.Corruption);
                return cmp != 0 ? cmp : string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
            };

        private enum SortMode
        {
            Name,
            Level,
            Rarity,
            Brand,
            Corruption
        }

        // =============================================================================
        // UI ELEMENTS
        // =============================================================================

        // Header
        private Button _btnBack;
        private Label _collectionCount;

        // Filters
        private VBDropdownField _filterBrandDropdown;
        private VBDropdownField _filterRarityDropdown;
        private VBDropdownField _sortDropdown;
        private TextField _searchField;

        // Grid
        private VisualElement _monsterGrid;
        private VisualElement _emptyState;

        // Details Panel
        private VisualElement _portraitFrame;
        private VisualElement _monsterPortrait;
        private Label _monsterName;
        private Label _monsterLevel;
        private VisualElement _primaryBrandIcon;
        private Label _primaryBrandName;
        private VisualElement _secondaryBrandIcon;
        private Label _secondaryBrandName;
        private VisualElement _corruptionBarFill;
        private Label _corruptionPercent;
        private Label _corruptionState;
        private Label _statHp;
        private Label _statMp;
        private Label _statAttack;
        private Label _statDefense;
        private Label _statMagic;
        private Label _statSpeed;
        private VisualElement _skillsList;
        private Button _btnDetails;
        private Button _btnParty;
        private Button _btnRelease;

        private List<VisualElement> _monsterCards = new List<VisualElement>();

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action OnBackClicked;
        public event Action<CapturedMonster> OnMonsterSelected;
        public event Action<CapturedMonster> OnViewDetails;
        public event Action<CapturedMonster> OnAddToParty;
        public event Action<CapturedMonster> OnReleaseMonster;

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
            
            // Subscribe to InputManager for universal navigation
            if (InputManager.HasInstance)
            {
                InputManager.Instance.OnActionTriggered += OnActionTriggered;
            }
        }

        private void OnDisable()
        {
            UnbindEvents();

            // Unsubscribe from InputManager (use HasInstance to avoid auto-create during quit)
            if (InputManager.HasInstance)
            {
                InputManager.Instance.OnActionTriggered -= OnActionTriggered;
            }
        }

        private void OnActionTriggered(InputManager.GameAction action)
        {
            if (!gameObject.activeInHierarchy || _root?.style.display == DisplayStyle.None) return;

            switch (action)
            {
                case InputManager.GameAction.Cancel:
                    OnBackButtonClicked(null);
                    break;
                case InputManager.GameAction.MoveLeft:
                    SelectPreviousMonster();
                    break;
                case InputManager.GameAction.MoveRight:
                    SelectNextMonster();
                    break;
                case InputManager.GameAction.Confirm:
                    // Currently no primary action for selection, but could be 'Select for Party'
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
                ErrorLogger.Error("MonsterCollectionController: UIDocument is null!");
                return;
            }

            _root = _uiDocument.rootVisualElement;

            // Query header
            _btnBack = _root.Q<Button>("btn-back");
            _collectionCount = _root.Q<Label>("collection-count");

            // Query filters
            _filterBrandDropdown = _root.Q<VBDropdownField>("filter-brand");
            _filterRarityDropdown = _root.Q<VBDropdownField>("filter-rarity");
            _sortDropdown = _root.Q<VBDropdownField>("sort-dropdown");
            _searchField = _root.Q<TextField>("search-field");

            // Query grid
            _monsterGrid = _root.Q<VisualElement>("monster-grid");
            _emptyState = _root.Q<VisualElement>("empty-state");

            // Query details panel
            _portraitFrame = _root.Q<VisualElement>("portrait-frame");
            _monsterPortrait = _root.Q<VisualElement>("monster-portrait");
            _monsterName = _root.Q<Label>("monster-name");
            _monsterLevel = _root.Q<Label>("monster-level");
            _primaryBrandIcon = _root.Q<VisualElement>("primary-brand-icon");
            _primaryBrandName = _root.Q<Label>("primary-brand-name");
            _secondaryBrandIcon = _root.Q<VisualElement>("secondary-brand-icon");
            _secondaryBrandName = _root.Q<Label>("secondary-brand-name");
            _corruptionBarFill = _root.Q<VisualElement>("corruption-bar-fill");
            _corruptionPercent = _root.Q<Label>("corruption-percent");
            _corruptionState = _root.Q<Label>("corruption-state");
            _statHp = _root.Q<Label>("stat-hp");
            _statMp = _root.Q<Label>("stat-mp");
            _statAttack = _root.Q<Label>("stat-attack");
            _statDefense = _root.Q<Label>("stat-defense");
            _statMagic = _root.Q<Label>("stat-magic");
            _statSpeed = _root.Q<Label>("stat-speed");
            _skillsList = _root.Q<VisualElement>("skills-list");
            _btnDetails = _root.Q<Button>("btn-details");
            _btnParty = _root.Q<Button>("btn-party");
            _btnRelease = _root.Q<Button>("btn-release");

            SetupDropdowns();
            BindEvents();

            ErrorLogger.UI("MonsterCollection initialized");
        }

        private void SetupDropdowns()
        {
            // Brand filter
            if (_filterBrandDropdown != null)
            {
                var brandNames = Enum.GetNames(typeof(Brand));
                var brandChoices = new List<string>(brandNames.Length) { "All Brands" };
                for (int i = 0; i < brandNames.Length; i++)
                {
                    if (brandNames[i] != "NONE")
                        brandChoices.Add(brandNames[i]);
                }
                _filterBrandDropdown.choices = brandChoices;
                _filterBrandDropdown.index = 0;
            }

            // Rarity filter
            if (_filterRarityDropdown != null)
            {
                var rarityChoices = new List<string> { "All Rarities" };
                rarityChoices.AddRange(Enum.GetNames(typeof(Rarity)));
                _filterRarityDropdown.choices = rarityChoices;
                _filterRarityDropdown.index = 0;
            }

            // Sort dropdown
            if (_sortDropdown != null)
            {
                _sortDropdown.choices = new List<string>
                {
                    "Level (High-Low)",
                    "Name (A-Z)",
                    "Rarity",
                    "Brand",
                    "Corruption"
                };
                _sortDropdown.index = 0;
            }
        }

        private void BindEvents()
        {
            _btnBack?.RegisterCallback<ClickEvent>(OnBackButtonClicked);
            _btnDetails?.RegisterCallback<ClickEvent>(OnDetailsButtonClicked);
            _btnParty?.RegisterCallback<ClickEvent>(OnPartyButtonClicked);
            _btnRelease?.RegisterCallback<ClickEvent>(OnReleaseButtonClicked);

            _filterBrandDropdown?.RegisterValueChangedCallback(OnBrandFilterChanged);
            _filterRarityDropdown?.RegisterValueChangedCallback(OnRarityFilterChanged);
            _sortDropdown?.RegisterValueChangedCallback(OnSortChanged);
            _searchField?.RegisterValueChangedCallback(OnSearchChanged);

            _root?.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void UnbindEvents()
        {
            _btnBack?.UnregisterCallback<ClickEvent>(OnBackButtonClicked);
            _btnDetails?.UnregisterCallback<ClickEvent>(OnDetailsButtonClicked);
            _btnParty?.UnregisterCallback<ClickEvent>(OnPartyButtonClicked);
            _btnRelease?.UnregisterCallback<ClickEvent>(OnReleaseButtonClicked);
            _root?.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        // =============================================================================
        // DATA LOADING
        // =============================================================================

        private void LoadCollection()
        {
            _capturedMonsters.Clear();
            _lowercaseNameCache.Clear();

            // TODO: Load from SaveManager/GameManager
            // For now, create test data
            LoadTestData();

            // Pre-cache lowercased display names for search
            RebuildNameCache();

            RefreshGrid();
            UpdateCollectionCount();
        }

        private void RebuildNameCache()
        {
            _lowercaseNameCache.Clear();
            for (int i = 0; i < _capturedMonsters.Count; i++)
            {
                var m = _capturedMonsters[i];
                _lowercaseNameCache[m] = m.DisplayName.ToLower();
            }
        }

        private void LoadTestData()
        {
            // Create some test captured monsters
            var allMonsters = GameDatabase.Instance?.GetAllMonsters() ?? new List<MonsterData>();

            if (allMonsters.Count == 0) return;

            // Simulate captured monsters with levels and corruption
            int captureCount = Mathf.Min(12, allMonsters.Count);
            for (int i = 0; i < captureCount; i++)
            {
                var monsterData = allMonsters[i % allMonsters.Count];
                _capturedMonsters.Add(new CapturedMonster
                {
                    Data = monsterData,
                    Level = UnityEngine.Random.Range(1, 25),
                    Experience = UnityEngine.Random.Range(0, 1000),
                    Corruption = UnityEngine.Random.Range(0f, 0.75f),
                    CaptureDate = DateTime.Now.AddDays(-UnityEngine.Random.Range(1, 30)),
                    Nickname = null
                });
            }

            ErrorLogger.UI($"Loaded {_capturedMonsters.Count} captured monsters (test data)");
        }

        private void UpdateCollectionCount()
        {
            if (_collectionCount == null) return;

            int maxCapacity = 100; // TODO: Get from GameManager
            _collectionCount.text = $"{_capturedMonsters.Count} / {maxCapacity}";
        }

        // =============================================================================
        // FILTERING & SORTING
        // =============================================================================

        private void OnBrandFilterChanged(ChangeEvent<string> evt)
        {
            if (_filterBrandDropdown.index == 0)
            {
                _filterBrand = null;
            }
            else
            {
                var brandName = _filterBrandDropdown.value;
                if (Enum.TryParse<Brand>(brandName, out var brand))
                {
                    _filterBrand = brand;
                }
            }
            RefreshGrid();
        }

        private void OnRarityFilterChanged(ChangeEvent<string> evt)
        {
            if (_filterRarityDropdown.index == 0)
            {
                _filterRarity = null;
            }
            else
            {
                var rarityName = _filterRarityDropdown.value;
                if (Enum.TryParse<Rarity>(rarityName, out var rarity))
                {
                    _filterRarity = rarity;
                }
            }
            RefreshGrid();
        }

        private void OnSortChanged(ChangeEvent<string> evt)
        {
            _currentSortMode = _sortDropdown.index switch
            {
                0 => SortMode.Level,
                1 => SortMode.Name,
                2 => SortMode.Rarity,
                3 => SortMode.Brand,
                4 => SortMode.Corruption,
                _ => SortMode.Level
            };
            RefreshGrid();
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            _searchQuery = evt.newValue?.ToLower() ?? "";
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            // Filter — single pass, no LINQ, reuses existing list
            _filteredMonsters.Clear();
            bool hasSearchQuery = !string.IsNullOrEmpty(_searchQuery);

            for (int i = 0; i < _capturedMonsters.Count; i++)
            {
                var m = _capturedMonsters[i];

                if (_filterBrand.HasValue && m.Data.GetPrimaryBrand() != _filterBrand.Value)
                    continue;

                if (_filterRarity.HasValue && m.Data.GetRarity() != _filterRarity.Value)
                    continue;

                if (hasSearchQuery)
                {
                    if (!_lowercaseNameCache.TryGetValue(m, out string cachedName))
                    {
                        cachedName = m.DisplayName.ToLower();
                        _lowercaseNameCache[m] = cachedName;
                    }
                    if (!cachedName.Contains(_searchQuery))
                        continue;
                }

                _filteredMonsters.Add(m);
            }

            // Sort — in-place with cached comparison delegates, no LINQ
            switch (_currentSortMode)
            {
                case SortMode.Name:
                    _filteredMonsters.Sort(_sortByName);
                    break;
                case SortMode.Level:
                    _filteredMonsters.Sort(_sortByLevelThenName);
                    break;
                case SortMode.Rarity:
                    _filteredMonsters.Sort(_sortByRarityThenLevel);
                    break;
                case SortMode.Brand:
                    _filteredMonsters.Sort(_sortByBrandThenName);
                    break;
                case SortMode.Corruption:
                    _filteredMonsters.Sort(_sortByCorruptionThenName);
                    break;
            }

            PopulateGrid();
            UpdateEmptyState();

            // Clear selection if not in filtered list
            if (_selectedMonster != null && !_filteredMonsters.Contains(_selectedMonster))
            {
                ClearSelection();
            }
        }

        // =============================================================================
        // GRID POPULATION
        // =============================================================================

        private void PopulateGrid()
        {
            if (_monsterGrid == null) return;

            _monsterGrid.Clear();
            _monsterCards.Clear();

            for (int i = 0; i < _filteredMonsters.Count; i++)
            {
                var monster = _filteredMonsters[i];
                var card = CreateMonsterCard(monster, i);
                _monsterGrid.Add(card);
                _monsterCards.Add(card);
            }
        }

        private VisualElement CreateMonsterCard(CapturedMonster monster, int index)
        {
            var card = new VisualElement();
            card.AddToClassList("monster-card");
            card.AddToClassList($"rarity-{GetRarityClass(monster.Data.GetRarity())}");
            card.AddToClassList(GetCorruptionClass(monster.Corruption));

            // Portrait
            var portrait = new VisualElement();
            portrait.AddToClassList("monster-card-portrait");

            // Try to get portrait from mappings
            MonsterPortraitMapping mapping = null;
            if (_portraitMappings != null)
            {
                for (int j = 0; j < _portraitMappings.Count; j++)
                {
                    if (_portraitMappings[j].monsterId == monster.Data.monster_id)
                    {
                        mapping = _portraitMappings[j];
                        break;
                    }
                }
            }
            if (mapping?.portrait != null)
            {
                portrait.style.backgroundImage = new StyleBackground(mapping.portrait);
            }
            else
            {
                // Use brand color as placeholder
                portrait.style.backgroundColor = GetBrandColor(monster.Data.GetPrimaryBrand());
            }
            card.Add(portrait);

            // Name
            var nameLabel = new Label(monster.DisplayName);
            nameLabel.AddToClassList("monster-card-name");
            card.Add(nameLabel);

            // Level
            var levelLabel = new Label($"Lv.{monster.Level}");
            levelLabel.AddToClassList("monster-card-level");
            card.Add(levelLabel);

            // Click handler
            int capturedIndex = index;
            card.RegisterCallback<ClickEvent>(e => OnMonsterCardClicked(capturedIndex));

            return card;
        }

        private string GetRarityClass(Rarity rarity)
        {
            return rarity switch
            {
                Rarity.COMMON => "common",
                Rarity.UNCOMMON => "uncommon",
                Rarity.RARE => "rare",
                Rarity.EPIC => "epic",
                Rarity.LEGENDARY => "legendary",
                Rarity.MYTHIC => "mythic",
                _ => "common"
            };
        }

        private string GetCorruptionClass(float corruption)
        {
            return corruption switch
            {
                < 0.25f => "corruption-low",
                < 0.50f => "corruption-medium",
                < 0.75f => "corruption-high",
                _ => "corruption-critical"
            };
        }

        private Color GetBrandColor(Brand brand)
        {
            return brand switch
            {
                Brand.IRON => new Color(0.5f, 0.5f, 0.6f),
                Brand.SAVAGE => new Color(0.8f, 0.3f, 0.2f),
                Brand.SURGE => new Color(0.9f, 0.8f, 0.2f),
                Brand.VENOM => new Color(0.4f, 0.7f, 0.3f),
                Brand.DREAD => new Color(0.4f, 0.2f, 0.5f),
                Brand.LEECH => new Color(0.6f, 0.2f, 0.4f),
                Brand.GRACE => new Color(0.9f, 0.9f, 0.8f),
                Brand.MEND => new Color(0.3f, 0.7f, 0.5f),
                Brand.RUIN => new Color(0.3f, 0.3f, 0.4f),
                Brand.VOID => new Color(0.2f, 0.1f, 0.3f),
                _ => new Color(0.5f, 0.5f, 0.5f)
            };
        }

        private void UpdateEmptyState()
        {
            if (_emptyState == null) return;

            bool isEmpty = _filteredMonsters.Count == 0;
            _emptyState.style.display = isEmpty ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // =============================================================================
        // SELECTION
        // =============================================================================

        private void OnMonsterCardClicked(int index)
        {
            // Validate index against BOTH lists (they should be synced, but be defensive)
            if (index < 0 || index >= _filteredMonsters.Count || index >= _monsterCards.Count) return;

            // Update visual selection
            if (_selectedIndex >= 0 && _selectedIndex < _monsterCards.Count)
            {
                _monsterCards[_selectedIndex].RemoveFromClassList("monster-card-selected");
            }

            _selectedIndex = index;
            _selectedMonster = _filteredMonsters[index];
            _monsterCards[index].AddToClassList("monster-card-selected");

            UpdateDetailsPanel();
            OnMonsterSelected?.Invoke(_selectedMonster);

            PlaySelectSound();
        }

        private void ClearSelection()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _monsterCards.Count)
            {
                _monsterCards[_selectedIndex].RemoveFromClassList("monster-card-selected");
            }

            _selectedIndex = -1;
            _selectedMonster = null;
            UpdateDetailsPanel();
        }

        private void UpdateDetailsPanel()
        {
            if (_selectedMonster == null)
            {
                // Default state
                if (_monsterName != null) _monsterName.text = "Select a Monster";
                if (_monsterLevel != null) _monsterLevel.text = "1";
                if (_primaryBrandName != null) _primaryBrandName.text = "None";
                if (_secondaryBrandName != null) _secondaryBrandName.text = "None";
                if (_corruptionPercent != null) _corruptionPercent.text = "0%";
                if (_corruptionState != null) _corruptionState.text = "";
                if (_corruptionBarFill != null) _corruptionBarFill.style.width = Length.Percent(0);

                // Zero stats
                if (_statHp != null) _statHp.text = "0";
                if (_statMp != null) _statMp.text = "0";
                if (_statAttack != null) _statAttack.text = "0";
                if (_statDefense != null) _statDefense.text = "0";
                if (_statMagic != null) _statMagic.text = "0";
                if (_statSpeed != null) _statSpeed.text = "0";

                // Clear skills
                _skillsList?.Clear();

                // Disable buttons
                _btnDetails?.SetEnabled(false);
                _btnParty?.SetEnabled(false);
                _btnRelease?.SetEnabled(false);

                return;
            }

            var monster = _selectedMonster;
            var data = monster.Data;

            // Name & Level
            if (_monsterName != null) _monsterName.text = monster.DisplayName;
            if (_monsterLevel != null) _monsterLevel.text = monster.Level.ToString();

            // Brands
            UpdateBrandDisplay(data.GetPrimaryBrand(), _primaryBrandIcon, _primaryBrandName);
            UpdateBrandDisplay(data.GetSecondaryBrand(), _secondaryBrandIcon, _secondaryBrandName);

            // Corruption
            UpdateCorruptionDisplay(monster.Corruption);

            // Stats
            UpdateStatsDisplay(monster);

            // Skills
            UpdateSkillsDisplay(data);

            // Enable buttons
            _btnDetails?.SetEnabled(true);
            _btnParty?.SetEnabled(true);
            _btnRelease?.SetEnabled(true);
        }

        private void UpdateBrandDisplay(Brand brand, VisualElement icon, Label nameLabel)
        {
            if (icon != null)
            {
                icon.style.backgroundColor = GetBrandColor(brand);
            }
            if (nameLabel != null)
            {
                nameLabel.text = brand == Brand.NONE ? "None" : brand.ToString();
            }
        }

        private void UpdateCorruptionDisplay(float corruption)
        {
            int percent = Mathf.RoundToInt(corruption * 100);

            if (_corruptionPercent != null) _corruptionPercent.text = $"{percent}%";
            if (_corruptionBarFill != null) _corruptionBarFill.style.width = Length.Percent(percent);

            // Corruption state text and color
            string state;
            Color stateColor;

            if (corruption <= 0.10f)
            {
                state = "ASCENDED";
                stateColor = new Color(0.9f, 0.85f, 0.5f);
            }
            else if (corruption <= 0.25f)
            {
                state = "Purified";
                stateColor = new Color(0.3f, 0.7f, 0.5f);
            }
            else if (corruption <= 0.50f)
            {
                state = "Unstable";
                stateColor = new Color(0.7f, 0.6f, 0.3f);
            }
            else if (corruption <= 0.75f)
            {
                state = "Corrupted";
                stateColor = new Color(0.7f, 0.3f, 0.3f);
            }
            else
            {
                state = "ABYSSAL";
                stateColor = new Color(0.6f, 0.2f, 0.4f);
            }

            if (_corruptionState != null)
            {
                _corruptionState.text = state;
                _corruptionState.style.color = stateColor;
            }
        }

        private void UpdateStatsDisplay(CapturedMonster monster)
        {
            var data = monster.Data;
            int level = monster.Level;

            if (_statHp != null) _statHp.text = data.GetStatAtLevel(Stat.HP, level).ToString();
            if (_statMp != null) _statMp.text = data.GetStatAtLevel(Stat.MP, level).ToString();
            if (_statAttack != null) _statAttack.text = data.GetStatAtLevel(Stat.ATTACK, level).ToString();
            if (_statDefense != null) _statDefense.text = data.GetStatAtLevel(Stat.DEFENSE, level).ToString();
            if (_statMagic != null) _statMagic.text = data.GetStatAtLevel(Stat.MAGIC, level).ToString();
            if (_statSpeed != null) _statSpeed.text = data.GetStatAtLevel(Stat.SPEED, level).ToString();
        }

        private void UpdateSkillsDisplay(MonsterData data)
        {
            if (_skillsList == null) return;
            _skillsList.Clear();

            if (data.innate_skills == null) return;

            foreach (var skillId in data.innate_skills)
            {
                var skillRow = new VisualElement();
                skillRow.AddToClassList("monster-skill");

                var icon = new VisualElement();
                icon.AddToClassList("monster-skill-icon");
                skillRow.Add(icon);

                var nameLabel = new Label(FormatSkillName(skillId));
                nameLabel.AddToClassList("monster-skill-name");
                skillRow.Add(nameLabel);

                _skillsList.Add(skillRow);
            }
        }

        private string FormatSkillName(string skillId)
        {
            // Convert skill_id to display name
            // e.g., "iron_bash" -> "Iron Bash"
            if (string.IsNullOrEmpty(skillId)) return "Unknown";

            var words = skillId.Split('_');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                }
            }
            return string.Join(" ", words);
        }

        // =============================================================================
        // BUTTON HANDLERS
        // =============================================================================

        private void OnBackButtonClicked(ClickEvent evt)
        {
            PlayButtonSound();
            OnBackClicked?.Invoke();
        }

        private void OnDetailsButtonClicked(ClickEvent evt)
        {
            if (_selectedMonster == null) return;
            PlayButtonSound();
            OnViewDetails?.Invoke(_selectedMonster);
            ErrorLogger.UI($"View details: {_selectedMonster.DisplayName}");
        }

        private void OnPartyButtonClicked(ClickEvent evt)
        {
            if (_selectedMonster == null) return;
            PlayButtonSound();
            OnAddToParty?.Invoke(_selectedMonster);
            ErrorLogger.UI($"Add to party: {_selectedMonster.DisplayName}");
        }

        private void OnReleaseButtonClicked(ClickEvent evt)
        {
            if (_selectedMonster == null) return;
            PlayButtonSound();

            // TODO: Show confirmation dialog
            OnReleaseMonster?.Invoke(_selectedMonster);
            ErrorLogger.UI($"Release monster: {_selectedMonster.DisplayName}");
        }

        // =============================================================================
        // KEYBOARD NAVIGATION
        // =============================================================================

        private void OnKeyDown(KeyDownEvent evt)
        {
            // Primary navigation handled by OnActionTriggered.
            if (evt.keyCode == KeyCode.Escape)
            {
                OnBackButtonClicked(null);
            }
        }

        private void SelectPreviousMonster()
        {
            if (_filteredMonsters.Count == 0) return;
            int newIndex = _selectedIndex <= 0 ? _filteredMonsters.Count - 1 : _selectedIndex - 1;
            OnMonsterCardClicked(newIndex);
        }

        private void SelectNextMonster()
        {
            if (_filteredMonsters.Count == 0) return;
            int newIndex = (_selectedIndex + 1) % _filteredMonsters.Count;
            OnMonsterCardClicked(newIndex);
        }

        // =============================================================================
        // AUDIO
        // =============================================================================

        private void PlayButtonSound()
        {
            // TODO: Integrate with AudioManager
        }

        private void PlaySelectSound()
        {
            // TODO: Integrate with AudioManager
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Initialize with external root element.
        /// </summary>
        public void Initialize(VisualElement root)
        {
            _root = root;

            // Re-query all elements
            _btnBack = _root.Q<Button>("btn-back");
            _collectionCount = _root.Q<Label>("collection-count");
            _filterBrandDropdown = _root.Q<VBDropdownField>("filter-brand");
            _filterRarityDropdown = _root.Q<VBDropdownField>("filter-rarity");
            _sortDropdown = _root.Q<VBDropdownField>("sort-dropdown");
            _searchField = _root.Q<TextField>("search-field");
            _monsterGrid = _root.Q<VisualElement>("monster-grid");
            _emptyState = _root.Q<VisualElement>("empty-state");
            _portraitFrame = _root.Q<VisualElement>("portrait-frame");
            _monsterPortrait = _root.Q<VisualElement>("monster-portrait");
            _monsterName = _root.Q<Label>("monster-name");
            _monsterLevel = _root.Q<Label>("monster-level");
            _primaryBrandIcon = _root.Q<VisualElement>("primary-brand-icon");
            _primaryBrandName = _root.Q<Label>("primary-brand-name");
            _secondaryBrandIcon = _root.Q<VisualElement>("secondary-brand-icon");
            _secondaryBrandName = _root.Q<Label>("secondary-brand-name");
            _corruptionBarFill = _root.Q<VisualElement>("corruption-bar-fill");
            _corruptionPercent = _root.Q<Label>("corruption-percent");
            _corruptionState = _root.Q<Label>("corruption-state");
            _statHp = _root.Q<Label>("stat-hp");
            _statMp = _root.Q<Label>("stat-mp");
            _statAttack = _root.Q<Label>("stat-attack");
            _statDefense = _root.Q<Label>("stat-defense");
            _statMagic = _root.Q<Label>("stat-magic");
            _statSpeed = _root.Q<Label>("stat-speed");
            _skillsList = _root.Q<VisualElement>("skills-list");
            _btnDetails = _root.Q<Button>("btn-details");
            _btnParty = _root.Q<Button>("btn-party");
            _btnRelease = _root.Q<Button>("btn-release");

            SetupDropdowns();
            BindEvents();
            LoadCollection();

            ErrorLogger.UI("MonsterCollection initialized via external root");
        }

        /// <summary>
        /// Refresh collection data.
        /// </summary>
        public void Refresh()
        {
            LoadCollection();
        }

        /// <summary>
        /// Add a newly captured monster.
        /// </summary>
        public void AddMonster(CapturedMonster monster)
        {
            _capturedMonsters.Add(monster);
            _lowercaseNameCache[monster] = monster.DisplayName.ToLower();
            UpdateCollectionCount();
            RefreshGrid();
        }

        /// <summary>
        /// Remove a monster from collection.
        /// </summary>
        public bool RemoveMonster(CapturedMonster monster)
        {
            bool removed = _capturedMonsters.Remove(monster);
            if (removed)
            {
                _lowercaseNameCache.Remove(monster);
                UpdateCollectionCount();
                RefreshGrid();
            }
            return removed;
        }
    }

    // =============================================================================
    // CAPTURED MONSTER DATA
    // =============================================================================

    /// <summary>
    /// Represents a monster in the player's collection with instance-specific data.
    /// </summary>
    [Serializable]
    public class CapturedMonster
    {
        public MonsterData Data;
        public int Level = 1;
        public int Experience = 0;
        public float Corruption = 0f;
        public string Nickname;
        public DateTime CaptureDate;

        public string DisplayName => string.IsNullOrEmpty(Nickname) ? Data?.display_name ?? "Unknown" : Nickname;

        /// <summary>
        /// Get corruption state based on current corruption level.
        /// </summary>
        public CorruptionState GetCorruptionState()
        {
            return Corruption switch
            {
                <= 0.10f => CorruptionState.ASCENDED,
                <= 0.25f => CorruptionState.PURIFIED,
                <= 0.50f => CorruptionState.UNSTABLE,
                <= 0.75f => CorruptionState.CORRUPTED,
                _ => CorruptionState.ABYSSAL
            };
        }
    }
}
