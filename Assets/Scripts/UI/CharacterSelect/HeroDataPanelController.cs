using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Populates the left info panel: name, title, quote, path/role/synergy,
    /// starter stats grid, champion monster info, and backstory lore.
    /// </summary>
    public class HeroDataPanelController : MonoBehaviour
    {
        // =============================================================================
        // CONSTANTS
        // =============================================================================

        private const string kHeroBackstory = "hero-backstory";
        private const string kHeroLoreSection = "hero-lore-section";

        // =============================================================================
        // SERIALIZED FIELDS
        // =============================================================================

        [SerializeField] private UIDocument _uiDocument;

        // =============================================================================
        // CACHED REFERENCES
        // =============================================================================

        private VisualElement _panel;
        private Label _heroName;
        private Label _heroTitle;
        private Label _heroQuote;
        private Label _heroPath;
        private Label _heroRole;
        private Label _heroSynergy;
        private Label _statHp;
        private Label _statAtk;
        private Label _statDef;
        private Label _statSpd;
        private Label _championName;
        private Label _championBrand;
        private Label _championRole;
        private VisualElement _championSection;
        private VisualElement _heroLoreSection;
        private Label _heroBackstory;

        private void OnEnable()
        {
            CacheReferences();
            CharSelectEvents.OnHeroChanged += HandleHeroChanged;
        }

        private void OnDisable()
        {
            CharSelectEvents.OnHeroChanged -= HandleHeroChanged;
        }

        private void CacheReferences()
        {
            if (_uiDocument == null) { Debug.LogError("[HeroDataPanelController] UIDocument not assigned!"); return; }
            var root = _uiDocument.rootVisualElement;
            _panel = root.Q<VisualElement>("hero-info-panel");
            if (_panel != null) _panel.usageHints |= UsageHints.DynamicTransform;
            _heroName = root.Q<Label>("hero-name");
            _heroTitle = root.Q<Label>("hero-title");
            _heroQuote = root.Q<Label>("hero-quote");
            _heroPath = root.Q<Label>("hero-path");
            _heroRole = root.Q<Label>("hero-role");
            _heroSynergy = root.Q<Label>("hero-synergy");
            _statHp = root.Q<Label>("stat-hp");
            _statAtk = root.Q<Label>("stat-atk");
            _statDef = root.Q<Label>("stat-def");
            _statSpd = root.Q<Label>("stat-spd");
            _championName = root.Q<Label>("champion-name");
            _championBrand = root.Q<Label>("champion-brand");
            _championRole = root.Q<Label>("champion-role");
            _championSection = root.Q<VisualElement>("champion-section");
            _heroLoreSection = root.Q<VisualElement>(kHeroLoreSection);
            _heroBackstory = root.Q<Label>(kHeroBackstory);

            Debug.Assert(_heroBackstory != null, $"[HeroDataPanel] Missing element: {kHeroBackstory}");
            Debug.Assert(_heroLoreSection != null, $"[HeroDataPanel] Missing element: {kHeroLoreSection}");
        }

        private void HandleHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            if (data == null) return;

            // Identity
            CharSelectUIUtils.SetLabel(_heroName, (data.display_name ?? data.hero_id ?? "UNKNOWN").ToUpper());
            CharSelectUIUtils.SetLabel(_heroTitle, data.title?.ToUpper() ?? "");
            CharSelectUIUtils.SetLabel(_heroQuote, !string.IsNullOrEmpty(data.quote) ? $"\"{data.quote}\"" : "");

            // Class info
            CharSelectUIUtils.SetLabel(_heroPath, data.GetPrimaryPath().ToString());
            CharSelectUIUtils.SetLabel(_heroRole, data.role?.ToUpper() ?? "");
            // Synergy: show primary brand + synergy explanation if available
            string synergy = data.GetPrimaryBrand().ToString().ToUpper();
            if (!string.IsNullOrEmpty(data.synergy_explanation))
            {
                synergy = data.synergy_explanation;
            }
            CharSelectUIUtils.SetLabel(_heroSynergy, synergy);

            // Starter stats
            CharSelectUIUtils.SetLabel(_statHp, data.base_hp.ToString());
            CharSelectUIUtils.SetLabel(_statAtk, data.base_attack.ToString());
            CharSelectUIUtils.SetLabel(_statDef, data.base_defense.ToString());
            CharSelectUIUtils.SetLabel(_statSpd, data.base_speed.ToString());

            // Champion monster
            PopulateChampion(data);

            // Backstory / Lore
            PopulateBackstory(data);

            // Panel slide-in animation
            CharSelectUIUtils.AnimatePanel(_panel);
        }

        private void PopulateBackstory(HeroData data)
        {
            bool hasBackstory = !string.IsNullOrEmpty(data.backstory);

            if (_heroLoreSection != null)
            {
                if (hasBackstory)
                    _heroLoreSection.RemoveFromClassList("hidden");
                else
                    _heroLoreSection.AddToClassList("hidden");
            }

            CharSelectUIUtils.SetLabel(_heroBackstory, data.backstory ?? string.Empty);
        }

        private void PopulateChampion(HeroData data)
        {
            if (string.IsNullOrEmpty(data.starter_monster_id))
            {
                _championSection?.AddToClassList("hidden");
                return;
            }

            _championSection?.RemoveFromClassList("hidden");

            if (!GameDatabase.HasInstance) return;
            var monster = GameDatabase.Instance.GetMonster(data.starter_monster_id);
            if (monster == null)
            {
                CharSelectUIUtils.SetLabel(_championName, data.starter_monster_id);
                CharSelectUIUtils.SetLabel(_championBrand, "");
                CharSelectUIUtils.SetLabel(_championRole, "");
                return;
            }

            CharSelectUIUtils.SetLabel(_championName, monster.display_name ?? data.starter_monster_id);
            CharSelectUIUtils.SetLabel(_championBrand, monster.GetPrimaryBrand().ToString());
            CharSelectUIUtils.SetLabel(_championRole, monster.GetAIPattern().ToString());
        }
    }
}
