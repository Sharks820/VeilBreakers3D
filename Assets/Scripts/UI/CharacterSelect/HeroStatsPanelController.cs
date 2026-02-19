using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Populates the right panel: D&D attribute bars (STR/DEX/CON/INT/WIS/CHA)
    /// with animated fills, and the abilities list.
    /// </summary>
    public class HeroStatsPanelController : MonoBehaviour
    {
        private const float kMaxStatValue = 20f; // D&D max for percentage calc

        [SerializeField] private UIDocument _uiDocument;

        // Cached stat bar references
        private VisualElement _panel;
        private readonly VisualElement[] _barFills = new VisualElement[6];
        private readonly Label[] _barValues = new Label[6];
        private readonly Label[] _abilitySlots = new Label[5];

        private static readonly string[] kStatNames = { "str", "dex", "con", "int", "wis", "cha" };

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
            var root = _uiDocument.rootVisualElement;
            _panel = root.Q<VisualElement>("stats-panel");

            for (int i = 0; i < kStatNames.Length; i++)
            {
                _barFills[i] = root.Q<VisualElement>($"bar-{kStatNames[i]}-fill");
                _barValues[i] = root.Q<Label>($"bar-{kStatNames[i]}-value");
            }

            for (int i = 0; i < _abilitySlots.Length; i++)
            {
                _abilitySlots[i] = root.Q<Label>($"ability-{i}");
            }
        }

        private void HandleHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            if (data == null) return;

            UpdateStatBars(data);
            UpdateAbilities(data);
            AnimatePanel();
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
                }

                if (_barValues[i] != null)
                {
                    _barValues[i].text = values[i].ToString();
                }
            }
        }

        private void UpdateAbilities(HeroData data)
        {
            string[] skills = data.innate_skills;

            for (int i = 0; i < _abilitySlots.Length; i++)
            {
                if (_abilitySlots[i] == null) continue;

                if (skills != null && i < skills.Length)
                {
                    string skillId = skills[i];

                    // Try to get skill display name from database
                    var skillData = GameDatabase.Instance.GetSkill(skillId);
                    string displayName = skillData?.display_name ?? FormatSkillId(skillId);

                    _abilitySlots[i].text = displayName;
                    _abilitySlots[i].style.display = DisplayStyle.Flex;
                }
                else
                {
                    _abilitySlots[i].style.display = DisplayStyle.None;
                }
            }
        }

        private static string FormatSkillId(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return "";

            // Convert snake_case to Title Case: "shackle_strike" -> "Shackle Strike"
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

        private void AnimatePanel()
        {
            if (_panel == null) return;
            _panel.AddToClassList("panel-hidden");
            _panel.schedule.Execute(() => _panel.RemoveFromClassList("panel-hidden")).ExecuteLater(50);
        }
    }
}
