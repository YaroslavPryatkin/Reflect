using UnityEngine;
using System.Collections.Generic;

public class MeleWeaponHitboxController : MonoBehaviour
{
    private int _targetLayers; 
    private HashSet<HealthController> _alreadyHitTargets = new ();
    private float _currentAttackDamage;

    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();   
        _collider.isTrigger = true;
        _collider.enabled = false;
    }

    public void SetTargetLayers(int targetLayers)
    {
        _targetLayers = targetLayers;
    }
    
    public void StartSwing(float damage)
    {
        _currentAttackDamage = damage;
        _alreadyHitTargets.Clear();
        _collider.enabled = true;
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
                health.ChangeHealth(-_currentAttackDamage);
                _alreadyHitTargets.Add(health);
            }
        }
        else
        {
            Debug.Log("No health controller on target object " + other.gameObject.name);
        }
    }
    
}
