using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Populates the left info panel: name, title, quote, path/role/resource,
    /// starter stats grid, and champion monster info.
    /// </summary>
    public class HeroDataPanelController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        // Cached references
        private VisualElement _panel;
        private Label _heroName;
        private Label _heroTitle;
        private Label _heroQuote;
        private Label _heroPath;
        private Label _heroRole;
        private Label _heroResource;
        private Label _statHp;
        private Label _statAtk;
        private Label _statDef;
        private Label _statSpd;
        private Label _championName;
        private Label _championBrand;
        private Label _championRole;
        private VisualElement _championSection;

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
            _heroName = root.Q<Label>("hero-name");
            _heroTitle = root.Q<Label>("hero-title");
            _heroQuote = root.Q<Label>("hero-quote");
            _heroPath = root.Q<Label>("hero-path");
            _heroRole = root.Q<Label>("hero-role");
            _heroResource = root.Q<Label>("hero-resource");
            _statHp = root.Q<Label>("stat-hp");
            _statAtk = root.Q<Label>("stat-atk");
            _statDef = root.Q<Label>("stat-def");
            _statSpd = root.Q<Label>("stat-spd");
            _championName = root.Q<Label>("champion-name");
            _championBrand = root.Q<Label>("champion-brand");
            _championRole = root.Q<Label>("champion-role");
            _championSection = root.Q<VisualElement>("champion-section");
        }

        private void HandleHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            if (data == null) return;

            // Identity
            SetLabel(_heroName, (data.display_name ?? data.hero_id ?? "UNKNOWN").ToUpper());
            SetLabel(_heroTitle, data.title?.ToUpper() ?? "");
            SetLabel(_heroQuote, !string.IsNullOrEmpty(data.quote) ? $"\"{data.quote}\"" : "");

            // Class info
            SetLabel(_heroPath, data.GetPrimaryPath().ToString());
            SetLabel(_heroRole, data.role?.ToUpper() ?? "");
            SetLabel(_heroResource, data.resource_type?.ToUpper() ?? "");

            // Starter stats
            SetLabel(_statHp, data.base_hp.ToString());
            SetLabel(_statAtk, data.base_attack.ToString());
            SetLabel(_statDef, data.base_defense.ToString());
            SetLabel(_statSpd, data.base_speed.ToString());

            // Champion monster
            PopulateChampion(data);

            // Panel slide-in animation
            AnimatePanel();
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
                SetLabel(_championName, data.starter_monster_id);
                SetLabel(_championBrand, "");
                SetLabel(_championRole, "");
                return;
            }

            SetLabel(_championName, monster.display_name ?? data.starter_monster_id);
            SetLabel(_championBrand, monster.GetPrimaryBrand().ToString());
            SetLabel(_championRole, monster.role?.ToUpper() ?? "");
        }

        private void AnimatePanel()
        {
            if (_panel == null) return;

            // Trigger slide-in by toggling class
            _panel.AddToClassList("panel-hidden");
            _panel.schedule.Execute(() => _panel.RemoveFromClassList("panel-hidden")).ExecuteLater(50);
        }

        private static void SetLabel(Label label, string text)
        {
            if (label != null) label.text = text;
        }
    }
}
