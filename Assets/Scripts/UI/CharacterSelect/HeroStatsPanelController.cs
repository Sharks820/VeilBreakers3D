using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Populates the abilities tab content: D&D attribute bars (STR/DEX/CON/INT/WIS/CHA)
    /// with width-based fills, and the abilities list with skill names from GameDatabase.
    /// All Q() calls are confined to CacheReferences() at init time.
    /// </summary>
    public class HeroStatsPanelController : MonoBehaviour
    {
        private const float kMaxStatValue = 20f;

        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _panel;
        private readonly VisualElement[] _barFills = new VisualElement[6];
        private readonly Label[] _barValues = new Label[6];
        private readonly Label[] _abilitySlots = new Label[5];

        private static readonly string[] kStatNames = { "str", "dex", "con", "int", "wis", "cha" };
        private static readonly Color[] kStatColors =
        {
            new Color(0.85f, 0.25f, 0.20f),  // STR - Crimson red
            new Color(0.30f, 0.80f, 0.35f),  // DEX - Emerald green
            new Color(0.90f, 0.55f, 0.15f),  // CON - Burnt orange
            new Color(0.55f, 0.30f, 0.85f),  // INT - Royal purple
            new Color(0.25f, 0.55f, 0.90f),  // WIS - Deep blue
            new Color(0.90f, 0.75f, 0.20f),  // CHA - Gold
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
        // ABILITIES
        // =============================================================================

        /// <summary>
        /// Updates ability slot labels with skill display names from GameDatabase.
        /// </summary>
        private void UpdateAbilities(HeroData data)
        {
            string[] skills = data.innate_skills;

            for (int i = 0; i < _abilitySlots.Length; i++)
            {
                if (_abilitySlots[i] == null) continue;

                if (skills != null && i < skills.Length)
                {
                    string skillId = skills[i];
                    var skillData = GameDatabase.HasInstance ? GameDatabase.Instance.GetSkill(skillId) : null;
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
    }
}
