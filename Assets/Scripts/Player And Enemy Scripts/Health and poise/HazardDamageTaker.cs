using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(HealthController))]
public class HazardDamageTaker : MonoBehaviour
{
    private class Hazard
    {
        private readonly HazardType _type; 
        private readonly UtilityClasses.MultipleBoolValue _isEntered=new();
        private readonly UtilityTimers.TemporaryValue<bool> _canTakeDamage;

        public Hazard(HazardType type)
        {
            _type = type;
            _isEntered.Set();
            _canTakeDamage = new(true, false);
        }
        
        public void EnterHazard()
        {
            _isEntered.Set();
        }

        public void ExitHazard()
        {
            _isEntered.Unset();
        }
        
        public void Update(HealthController healthController)
        {
            if (_isEntered.Value)
            {
                if (_canTakeDamage)
                {
                    _canTakeDamage.Activate(_type.InvulnerabilityDuration);
                    healthController.DoNormalDamage(false, _type.Damage, 0, HealthController.DamageDealer.Hazard);
                }
            }
            else
            {
                _canTakeDamage.Deactivate();
            }
        }
    }

    private readonly Dictionary<HazardType, Hazard> _hazards = new();
    
    private HealthController _healthController;

    private void Awake()
    {
        _healthController = GetComponent<HealthController>();
    }
    
    public void EnterHazard(HazardType type)
    {
        if (_hazards.TryGetValue(type, out var hazard))
        {
            hazard.EnterHazard();
        }
        else
        {
            _hazards.Add(type, new Hazard(type));
        }
        UpdateHazards();
        _healthController.OnHazardEntered();
    }

    public void ExitHazard(HazardType type)
    {
        if (_hazards.TryGetValue(type, out var hazard))
        {
            hazard.ExitHazard();
        }
    }

    private void UpdateHazards()
    {
        foreach (var hazard in _hazards.Values)
        {
            hazard.Update(_healthController);
        }
    }
    
    
    private void Update()
    {
        UpdateHazards();
    }
}
