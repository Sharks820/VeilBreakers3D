using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Controls
{
    public class VBDropdownField : VisualElement, INotifyValueChanged<string>
    {
        public new class UxmlFactory : UxmlFactory<VBDropdownField, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlStringAttributeDescription _choices = new UxmlStringAttributeDescription
            {
                name = "choices",
                defaultValue = string.Empty
            };

            private readonly UxmlIntAttributeDescription _index = new UxmlIntAttributeDescription
            {
                name = "index",
                defaultValue = 0
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                if (ve is not VBDropdownField dropdown) return;

                var rawChoices = _choices.GetValueFromBag(bag, cc);
                if (!string.IsNullOrWhiteSpace(rawChoices))
                {
                    var parsed = rawChoices
                        .Split(',')
                        .Select(choice => choice.Trim())
                        .Where(choice => !string.IsNullOrWhiteSpace(choice))
                        .ToList();
                    dropdown.SetChoices(parsed);
                }

                var index = _index.GetValueFromBag(bag, cc);
                dropdown.SetIndex(index, sendEvent: false);
            }
        }

        private readonly VisualElement _display;
        private readonly Label _valueLabel;
        private readonly Label _arrowLabel;
        private VisualElement _popup;
        private ScrollView _popupScroll;
        private VisualElement _popupLayer;
        private VisualElement _panelRoot;
        private bool _isOpen;
        private int _positionAttempts;

        private List<string> _choices = new List<string>();
        private int _index = -1;

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

            _display = new VisualElement();
            _display.AddToClassList("vb-dropdown__display");
            hierarchy.Add(_display);

            _valueLabel = new Label(string.Empty);
            _valueLabel.AddToClassList("vb-dropdown__text");
            _display.Add(_valueLabel);

            _arrowLabel = new Label("v");
            _arrowLabel.AddToClassList("vb-dropdown__arrow");
            _display.Add(_arrowLabel);

            _display.RegisterCallback<PointerDownEvent>(OnDisplayPointerDown);
            RegisterCallback<GeometryChangedEvent>(_ => PositionPopupIfOpen());

            // Clean up popup when this element is detached from panel
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
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

        private void OnDisplayPointerDown(PointerDownEvent evt)
        {
            evt.StopPropagation();
            TogglePopup();
        }

        private void TogglePopup()
        {
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
            // If already open, just return
            if (_isOpen) return;

            EnsurePopupLayer();
            if (_popupLayer == null)
            {
                return;
            }
            EnsurePopup();
            _isOpen = true;
            _positionAttempts = 0;

            // Ensure popup layer is at the front and visible
            _popupLayer.BringToFront();
            _popupLayer.style.display = DisplayStyle.Flex;

            // Show and position this dropdown's popup
            _popup.style.display = DisplayStyle.Flex;
            _popup.BringToFront();
            AddToClassList("vb-dropdown--open");

            // Rebuild popup items to ensure fresh state
            RebuildPopupItems();

            // Position popup next frame to ensure layout is calculated
            _positionAttempts = 0;
            PositionPopupIfOpen();

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

            _popupLayer = _panelRoot.Q<VisualElement>("vb-dropdown-popup-layer");
            if (_popupLayer != null)
            {
                // Always ensure layer is at front when accessed
                _popupLayer.BringToFront();
                return;
            }

            _popupLayer = new VisualElement();
            _popupLayer.name = "vb-dropdown-popup-layer";
            _popupLayer.style.position = Position.Absolute;
            _popupLayer.style.left = 0;
            _popupLayer.style.top = 0;
            _popupLayer.style.right = 0;
            _popupLayer.style.bottom = 0;
            _popupLayer.style.overflow = Overflow.Visible;
            _popupLayer.style.display = DisplayStyle.None;
            // Layer ignores pointer events - only the popup inside will catch them
            _popupLayer.pickingMode = PickingMode.Ignore;
            // Bring to front of visual tree
            _panelRoot.Add(_popupLayer);
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

            _popupScroll = new ScrollView(ScrollViewMode.Vertical);
            _popupScroll.AddToClassList("vb-dropdown-popup__scroll");
            _popupScroll.pickingMode = PickingMode.Position;
            _popup.Add(_popupScroll);

            _popupLayer?.Add(_popup);
            RebuildPopupItems();
        }

        private void RebuildPopupItems()
        {
            if (_popupScroll == null) return;

            _popupScroll.Clear();
            for (int i = 0; i < _choices.Count; i++)
            {
                var index = i;
                var button = new Button(() => SelectIndex(index));
                button.text = _choices[i];
                button.AddToClassList("vb-dropdown-item");
                if (index == _index)
                {
                    button.AddToClassList("vb-dropdown-item--selected");
                }
                _popupScroll.Add(button);
            }
        }

        private void RefreshSelectionVisuals()
        {
            if (_popupScroll == null) return;

            int i = 0;
            foreach (var child in _popupScroll.Children())
            {
                if (child is Button button)
                {
                    if (i == _index)
                    {
                        button.AddToClassList("vb-dropdown-item--selected");
                    }
                    else
                    {
                        button.RemoveFromClassList("vb-dropdown-item--selected");
                    }
                }
                i++;
            }
        }

        private void SelectIndex(int newIndex)
        {
            SetIndex(newIndex, sendEvent: true);
            ClosePopup();
        }

        private void PositionPopupIfOpen()
        {
            if (!_isOpen || _popup == null || _panelRoot == null || _popupLayer == null) return;

            if (_popup.resolvedStyle.height <= 0 && _positionAttempts < 3)
            {
                _positionAttempts++;
                schedule.Execute(PositionPopupIfOpen).ExecuteLater(0);
                return;
            }

            var fieldBounds = _display.worldBound;
            if (fieldBounds.height <= 0 || fieldBounds.width <= 0) return;

            // Get the popup layer's world position to calculate relative coordinates
            var layerBounds = _popupLayer.worldBound;

            float panelHeight = _panelRoot.resolvedStyle.height;
            if (panelHeight <= 0)
            {
                panelHeight = Screen.height;
            }

            float popupHeight = _popup.resolvedStyle.height;
            if (popupHeight <= 0) popupHeight = 200; // Estimate if not yet laid out

            float below = fieldBounds.y + fieldBounds.height;
            float above = fieldBounds.y - popupHeight;
            float targetY = below;

            // Check if popup would overflow bottom of panel, prefer showing above if there's room
            if (popupHeight > 0 && panelHeight > 0 && (below + popupHeight) > panelHeight && above >= 0)
            {
                targetY = above;
            }

            // Calculate position relative to the popup layer (not absolute screen coords)
            float relativeX = fieldBounds.x - layerBounds.x;
            float relativeY = targetY - layerBounds.y;

            _popup.style.left = relativeX;
            _popup.style.top = relativeY;
            _popup.style.width = fieldBounds.width;
        }

        private void UpdateLabel()
        {
            _valueLabel.text = value;
        }
    }
}
