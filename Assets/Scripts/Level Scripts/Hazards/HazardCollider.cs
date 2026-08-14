using System;
using UnityEngine;
using System.Collections.Generic;

public class HazardCollider : MonoBehaviour
{
    [SerializeField] public HazardType hazardType;

    private readonly HashSet<HazardDamageTaker> _occupants = new();
    
    private void Start()
    {
        if (hazardType == null)
        {
            Debug.LogError("Hazard type can not be null, " +  gameObject.name);
            enabled=false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out HazardDamageTaker damageTaker))
        {
            if (_occupants.Add(damageTaker))
            {
                damageTaker.EnterHazard(hazardType);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out HazardDamageTaker damageTaker))
        {
            if (_occupants.Remove(damageTaker))
            {
                damageTaker.ExitHazard(hazardType);
            }
        }
    }

    private void OnDisable()
    {
        foreach (var damageTaker in _occupants)
        {
            if (damageTaker != null) 
            {
                damageTaker.ExitHazard(hazardType);
            }
        }
        
        _occupants.Clear();
    }
}
