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
        ProcessEnter(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        ProcessExit(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProcessEnter(collision.gameObject);
    }

    private void OnCollisionExit(Collision collision)
    {
        ProcessExit(collision.gameObject);
    }

    private void ProcessEnter(GameObject obj)
    {
        if (obj.TryGetComponent(out HazardDamageTaker damageTaker))
        {
            if (_occupants.Add(damageTaker))
            {
                damageTaker.EnterHazard(hazardType);
            }
        }
    }

    private void ProcessExit(GameObject obj)
    {
        if (obj.TryGetComponent(out HazardDamageTaker damageTaker))
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
