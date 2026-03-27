using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Populates the tabbed info panel: overview tab (name, title, quote, path/role/synergy,
    /// starter stats grid, champion monster info) and lore tab (backstory, synergy detail, brands).
    /// All Q() calls are confined to CacheReferences() at init time.
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
        private VisualElement _infoPanelContainer;
        private Label _heroName;
        private Label _heroTitle;
        private Label _heroQuote;
        private Label _heroPath;
        private Label _heroRole;
        private Label _heroSynergy;
        private Label _statHp;
        private Label _statAtk;
        private Label _statDef;
        private Label _statStamina;
        private VisualElement _statHpFill;
        private VisualElement _statAtkFill;
        private VisualElement _statDefFill;
        private VisualElement _statStaminaFill;
        private Label _championName;
        private Label _championBrandLabel;
        private Label _championBrand;
        private Label _championRole;
        private Label _championDesc;
        private VisualElement _championSection;
        private VisualElement _championSkillsContainer;
        private VisualElement _heroLoreSection;
        private Label _heroBackstory;
        private Label _heroSynergyDetail;
        private Label _heroBrandsDetail;

        // Stage name plate (floating text on hero stage)
        private Label _stageHeroName;
        private Label _stageHeroTitle;

        // Previous stat values for animated lerp transitions
        private int _prevHp;
        private int _prevAtk;
        private int _prevDef;
        private int _prevStamina;

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

            _panel = root.Q<VisualElement>("tab-overview-content");
            _infoPanelContainer = root.Q<VisualElement>("info-panel-container");
            if (_infoPanelContainer != null) _infoPanelContainer.usageHints |= UsageHints.DynamicTransform;

            _heroName = root.Q<Label>("hero-name");
            _heroTitle = root.Q<Label>("hero-title");
            _heroQuote = root.Q<Label>("hero-quote");
            _heroPath = root.Q<Label>("hero-path");
            _heroRole = root.Q<Label>("hero-role");
            _heroSynergy = root.Q<Label>("hero-synergy");
            _statHp = root.Q<Label>("stat-hp");
            _statAtk = root.Q<Label>("stat-atk");
            _statDef = root.Q<Label>("stat-def");
            _statStamina = root.Q<Label>("stat-stamina");
            _statHpFill = root.Q<VisualElement>("stat-hp-fill");
            _statAtkFill = root.Q<VisualElement>("stat-atk-fill");
            _statDefFill = root.Q<VisualElement>("stat-def-fill");
            _statStaminaFill = root.Q<VisualElement>("stat-stamina-fill");
            _championName = root.Q<Label>("champion-name");
            _championBrandLabel = root.Q<Label>("champion-brand-label");
            _championBrand = root.Q<Label>("champion-brand");
            _championRole = root.Q<Label>("champion-role");
            _championDesc = root.Q<Label>("champion-desc");
            _championSection = root.Q<VisualElement>("champion-section");
            _championSkillsContainer = root.Q<VisualElement>("champion-skills-container");
            _heroLoreSection = root.Q<VisualElement>(kHeroLoreSection);
            _heroBackstory = root.Q<Label>(kHeroBackstory);
            _heroSynergyDetail = root.Q<Label>("hero-synergy-detail");
            _heroBrandsDetail = root.Q<Label>("hero-brands-detail");

            // Stage name plate (optional - only present in v3.0+ UXML)
            _stageHeroName = root.Q<Label>("stage-hero-name");
            _stageHeroTitle = root.Q<Label>("stage-hero-title");

            Debug.Assert(_heroBackstory != null, $"[HeroDataPanel] Missing element: {kHeroBackstory}");
            Debug.Assert(_heroLoreSection != null, $"[HeroDataPanel] Missing element: {kHeroLoreSection}");
            Debug.Assert(_heroSynergyDetail != null, "[HeroDataPanel] Missing element: hero-synergy-detail");
            Debug.Assert(_heroBrandsDetail != null, "[HeroDataPanel] Missing element: hero-brands-detail");
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
            // Show short brand name (e.g. "IRON / DREAD") — full explanation goes in Lore tab
            string synergy = data.GetPrimaryBrand().ToString().ToUpper();
            CharSelectUIUtils.SetLabel(_heroSynergy, synergy);

            // Starter stats — animated counting effect on hero switch
            StatNumberAnimator.AnimateValue(_statHp, _prevHp, data.base_hp);
            StatNumberAnimator.AnimateValue(_statAtk, _prevAtk, data.base_attack);
            StatNumberAnimator.AnimateValue(_statDef, _prevDef, data.base_defense);
            StatNumberAnimator.AnimateValue(_statStamina, _prevStamina, data.base_speed);

            // Cache for next switch
            _prevHp = data.base_hp;
            _prevAtk = data.base_attack;
            _prevDef = data.base_defense;
            _prevStamina = data.base_speed;

            // Bar fill widths (percentage of max expected value)
            const float kMaxHp = 100f;
            const float kMaxStat = 40f;
            SetBarFillWidth(_statHpFill, data.base_hp / kMaxHp);
            SetBarFillWidth(_statAtkFill, data.base_attack / kMaxStat);
            SetBarFillWidth(_statDefFill, data.base_defense / kMaxStat);
            SetBarFillWidth(_statStaminaFill, data.base_speed / kMaxStat);

            // Dynamic resource label: MANA for caster/hybrid paths, STAMINA for physical paths
            UpdateResourceStatLabel(data);

            // Champion monster
            PopulateChampion(data);

            // Backstory / Lore
            PopulateBackstory(data);

            // Lore tab -- synergy detail and brands
            PopulateLoreDetails(data);

            // Stage name plate (hero name watermark on 3D stage)
            string displayName = (data.display_name ?? data.hero_id ?? "UNKNOWN").ToUpper();
            CharSelectUIUtils.SetLabel(_stageHeroName, displayName);
            CharSelectUIUtils.SetLabel(_stageHeroTitle, data.title?.ToUpper() ?? "");

            // Panel slide-in animation on the info-panel-container
            CharSelectUIUtils.AnimatePanel(_infoPanelContainer);
        }

        private static void SetBarFillWidth(VisualElement fill, float normalizedValue)
        {
            if (fill == null) return;
            float pct = Mathf.Clamp01(normalizedValue) * 100f;
            fill.style.width = new Length(pct, LengthUnit.Percent);
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

        private void PopulateLoreDetails(HeroData data)
        {
            string synDetail = !string.IsNullOrEmpty(data.synergy_explanation)
                ? data.synergy_explanation
                : data.GetPrimaryBrand().ToString();
            CharSelectUIUtils.SetLabel(_heroSynergyDetail, synDetail);

            string brandsText = data.GetPrimaryBrand().ToString();
            CharSelectUIUtils.SetLabel(_heroBrandsDetail, brandsText);
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
                // Fallback: show monster ID with generic labels
                CharSelectUIUtils.SetLabel(_championName, data.starter_monster_id.ToUpper());
                CharSelectUIUtils.SetLabel(_championBrandLabel, "\u25C6 UNKNOWN BRAND");
                CharSelectUIUtils.SetLabel(_championBrand, "?");
                CharSelectUIUtils.SetLabel(_championRole, "?");
                CharSelectUIUtils.SetLabel(_championDesc, "A creature yet to be discovered.");
                return;
            }

            string brandName = monster.GetPrimaryBrand().ToString().ToUpper();
            string roleName = monster.GetAIPattern().ToString().ToUpper();

            CharSelectUIUtils.SetLabel(_championName, (monster.display_name ?? data.starter_monster_id).ToUpper());
            CharSelectUIUtils.SetLabel(_championBrandLabel, $"\u25C6 {brandName} BRAND");
            CharSelectUIUtils.SetLabel(_championBrand, brandName);
            CharSelectUIUtils.SetLabel(_championRole, roleName);
            CharSelectUIUtils.SetLabel(_championDesc, $"Your starting {brandName.ToLower()} companion. A {roleName.ToLower()} fighter bound to your path.");

            // Populate starter monster skills
            PopulateChampionSkills(monster);
        }

        /// <summary>
        /// Builds the starter monster skill list from innate_skills and learnable_skills data.
        /// Shows skill name, brand dot, and power in the champion info panel.
        /// </summary>
        private void PopulateChampionSkills(MonsterData monster)
        {
            if (_championSkillsContainer == null) return;
            _championSkillsContainer.Clear();

            // Header
            var header = new Label("STARTER SKILLS");
            header.AddToClassList("champion-skills-header");
            _championSkillsContainer.Add(header);

            // Collect innate skills
            string[] skills = monster.innate_skills;
            if (skills == null || skills.Length == 0)
            {
                var empty = new Label("No known skills");
                empty.style.fontSize = 9;
                empty.style.color = new Color(0.5f, 0.47f, 0.6f, 0.4f);
                empty.style.unityFontStyleAndWeight = FontStyle.Italic;
                _championSkillsContainer.Add(empty);
                return;
            }

            // Brand color for dots
            Brand monsterBrand = monster.GetPrimaryBrand();
            Color brandColor = GetBrandDotColor(monsterBrand);

            for (int i = 0; i < skills.Length && i < 4; i++) // Show max 4 skills to keep compact
            {
                string skillId = skills[i];
                var skillData = GameDatabase.HasInstance ? GameDatabase.Instance.GetSkill(skillId) : null;

                var row = new VisualElement();
                row.AddToClassList("champion-skill-row");

                // Brand dot
                var dot = new VisualElement();
                dot.AddToClassList("champion-skill-dot");
                dot.style.backgroundColor = brandColor;
                row.Add(dot);

                // Skill name
                string displayName = skillData?.display_name ?? FormatSkillId(skillId);
                var nameLabel = new Label(displayName);
                nameLabel.AddToClassList("champion-skill-name");
                row.Add(nameLabel);

                // Power (if applicable)
                if (skillData != null && skillData.base_power > 0)
                {
                    var powerLabel = new Label($"PWR {skillData.base_power}");
                    powerLabel.AddToClassList("champion-skill-power");
                    row.Add(powerLabel);
                }

                _championSkillsContainer.Add(row);
            }
        }

        private static Color GetBrandDotColor(Brand brand)
        {
            switch (brand)
            {
                case Brand.IRON:    return new Color(0.65f, 0.65f, 0.70f, 0.9f);
                case Brand.SAVAGE:  return new Color(0.85f, 0.25f, 0.15f, 0.9f);
                case Brand.SURGE:   return new Color(0.90f, 0.80f, 0.20f, 0.9f);
                case Brand.VENOM:   return new Color(0.30f, 0.80f, 0.25f, 0.9f);
                case Brand.DREAD:   return new Color(0.55f, 0.20f, 0.70f, 0.9f);
                case Brand.LEECH:   return new Color(0.70f, 0.15f, 0.35f, 0.9f);
                case Brand.GRACE:   return new Color(1.00f, 0.90f, 0.55f, 0.9f);
                case Brand.MEND:    return new Color(0.40f, 0.85f, 0.75f, 0.9f);
                case Brand.RUIN:    return new Color(0.90f, 0.40f, 0.10f, 0.9f);
                case Brand.VOID:    return new Color(0.35f, 0.75f, 0.90f, 0.9f);
                default:            return new Color(0.5f, 0.5f, 0.5f, 0.6f);
            }
        }

        private static string FormatSkillId(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return "";
            var parts = skillId.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
            }
            return string.Join(" ", parts);
        }

        /// <summary>
        /// Updates the resource stat chip label and CSS class based on the hero's path.
        /// VOIDTOUCHED and UNCHAINED paths use MANA; all others use STAMINA.
        /// </summary>
        private void UpdateResourceStatLabel(HeroData data)
        {
            bool isManaUser = data.GetPrimaryPath() == Path.VOIDTOUCHED
                           || data.GetPrimaryPath() == Path.UNCHAINED;

            // The stat chip container is the GRANDPARENT of the stat value label
            // (label → stat-chip-header → stat-chip)
            var resourceChip = _statStamina?.parent?.parent;
            if (resourceChip == null) return;

            var resourceLabel = resourceChip.Q<Label>(className: "stat-chip-label");
            if (resourceLabel != null)
                resourceLabel.text = isManaUser ? "MANA" : "STAMINA";

            resourceChip.RemoveFromClassList("stat-chip-stamina");
            resourceChip.RemoveFromClassList("stat-chip-mana");
            resourceChip.AddToClassList(isManaUser ? "stat-chip-mana" : "stat-chip-stamina");
        }
    }
}
