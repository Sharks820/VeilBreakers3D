using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Controls
{
    [UxmlElement]
    public partial class VBDropdownField : VisualElement, INotifyValueChanged<string>
    {
        // =============================================================================
        // UXML ATTRIBUTES (Unity 6 style)
        // =============================================================================

        [UxmlAttribute]
        public string Choices
        {
            get => string.Join(",", _choices);
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var parsed = value
                        .Split(',')
                        .Select(choice => choice.Trim())
                        .Where(choice => !string.IsNullOrWhiteSpace(choice))
                        .ToList();
                    SetChoices(parsed);
                }
            }
        }

        [UxmlAttribute]
        public int Index
        {
            get => _index;
            set => SetIndex(value, sendEvent: false);
        }

        // =============================================================================
        // PRIVATE FIELDS
        // =============================================================================

        private readonly VisualElement _display;
        private readonly Label _valueLabel;
        private readonly Label _arrowLabel;
        private VisualElement _popup;
        private ScrollView _popupScroll;
        private VisualElement _popupLayer;
        private VisualElement _panelRoot;
        private bool _isOpen;
        private bool _scrollResetPending;
        private int _positionAttempts;

        private List<string> _choices = new List<string>();
        private int _index = -1;

        // Cached UI tokens for zero-allocation updates
        private static readonly StyleColor ColorClear = new StyleColor(Color.clear);
        private static readonly StyleColor ColorWhite = new StyleColor(Color.white);
        private static readonly StyleColor ColorTextDefault = new StyleColor(new Color(0.73f, 0.82f, 0.78f, 1f));
        private static readonly StyleColor ColorSelectedBg = new StyleColor(new Color(0.7f, 0.31f, 0.08f, 1f));
        private static readonly StyleColor ColorHoverBg = new StyleColor(new Color(0.15f, 0.12f, 0.15f, 1f));
        private static readonly StyleColor ColorPopupBg = new StyleColor(new Color(0.07f, 0.055f, 0.07f, 1f));

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private void LogDebug(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        public List<string> choices
        {
            get => _choices;
            set => SetChoices(value);
        }

        public int index
        {
            get => _index;
            set => SetIndex(value, sendEvent: true);
        }

        public string value
        {
            get => _index >= 0 && _index < _choices.Count ? _choices[_index] : string.Empty;
            set => SetValue(value, sendEvent: true);
        }

        public VBDropdownField()
        {
            AddToClassList("vb-dropdown");
            focusable = true;
            pickingMode = PickingMode.Position;

            _display = new VisualElement();
            _display.AddToClassList("vb-dropdown__display");
            _display.style.flexGrow = 1;
            _display.style.height = new StyleLength(StyleKeyword.Auto);
            _display.pickingMode = PickingMode.Ignore; // Let clicks pass through to parent
            hierarchy.Add(_display);

            _valueLabel = new Label(string.Empty);
            _valueLabel.AddToClassList("vb-dropdown__text");
            _valueLabel.pickingMode = PickingMode.Ignore;
            _display.Add(_valueLabel);

            _arrowLabel = new Label("▼");
            _arrowLabel.AddToClassList("vb-dropdown__arrow");
            _arrowLabel.pickingMode = PickingMode.Ignore;
            _display.Add(_arrowLabel);

            // Use PointerDownEvent with TrickleDown to capture clicks before ScrollView can intercept
            // IMPORTANT: Only use ONE handler to prevent multiple toggle calls
            RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);

            RegisterCallback<GeometryChangedEvent>(_ => PositionPopupIfOpen());
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
#if VB_DROPDOWN_DEBUG
            LogDebug($"[VBDropdownField] OnPointerDown triggered on {name}, button={evt.button}");
#endif
            if (evt.button == 0) // Left click only
            {
                evt.StopPropagation();
                TogglePopup();
            }
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            // Force close and clean up popup resources
            ForceCloseAndCleanup();
        }

        /// <summary>
        /// Force close the popup and clean up all resources.
        /// Call this when the parent panel is hidden or the dropdown needs to be reset.
        /// </summary>
        public void ForceCloseAndCleanup()
        {
            _isOpen = false;
            RemoveFromClassList("vb-dropdown--open");

            if (_popup != null)
            {
                _popup.RemoveFromHierarchy();
                _popup = null;
                _popupScroll = null;
            }

            if (_panelRoot != null)
            {
                _panelRoot.UnregisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
            }

            // Don't remove the shared popup layer, other dropdowns may use it
            // Just hide it if no popups are open
            if (_popupLayer != null && _popupLayer.childCount == 0)
            {
                _popupLayer.style.display = DisplayStyle.None;
            }

            _popupLayer = null;
            _panelRoot = null;
        }

        public void SetChoices(List<string> newChoices)
        {
            _choices = newChoices ?? new List<string>();
            RebuildPopupItems();

            if (_choices.Count == 0)
            {
                SetIndex(-1, sendEvent: false);
                return;
            }

            if (_index < 0 || _index >= _choices.Count)
            {
                SetIndex(0, sendEvent: false);
            }
            else
            {
                UpdateLabel();
            }
        }

        public void SetValueWithoutNotify(string newValue)
        {
            SetValue(newValue, sendEvent: false);
        }

        private void SetValue(string newValue, bool sendEvent)
        {
            var newIndex = _choices.IndexOf(newValue);
            if (newIndex < 0)
            {
                SetIndex(-1, sendEvent);
            }
            else
            {
                SetIndex(newIndex, sendEvent);
            }
        }

        public void SetIndex(int newIndex, bool sendEvent)
        {
            if (_choices.Count == 0)
            {
                newIndex = -1;
            }
            else
            {
                newIndex = Mathf.Clamp(newIndex, 0, _choices.Count - 1);
            }

            if (newIndex == _index)
            {
                UpdateLabel();
                RefreshSelectionVisuals();
                return;
            }

            var previousValue = value;
            _index = newIndex;
            UpdateLabel();
            RefreshSelectionVisuals();

            if (!sendEvent) return;

            var evt = ChangeEvent<string>.GetPooled(previousValue, value);
            evt.target = this;
            SendEvent(evt);
        }

        private void TogglePopup()
        {
#if VB_DROPDOWN_DEBUG
            LogDebug($"[VBDropdownField] TogglePopup called, _isOpen={_isOpen}");
#endif
            if (_isOpen)
            {
                ClosePopup();
            }
            else
            {
                OpenPopup();
            }
        }

        private void OpenPopup()
        {
#if VB_DROPDOWN_DEBUG
            LogDebug($"[VBDropdownField] OpenPopup called on {name}");
#endif

            // If already open, just return
            if (_isOpen)
            {
#if VB_DROPDOWN_DEBUG
                LogDebug($"[VBDropdownField] Already open, returning");
#endif
                return;
            }

            // If not attached to panel yet, we can't open
            if (panel == null)
            {
#if VB_DROPDOWN_DEBUG
                LogDebug($"[VBDropdownField] panel is null, returning");
#endif
                return;
            }

            EnsurePopupLayer();
            if (_popupLayer == null)
            {
#if VB_DROPDOWN_DEBUG
                LogDebug($"[VBDropdownField] _popupLayer is null after EnsurePopupLayer, returning");
#endif
                return;
            }

            EnsurePopup();
            if (_popup == null)
            {
#if VB_DROPDOWN_DEBUG
                LogDebug($"[VBDropdownField] _popup is null after EnsurePopup, returning");
#endif
                return;
            }

#if VB_DROPDOWN_DEBUG
            LogDebug($"[VBDropdownField] Opening popup successfully, choices count={_choices.Count}");
#endif
            _isOpen = true;
            _scrollResetPending = true;
            _positionAttempts = 0;

            // Ensure popup layer is at the front and visible
            _popupLayer.BringToFront();
            _popupLayer.style.display = DisplayStyle.Flex;
#if VB_DROPDOWN_DEBUG
            LogDebug($"[VBDropdownField] Popup layer set to Flex, parent={_popupLayer.parent?.name}");
#endif

            // CRITICAL: Hide popup with opacity until positioned to prevent flash
            _popup.style.opacity = 0;
            _popup.style.display = DisplayStyle.Flex;
            _popup.BringToFront();
            AddToClassList("vb-dropdown--open");
#if VB_DROPDOWN_DEBUG
            LogDebug($"[VBDropdownField] Popup set to Flex, popup parent={_popup.parent?.name}");
#endif

            // Rebuild popup items to ensure fresh state
            RebuildPopupItems();

            // Position popup next frame to ensure layout is calculated
            _positionAttempts = 0;
            PositionPopupIfOpen();

            // Force scroll to top with multiple attempts
            ForceScrollToTop();

            _panelRoot?.RegisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
        }

        private void ClosePopup()
        {
            if (!_isOpen) return;

            _isOpen = false;
            RemoveFromClassList("vb-dropdown--open");

            if (_popup != null)
            {
                _popup.style.display = DisplayStyle.None;
            }

            // Only hide popup layer if no other popups are visible
            if (_popupLayer != null)
            {
                bool anyPopupVisible = false;
                foreach (var child in _popupLayer.Children())
                {
                    if (child.resolvedStyle.display == DisplayStyle.Flex)
                    {
                        anyPopupVisible = true;
                        break;
                    }
                }
                if (!anyPopupVisible)
                {
                    _popupLayer.style.display = DisplayStyle.None;
                }
            }

            if (_panelRoot != null)
            {
                _panelRoot.UnregisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
            }
        }

        private void OnRootPointerDown(PointerDownEvent evt)
        {
            if (_popup == null || _display == null) return;

            var position = evt.position;
            if (_popup.worldBound.Contains(position)) return;
            if (_display.worldBound.Contains(position)) return;

            ClosePopup();
        }

        private void EnsurePopupLayer()
        {
            if (panel == null) return;
            _panelRoot = panel.visualTree;
            if (_panelRoot == null) return;

            // Find the rootVisualElement (first child of panel.visualTree that has stylesheets)
            VisualElement rootVisualElement = null;
            foreach (var child in _panelRoot.Children())
            {
                if (child.styleSheets.count > 0 || child.ClassListContains("vb-root"))
                {
                    rootVisualElement = child;
                    break;
                }
            }

            // Fallback to first child if no styled element found
            if (rootVisualElement == null && _panelRoot.childCount > 0)
            {
                rootVisualElement = _panelRoot[0];
            }

            if (rootVisualElement == null)
            {
                LogDebug("[VBDropdownField] Could not find rootVisualElement!");
                return;
            }

            // Add popup layer as CHILD of rootVisualElement so it INHERITS stylesheets!
            _popupLayer = rootVisualElement.Q<VisualElement>("vb-dropdown-popup-layer");
            if (_popupLayer == null)
            {
                _popupLayer = new VisualElement();
                _popupLayer.name = "vb-dropdown-popup-layer";
                rootVisualElement.Add(_popupLayer);
                LogDebug($"[VBDropdownField] Created popup layer as child of {rootVisualElement.name}, stylesheets={rootVisualElement.styleSheets.count}");
            }

            // Set properties for overlay behavior
            _popupLayer.style.position = Position.Absolute;
            _popupLayer.style.left = 0;
            _popupLayer.style.top = 0;
            _popupLayer.style.right = 0;
            _popupLayer.style.bottom = 0;
            _popupLayer.style.overflow = Overflow.Visible;
            _popupLayer.style.display = DisplayStyle.None;
            _popupLayer.style.backgroundColor = ColorClear;
            _popupLayer.pickingMode = PickingMode.Ignore;
            _popupLayer.BringToFront();
        }

        private void EnsurePopup()
        {
            if (_popup != null)
            {
                // Ensure popup is still attached to layer
                if (_popup.parent != _popupLayer && _popupLayer != null)
                {
                    _popupLayer.Add(_popup);
                }
                return;
            }

            _popup = new VisualElement();
            _popup.AddToClassList("vb-dropdown-popup");
            _popup.style.position = Position.Absolute;
            _popup.style.display = DisplayStyle.None;
            _popup.pickingMode = PickingMode.Position;

            // Use cached tokens
            _popup.style.backgroundColor = ColorPopupBg;
            _popup.style.borderTopColor = ColorSelectedBg;
            _popup.style.borderBottomColor = ColorSelectedBg;
            _popup.style.borderLeftColor = ColorSelectedBg;
            _popup.style.borderRightColor = ColorSelectedBg;
            _popup.style.borderTopWidth = 2;
            _popup.style.borderBottomWidth = 2;
            _popup.style.borderLeftWidth = 2;
            _popup.style.borderRightWidth = 2;
            _popup.style.borderTopLeftRadius = 8;
            _popup.style.borderTopRightRadius = 8;
            _popup.style.borderBottomLeftRadius = 8;
            _popup.style.borderBottomRightRadius = 8;
            _popup.style.paddingTop = 6;
            _popup.style.paddingBottom = 6;
            _popup.style.paddingLeft = 6;
            _popup.style.paddingRight = 6;
            _popup.style.minWidth = 150;

            // ScrollView for scrollable options list
            _popupScroll = new ScrollView(ScrollViewMode.Vertical);
            _popupScroll.name = "popup-scroll";
            _popupScroll.pickingMode = PickingMode.Position;
            _popupScroll.style.flexGrow = 1;
            _popupScroll.style.maxHeight = 250;

            // Scrolling: mouse wheel only, no visible scrollbar
            _popupScroll.mouseWheelScrollSize = 80f;
            _popupScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _popupScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            _popup.Add(_popupScroll);

            // Clip overflow but DON'T set fixed maxHeight - let content determine size
            _popup.style.overflow = Overflow.Hidden;

            // Register for geometry changes to reset scroll after layout
            _popupScroll.RegisterCallback<GeometryChangedEvent>(OnPopupScrollGeometryChanged);

            _popupLayer?.Add(_popup);
            RebuildPopupItems();
        }

        private void OnPopupScrollGeometryChanged(GeometryChangedEvent evt)
        {
            // Only reset scroll if popup just opened (flag is set) - prevents interfering with user scrolling
            if (_scrollResetPending && _isOpen && _popupScroll != null)
            {
                _scrollResetPending = false;

                // Use schedule to avoid recursive layout issues
                schedule.Execute(() =>
                {
                    if (_isOpen && _popupScroll != null)
                    {
                        _popupScroll.scrollOffset = Vector2.zero;
                        if (_popupScroll.verticalScroller != null)
                        {
                            _popupScroll.verticalScroller.value = 0;
                        }
                    }
                }).ExecuteLater(1);
            }
        }

        private void RebuildPopupItems()
        {
            if (_popupScroll == null) return;

            // Reset scroll BEFORE clearing to prevent position preservation
            _popupScroll.scrollOffset = Vector2.zero;
            if (_popupScroll.verticalScroller != null)
            {
                _popupScroll.verticalScroller.value = 0;
            }

            _popupScroll.Clear();

            for (int i = 0; i < _choices.Count; i++)
            {
                var index = i;
                var choiceText = _choices[i];

                // Create a simple label-based button instead of using Button
                var item = new VisualElement();
                item.AddToClassList("vb-dropdown-item");
                item.focusable = true;
                item.pickingMode = PickingMode.Position;

                // Force item visibility with inline styles using cached tokens
                item.style.backgroundColor = ColorClear;
                item.style.paddingTop = 8;
                item.style.paddingBottom = 8;
                item.style.paddingLeft = 12;
                item.style.paddingRight = 12;
                item.style.marginTop = 2;
                item.style.marginBottom = 2;
                item.style.borderTopLeftRadius = 4;
                item.style.borderTopRightRadius = 4;
                item.style.borderBottomLeftRadius = 4;
                item.style.borderBottomRightRadius = 4;
                item.style.overflow = Overflow.Visible;
                item.style.minHeight = 30;
                item.style.flexDirection = FlexDirection.Row;
                item.style.alignItems = Align.Center;

                // Create label - now inherits font from USS since popup layer is child of rootVisualElement
                var label = new Label(choiceText);
                label.AddToClassList("vb-dropdown__text");
                label.pickingMode = PickingMode.Ignore;
                label.style.flexGrow = 1;
                item.Add(label);

                if (index == _index)
                {
                    item.AddToClassList("vb-dropdown-item--selected");
                    item.style.backgroundColor = ColorSelectedBg;
                    label.style.color = ColorWhite;
                    label.style.backgroundColor = StyleKeyword.None; // Remove debug bg for selected
                }

                // Register click handler
                var capturedIndex = index;
                item.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 0)
                    {
                        evt.StopPropagation();
                        SelectIndex(capturedIndex);
                    }
                });

                // Hover effect
                item.RegisterCallback<PointerEnterEvent>(evt =>
                {
                    if (!item.ClassListContains("vb-dropdown-item--selected"))
                    {
                        item.style.backgroundColor = ColorHoverBg;
                    }
                });
                item.RegisterCallback<PointerLeaveEvent>(evt =>
                {
                    if (!item.ClassListContains("vb-dropdown-item--selected"))
                    {
                        item.style.backgroundColor = ColorClear;
                    }
                });

                _popupScroll.Add(item);
            }
        }

        private void RefreshSelectionVisuals()
        {
            if (_popupScroll == null) return;

            int i = 0;
            foreach (var child in _popupScroll.Children())
            {
                var label = child.Q<Label>();
                if (i == _index)
                {
                    child.AddToClassList("vb-dropdown-item--selected");
                    child.style.backgroundColor = ColorSelectedBg;
                    if (label != null) label.style.color = ColorWhite;
                }
                else
                {
                    child.RemoveFromClassList("vb-dropdown-item--selected");
                    child.style.backgroundColor = ColorClear;
                    if (label != null) label.style.color = ColorTextDefault;
                }
                i++;
            }
        }

        private void SelectIndex(int newIndex)
        {
            SetIndex(newIndex, sendEvent: true);
            ClosePopup();
        }

        private void ForceScrollToTop()
        {
            if (_popupScroll == null) return;

            // Immediate attempt
            ResetScrollPosition();

            // Try again after 1 frame (~16ms at 60fps)
            schedule.Execute(ResetScrollPosition).ExecuteLater(1);

            // Try again after 2 frames
            schedule.Execute(ResetScrollPosition).ExecuteLater(16);

            // Try again after layout pass
            schedule.Execute(ResetScrollPosition).ExecuteLater(50);

            // Final attempt after 150ms to catch any delayed layout
            schedule.Execute(ResetScrollPosition).ExecuteLater(150);
        }

        private void ResetScrollPosition()
        {
            if (_popupScroll == null || !_isOpen) return;

            // Force scroll to absolute top - set value to 0 explicitly
            _popupScroll.scrollOffset = Vector2.zero;

            var scroller = _popupScroll.verticalScroller;
            if (scroller != null)
            {
                // Set to 0 directly, not lowValue (which might not be 0)
                scroller.value = 0;
            }

            // Also scroll to first element as backup
            var content = _popupScroll.contentContainer;
            if (content != null && content.childCount > 0)
            {
                _popupScroll.ScrollTo(content[0]);
            }
        }

        private void PositionPopupIfOpen()
        {
            if (!_isOpen || _popup == null || _panelRoot == null || _popupLayer == null) return;

            var fieldBounds = _display.worldBound;

            // If layout not ready, retry with actual delay (not 0) - fixes first-open positioning bug
            if (fieldBounds.height <= 0 || fieldBounds.width <= 0 || _popupLayer.resolvedStyle.width <= 0)
            {
                if (_positionAttempts < 5)
                {
                    _positionAttempts++;
                    schedule.Execute(PositionPopupIfOpen).ExecuteLater(16); // One frame at 60fps
                    return;
                }
                // Fallback: position directly below the display element
                _popup.style.translate = StyleKeyword.None;
                _popup.style.left = 0;
                _popup.style.top = resolvedStyle.height;
                _popup.style.opacity = 1; // Show after fallback positioning
                return;
            }

            // Convert the display bounds into popup-layer local space for accurate positioning
            var localTopLeft = _popupLayer.WorldToLocal(fieldBounds.position);
            var localBottomLeft = _popupLayer.WorldToLocal(new Vector2(fieldBounds.xMin, fieldBounds.yMax));

            float popupHeight = _popup.resolvedStyle.height;
            if (popupHeight <= 0) popupHeight = 200; // Fallback estimate

            float targetY = localBottomLeft.y;
            float layerHeight = _popupLayer.resolvedStyle.height > 0 ? _popupLayer.resolvedStyle.height : Screen.height;

            // If the popup would overflow the layer bottom, try positioning above
            if (targetY + popupHeight > layerHeight && (localTopLeft.y - popupHeight) >= 0)
            {
                targetY = localTopLeft.y - popupHeight;
            }

            // Clamp horizontally inside the layer - ensure X is never negative
            float targetX = Mathf.Max(0, localTopLeft.x);

            // Use left/top for reliable positioning instead of translate
            _popup.style.width = fieldBounds.width;
            _popup.style.translate = StyleKeyword.None;
            _popup.style.left = targetX;
            _popup.style.top = targetY;

            // Now that positioning is complete, show the popup
            _popup.style.opacity = 1;
        }

        private void UpdateLabel()
        {
            _valueLabel.text = value;
        }
    }
}
