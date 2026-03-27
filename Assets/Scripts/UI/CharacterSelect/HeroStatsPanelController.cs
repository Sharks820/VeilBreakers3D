using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Populates the abilities tab content: D&D attribute bars (STR/DEX/CON/INT/WIS/CHA)
    /// with width-based fills, and dynamically-built AAA ability cards from GameDatabase.
    /// Each ability card shows brand dot, skill name, damage/cost tags, and description.
    /// All Q() calls are confined to CacheReferences() at init time.
    /// </summary>
    public class HeroStatsPanelController : MonoBehaviour
    {
        private const float kMaxStatValue = 20f;

        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _panel;
        private VisualElement _abilitiesList;
        private readonly VisualElement[] _barFills = new VisualElement[6];
        private readonly Label[] _barValues = new Label[6];

        // D&D standard 6 attributes: STR/DEX/CON/INT/WIS/CHA (V5 spec)
        private static readonly string[] kStatNames = { "str", "dex", "con", "int", "wis", "cha" };
        private static readonly Color[] kStatColors =
        {
            new Color(0.85f, 0.25f, 0.20f),  // STR - Crimson red
            new Color(0.30f, 0.80f, 0.35f),  // DEX - Emerald green
            new Color(0.90f, 0.55f, 0.15f),  // CON - Burnt orange
            new Color(0.55f, 0.30f, 0.85f),  // INT - Arcane purple
            new Color(0.25f, 0.55f, 0.90f),  // WIS - Deep blue
            new Color(0.90f, 0.75f, 0.20f),  // CHA - Gold
        };

        // Brand colors for ability dot indicators (matches 10-brand system)
        private static readonly Dictionary<Brand, Color> kBrandColors = new Dictionary<Brand, Color>
        {
            { Brand.NONE,    new Color(0.5f, 0.5f, 0.5f, 0.6f) },
            { Brand.IRON,    new Color(0.65f, 0.65f, 0.70f, 0.9f) },   // Steel gray
            { Brand.SAVAGE,  new Color(0.85f, 0.25f, 0.15f, 0.9f) },   // Blood red
            { Brand.SURGE,   new Color(0.90f, 0.80f, 0.20f, 0.9f) },   // Electric gold
            { Brand.VENOM,   new Color(0.30f, 0.80f, 0.25f, 0.9f) },   // Toxic green
            { Brand.DREAD,   new Color(0.55f, 0.20f, 0.70f, 0.9f) },   // Dark purple
            { Brand.LEECH,   new Color(0.70f, 0.15f, 0.35f, 0.9f) },   // Crimson
            { Brand.GRACE,   new Color(1.00f, 0.90f, 0.55f, 0.9f) },   // Holy gold
            { Brand.MEND,    new Color(0.40f, 0.85f, 0.75f, 0.9f) },   // Healing teal
            { Brand.RUIN,    new Color(0.90f, 0.40f, 0.10f, 0.9f) },   // Flame orange
            { Brand.VOID,    new Color(0.35f, 0.75f, 0.90f, 0.9f) },   // Void cyan
        };

        private Sequence _statCascadeSequence;

        private void OnEnable()
        {
            CacheReferences();
            CharSelectEvents.OnHeroChanged += HandleHeroChanged;
        }

        private void OnDisable()
        {
            if (_statCascadeSequence.isAlive) _statCascadeSequence.Stop();
            CharSelectEvents.OnHeroChanged -= HandleHeroChanged;
        }

        /// <summary>
        /// Caches all VisualElement references at init time. Zero Q() calls outside this method.
        /// </summary>
        private void CacheReferences()
        {
            if (_uiDocument == null) { Debug.LogError("[HeroStatsPanelController] UIDocument not assigned!"); return; }
            var root = _uiDocument.rootVisualElement;
            _panel = root.Q<VisualElement>("tab-abilities-content");
            _abilitiesList = root.Q<VisualElement>("abilities-list");

            for (int i = 0; i < kStatNames.Length; i++)
            {
                _barFills[i] = root.Q<VisualElement>($"bar-{kStatNames[i]}-fill");
                _barValues[i] = root.Q<Label>($"bar-{kStatNames[i]}-value");
            }
        }

        /// <summary>
        /// Handles hero change events by updating stat bars and abilities.
        /// Panel animation is handled by HeroDataPanelController on the parent info-panel-container.
        /// </summary>
        private void HandleHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            if (data == null) return;

            UpdateStatBars(data);
            UpdateAbilities(data);
        }

        private void UpdateStatBars(HeroData data)
        {
            var stats = data.base_stats;
            if (stats == null) return;

            int[] values = {
                stats.strength, stats.dexterity, stats.constitution,
                stats.intelligence, stats.wisdom, stats.charisma
            };

            for (int i = 0; i < values.Length && i < _barFills.Length; i++)
            {
                float pct = Mathf.Clamp01(values[i] / kMaxStatValue) * 100f;

                if (_barFills[i] != null)
                {
                    _barFills[i].style.width = new StyleLength(new Length(pct, LengthUnit.Percent));
                    _barFills[i].style.backgroundColor = kStatColors[i];
                }

                if (_barValues[i] != null)
                {
                    _barValues[i].text = values[i].ToString();
                }
            }
        }

        // =============================================================================
        // STAT BAR CASCADE ANIMATION
        // =============================================================================

        /// <summary>
        /// Animates stat bars with staggered cascade fill.
        /// Each bar starts 100ms after the previous (STR -> DEX -> CON -> INT -> WIS -> CHA).
        /// Uses PrimeTween scaleX on the fill element (per Phase 2 decision: no width transitions).
        /// </summary>
        public Sequence BuildStatBarCascade(HeroData heroData)
        {
            _statCascadeSequence.Stop();

            var stats = heroData?.base_stats;
            if (stats == null) return Sequence.Create();

            int[] values = {
                stats.strength, stats.dexterity, stats.constitution,
                stats.intelligence, stats.wisdom, stats.charisma
            };

            var seq = Sequence.Create();

            for (int i = 0; i < values.Length && i < _barFills.Length; i++)
            {
                if (_barFills[i] == null) continue;

                float pct = Mathf.Clamp01(values[i] / kMaxStatValue);
                float stagger = i * 0.1f; // 100ms stagger per bar

                // Set initial state: scaleX 0 with left-anchored origin
                _barFills[i].style.transformOrigin = new TransformOrigin(0, Length.Percent(50));
                _barFills[i].style.scale = new Scale(new Vector2(0f, 1f));

                // Set width to target (scaleX will reveal it)
                _barFills[i].style.width = new StyleLength(new Length(pct * 100f, LengthUnit.Percent));

                // Animate scaleX from 0 to 1 over 300ms with OutCubic
                int capturedIndex = i;
                seq.Insert(stagger, Tween.Custom(_barFills[capturedIndex], 0f, 1f, 0.3f,
                    onValueChange: (fill, t) =>
                    {
                        fill.style.scale = new Scale(new Vector2(t, 1f));
                    }, ease: Ease.OutCubic));
            }

            _statCascadeSequence = seq;
            return seq;
        }

        // =============================================================================
        // ABILITIES — AAA dynamic card builder
        // =============================================================================

        /// <summary>
        /// Rebuilds ability cards from scratch using GameDatabase skill data.
        /// Each card shows: brand color dot | skill name + tags | description.
        /// </summary>
        private void UpdateAbilities(HeroData data)
        {
            if (_abilitiesList == null) return;

            // Clear existing ability cards
            _abilitiesList.Clear();

            string[] skills = data.innate_skills;
            if (skills == null || skills.Length == 0)
            {
                var emptyLabel = new Label("No abilities learned yet.");
                emptyLabel.style.color = new Color(0.6f, 0.55f, 0.7f, 0.4f);
                emptyLabel.style.fontSize = 11;
                emptyLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                emptyLabel.style.paddingTop = 10;
                emptyLabel.style.paddingLeft = 8;
                _abilitiesList.Add(emptyLabel);
                return;
            }

            for (int i = 0; i < skills.Length; i++)
            {
                string skillId = skills[i];
                var skillData = GameDatabase.HasInstance ? GameDatabase.Instance.GetSkill(skillId) : null;

                var card = BuildAbilityCard(skillId, skillData);
                _abilitiesList.Add(card);
            }
        }

        /// <summary>
        /// Builds a single AAA ability card VisualElement.
        /// Layout: [brand-dot] [info-column: [header-row: name + tags] [description]]
        /// </summary>
        private VisualElement BuildAbilityCard(string skillId, SkillData skillData)
        {
            var card = new VisualElement();
            card.AddToClassList("ability-card");

            // Brand color dot
            var dot = new VisualElement();
            dot.AddToClassList("ability-brand-dot");
            Brand brand = skillData != null ? skillData.GetBrandRequirement() : Brand.NONE;
            Color dotColor;
            if (!kBrandColors.TryGetValue(brand, out dotColor))
                dotColor = kBrandColors[Brand.NONE];
            dot.style.backgroundColor = dotColor;
            card.Add(dot);

            // Info column
            var info = new VisualElement();
            info.AddToClassList("ability-info");

            // Header row: name + tags
            var headerRow = new VisualElement();
            headerRow.AddToClassList("ability-header-row");

            string displayName = skillData?.display_name ?? FormatSkillId(skillId);
            var nameLabel = new Label(displayName);
            nameLabel.AddToClassList("ability-name");
            headerRow.Add(nameLabel);

            // Tags container
            var tags = new VisualElement();
            tags.AddToClassList("ability-tags");

            if (skillData != null)
            {
                // Power tag
                if (skillData.base_power > 0)
                {
                    var powerTag = new Label($"PWR {skillData.base_power}");
                    powerTag.AddToClassList("ability-power-tag");
                    tags.Add(powerTag);
                }

                // Cost tag
                if (skillData.mp_cost > 0)
                {
                    var costTag = new Label($"MP {skillData.mp_cost}");
                    costTag.AddToClassList("ability-tag");
                    tags.Add(costTag);
                }
                else if (skillData.hp_cost > 0)
                {
                    var costTag = new Label($"HP {skillData.hp_cost}");
                    costTag.AddToClassList("ability-tag");
                    tags.Add(costTag);
                }

                // Cooldown tag
                if (skillData.cooldown_turns > 0)
                {
                    var cdTag = new Label($"CD {skillData.cooldown_turns}");
                    cdTag.AddToClassList("ability-tag");
                    tags.Add(cdTag);
                }

                // Brand tag
                if (brand != Brand.NONE)
                {
                    var brandTag = new Label(brand.ToString());
                    brandTag.AddToClassList("ability-tag");
                    brandTag.style.color = new StyleColor(dotColor);
                    tags.Add(brandTag);
                }
            }

            headerRow.Add(tags);
            info.Add(headerRow);

            // Description (with fallback for missing descriptions)
            if (skillData != null)
            {
                string descriptionText = skillData.description;

                // If description is empty, generate a brief fallback based on skill type/data
                if (string.IsNullOrEmpty(descriptionText))
                {
                    descriptionText = GenerateFallbackDescription(skillData);
                }

                if (!string.IsNullOrEmpty(descriptionText))
                {
                    var desc = new Label(descriptionText);
                    desc.AddToClassList("ability-desc");
                    info.Add(desc);
                }
            }

            card.Add(info);
            return card;
        }

        private static string FormatSkillId(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return "";
            var parts = skillId.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
                }
            }
            return string.Join(" ", parts);
        }

        /// <summary>
        /// Generate a brief fallback description for skills with missing descriptions.
        /// Based on skill properties: power, cost, cooldown, target type.
        /// </summary>
        private static string GenerateFallbackDescription(SkillData skill)
        {
            if (skill == null) return "";

            var targetType = skill.GetTargetType();
            var skillType = skill.GetSkillType();

            // Determine skill category
            string category = "";
            if (skillType == SkillType.ATTACK) category = "Attack";
            else if (skillType == SkillType.DEFENSE) category = "Defense";
            else if (skillType == SkillType.HEAL) category = "Heal";
            else if (skillType == SkillType.ULTIMATE) category = "Ultimate";
            else category = "Ability";

            // Determine target description
            string target = "";
            if (targetType == TargetType.SINGLE_ENEMY) target = "single enemy";
            else if (targetType == TargetType.ALL_ENEMIES) target = "all enemies";
            else if (targetType == TargetType.SINGLE_ALLY) target = "single ally";
            else if (targetType == TargetType.ALL_ALLIES) target = "all allies";
            else if (targetType == TargetType.SELF) target = "yourself";

            // Build description
            string desc = $"{category} ability";
            if (!string.IsNullOrEmpty(target)) desc += $" targeting {target}";

            if (skill.base_power > 0)
                desc += $". Power: {skill.base_power}";

            if (skill.mp_cost > 0)
                desc += $", MP Cost: {skill.mp_cost}";
            else if (skill.hp_cost > 0)
                desc += $", HP Cost: {skill.hp_cost}";

            if (skill.cooldown_turns > 0)
                desc += $", Cooldown: {skill.cooldown_turns} turns";

            return desc + ".";
        }
    }
}
