using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public abstract class Poolable<T> : MonoBehaviour where T : Poolable<T>
{
    private IObjectPool<T> _pool;

    public void SetPool(IObjectPool<T> pool)
    {
        _pool = pool;
    }

    public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
    }

    public void ReturnToPool()
    {
        OnReturnToPool();
        if (_pool != null)
        {
            _pool.Release((T)this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    protected abstract void OnReturnToPool();
}