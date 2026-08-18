using UnityEngine;
using System.Collections.Generic;

public class MeleeWeaponHitboxController : MonoBehaviour
{
    [SerializeField] private Transform myTransform;
    [SerializeField] private float bulletRechargeOnParryFraction = 0f;
    [SerializeField] private ParticleSystem bloodParticles;

    private SparkSpawner _sparkSpawner;
    
    private int _targetLayers; 
    private readonly HashSet<HealthController> _alreadyHitTargets = new ();
    private float _attackDamage;
    private float _poiseDamage;
    private bool _triggerReaction;

    private Collider _collider;
    
    public bool DidHit => _alreadyHitTargets.Count != 0;
    public Vector3 LastHitTargetPosition { get; private set; }

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
    
    public void StartSwing(bool triggerReaction, float damage,float poiseDamage)
    {
        _triggerReaction = triggerReaction;
        _attackDamage = damage;
        _alreadyHitTargets.Clear();
        _collider.enabled = true;
        _poiseDamage = poiseDamage;
    }

    public void FinishSwing()
    {
        _collider.enabled = false;
        _alreadyHitTargets.Clear();
    }
    

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & _targetLayers) == 0) return;

        if (other.TryGetComponent<HealthController>(out var health))
        {
            if (!health.IsDead && !_alreadyHitTargets.Contains(health))
            {
                var dir = other.transform.position -  myTransform.position;
                if (health.TryDeflecting(dir))
                {
                    health.DoDeflectDamage(_triggerReaction, _attackDamage, _poiseDamage,bulletRechargeOnParryFraction, HealthController.DamageDealer.Melee);
                }
                else
                {
                    bloodParticles.Play();
                    health.DoNormalDamage(_triggerReaction, _attackDamage, _poiseDamage, HealthController.DamageDealer.Melee);
                }
                
                LastHitTargetPosition = _collider.transform.position;
                
                _alreadyHitTargets.Add(health);
            }
        }
        else
        {
            Debug.Log("No health controller on target object " + other.gameObject.name);
        }
    }
    
}
