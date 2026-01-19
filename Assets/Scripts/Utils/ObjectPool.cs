using System;
using System.Collections.Generic;
using UnityEngine;

namespace VeilBreakers.Utils
{
    /// <summary>
    /// Interface for objects that need custom behavior when pooled.
    /// Implement this on MonoBehaviours that need reset logic.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>Called when object is retrieved from pool</summary>
        void OnGetFromPool();

        /// <summary>Called when object is returned to pool</summary>
        void OnReturnToPool();
    }

    /// <summary>
    /// Generic object pool for Unity Components.
    /// Uses Stack for O(1) get/return operations.
    /// Auto-expands when empty (no hard limit unless maxSize specified).
    /// </summary>
    /// <typeparam name="T">Component type to pool</typeparam>
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _pool;
        private readonly int _maxSize;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onReturn;

        private int _totalCreated;

        /// <summary>Number of objects currently in use</summary>
        public int CountActive => _totalCreated - _pool.Count;

        /// <summary>Number of objects available in pool</summary>
        public int CountInactive => _pool.Count;

        /// <summary>Total objects created by this pool</summary>
        public int CountTotal => _totalCreated;

        /// <summary>
        /// Creates a new object pool.
        /// </summary>
        /// <param name="prefab">Prefab to instantiate</param>
        /// <param name="parent">Parent transform for pooled objects (keeps hierarchy clean)</param>
        /// <param name="initialSize">Number of objects to pre-create</param>
        /// <param name="maxSize">Maximum pool size (0 = unlimited)</param>
        /// <param name="onGet">Optional callback when object is retrieved</param>
        /// <param name="onReturn">Optional callback when object is returned</param>
        public ObjectPool(
            T prefab,
            Transform parent = null,
            int initialSize = 10,
            int maxSize = 0,
            Action<T> onGet = null,
            Action<T> onReturn = null)
        {
            _prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            _parent = parent;
            _maxSize = maxSize;
            _onGet = onGet;
            _onReturn = onReturn;
            _pool = new Stack<T>(initialSize > 0 ? initialSize : 10);
            _totalCreated = 0;

            // Pre-warm the pool
            if (initialSize > 0)
            {
                Prewarm(initialSize);
            }
        }

        /// <summary>
        /// Gets an object from the pool. Creates a new one if pool is empty.
        /// </summary>
        /// <returns>Active, ready-to-use object</returns>
        public T Get()
        {
            T obj = null;

            // Try to get from pool, handling destroyed objects
            while (_pool.Count > 0 && obj == null)
            {
                obj = _pool.Pop();

                // Check if object was destroyed outside the pool
                if (obj == null)
                {
                    Debug.LogWarning($"[ObjectPool<{typeof(T).Name}>] Found destroyed object in pool. Was it destroyed externally?");
                    _totalCreated--;
                }
            }

            // Create new if pool is empty
            if (obj == null)
            {
                obj = CreateNew();
            }

            // Activate and notify
            obj.gameObject.SetActive(true);

            // Call IPoolable interface if implemented
            if (obj is IPoolable poolable)
            {
                poolable.OnGetFromPool();
            }

            // Call custom callback
            _onGet?.Invoke(obj);

            return obj;
        }

        /// <summary>
        /// Gets an object and sets its position/rotation.
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation)
        {
            var obj = Get();
            obj.transform.SetPositionAndRotation(position, rotation);
            return obj;
        }

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        /// <param name="obj">Object to return</param>
        public void Return(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning($"[ObjectPool<{typeof(T).Name}>] Attempted to return null object");
                return;
            }

            // Prevent double-return (object already deactivated means it was already returned)
            if (!obj.gameObject.activeSelf)
            {
                Debug.LogWarning($"[ObjectPool<{typeof(T).Name}>] Object already returned to pool: {obj.name}");
                return;
            }

            // Check max size - destroy if over limit
            if (_maxSize > 0 && _pool.Count >= _maxSize)
            {
                UnityEngine.Object.Destroy(obj.gameObject);
                _totalCreated--;
                return;
            }

            // Call IPoolable interface if implemented
            if (obj is IPoolable poolable)
            {
                poolable.OnReturnToPool();
            }

            // Call custom callback
            _onReturn?.Invoke(obj);

            // Deactivate and return to pool
            obj.gameObject.SetActive(false);
            _pool.Push(obj);
        }

        /// <summary>
        /// Pre-creates objects to avoid runtime instantiation spikes.
        /// </summary>
        /// <param name="count">Number of objects to create</param>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Respect max size
                if (_maxSize > 0 && _totalCreated >= _maxSize)
                {
                    break;
                }

                var obj = CreateNew();
                obj.gameObject.SetActive(false);
                _pool.Push(obj);
            }
        }

        /// <summary>
        /// Destroys all pooled objects and clears the pool.
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var obj = _pool.Pop();
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }
            _totalCreated = 0;
        }

        /// <summary>
        /// Creates a new instance of the pooled object.
        /// </summary>
        private T CreateNew()
        {
            var obj = UnityEngine.Object.Instantiate(_prefab, _parent);
            obj.name = $"{_prefab.name}_Pool_{_totalCreated}";
            _totalCreated++;
            return obj;
        }
    }

    /// <summary>
    /// MonoBehaviour wrapper for ObjectPool that can be configured in Inspector.
    /// Useful for simple use cases where you want to set up pooling without code.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameObjectPool : MonoBehaviour
    {
        [Header("Pool Configuration")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _initialSize = 10;
        [SerializeField] private int _maxSize = 0;
        [SerializeField] private Transform _poolParent;

        private ObjectPool<Transform> _pool;

        /// <summary>Number of objects currently in use</summary>
        public int CountActive => _pool?.CountActive ?? 0;

        /// <summary>Number of objects available in pool</summary>
        public int CountInactive => _pool?.CountInactive ?? 0;

        private void Awake()
        {
            if (_prefab == null)
            {
                Debug.LogError($"[GameObjectPool] No prefab assigned on {gameObject.name}");
                return;
            }

            var parent = _poolParent != null ? _poolParent : transform;
            _pool = new ObjectPool<Transform>(
                _prefab.transform,
                parent,
                _initialSize,
                _maxSize
            );
        }

        /// <summary>Gets a GameObject from the pool</summary>
        public GameObject Get()
        {
            return _pool?.Get()?.gameObject;
        }

        /// <summary>Gets a GameObject at a specific position and rotation</summary>
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            return _pool?.Get(position, rotation)?.gameObject;
        }

        /// <summary>Returns a GameObject to the pool</summary>
        public void Return(GameObject obj)
        {
            if (obj != null)
            {
                _pool?.Return(obj.transform);
            }
        }

        /// <summary>Pre-warms the pool with additional objects</summary>
        public void Prewarm(int count)
        {
            _pool?.Prewarm(count);
        }

        /// <summary>Clears all pooled objects</summary>
        public void Clear()
        {
            _pool?.Clear();
        }

        private void OnDestroy()
        {
            _pool?.Clear();
        }
    }
}
