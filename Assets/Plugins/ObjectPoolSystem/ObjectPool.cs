using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Plugins.ObjectPoolSystem
{
    public class ObjectPool
    {
        private readonly HashSet<PooledObject> _totalPool = new HashSet<PooledObject>();
        private readonly HashSet<PooledObject> _activePool = new HashSet<PooledObject>();
        private readonly HashSet<PooledObject> _inactivePool = new HashSet<PooledObject>();

        private readonly int _initialSize;
        private readonly int _maxSize;

        private readonly Func<GameObject> _createFunc;

        public event Action<GameObject> OnEnabledObject;
        public event Action<GameObject> OnDisabledObject;
        public event Action<GameObject> OnDestroyedObject;
        public event Action OnCleared;

        public ObjectPool(Func<GameObject> createFunc, int initialSize = 16, int maxSize = 256)
        {
            initialSize = Mathf.Max(0, initialSize);
            maxSize = Mathf.Max(initialSize, maxSize);

            _createFunc = createFunc;
            _initialSize = initialSize;
            _maxSize = maxSize;

            Init();
        }

        public GameObject Get()
        {
            if (_inactivePool.Count > 0)
            {
                PooledObject pooledObject = _inactivePool.First();
                pooledObject.GameObject.SetActive(true);
                return pooledObject.GameObject;
            }

            if (_totalPool.Count < _maxSize)
            {
                GameObject instance = _createFunc();
                instance.SetActive(true);

                PooledObject pooledObject = new PooledObject
                {
                    GameObject = instance
                };

                StartObserving(pooledObject);

                _totalPool.Add(pooledObject);
                _activePool.Add(pooledObject);
                return instance;
            }

            PooledObject lastPooledObject = _activePool.Last();

            lastPooledObject.GameObject.SetActive(false);
            lastPooledObject.GameObject.SetActive(true);

            return lastPooledObject.GameObject;
        }

        public void Clear()
        {
            foreach (var pooledObject in _totalPool.ToList())
            {
                Object.Destroy(pooledObject.GameObject);
            }

            OnCleared?.Invoke();
        }

        private void Init()
        {
            for (int i = 0; i < _initialSize; i++)
            {
                GameObject instance = _createFunc();
                instance.SetActive(false);

                PooledObject pooledObject = new PooledObject
                {
                    GameObject = instance
                };

                StartObserving(pooledObject);

                _totalPool.Add(pooledObject);
                _inactivePool.Add(pooledObject);
            }
        }

        private void StartObserving(PooledObject pooledObject)
        {
            StopObserving(pooledObject);

            pooledObject.GameObject.OnEnableAsObservable().Subscribe(_ => OnEnabled(pooledObject)).AddTo(pooledObject.Subscriptions);
            pooledObject.GameObject.OnDisableAsObservable().Subscribe(_ => OnDisabled(pooledObject)).AddTo(pooledObject.Subscriptions);
            pooledObject.GameObject.OnDestroyAsObservable().Subscribe(_ => OnDestroyed(pooledObject)).AddTo(pooledObject.Subscriptions);
        }

        private void StopObserving(PooledObject pooledObject)
        {
            pooledObject.Subscriptions.Clear();
        }

        private void OnEnabled(PooledObject pooledObject)
        {
            _inactivePool.Remove(pooledObject);
            _activePool.Add(pooledObject);

            OnEnabledObject?.Invoke(pooledObject.GameObject);
        }

        private void OnDisabled(PooledObject pooledObject)
        {
            _activePool.Remove(pooledObject);
            _inactivePool.Add(pooledObject);

            OnDisabledObject?.Invoke(pooledObject.GameObject);
        }

        private void OnDestroyed(PooledObject pooledObject)
        {
            _totalPool.Remove(pooledObject);
            _inactivePool.Remove(pooledObject);
            _activePool.Remove(pooledObject);
            pooledObject.Subscriptions.Dispose();

            OnDestroyedObject?.Invoke(pooledObject.GameObject);
        }
    }
}
