using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Animates stat numbers (HP, ATK, DEF, STAMINA) with a counting/lerp effect
    /// when switching heroes. Each stat ticks independently.
    /// </summary>
    public static class StatNumberAnimator
    {
        public static Tween AnimateValue(Label label, int fromValue, int toValue, float duration = 0.4f)
        {
            if (label == null) return default;
            if (fromValue == toValue) { label.text = toValue.ToString(); return default; }

            return Tween.Custom(fromValue, toValue, duration,
                onValueChange: val => { if (label != null) label.text = Mathf.RoundToInt(val).ToString(); },
                ease: Ease.OutQuad);
        }
    }
}
