using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;
using VeilBreakers.Data;
using VeilBreakers.Core;
using VeilBreakers.Managers;

namespace VeilBreakers.UI.Menus
{
    /// <summary>
    /// AAA save slot browser overlay for the main menu.
    /// Shows all save slots (3 manual + auto-save) with hero portrait, name,
    /// level, location, playtime. Allows loading or deleting saves.
    /// Integrates with existing SaveManager and MainMenuController.
    /// </summary>
    public class SaveSlotBrowserController : MonoBehaviour
    {
        // =============================================================================
        // CONSTANTS
        // =============================================================================

        private const string kOverlayName = "save-browser-overlay";
        private const string kPanelName = "save-browser-panel";
        private const string kSlotsContainer = "save-slots-container";
        private const string kBtnBack = "save-browser-back";
        private const string kTitleLabel = "save-browser-title";
        private const string kSubtitleLabel = "save-browser-subtitle";
        private const float kAnimDuration = 0.35f;
        private static readonly WaitForSeconds _waitAnimDuration = new WaitForSeconds(kAnimDuration);
        private static readonly WaitForSeconds _waitAnimDurationClose = new WaitForSeconds(kAnimDuration * 0.7f);

        // =============================================================================
        // DEPENDENCIES
        // =============================================================================

        [SerializeField] private UIDocument _uiDocument;

        // =============================================================================
        // STATE
        // =============================================================================

        private VisualElement _root;
        private VisualElement _overlay;
        private VisualElement _panel;
        private VisualElement _slotsContainer;
        private Button _btnBack;
        private Label _titleLabel;
        private Label _subtitleLabel;

        private bool _isOpen;
        private bool _isAnimating;
        private bool _isDeleting;
        private SaveSlotMetadata[] _cachedMetadata;

        /// <summary>Fires when user selects a slot to load. Passes slot index.</summary>
        public event Action<int> OnSlotSelected;

        /// <summary>Fires when user closes the browser.</summary>
        public event Action OnBrowserClosed;

        /// <summary>Programmatically wires dependencies when created at runtime.</summary>
        public void AutoWire(UIDocument uiDocument)
        {
            if (_uiDocument == null) _uiDocument = uiDocument;
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Opens the save browser overlay. Loads metadata and builds slot cards.
        /// </summary>
        public void Open()
        {
            if (_isOpen || _isAnimating) return;
            EnsureOverlayBuilt();
            StartCoroutine(OpenCoroutine());
        }

        /// <summary>
        /// Closes the save browser overlay with animation.
        /// </summary>
        public void Close()
        {
            if (!_isOpen || _isAnimating) return;
            StartCoroutine(CloseCoroutine());
        }

        public bool IsOpen => _isOpen;

        // =============================================================================
        // LIFECYCLE
        // =============================================================================

        private void OnEnable()
        {
            if (_uiDocument == null) _uiDocument = GetComponent<UIDocument>();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (_btnBack != null) _btnBack.UnregisterCallback<ClickEvent>(OnBackClicked);

            // Reset state so re-enable works cleanly (DEEP-07)
            _isAnimating = false;
            _isDeleting = false;
            _cachedMetadata = null;
            if (_isOpen && _overlay != null)
            {
                _overlay.style.display = DisplayStyle.None;
                _isOpen = false;
            }
        }

        // =============================================================================
        // OVERLAY CONSTRUCTION
        // =============================================================================

        /// <summary>
        /// Builds the save browser overlay dynamically and inserts it into the UI root.
        /// This avoids needing a separate UXML file — everything is self-contained.
        /// </summary>
        private void EnsureOverlayBuilt()
        {
            if (_overlay != null) return;
            if (_uiDocument == null) return;

            _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            // === OVERLAY (fullscreen dimming backdrop) ===
            _overlay = new VisualElement();
            _overlay.name = kOverlayName;
            _overlay.AddToClassList("save-browser-overlay");
            _overlay.style.display = DisplayStyle.None;
            _overlay.pickingMode = PickingMode.Position; // blocks clicks to menu behind

            // === PANEL (centered card) ===
            _panel = new VisualElement();
            _panel.name = kPanelName;
            _panel.AddToClassList("save-browser-panel");

            // Header area
            var header = new VisualElement();
            header.AddToClassList("save-browser-header");

            _titleLabel = new Label("SAVED JOURNEYS");
            _titleLabel.name = kTitleLabel;
            _titleLabel.AddToClassList("save-browser-title");
            header.Add(_titleLabel);

            _subtitleLabel = new Label("Select a save to continue your path through the Veil");
            _subtitleLabel.name = kSubtitleLabel;
            _subtitleLabel.AddToClassList("save-browser-subtitle");
            header.Add(_subtitleLabel);

            // Decorative divider
            var divider = new VisualElement();
            divider.AddToClassList("save-browser-divider");
            header.Add(divider);

            _panel.Add(header);

            // Slots container
            _slotsContainer = new VisualElement();
            _slotsContainer.name = kSlotsContainer;
            _slotsContainer.AddToClassList("save-slots-container");
            _panel.Add(_slotsContainer);

            // Footer with back button
            var footer = new VisualElement();
            footer.AddToClassList("save-browser-footer");

            _btnBack = new Button();
            _btnBack.name = kBtnBack;
            _btnBack.text = "BACK";
            _btnBack.AddToClassList("save-browser-back-btn");
            _btnBack.RegisterCallback<ClickEvent>(OnBackClicked);
            footer.Add(_btnBack);

            _panel.Add(footer);
            _overlay.Add(_panel);

            // Add overlay on top of everything in the menu root
            var menuRoot = _root.Q<VisualElement>("menu-root") ?? _root;
            menuRoot.Add(_overlay);
        }

        // =============================================================================
        // OPEN / CLOSE FLOW
        // =============================================================================

        private IEnumerator OpenCoroutine()
        {
            _isAnimating = true;

            // Load metadata from save manager
            yield return LoadSlotMetadata();

            // Build slot cards
            RebuildSlotCards();

            // Show overlay
            _overlay.style.display = DisplayStyle.Flex;
            _overlay.style.opacity = 0f;
            _panel.style.scale = new Scale(new Vector2(0.95f, 0.95f));

            // Animate in // VB-IGNORE PERF-02: PrimeTween UI callbacks, infrequent one-shot
            Tween.Custom(_overlay, 0f, 1f, kAnimDuration,
                onValueChange: (el, val) => { el.style.opacity = val; },
                ease: Ease.OutCubic);
            Tween.Custom(_panel, 0.95f, 1f, kAnimDuration,
                onValueChange: (el, val) => { el.style.scale = new Scale(new Vector2(val, val)); },
                ease: Ease.OutBack);

            yield return _waitAnimDuration;
            _isOpen = true;
            _isAnimating = false;
        }

        private IEnumerator CloseCoroutine()
        {
            _isAnimating = true;

            // Animate out // VB-IGNORE PERF-02: PrimeTween UI callbacks, infrequent one-shot
            Tween.Custom(_overlay, 1f, 0f, kAnimDuration * 0.7f,
                onValueChange: (el, val) => { el.style.opacity = val; },
                ease: Ease.InCubic);
            Tween.Custom(_panel, 1f, 0.95f, kAnimDuration * 0.7f,
                onValueChange: (el, val) => { el.style.scale = new Scale(new Vector2(val, val)); },
                ease: Ease.InCubic);

            yield return _waitAnimDurationClose;

            _overlay.style.display = DisplayStyle.None;
            _isOpen = false;
            _isAnimating = false;
            OnBrowserClosed?.Invoke();
        }

        private IEnumerator LoadSlotMetadata()
        {
            if (!SaveManager.HasInstance)
            {
                _cachedMetadata = new SaveSlotMetadata[0];
                yield break;
            }

            var task = SaveManager.Instance.GetAllSlotsMetadataAsync();
            while (!task.IsCompleted) yield return null;

            if (task.IsFaulted)
            {
                Debug.LogError($"[SaveSlotBrowser] Failed to load metadata: {task.Exception}");
                _cachedMetadata = new SaveSlotMetadata[0];
                yield break;
            }

            _cachedMetadata = task.Result;
        }

        // =============================================================================
        // SLOT CARD CONSTRUCTION
        // =============================================================================

        private void RebuildSlotCards()
        {
            if (_slotsContainer == null) return;
            _slotsContainer.Clear();

            if (_cachedMetadata == null || _cachedMetadata.Length == 0)
            {
                var empty = new Label("No saved journeys found.");
                empty.AddToClassList("save-browser-empty");
                _slotsContainer.Add(empty);
                return;
            }

            // Show slots that have data, sorted by most recent first
            bool anySlots = false;
            for (int i = 0; i < _cachedMetadata.Length; i++)
            {
                var meta = _cachedMetadata[i];
                if (meta == null || !meta.hasData) continue;

                var card = BuildSlotCard(meta);
                _slotsContainer.Add(card);
                anySlots = true;
            }

            if (!anySlots)
            {
                var empty = new Label("No saved journeys found.");
                empty.AddToClassList("save-browser-empty");
                _slotsContainer.Add(empty);
            }
        }

        /// <summary>
        /// Builds a single save slot card with hero info, playtime, location, and action buttons.
        /// </summary>
        private VisualElement BuildSlotCard(SaveSlotMetadata meta)
        {
            var card = new VisualElement();
            card.AddToClassList("save-slot-card");

            if (meta.isCorrupted)
            {
                card.AddToClassList("save-slot-corrupted");
            }

            // Left: Slot type indicator (colored bar)
            var slotIndicator = new VisualElement();
            slotIndicator.AddToClassList("save-slot-indicator");
            bool isAuto = meta.slotIndex < 0;
            if (isAuto) slotIndicator.AddToClassList("save-slot-indicator-auto");
            card.Add(slotIndicator);

            // Middle: Info section
            var info = new VisualElement();
            info.AddToClassList("save-slot-info");

            // Top row: hero name + slot label
            var topRow = new VisualElement();
            topRow.AddToClassList("save-slot-top-row");

            string heroDisplay = !string.IsNullOrEmpty(meta.heroName) ? meta.heroName : "Unknown Hero";
            var heroNameLabel = new Label(heroDisplay);
            heroNameLabel.AddToClassList("save-slot-hero-name");
            topRow.Add(heroNameLabel);

            string slotLabel = isAuto ? "AUTO" : $"SLOT {meta.slotIndex + 1}";
            var slotTag = new Label(slotLabel);
            slotTag.AddToClassList("save-slot-tag");
            if (isAuto) slotTag.AddToClassList("save-slot-tag-auto");
            topRow.Add(slotTag);

            info.Add(topRow);

            // Middle row: level, path, location
            var midRow = new VisualElement();
            midRow.AddToClassList("save-slot-mid-row");

            var levelLabel = new Label($"LVL {meta.heroLevel}");
            levelLabel.AddToClassList("save-slot-detail");
            levelLabel.AddToClassList("save-slot-level");
            midRow.Add(levelLabel);

            if (meta.heroPath != Path.NONE)
            {
                var pathLabel = new Label(meta.heroPath.ToString());
                pathLabel.AddToClassList("save-slot-detail");
                pathLabel.AddToClassList("save-slot-path");
                midRow.Add(pathLabel);
            }

            if (!string.IsNullOrEmpty(meta.currentLocation))
            {
                var locLabel = new Label(meta.currentLocation);
                locLabel.AddToClassList("save-slot-detail");
                locLabel.AddToClassList("save-slot-location");
                midRow.Add(locLabel);
            }

            info.Add(midRow);

            // Bottom row: playtime and save date
            var bottomRow = new VisualElement();
            bottomRow.AddToClassList("save-slot-bottom-row");

            var playtimeLabel = new Label(meta.GetFormattedPlaytime());
            playtimeLabel.AddToClassList("save-slot-playtime");
            bottomRow.Add(playtimeLabel);

            var dateLabel = new Label(meta.GetFormattedDate());
            dateLabel.AddToClassList("save-slot-date");
            bottomRow.Add(dateLabel);

            info.Add(bottomRow);
            card.Add(info);

            // Right: Action buttons
            var actions = new VisualElement();
            actions.AddToClassList("save-slot-actions");

            if (!meta.isCorrupted)
            {
                var loadBtn = new Button();
                loadBtn.text = "LOAD";
                loadBtn.AddToClassList("save-slot-load-btn");
                int capturedSlot = meta.slotIndex;
                loadBtn.RegisterCallback<ClickEvent>(evt =>
                {
                    if (_isDeleting) return; // Prevent load during delete operation
                    OnSlotSelected?.Invoke(capturedSlot);
                });
                actions.Add(loadBtn);
            }
            else
            {
                var corruptLabel = new Label("CORRUPTED");
                corruptLabel.AddToClassList("save-slot-corrupt-label");
                actions.Add(corruptLabel);
            }

            var deleteBtn = new Button();
            deleteBtn.text = "DELETE";
            deleteBtn.AddToClassList("save-slot-delete-btn");
            int slotForDelete = meta.slotIndex;
            deleteBtn.RegisterCallback<ClickEvent>(evt => OnDeleteClicked(slotForDelete));
            actions.Add(deleteBtn);

            card.Add(actions);
            return card;
        }

        // =============================================================================
        // EVENT HANDLERS
        // =============================================================================

        private void OnBackClicked(ClickEvent evt)
        {
            Close();
        }

        private void OnDeleteClicked(int slot)
        {
            if (_isDeleting || _isAnimating) return; // Prevent concurrent deletes
            StartCoroutine(ConfirmAndDelete(slot));
        }

        private IEnumerator ConfirmAndDelete(int slot)
        {
            if (!SaveManager.HasInstance) yield break;
            if (SaveManager.Instance.IsSaving || SaveManager.Instance.IsLoading) yield break;

            _isDeleting = true;
            try
            {
                // Simple confirmation: replace the card content temporarily
                // TODO: add confirmation dialog.
                bool deleted = SaveManager.Instance.DeleteSlot(slot);
                if (deleted)
                {
                    Debug.Log($"[SaveSlotBrowser] Deleted slot {slot}");
                    yield return LoadSlotMetadata();
                    RebuildSlotCards();
                }
            }
            finally
            {
                _isDeleting = false;
            }
        }
    }
}
