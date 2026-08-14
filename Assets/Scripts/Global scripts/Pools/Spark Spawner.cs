using UnityEngine;
using UnityEngine.Pool;

[DefaultExecutionOrder(-100)]
public class SparkSpawner : MonoBehaviour
{
    private ParticleSystem _sparkPrefab;
    private Transform _parent;
    
    private IObjectPool<ParticleSystem> _pool;

    private void Awake()
    {
        _sparkPrefab = GetComponentInChildren<ParticleSystem>();
    }
    
    public void Initialize(int capacity, Transform parent=null)
    {
        Initialize(capacity, capacity * 2, parent);
    }
    
    public void Initialize(int defaultCapacity, int maxSize, Transform parent=null)
    {
        _parent=parent;
        _pool = new ObjectPool<ParticleSystem>(
            createFunc: () => Instantiate(_sparkPrefab, _parent),
            actionOnGet: (sparks) => sparks.gameObject.SetActive(true),
            actionOnRelease: (sparks) => sparks.gameObject.SetActive(false),
            actionOnDestroy: (sparks) => Destroy(sparks.gameObject),
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }
        
    public void SpawnSparks()
    {
        var sparks = _pool.Get();
        sparks.transform.SetPositionAndRotation(transform.position, transform.rotation);
        sparks.Play();

        StartCoroutine(ReturnToPool(sparks));
    }

    public void SpawnSparks(Vector3 position, Quaternion rotation)
    {
        var sparks = _pool.Get();
        sparks.transform.SetPositionAndRotation(position, rotation);
        sparks.Play();

        StartCoroutine(ReturnToPool(sparks));
    }

    private System.Collections.IEnumerator ReturnToPool(ParticleSystem sparks)
    {
        yield return new WaitUntil(() => !sparks.IsAlive(true));
        _pool.Release(sparks);
    }
}
