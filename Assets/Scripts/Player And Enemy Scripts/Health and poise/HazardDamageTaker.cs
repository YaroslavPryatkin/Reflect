using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(HealthController))]
public class HazardDamageTaker : MonoBehaviour
{
    private class Hazard
    {
        private readonly HazardType _type; 
        private int _hazardsEntered;
        private readonly UtilityClasses.TemporaryValue<bool> _canTakeDamage;

        public Hazard(HazardType type)
        {
            _type = type;
            _hazardsEntered = 1;
            _canTakeDamage = new(true, false);
        }
        
        public void EnterHazard()
        {
            ++_hazardsEntered;
        }

        public void ExitHazard()
        {
            --_hazardsEntered;
            if(_hazardsEntered < 0)
                _hazardsEntered = 0;
        }
        
        public void Update(HealthController healthController)
        {
            if (_hazardsEntered > 0)
            {
                if (_canTakeDamage)
                {
                    _canTakeDamage.Activate(_type.InvulnerabilityDuration);
                    healthController.DoNormalDamage(_type.Damage, 0, HealthController.DamageDealer.Hazard);
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
        _healthController.OnHazardEntered();
    }

    public void ExitHazard(HazardType type)
    {
        if (_hazards.TryGetValue(type, out var hazard))
        {
            hazard.ExitHazard();
        }
    }
    

    
    protected virtual void Update()
    {
        foreach (var hazard in _hazards.Values)
        {
            hazard.Update(_healthController);
        }
    }
}
