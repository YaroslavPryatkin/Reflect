using UnityEngine;
using System.Collections.Generic;

public class MeleeWeaponHitboxController : MonoBehaviour
{
    [SerializeField] private Transform myTransform;
    [SerializeField] private float bulletRechargeOnParryFraction = 0f;

    private SparkSpawner _sparkSpawner;
    
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
        _sparkSpawner = GetComponentInChildren<SparkSpawner>();
        var poolRoot = UtilityFunctions.MakeEmptyObjectOrphan("Pool root melee for " + gameObject.name, transform);
        _sparkSpawner.Initialize(10, poolRoot);
    }

    public void SpawnSparks()
    {
        _sparkSpawner.SpawnSparks();
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
                    health.DoDeflectDamage(_attackDamage, _poiseDamage,bulletRechargeOnParryFraction, HealthController.DamageDealer.Melee);
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
