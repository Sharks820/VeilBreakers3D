using UnityEngine.UIElements;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Shared UI utilities for CharacterSelect panel controllers.
    /// Eliminates duplicate logic across HeroDataPanelController and HeroStatsPanelController.
    /// </summary>
    public static class CharSelectUIUtils
    {
        /// <summary>
        /// Triggers a slide-in animation by toggling the "panel-hidden" class.
        /// Adds the class immediately (making panel invisible), then schedules
        /// removal after a brief delay so the USS transition animates the entrance.
        /// </summary>
        public static void AnimatePanel(VisualElement panel)
        {
            if (panel == null) return;
            panel.AddToClassList("panel-hidden");
            panel.schedule.Execute(() => panel.RemoveFromClassList("panel-hidden")).ExecuteLater(50);
        }

        /// <summary>
        /// Sets label text safely (null-checks the label).
        /// </summary>
        public static void SetLabel(Label label, string text)
        {
            if (label != null) label.text = text;
        }
    }
}
