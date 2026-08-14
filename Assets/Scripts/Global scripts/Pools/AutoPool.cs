using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class AutoPool<T> where T : Poolable<T>
{
    private readonly IObjectPool<T> _pool;
    private readonly T _prefab;
    private readonly Transform _parent;

    private readonly HashSet<T> _activeItems = new();
    private readonly List<T> _buffer = new();
    public AutoPool(T prefab, int defaultCapacity = 10, int maxSize = 100, Transform parent = null)
    {
        _prefab = prefab;
        _parent = parent;

        _pool = new ObjectPool<T>(
            createFunc: CreateObject,
            actionOnGet: obj => 
            {
                _activeItems.Add(obj);
                obj.gameObject.SetActive(true);
            },
            actionOnRelease: obj => 
            {
                _activeItems.Remove(obj);
                obj.gameObject.SetActive(false);
            },
            actionOnDestroy: obj => 
            {
                _activeItems.Remove(obj);
                Object.Destroy(obj.gameObject);
            },
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }

    private T CreateObject()
    {
        var instance = Object.Instantiate(_prefab, _parent);
        instance.SetPool(_pool);
        return instance;
    }

    public T Get(Vector3 position, Quaternion rotation)
    {
        var instance = _pool.Get();
        instance.SetPositionAndRotation(position, rotation);
        return instance;
    }
    
    public T Get(Transform spawnPoint)
    {
        return Get(spawnPoint.position, spawnPoint.rotation);
    }
    
    public T Get()
    {
        return Get(_prefab.transform.position, _prefab.transform.rotation);
    }

    public HashSet<T> ActiveItems => _activeItems;
    public int ActiveItemsCount => _activeItems.Count;
    
    public void ReleaseAll()
    {
        if (ActiveItemsCount <= 0) return;

        _buffer.Clear();
        _buffer.AddRange(_activeItems);

        for (int i = 0; i < _buffer.Count; i++)
        {
            _buffer[i].ReturnToPool();
        }
        _buffer.Clear();
    }
}