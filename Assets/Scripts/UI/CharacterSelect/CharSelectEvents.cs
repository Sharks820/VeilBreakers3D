using System;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Shared event definitions for the character select screen.
    /// All events are raised by CharacterSelectManager.
    /// Controllers subscribe in OnEnable, unsubscribe in OnDisable.
    /// </summary>
    public static class CharSelectEvents
    {
        /// <summary>Hero index changed. Args: index, HeroData, HeroDisplayConfig</summary>
        public static event Action<int, HeroData, HeroDisplayConfig> OnHeroChanged;

        /// <summary>Hero JSON data finished loading for current hero.</summary>
        public static event Action<HeroData> OnHeroDataLoaded;

        /// <summary>A hero has been actively selected (embark button should start breathing).</summary>
        public static event Action OnHeroSelected;

        /// <summary>Embark button was clicked -- show confirmation popup.</summary>
        public static event Action OnEmbarkRequested;

        /// <summary>Player confirmed embark -- proceed to gameplay.</summary>
        public static event Action OnEmbarkConfirmed;

        /// <summary>Player cancelled embark -- dismiss popup, return to browsing.</summary>
        public static event Action OnEmbarkCancelled;

        /// <summary>Screen is fully initialized and ready for interaction.</summary>
        public static event Action OnScreenReady;

        /// <summary>Screen is about to exit (transition starting).</summary>
        public static event Action OnScreenExiting;

        // =========================================================================
        // INVOCATION HELPERS (null-safe)
        // =========================================================================

        public static void RaiseHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            OnHeroChanged?.Invoke(index, data, config);
        }

        public static void RaiseHeroDataLoaded(HeroData data)
        {
            OnHeroDataLoaded?.Invoke(data);
        }

        public static void RaiseHeroSelected() => OnHeroSelected?.Invoke();
        public static void RaiseEmbarkRequested() => OnEmbarkRequested?.Invoke();
        public static void RaiseEmbarkConfirmed() => OnEmbarkConfirmed?.Invoke();
        public static void RaiseEmbarkCancelled() => OnEmbarkCancelled?.Invoke();
        public static void RaiseScreenReady() => OnScreenReady?.Invoke();
        public static void RaiseScreenExiting() => OnScreenExiting?.Invoke();

        /// <summary>
        /// Clears ALL event subscribers. Call on scene unload to prevent leaks.
        /// </summary>
        public static void ClearAll()
        {
            OnHeroChanged = null;
            OnHeroDataLoaded = null;
            OnHeroSelected = null;
            OnEmbarkRequested = null;
            OnEmbarkConfirmed = null;
            OnEmbarkCancelled = null;
            OnScreenReady = null;
            OnScreenExiting = null;
        }
    }
}
