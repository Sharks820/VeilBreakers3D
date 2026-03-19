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

        /// <summary>
        /// Triggers exit-then-enter choreography for tab content switching.
        /// Removes tab-active from outgoing section, adds it to incoming section
        /// with an entrance animation via AnimatePanel.
        /// </summary>
        public static void SwitchTabContent(VisualElement outgoing, VisualElement incoming)
        {
            if (outgoing != null)
            {
                outgoing.RemoveFromClassList("tab-active");
            }
            if (incoming != null)
            {
                incoming.AddToClassList("tab-active");
                AnimatePanel(incoming);
            }
        }
    }
}
