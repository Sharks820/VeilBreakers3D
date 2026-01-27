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

        public IReadOnlyList<string> choices => _choices;

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
            EnsurePopupLayer();
            EnsurePopup();
            _isOpen = true;
            _positionAttempts = 0;

            _popup.style.display = DisplayStyle.Flex;
            AddToClassList("vb-dropdown--open");
            _popup.BringToFront();
            PositionPopupIfOpen();

            _panelRoot?.RegisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
        }

        private void ClosePopup()
        {
            _isOpen = false;
            RemoveFromClassList("vb-dropdown--open");
            if (_popup != null)
            {
                _popup.style.display = DisplayStyle.None;
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
            if (_popupLayer != null) return;

            _popupLayer = new VisualElement();
            _popupLayer.name = "vb-dropdown-popup-layer";
            _popupLayer.style.position = Position.Absolute;
            _popupLayer.style.left = 0;
            _popupLayer.style.top = 0;
            _popupLayer.style.right = 0;
            _popupLayer.style.bottom = 0;
            _popupLayer.style.overflow = Overflow.Visible;
            _panelRoot.Add(_popupLayer);
        }

        private void EnsurePopup()
        {
            if (_popup != null) return;

            _popup = new VisualElement();
            _popup.AddToClassList("vb-dropdown-popup");
            _popup.style.position = Position.Absolute;
            _popup.style.display = DisplayStyle.None;

            _popupScroll = new ScrollView(ScrollViewMode.Vertical);
            _popupScroll.AddToClassList("vb-dropdown-popup__scroll");
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
            if (!_isOpen || _popup == null || _panelRoot == null) return;

            if (_popup.resolvedStyle.height <= 0 && _positionAttempts < 3)
            {
                _positionAttempts++;
                schedule.Execute(PositionPopupIfOpen).ExecuteLater(0);
                return;
            }

            var fieldBounds = _display.worldBound;
            if (fieldBounds.height <= 0 || fieldBounds.width <= 0) return;

            float panelHeight = _panelRoot.resolvedStyle.height;
            if (panelHeight <= 0)
            {
                panelHeight = Screen.height;
            }

            float popupHeight = _popup.resolvedStyle.height;
            float below = fieldBounds.y + fieldBounds.height;
            float above = fieldBounds.y - popupHeight;
            float targetY = below;

            if (popupHeight > 0 && panelHeight > 0 && (below + popupHeight) > panelHeight && above >= 0)
            {
                targetY = above;
            }

            _popup.style.left = fieldBounds.x;
            _popup.style.top = targetY;
            _popup.style.width = fieldBounds.width;
        }

        private void UpdateLabel()
        {
            _valueLabel.text = value;
        }
    }
}
