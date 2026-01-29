using UnityEngine;

namespace VeilBreakers.Core
{
    /// <summary>
    /// Base class for singleton MonoBehaviours that persist across scenes.
    /// Handles instance management and DontDestroyOnLoad automatically.
    /// </summary>
    /// <typeparam name="T">The concrete singleton type</typeparam>
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : SingletonMonoBehaviour<T>
    {
        private static T _instance;
        private static bool _isQuitting;

        public static T Instance
        {
            get
            {
                // Note: Instance being null before bootstrap creates it is expected
                // Only log if we're past initialization and instance is still null
                return _instance;
            }
        }

        /// <summary>
        /// Returns true if a valid instance exists.
        /// </summary>
        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[{typeof(T).Name}] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }

            _instance = (T)this;
            DontDestroyOnLoad(gameObject);

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
