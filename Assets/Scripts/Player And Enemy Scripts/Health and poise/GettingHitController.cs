using System.Collections.Generic;
using UnityEngine;

public class GettingHitController : MonoBehaviour
{
    [SerializeField] private GettingHitSettings settings;
    
    private AnimationLayerController _animationLayerController;
    private HealthController _healthController;

    private UtilityClasses.BaseActionAutomaticTransition _transitionState;

    private readonly UtilityClasses.FractionTemporaryValue<bool> _stun = new(false, true);
    private AnimationClip _lastClip;
    
    public bool CanGetStunned { get; set; } = true;
    
    public bool IsStunned => _stun.Value;

    public bool IsActivelyStunned => _stun.Value && _stun.TimeFraction < settings.activeStunDurationFraction;
    
    
    private readonly UtilityClasses.ChangeableFractionValue _hyperArmor = new();
    
    private void Awake()
    {
        if (!TryGetComponent(out AnimationAndRigManager controller))
        {
            Debug.LogError("No AnimationController found!", this);
            enabled = false;
            return;
        }
        
        _healthController = GetComponent<HealthController>();

        settings.InitializeController(controller, out _animationLayerController, out _transitionState, out _lastClip);
    }
    
    
    public void ActivateHyperArmor(UtilityClasses.FractionTemporaryValue<bool> timer)
    {
        _hyperArmor.Set(timer);
    }

    public void StopHyperArmor()
    {
        _hyperArmor.Unset();
    }

    public void InterruptStunIfNotActive()
    {
        if(_stun.Value && _stun.TimeFraction > settings.activeStunDurationFraction)
            _stun.Deactivate();
    }
    
    private void Update()
    {
        if (_healthController.IsDead)
        {
            _animationLayerController.SetLayerWeight(1f);
            return;
        }
        
        if (!CanGetStunned)
        {
            _stun.Deactivate();
        }
        _animationLayerController.SetLayerWeight(_transitionState.GetFraction( _stun.Value));
    }

    public void Stun(float poiseDamage, bool haveParried)
    {
        if (_stun.TimeFraction < settings.stunDurationFractionToGetStunnedAgain || !CanGetStunned || _hyperArmor.Value) return;

        if (settings.TryStun(poiseDamage, haveParried, out var stunDuration))
        {
            settings.SetStunAnimation(stunDuration, _animationLayerController, ref _lastClip);
            _stun.Activate(stunDuration);
        }
    }

    public void Death()
    {
        settings.SetDeathAnimation(_animationLayerController, ref _lastClip);
    }

    public void Revive()
    {
        settings.UnsetDeathAnimation(_animationLayerController, ref _lastClip);
    }

}
