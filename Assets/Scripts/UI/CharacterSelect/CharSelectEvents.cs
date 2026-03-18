using System;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Shared event definitions for the character select screen.
    /// All events are raised by CharacterSelectManager and sub-controllers.
    /// Controllers subscribe in OnEnable, unsubscribe in OnDisable.
    /// </summary>
    public static class CharSelectEvents
    {
        /// <summary>Hero index changed. Args: index, HeroData, HeroDisplayConfig.</summary>
        public static event Action<int, HeroData, HeroDisplayConfig> OnHeroChanged;

        /// <summary>Screen is fully initialized and ready for interaction.</summary>
        public static event Action OnScreenReady;

        /// <summary>Screen is about to exit (transition starting).</summary>
        public static event Action OnScreenExiting;

        /// <summary>A sub-controller requests navigation to a specific hero index.</summary>
        public static event Action<int> OnNavigationRequested;

        /// <summary>Info tab changed. Arg: tab index (0=Overview, 1=Abilities, 2=Lore).</summary>
        public static event Action<int> OnTabChanged;

        /// <summary>Embark hold progress updated each frame during hold. Arg: 0..1 progress.</summary>
        public static event Action<float> OnEmbarkHoldProgress;

        /// <summary>Embark hold completed -- hold-to-confirm succeeded, begin embark sequence.</summary>
        public static event Action OnEmbarkTriggered;

        /// <summary>Error occurred that should display a toast notification.</summary>
        public static event Action<string> OnErrorOccurred;

        /// <summary>Async loading started -- show skeleton shimmer.</summary>
        public static event Action OnLoadingStarted;

        /// <summary>Async loading complete -- hide skeleton shimmer.</summary>
        public static event Action OnLoadingComplete;

        /// <summary>Focus zone changed via gamepad navigation. Arg: zone index.</summary>
        public static event Action<int> OnFocusZoneChanged;

        // =========================================================================
        // INVOCATION HELPERS (null-safe)
        // =========================================================================

        public static void RaiseHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            OnHeroChanged?.Invoke(index, data, config);
        }

        public static void RaiseScreenReady() => OnScreenReady?.Invoke();
        public static void RaiseScreenExiting() => OnScreenExiting?.Invoke();
        public static void RaiseNavigationRequested(int index) => OnNavigationRequested?.Invoke(index);
        public static void RaiseTabChanged(int tabIndex) => OnTabChanged?.Invoke(tabIndex);
        public static void RaiseEmbarkHoldProgress(float progress) => OnEmbarkHoldProgress?.Invoke(progress);
        public static void RaiseEmbarkTriggered() => OnEmbarkTriggered?.Invoke();
        public static void RaiseErrorOccurred(string message) => OnErrorOccurred?.Invoke(message);
        public static void RaiseLoadingStarted() => OnLoadingStarted?.Invoke();
        public static void RaiseLoadingComplete() => OnLoadingComplete?.Invoke();
        public static void RaiseFocusZoneChanged(int zoneIndex) => OnFocusZoneChanged?.Invoke(zoneIndex);

        /// <summary>
        /// Clears ALL event subscribers. Call on scene unload to prevent leaks.
        /// </summary>
        public static void ClearAll()
        {
            OnHeroChanged = null;
            OnScreenReady = null;
            OnScreenExiting = null;
            OnNavigationRequested = null;
            OnTabChanged = null;
            OnEmbarkHoldProgress = null;
            OnEmbarkTriggered = null;
            OnErrorOccurred = null;
            OnLoadingStarted = null;
            OnLoadingComplete = null;
            OnFocusZoneChanged = null;
        }
    }
}
