using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Pool;

public class MeleeWeaponHitboxController : MonoBehaviour
{
    [SerializeField] private Transform myTransform;
    [SerializeField] private Transform sparksPoint;
    [SerializeField] private ParticleSystem sparkPrefab;
    
    private IObjectPool<ParticleSystem> _pool;
    
    private int _targetLayers; 
    private HashSet<HealthController> _alreadyHitTargets = new ();
    private float _attackDamage;
    private float _poiseDamage;

    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();   
        _collider.isTrigger = true;
        _collider.enabled = false;
        _pool = new ObjectPool<ParticleSystem>(
            createFunc: () => Instantiate(sparkPrefab),
            actionOnGet: (sparks) => sparks.gameObject.SetActive(true),
            actionOnRelease: (sparks) => sparks.gameObject.SetActive(false),
            actionOnDestroy: (sparks) => Destroy(sparks.gameObject),
            defaultCapacity: 10,
            maxSize: 20
        );
    }
    
    public void SpawnSparks()
    {
        var sparks = _pool.Get();
        sparks.transform.SetPositionAndRotation(sparksPoint.position, sparksPoint.rotation);
        sparks.Play();

        StartCoroutine(ReturnToPool(sparks));
    }

    private System.Collections.IEnumerator ReturnToPool(ParticleSystem sparks)
    {
        yield return new WaitUntil(() => !sparks.IsAlive(true));
        _pool.Release(sparks);
    }

    public void SetTargetLayers(int targetLayers)
    {
        _targetLayers = targetLayers;
    }
    
    public void StartSwing(float damage,float poiseDamage)
    {
        _attackDamage = damage;
        _alreadyHitTargets.Clear();
        _collider.enabled = true;
        _poiseDamage = poiseDamage;
    }

    public void FinishSwing()
    {
        _collider.enabled = false;
    }
    

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & _targetLayers) == 0) return;

        if (other.TryGetComponent<HealthController>(out var health))
        {
            if (!_alreadyHitTargets.Contains(health))
            {
                var dir = other.transform.position -  myTransform.position;
                if (health.TryDeflecting(dir))
                {
                    health.DoDeflectDamage(_attackDamage, _poiseDamage, HealthController.DamageDealer.Melee);
                }
                else
                {
                    health.DoNormalDamage(_attackDamage, _poiseDamage, HealthController.DamageDealer.Melee);
                }
                
                _alreadyHitTargets.Add(health);
            }
        }
        else
        {
            Debug.Log("No health controller on target object " + other.gameObject.name);
        }
    }
    
}
