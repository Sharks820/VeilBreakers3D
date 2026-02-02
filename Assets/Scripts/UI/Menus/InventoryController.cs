using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.UI.Controls;
using VeilBreakers.Data;
using VeilBreakers.UI.Core;

namespace VeilBreakers.UI.Menus
{
    /// <summary>
    /// Controller for the Inventory UI.
    /// Manages item display, filtering, sorting, and item actions.
    /// </summary>
    public class InventoryController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Item Icons (keyed by item_id)")]
        [SerializeField] private List<ItemIconMapping> _itemIconMappings;

        [Serializable]
        public class ItemIconMapping
        {
            public string itemId;
            public Sprite icon;
        }

        // =============================================================================
        // RUNTIME STATE
        // =============================================================================

        private VisualElement _root;
        private Dictionary<string, int> _playerInventory = new Dictionary<string, int>();
        private List<ItemData> _allItems;
        private List<ItemData> _filteredItems = new List<ItemData>();
        private ItemData _selectedItem;
        private int _selectedIndex = -1;
        private ItemCategory _currentCategory = ItemCategory.CONSUMABLE;
        private bool _showAllCategories = true;
        private SortMode _currentSortMode = SortMode.Name;

        private enum SortMode
        {
            Name,
            Rarity,
            Value,
            Quantity,
            Category
        }

        // =============================================================================
        // UI ELEMENTS
        // =============================================================================

        // Header
        private Button _btnBack;
        private Label _currencyAmount;

        // Tabs
        private Button _tabAll;
        private Button _tabConsumables;
        private Button _tabEquipment;
        private Button _tabKeyItems;
        private Button _tabMaterials;

        // Grid
        private VisualElement _itemGrid;
        private VisualElement _emptyState;
        private Label _itemCount;
        private VBDropdownField _sortDropdown;

        // Details Panel
        private VisualElement _itemIconFrame;
        private VisualElement _itemIcon;
        private Label _itemName;
        private Label _itemCategoryLabel;
        private Label _itemDescription;
        private VisualElement _statsSection;
        private VisualElement _statsList;
        private VisualElement _effectsSection;
        private VisualElement _effectsList;
        private VisualElement _brandSection;
        private VisualElement _brandDisplay;
        private Label _itemQuantity;
        private Label _itemValue;
        private Button _btnUse;
        private Button _btnDrop;

        private List<VisualElement> _itemSlots = new List<VisualElement>();

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action OnBackClicked;
        public event Action<ItemData> OnItemUsed;
        public event Action<ItemData, int> OnItemDropped;

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
            LoadInventory();
            
            // Subscribe to InputManager for universal navigation
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnActionTriggered += OnActionTriggered;
            }
        }

        private void OnDisable()
        {
            UnbindEvents();
            
            // Unsubscribe from InputManager
            if (InputManager.Instance != null)
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
                    OnBackClicked?.Invoke();
                    break;
                case InputManager.GameAction.MoveLeft:
                    SelectPreviousItem();
                    break;
                case InputManager.GameAction.MoveRight:
                    SelectNextItem();
                    break;
                case InputManager.GameAction.MoveUp:
                    SelectItemAbove();
                    break;
                case InputManager.GameAction.MoveDown:
                    SelectItemBelow();
                    break;
                case InputManager.GameAction.Confirm:
                    if (_selectedItem != null && _btnUse != null && _btnUse.enabledSelf)
                    {
                        OnUseButtonClicked(null);
                    }
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
                ErrorLogger.Error("InventoryController: UIDocument is null!");
                return;
            }

            _root = _uiDocument.rootVisualElement;

            // Query header elements
            _btnBack = _root.Q<Button>("btn-back");
            _currencyAmount = _root.Q<Label>("currency-amount");

            // Query tabs
            _tabAll = _root.Q<Button>("tab-all");
            _tabConsumables = _root.Q<Button>("tab-consumables");
            _tabEquipment = _root.Q<Button>("tab-equipment");
            _tabKeyItems = _root.Q<Button>("tab-key-items");
            _tabMaterials = _root.Q<Button>("tab-materials");

            // Query grid
            _itemGrid = _root.Q<VisualElement>("item-grid");
            _emptyState = _root.Q<VisualElement>("empty-state");
            _itemCount = _root.Q<Label>("item-count");
            _sortDropdown = _root.Q<VBDropdownField>("sort-dropdown");

            // Query details panel
            _itemIconFrame = _root.Q<VisualElement>("item-icon-frame");
            _itemIcon = _root.Q<VisualElement>("item-icon");
            _itemName = _root.Q<Label>("item-name");
            _itemCategoryLabel = _root.Q<Label>("item-category-label");
            _itemDescription = _root.Q<Label>("item-description");
            _statsSection = _root.Q<VisualElement>("stats-section");
            _statsList = _root.Q<VisualElement>("stats-list");
            _effectsSection = _root.Q<VisualElement>("effects-section");
            _effectsList = _root.Q<VisualElement>("effects-list");
            _brandSection = _root.Q<VisualElement>("brand-section");
            _brandDisplay = _root.Q<VisualElement>("brand-display");
            _itemQuantity = _root.Q<Label>("item-quantity");
            _itemValue = _root.Q<Label>("item-value");
            _btnUse = _root.Q<Button>("btn-use");
            _btnDrop = _root.Q<Button>("btn-drop");

            // Set up sort dropdown
            SetupSortDropdown();

            // Bind events
            BindEvents();

            ErrorLogger.UI("Inventory initialized");
        }

        private void SetupSortDropdown()
        {
            if (_sortDropdown == null) return;

            _sortDropdown.choices = new List<string>
            {
                "Name (A-Z)",
                "Rarity",
                "Value",
                "Quantity",
                "Category"
            };
            _sortDropdown.index = 0;
        }

        private void BindEvents()
        {
            _btnBack?.RegisterCallback<ClickEvent>(OnBackButtonClicked);
            _btnUse?.RegisterCallback<ClickEvent>(OnUseButtonClicked);
            _btnDrop?.RegisterCallback<ClickEvent>(OnDropButtonClicked);

            // Tab buttons
            _tabAll?.RegisterCallback<ClickEvent>(OnTabAllClicked);
            _tabConsumables?.RegisterCallback<ClickEvent>(OnTabConsumablesClicked);
            _tabEquipment?.RegisterCallback<ClickEvent>(OnTabEquipmentClicked);
            _tabKeyItems?.RegisterCallback<ClickEvent>(OnTabKeyItemsClicked);
            _tabMaterials?.RegisterCallback<ClickEvent>(OnTabMaterialsClicked);

            // Sort dropdown
            _sortDropdown?.RegisterValueChangedCallback(OnSortChanged);

            // Keyboard navigation
            _root?.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void UnbindEvents()
        {
            _btnBack?.UnregisterCallback<ClickEvent>(OnBackButtonClicked);
            _btnUse?.UnregisterCallback<ClickEvent>(OnUseButtonClicked);
            _btnDrop?.UnregisterCallback<ClickEvent>(OnDropButtonClicked);
            
            _tabAll?.UnregisterCallback<ClickEvent>(OnTabAllClicked);
            _tabConsumables?.UnregisterCallback<ClickEvent>(OnTabConsumablesClicked);
            _tabEquipment?.UnregisterCallback<ClickEvent>(OnTabEquipmentClicked);
            _tabKeyItems?.UnregisterCallback<ClickEvent>(OnTabKeyItemsClicked);
            _tabMaterials?.UnregisterCallback<ClickEvent>(OnTabMaterialsClicked);

            _sortDropdown?.UnregisterValueChangedCallback(OnSortChanged);
            _root?.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void OnTabAllClicked(ClickEvent e) => SetCategory(null);
        private void OnTabConsumablesClicked(ClickEvent e) => SetCategory(ItemCategory.CONSUMABLE);
        private void OnTabEquipmentClicked(ClickEvent e) => SetCategory(ItemCategory.EQUIPMENT);
        private void OnTabKeyItemsClicked(ClickEvent e) => SetCategory(ItemCategory.KEY_ITEM);
        private void OnTabMaterialsClicked(ClickEvent e) => SetCategory(ItemCategory.MATERIAL);

        // =============================================================================
        // DATA LOADING
        // =============================================================================

        private void LoadInventory()
        {
            // Load all item definitions from GameDatabase
            if (GameDatabase.Instance != null)
            {
                _allItems = GameDatabase.Instance.GetAllItems();
            }
            else
            {
                _allItems = new List<ItemData>();
                ErrorLogger.Warn("InventoryController: GameDatabase not available");
            }

            // Load player inventory from GameManager or SaveManager
            LoadPlayerInventory();

            // Update currency display
            UpdateCurrencyDisplay();

            // Apply filter and refresh
            RefreshItemGrid();
        }

        private void LoadPlayerInventory()
        {
            // TODO: Load from SaveManager/GameManager
            // For now, use test data
            _playerInventory.Clear();

            // Add some test items
            _playerInventory["potion_small"] = 10;
            _playerInventory["potion_medium"] = 5;
            _playerInventory["antidote"] = 3;
            _playerInventory["capture_orb_basic"] = 15;
            _playerInventory["iron_sword"] = 1;
            _playerInventory["leather_armor"] = 1;

            ErrorLogger.UI($"Loaded {_playerInventory.Count} inventory entries");
        }

        private void UpdateCurrencyDisplay()
        {
            // TODO: Get actual currency from GameManager
            int currency = 1250;
            if (_currencyAmount != null)
            {
                _currencyAmount.text = currency.ToString("N0");
            }
        }

        // =============================================================================
        // FILTERING & SORTING
        // =============================================================================

        private void SetCategory(ItemCategory? category)
        {
            _showAllCategories = !category.HasValue;
            if (category.HasValue)
            {
                _currentCategory = category.Value;
            }

            UpdateTabStyles();
            RefreshItemGrid();
        }

        private void UpdateTabStyles()
        {
            // Remove active from all tabs
            _tabAll?.RemoveFromClassList("inventory-tab-active");
            _tabConsumables?.RemoveFromClassList("inventory-tab-active");
            _tabEquipment?.RemoveFromClassList("inventory-tab-active");
            _tabKeyItems?.RemoveFromClassList("inventory-tab-active");
            _tabMaterials?.RemoveFromClassList("inventory-tab-active");

            // Add active to current tab
            if (_showAllCategories)
            {
                _tabAll?.AddToClassList("inventory-tab-active");
            }
            else
            {
                switch (_currentCategory)
                {
                    case ItemCategory.CONSUMABLE:
                        _tabConsumables?.AddToClassList("inventory-tab-active");
                        break;
                    case ItemCategory.EQUIPMENT:
                        _tabEquipment?.AddToClassList("inventory-tab-active");
                        break;
                    case ItemCategory.KEY_ITEM:
                        _tabKeyItems?.AddToClassList("inventory-tab-active");
                        break;
                    case ItemCategory.MATERIAL:
                        _tabMaterials?.AddToClassList("inventory-tab-active");
                        break;
                }
            }
        }

        private void OnSortChanged(ChangeEvent<string> evt)
        {
            _currentSortMode = _sortDropdown.index switch
            {
                0 => SortMode.Name,
                1 => SortMode.Rarity,
                2 => SortMode.Value,
                3 => SortMode.Quantity,
                4 => SortMode.Category,
                _ => SortMode.Name
            };

            RefreshItemGrid();
        }

        private void RefreshItemGrid()
        {
            // Filter items
            _filteredItems = _allItems
                .Where(item => _playerInventory.ContainsKey(item.item_id) && _playerInventory[item.item_id] > 0)
                .Where(item => _showAllCategories || item.GetCategory() == _currentCategory)
                .ToList();

            // Sort items
            _filteredItems = _currentSortMode switch
            {
                SortMode.Name => _filteredItems.OrderBy(i => i.display_name).ToList(),
                SortMode.Rarity => _filteredItems.OrderByDescending(i => i.rarity).ThenBy(i => i.display_name).ToList(),
                SortMode.Value => _filteredItems.OrderByDescending(i => i.sell_price).ToList(),
                SortMode.Quantity => _filteredItems.OrderByDescending(i => _playerInventory.GetValueOrDefault(i.item_id, 0)).ToList(),
                SortMode.Category => _filteredItems.OrderBy(i => i.GetCategory()).ThenBy(i => i.display_name).ToList(),
                _ => _filteredItems
            };

            // Update UI
            PopulateItemGrid();
            UpdateItemCount();
            UpdateEmptyState();

            // Clear selection if selected item not in filtered list
            if (_selectedItem != null && !_filteredItems.Contains(_selectedItem))
            {
                ClearSelection();
            }
        }

        // =============================================================================
        // GRID POPULATION
        // =============================================================================

        private void PopulateItemGrid()
        {
            if (_itemGrid == null) return;

            _itemGrid.Clear();
            _itemSlots.Clear();

            for (int i = 0; i < _filteredItems.Count; i++)
            {
                var item = _filteredItems[i];
                var slot = CreateItemSlot(item, i);
                _itemGrid.Add(slot);
                _itemSlots.Add(slot);
            }
        }

        private VisualElement CreateItemSlot(ItemData item, int index)
        {
            var slot = new VisualElement();
            slot.AddToClassList("item-slot");
            slot.AddToClassList($"rarity-{GetRarityClass(item.GetRarity())}");

            // Item icon
            var icon = new VisualElement();
            icon.AddToClassList("item-slot-icon");

            // Try to get icon from mappings
            var mapping = _itemIconMappings?.FirstOrDefault(m => m.itemId == item.item_id);
            if (mapping?.icon != null)
            {
                icon.style.backgroundImage = new StyleBackground(mapping.icon);
            }
            else
            {
                // Placeholder color based on category
                icon.style.backgroundColor = GetCategoryColor(item.GetCategory());
            }
            slot.Add(icon);

            // Quantity badge
            int quantity = _playerInventory.GetValueOrDefault(item.item_id, 0);
            if (quantity > 1)
            {
                var badge = new VisualElement();
                badge.AddToClassList("item-quantity-badge");

                var badgeText = new Label($"x{quantity}");
                badgeText.AddToClassList("item-quantity-text");
                badge.Add(badgeText);

                slot.Add(badge);
            }

            // Click handler
            int capturedIndex = index;
            slot.RegisterCallback<ClickEvent>(e => OnItemSlotClicked(capturedIndex));

            return slot;
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

        private Color GetCategoryColor(ItemCategory category)
        {
            return category switch
            {
                ItemCategory.CONSUMABLE => new Color(0.4f, 0.7f, 0.4f),
                ItemCategory.EQUIPMENT => new Color(0.5f, 0.5f, 0.8f),
                ItemCategory.KEY_ITEM => new Color(0.8f, 0.7f, 0.3f),
                ItemCategory.MATERIAL => new Color(0.6f, 0.5f, 0.4f),
                _ => new Color(0.4f, 0.4f, 0.4f)
            };
        }

        private void UpdateItemCount()
        {
            if (_itemCount == null) return;

            int totalItems = _playerInventory.Values.Sum();
            // TODO: Add capacity display when GameManager provides max capacity
            _itemCount.text = $"{_filteredItems.Count} items ({totalItems} total)";
        }

        private void UpdateEmptyState()
        {
            if (_emptyState == null) return;

            bool isEmpty = _filteredItems.Count == 0;
            _emptyState.style.display = isEmpty ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // =============================================================================
        // SELECTION
        // =============================================================================

        private void OnItemSlotClicked(int index)
        {
            if (index < 0 || index >= _filteredItems.Count) return;

            // Update visual selection
            if (_selectedIndex >= 0 && _selectedIndex < _itemSlots.Count)
            {
                _itemSlots[_selectedIndex].RemoveFromClassList("item-slot-selected");
            }

            _selectedIndex = index;
            _selectedItem = _filteredItems[index];
            _itemSlots[index].AddToClassList("item-slot-selected");

            // Update details panel
            UpdateDetailsPanel();

            PlaySelectSound();
        }

        private void ClearSelection()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _itemSlots.Count)
            {
                _itemSlots[_selectedIndex].RemoveFromClassList("item-slot-selected");
            }

            _selectedIndex = -1;
            _selectedItem = null;
            UpdateDetailsPanel();
        }

        private void UpdateDetailsPanel()
        {
            if (_selectedItem == null)
            {
                // Show default state
                if (_itemName != null) _itemName.text = "Select an Item";
                if (_itemCategoryLabel != null) _itemCategoryLabel.text = "";
                if (_itemDescription != null) _itemDescription.text = "Choose an item from your inventory to view its details.";
                if (_itemQuantity != null) _itemQuantity.text = "x0";
                if (_itemValue != null) _itemValue.text = "0";

                // Hide sections
                if (_statsSection != null) _statsSection.style.display = DisplayStyle.None;
                if (_effectsSection != null) _effectsSection.style.display = DisplayStyle.None;
                if (_brandSection != null) _brandSection.style.display = DisplayStyle.None;

                // Disable buttons
                _btnUse?.SetEnabled(false);
                _btnDrop?.SetEnabled(false);

                // Reset rarity border
                SetRarityBorder(Rarity.COMMON);
                return;
            }

            // Update basic info
            if (_itemName != null) _itemName.text = _selectedItem.display_name ?? "Unknown";
            if (_itemCategoryLabel != null) _itemCategoryLabel.text = GetCategoryDisplayName(_selectedItem.GetCategory());
            if (_itemDescription != null) _itemDescription.text = _selectedItem.description ?? "";

            int quantity = _playerInventory.GetValueOrDefault(_selectedItem.item_id, 0);
            if (_itemQuantity != null) _itemQuantity.text = $"x{quantity}";
            if (_itemValue != null) _itemValue.text = _selectedItem.sell_price.ToString("N0");

            // Set rarity border
            SetRarityBorder(_selectedItem.GetRarity());

            // Try to set icon
            var mapping = _itemIconMappings?.FirstOrDefault(m => m.itemId == _selectedItem.item_id);
            if (mapping?.icon != null && _itemIcon != null)
            {
                _itemIcon.style.backgroundImage = new StyleBackground(mapping.icon);
            }

            // Update sections based on item type
            UpdateStatsSection();
            UpdateEffectsSection();
            UpdateBrandSection();

            // Update action buttons
            UpdateActionButtons();
        }

        private string GetCategoryDisplayName(ItemCategory category)
        {
            return category switch
            {
                ItemCategory.CONSUMABLE => "CONSUMABLE",
                ItemCategory.EQUIPMENT => "EQUIPMENT",
                ItemCategory.KEY_ITEM => "KEY ITEM",
                ItemCategory.MATERIAL => "MATERIAL",
                _ => "ITEM"
            };
        }

        private void SetRarityBorder(Rarity rarity)
        {
            if (_itemIconFrame == null) return;

            // Remove all rarity classes
            _itemIconFrame.RemoveFromClassList("rarity-common");
            _itemIconFrame.RemoveFromClassList("rarity-uncommon");
            _itemIconFrame.RemoveFromClassList("rarity-rare");
            _itemIconFrame.RemoveFromClassList("rarity-epic");
            _itemIconFrame.RemoveFromClassList("rarity-legendary");
            _itemIconFrame.RemoveFromClassList("rarity-mythic");

            // Add current rarity class
            _itemIconFrame.AddToClassList($"rarity-{GetRarityClass(rarity)}");
        }

        private void UpdateStatsSection()
        {
            if (_statsSection == null || _statsList == null) return;

            bool isEquipment = _selectedItem.IsEquipment();
            _statsSection.style.display = isEquipment ? DisplayStyle.Flex : DisplayStyle.None;

            if (!isEquipment) return;

            _statsList.Clear();

            // Add stat rows for non-zero stats
            AddStatRow("Attack", _selectedItem.attack_bonus);
            AddStatRow("Defense", _selectedItem.defense_bonus);
            AddStatRow("Magic", _selectedItem.magic_bonus);
            AddStatRow("Resistance", _selectedItem.resistance_bonus);
            AddStatRow("Speed", _selectedItem.speed_bonus);
            AddStatRow("HP", _selectedItem.hp_bonus);
            AddStatRow("MP", _selectedItem.mp_bonus);
        }

        private void AddStatRow(string statName, int value)
        {
            if (value == 0) return;

            var row = new VisualElement();
            row.AddToClassList("item-stat-row");

            var nameLabel = new Label(statName);
            nameLabel.AddToClassList("item-stat-name");
            row.Add(nameLabel);

            var valueLabel = new Label(value > 0 ? $"+{value}" : value.ToString());
            valueLabel.AddToClassList("item-stat-value");
            valueLabel.AddToClassList(value > 0 ? "item-stat-positive" : "item-stat-negative");
            row.Add(valueLabel);

            _statsList.Add(row);
        }

        private void UpdateEffectsSection()
        {
            if (_effectsSection == null || _effectsList == null) return;

            bool isConsumable = _selectedItem.GetCategory() == ItemCategory.CONSUMABLE;
            bool hasEffects = _selectedItem.heal_amount > 0 || _selectedItem.mp_restore > 0 ||
                             _selectedItem.capture_rate_bonus > 0;

            _effectsSection.style.display = (isConsumable && hasEffects) ? DisplayStyle.Flex : DisplayStyle.None;

            if (!isConsumable || !hasEffects) return;

            _effectsList.Clear();

            if (_selectedItem.heal_amount > 0)
            {
                AddEffectRow($"Restores {_selectedItem.heal_amount} HP");
            }
            if (_selectedItem.mp_restore > 0)
            {
                AddEffectRow($"Restores {_selectedItem.mp_restore} MP");
            }
            if (_selectedItem.capture_rate_bonus > 0)
            {
                AddEffectRow($"+{_selectedItem.capture_rate_bonus * 100:F0}% capture rate");
            }
        }

        private void AddEffectRow(string effectText)
        {
            var effect = new VisualElement();
            effect.AddToClassList("item-effect");

            var text = new Label(effectText);
            text.AddToClassList("item-effect-text");
            effect.Add(text);

            _effectsList.Add(effect);
        }

        private void UpdateBrandSection()
        {
            if (_brandSection == null || _brandDisplay == null) return;

            var brand = _selectedItem.GetBrandAffinity();
            bool hasBrand = brand != Brand.NONE;

            _brandSection.style.display = hasBrand ? DisplayStyle.Flex : DisplayStyle.None;

            if (!hasBrand) return;

            var brandIcon = _brandDisplay.Q<VisualElement>(className: "brand-icon");
            var brandName = _brandDisplay.Q<Label>(className: "brand-name");

            if (brandIcon != null)
            {
                brandIcon.style.backgroundColor = GetBrandColor(brand);
            }
            if (brandName != null)
            {
                brandName.text = brand.ToString();
            }
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

        private void UpdateActionButtons()
        {
            bool hasItem = _selectedItem != null;
            bool isUsable = hasItem && (_selectedItem.GetCategory() == ItemCategory.CONSUMABLE ||
                                        _selectedItem.IsEquipment());
            bool canDrop = hasItem && _selectedItem.GetCategory() != ItemCategory.KEY_ITEM;

            _btnUse?.SetEnabled(isUsable);
            _btnDrop?.SetEnabled(canDrop);

            // Update use button text based on item type
            if (_btnUse != null && hasItem)
            {
                _btnUse.text = _selectedItem.IsEquipment() ? "EQUIP" : "USE";
            }
        }

        // =============================================================================
        // BUTTON HANDLERS
        // =============================================================================

        private void OnBackButtonClicked(ClickEvent evt)
        {
            PlayButtonSound();
            OnBackClicked?.Invoke();
        }

        private void OnUseButtonClicked(ClickEvent evt)
        {
            if (_selectedItem == null) return;

            PlayButtonSound();

            if (_selectedItem.IsEquipment())
            {
                EquipItem(_selectedItem);
            }
            else
            {
                UseItem(_selectedItem);
            }
        }

        private void OnDropButtonClicked(ClickEvent evt)
        {
            if (_selectedItem == null) return;
            if (_selectedItem.GetCategory() == ItemCategory.KEY_ITEM) return;

            PlayButtonSound();

            // TODO: Show confirmation dialog
            DropItem(_selectedItem, 1);
        }

        private void UseItem(ItemData item)
        {
            if (!_playerInventory.ContainsKey(item.item_id)) return;

            // Decrease quantity
            _playerInventory[item.item_id]--;
            if (_playerInventory[item.item_id] <= 0)
            {
                _playerInventory.Remove(item.item_id);
            }

            OnItemUsed?.Invoke(item);
            ErrorLogger.UI($"Used item: {item.display_name}");

            RefreshItemGrid();
            UpdateDetailsPanel();
        }

        private void EquipItem(ItemData item)
        {
            // TODO: Integrate with equipment system
            ErrorLogger.UI($"Equipped item: {item.display_name}");
            OnItemUsed?.Invoke(item);
        }

        private void DropItem(ItemData item, int amount)
        {
            if (!_playerInventory.ContainsKey(item.item_id)) return;

            int currentAmount = _playerInventory[item.item_id];
            int toDrop = Mathf.Min(amount, currentAmount);

            _playerInventory[item.item_id] -= toDrop;
            if (_playerInventory[item.item_id] <= 0)
            {
                _playerInventory.Remove(item.item_id);
            }

            OnItemDropped?.Invoke(item, toDrop);
            ErrorLogger.UI($"Dropped {toDrop}x {item.display_name}");

            RefreshItemGrid();
            UpdateDetailsPanel();
        }

        // =============================================================================
        // KEYBOARD NAVIGATION
        // =============================================================================

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                OnBackClicked?.Invoke();
            }
            else if (evt.keyCode == KeyCode.LeftArrow || evt.keyCode == KeyCode.A)
            {
                SelectPreviousItem();
            }
            else if (evt.keyCode == KeyCode.RightArrow || evt.keyCode == KeyCode.D)
            {
                SelectNextItem();
            }
            else if (evt.keyCode == KeyCode.UpArrow || evt.keyCode == KeyCode.W)
            {
                SelectItemAbove();
            }
            else if (evt.keyCode == KeyCode.DownArrow || evt.keyCode == KeyCode.S)
            {
                SelectItemBelow();
            }
            else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                if (_selectedItem != null && _btnUse != null && _btnUse.enabledSelf)
                {
                    OnUseButtonClicked(null);
                }
            }
        }

        private void SelectPreviousItem()
        {
            if (_filteredItems.Count == 0) return;
            int newIndex = _selectedIndex <= 0 ? _filteredItems.Count - 1 : _selectedIndex - 1;
            OnItemSlotClicked(newIndex);
        }

        private void SelectNextItem()
        {
            if (_filteredItems.Count == 0) return;
            int newIndex = (_selectedIndex + 1) % _filteredItems.Count;
            OnItemSlotClicked(newIndex);
        }

        private void SelectItemAbove()
        {
            // Assuming ~8 items per row (72px slots + margins in ~600px width)
            int itemsPerRow = 8;
            if (_filteredItems.Count == 0) return;

            int newIndex = _selectedIndex - itemsPerRow;
            if (newIndex < 0) newIndex = _filteredItems.Count + newIndex;
            if (newIndex >= 0 && newIndex < _filteredItems.Count)
            {
                OnItemSlotClicked(newIndex);
            }
        }

        private void SelectItemBelow()
        {
            int itemsPerRow = 8;
            if (_filteredItems.Count == 0) return;

            int newIndex = (_selectedIndex + itemsPerRow) % _filteredItems.Count;
            OnItemSlotClicked(newIndex);
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

            // Re-query all elements using new root
            // Query header elements
            _btnBack = _root.Q<Button>("btn-back");
            _currencyAmount = _root.Q<Label>("currency-amount");

            // Query tabs
            _tabAll = _root.Q<Button>("tab-all");
            _tabConsumables = _root.Q<Button>("tab-consumables");
            _tabEquipment = _root.Q<Button>("tab-equipment");
            _tabKeyItems = _root.Q<Button>("tab-key-items");
            _tabMaterials = _root.Q<Button>("tab-materials");

            // Query grid
            _itemGrid = _root.Q<VisualElement>("item-grid");
            _emptyState = _root.Q<VisualElement>("empty-state");
            _itemCount = _root.Q<Label>("item-count");
            _sortDropdown = _root.Q<VBDropdownField>("sort-dropdown");

            // Query details panel
            _itemIconFrame = _root.Q<VisualElement>("item-icon-frame");
            _itemIcon = _root.Q<VisualElement>("item-icon");
            _itemName = _root.Q<Label>("item-name");
            _itemCategoryLabel = _root.Q<Label>("item-category-label");
            _itemDescription = _root.Q<Label>("item-description");
            _statsSection = _root.Q<VisualElement>("stats-section");
            _statsList = _root.Q<VisualElement>("stats-list");
            _effectsSection = _root.Q<VisualElement>("effects-section");
            _effectsList = _root.Q<VisualElement>("effects-list");
            _brandSection = _root.Q<VisualElement>("brand-section");
            _brandDisplay = _root.Q<VisualElement>("brand-display");
            _itemQuantity = _root.Q<Label>("item-quantity");
            _itemValue = _root.Q<Label>("item-value");
            _btnUse = _root.Q<Button>("btn-use");
            _btnDrop = _root.Q<Button>("btn-drop");

            SetupSortDropdown();
            BindEvents();
            LoadInventory();

            ErrorLogger.UI("Inventory initialized via external root");
        }

        /// <summary>
        /// Refresh inventory data and display.
        /// </summary>
        public void Refresh()
        {
            LoadPlayerInventory();
            RefreshItemGrid();
        }

        /// <summary>
        /// Add item to player inventory.
        /// </summary>
        public void AddItem(string itemId, int amount = 1)
        {
            if (_playerInventory.ContainsKey(itemId))
            {
                _playerInventory[itemId] += amount;
            }
            else
            {
                _playerInventory[itemId] = amount;
            }

            RefreshItemGrid();
        }

        /// <summary>
        /// Remove item from player inventory.
        /// </summary>
        public bool RemoveItem(string itemId, int amount = 1)
        {
            if (!_playerInventory.ContainsKey(itemId)) return false;
            if (_playerInventory[itemId] < amount) return false;

            _playerInventory[itemId] -= amount;
            if (_playerInventory[itemId] <= 0)
            {
                _playerInventory.Remove(itemId);
            }

            RefreshItemGrid();
            return true;
        }

        /// <summary>
        /// Check if player has item.
        /// </summary>
        public bool HasItem(string itemId, int amount = 1)
        {
            return _playerInventory.ContainsKey(itemId) && _playerInventory[itemId] >= amount;
        }
    }
}
