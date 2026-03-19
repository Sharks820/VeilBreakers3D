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
    /// Communicates navigation intent via CharSelectEvents (no direct Manager reference).
    /// </summary>
    public class CarouselController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _carouselStrip;
        private Label _heroIndex;
        private readonly List<VisualElement> _heroCards = new List<VisualElement>();
        private int _selectedIndex = -1;
        private int _heroCount;

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
            if (_carouselStrip == null) return;

            _carouselStrip.Clear();
            _heroCards.Clear();

            // Fetch and sort once, not per card
            if (!GameDatabase.HasInstance) return;
            var heroes = GameDatabase.Instance.GetAllHeroes();
            heroes.Sort((a, b) => string.Compare(a.hero_id, b.hero_id, StringComparison.Ordinal));
            _heroCount = heroes.Count;

            for (int i = 0; i < heroes.Count; i++)
            {
                var card = CreateHeroCard(i, heroes[i]);
                _carouselStrip.Add(card);
                _heroCards.Add(card);
            }

            // Add teaser slot
            var teaser = CreateTeaserCard();
            _carouselStrip.Add(teaser);

            // Select first and update index label
            if (_heroCards.Count > 0)
            {
                UpdateSelection(0);
                UpdateHeroIndex(0);
            }
        }

        private VisualElement CreateHeroCard(int index, HeroData heroData)
        {
            string displayName = heroData.display_name?.ToUpper() ?? "???";
            string heroId = heroData.hero_id ?? "unknown";
            string role = heroData.role?.ToUpper() ?? "";

            var card = new VisualElement();
            card.AddToClassList("hero-card");
            card.AddToClassList($"hero-card-{heroId}");
            card.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
            card.focusable = true;
            card.tabIndex = index;

            // Large initial letter as visual identity
            string initial = displayName.Length > 0 ? displayName.Substring(0, 1) : "?";
            var initialLabel = new Label(initial);
            initialLabel.AddToClassList("hero-card-initial");
            card.Add(initialLabel);

            // Hero name below initial
            var nameLabel = new Label(displayName);
            nameLabel.AddToClassList("hero-card-name");
            card.Add(nameLabel);

            // Role tag at bottom
            if (!string.IsNullOrEmpty(role))
            {
                var roleLabel = new Label(role);
                roleLabel.AddToClassList("hero-card-role");
                card.Add(roleLabel);
            }

            // Use PointerDownEvent for instant response (ClickEvent waits for pointer up)
            int capturedIndex = index;
            card.RegisterCallback<PointerDownEvent>(_ => OnCardClicked(capturedIndex));

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
            CharSelectEvents.RaiseNavigationRequested(index);
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
                _heroIndex.text = $"HERO {index + 1} / {_heroCount}";
            }
        }
    }
}
