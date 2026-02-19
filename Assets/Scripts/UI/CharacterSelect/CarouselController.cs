using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Manages the hero carousel strip at the bottom.
    /// Dynamically generates hero cards from data, handles selection highlighting,
    /// and updates hero index label.
    /// </summary>
    public class CarouselController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private CharacterSelectManager _manager;

        private VisualElement _carouselStrip;
        private Label _heroIndex;
        private readonly List<VisualElement> _heroCards = new List<VisualElement>();
        private int _selectedIndex = -1;

        private void OnEnable()
        {
            CacheReferences();
            CharSelectEvents.OnScreenReady += HandleScreenReady;
            CharSelectEvents.OnHeroChanged += HandleHeroChanged;
        }

        private void OnDisable()
        {
            CharSelectEvents.OnScreenReady -= HandleScreenReady;
            CharSelectEvents.OnHeroChanged -= HandleHeroChanged;
        }

        private void CacheReferences()
        {
            if (_uiDocument == null) { Debug.LogError("[CarouselController] UIDocument not assigned!"); return; }
            var root = _uiDocument.rootVisualElement;
            _carouselStrip = root.Q<VisualElement>("carousel-strip");
            _heroIndex = root.Q<Label>("hero-index");
        }

        private void HandleScreenReady()
        {
            BuildCarousel();
        }

        private void BuildCarousel()
        {
            if (_carouselStrip == null || _manager == null) return;

            _carouselStrip.Clear();
            _heroCards.Clear();

            // Fetch and sort once, not per card
            if (!GameDatabase.HasInstance) return;
            var heroes = GameDatabase.Instance.GetAllHeroes();
            heroes.Sort((a, b) => string.Compare(a.hero_id, b.hero_id, StringComparison.Ordinal));

            for (int i = 0; i < heroes.Count; i++)
            {
                string name = heroes[i].display_name?.ToUpper() ?? "???";
                var card = CreateHeroCard(i, name);
                _carouselStrip.Add(card);
                _heroCards.Add(card);
            }

            // Add teaser slot
            var teaser = CreateTeaserCard();
            _carouselStrip.Add(teaser);

            // Select first
            if (_heroCards.Count > 0)
            {
                UpdateSelection(0);
            }
        }

        private VisualElement CreateHeroCard(int index, string displayName)
        {
            var card = new VisualElement();
            card.AddToClassList("hero-card");
            card.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;

            var label = new Label(displayName);
            label.AddToClassList("hero-card-name");
            card.Add(label);

            // Click handler (capture index)
            int capturedIndex = index;
            card.RegisterCallback<ClickEvent>(_ => OnCardClicked(capturedIndex));

            return card;
        }

        private VisualElement CreateTeaserCard()
        {
            var card = new VisualElement();
            card.AddToClassList("hero-card");
            card.AddToClassList("teaser");

            var label = new Label("?");
            label.AddToClassList("hero-card-name");
            card.Add(label);

            var subLabel = new Label("COMING SOON");
            subLabel.AddToClassList("hero-card-name");
            subLabel.style.fontSize = 7;
            subLabel.style.opacity = 0.5f;
            card.Add(subLabel);

            return card;
        }

        private void OnCardClicked(int index)
        {
            _manager?.NavigateToHero(index);
        }

        private void HandleHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            UpdateSelection(index);
            UpdateHeroIndex(index);
        }

        private void UpdateSelection(int index)
        {
            // Remove previous selection
            if (_selectedIndex >= 0 && _selectedIndex < _heroCards.Count)
            {
                _heroCards[_selectedIndex].RemoveFromClassList("selected");
            }

            // Apply new selection
            _selectedIndex = index;
            if (_selectedIndex >= 0 && _selectedIndex < _heroCards.Count)
            {
                _heroCards[_selectedIndex].AddToClassList("selected");
            }
        }

        private void UpdateHeroIndex(int index)
        {
            if (_heroIndex != null)
            {
                _heroIndex.text = $"HERO {index + 1} / {_manager?.HeroCount ?? 0}";
            }
        }
    }
}
