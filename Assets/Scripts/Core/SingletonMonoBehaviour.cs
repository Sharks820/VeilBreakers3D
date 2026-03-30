using System;
using System.Collections.Generic;
using UnityEngine;

namespace VeilBreakers.Core
{
    /// <summary>
    /// Non-generic helper that resets all SingletonMonoBehaviour statics on domain reload.
    /// [RuntimeInitializeOnLoadMethod] does not work on open generic classes,
    /// so this concrete class handles the reset for all closed generic singletons.
    /// </summary>
    internal static class SingletonResetHelper
    {
        private static readonly List<Action> _resetActions = new List<Action>(); // VB-IGNORE BUG-65 -- intentionally mutable; Register() adds reset callbacks at static ctor time

        internal static void Register(Action resetAction)
        {
            _resetActions.Add(resetAction);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAllSingletons()
        {
            foreach (var action in _resetActions)
                action();
        }
    }

    /// <summary>
    /// Base class for singleton MonoBehaviours that persist across scenes.
    /// Handles instance management and DontDestroyOnLoad automatically.
    /// </summary>
    /// <typeparam name="T">The concrete singleton type</typeparam>
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : SingletonMonoBehaviour<T>
    {
        private static T _instance;
        private static bool _isQuitting;

        /// <summary>
        /// Static constructor registers this closed generic type for domain-reload reset.
        /// </summary>
        static SingletonMonoBehaviour()
        {
            SingletonResetHelper.Register(() =>
            {
                _instance = default;
                _isQuitting = false;
            });
        }

        public static T Instance
        {
            get
            {
                // Don't return or create instances during application quit
                if (_isQuitting)
                    return null;
                return _instance;
            }
        }

        /// <summary>
        /// Returns true if a valid instance exists (and app is not quitting).
        /// </summary>
        public static bool HasInstance => _instance != null && !_isQuitting;

        /// <summary>
        /// Returns true if the application is quitting.
        /// </summary>
        protected static bool IsQuitting => _isQuitting;

        /// <summary>
        /// Override to false if this singleton should not persist across scenes.
        /// </summary>
        protected virtual bool IsPersistent => true;

        protected virtual void Awake()
        {
            // Reset quitting flag for Editor play mode support
            // In builds, this is harmless since Awake only runs once
            _isQuitting = false;

            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[{typeof(T).Name}] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }

            _instance = (T)this;

            if (IsPersistent)
            {
                DontDestroyOnLoad(gameObject);
            }

            OnSingletonAwake();
        }

        /// <summary>
        /// Called after singleton is established. Override for initialization.
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
        }
    }
}
