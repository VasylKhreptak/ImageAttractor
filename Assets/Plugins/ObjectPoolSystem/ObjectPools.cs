using System;
using System.Collections.Generic;
using UnityEngine;

namespace Plugins.ObjectPoolSystem
{
    public class ObjectPools<T> where T : Enum
    {
        private readonly Dictionary<T, ObjectPool> _pools = new Dictionary<T, ObjectPool>();

        public event Action<GameObject> OnEnabledObject;
        public event Action<GameObject> OnDisabledObject;
        public event Action<GameObject> OnDestroyedObject;
        public event Action OnCleared;

        public ObjectPools(ObjectPoolPreference<T>[] objectPoolPreferences)
        {
            foreach (var preference in objectPoolPreferences)
            {
                ObjectPool objectPool = new ObjectPool(preference.CreateFunc, preference.InitialSize, preference.MaxSize);
                _pools.Add(preference.Key, objectPool);

                objectPool.OnEnabledObject += InvokeOnEnabledObject;
                objectPool.OnDisabledObject += InvokeOnDisabledObject;
                objectPool.OnDestroyedObject += InvokeOnDestroyedObject;
            }
        }

        public bool TryGetPool(T key, out ObjectPool pool)
        {
            return _pools.TryGetValue(key, out pool);
        }

        public void Clear()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }

            OnCleared?.Invoke();
        }

        private void InvokeOnEnabledObject(GameObject gameObject) => OnEnabledObject?.Invoke(gameObject);

        private void InvokeOnDisabledObject(GameObject gameObject) => OnDisabledObject?.Invoke(gameObject);

        private void InvokeOnDestroyedObject(GameObject gameObject) => OnDestroyedObject?.Invoke(gameObject);
    }

    [Serializable]
    public class ObjectPoolPreference<T>
    {
        public T Key;
        public Func<GameObject> CreateFunc;
        public GameObject Prefab;
        public int InitialSize;
        public int MaxSize;
    }
}
