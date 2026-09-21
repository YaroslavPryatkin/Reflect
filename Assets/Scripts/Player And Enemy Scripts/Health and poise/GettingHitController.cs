using System;
using System.Collections.Generic;
using UnityEngine;

public class GettingHitController : MonoBehaviour
{
    [SerializeField] private GettingHitSettings settings;
    
    private AnimationLayerController _animationLayerController;
    private HealthController _healthController;

    private UtilityClasses.BaseActionAutomaticTransition _transitionState;

    /// <summary>
    /// stun timer or slow death timer
    /// </summary>
    private readonly UtilityTimers.FractionTemporaryValue<bool> _timer = new(false, true);
    private AnimationClip _lastClip;

    public bool CanBeFinished => _canBeFinished && _timer;
    
    private bool _canBeFinished = false;

    private bool _isBeingFinished = false;

    private bool _needRotate = false;
    private Vector3 _rotateTarget;
    
    public bool IsBeingFinished
    {
        set
        {
            if (value && !_canBeFinished) return;
            
            _isBeingFinished = value;
            
            if (!value)
            {
                RestoreDeathSpeed();
            }
        }
    }


    
    public bool CanGetStunned { get; set; } = true;
    
    public bool IsStunned => _timer.Value;

    public bool IsActivelyStunned => _timer.Value && _timer.TimeFraction < settings.activeStunDurationFraction;
    
    
    private readonly UtilityClasses.ChangeableFractionValueReference _hyperArmor = new();
    
    
    
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
    
    
    public void ActivateHyperArmor(UtilityTimers.FractionTemporaryValue<bool> timer)
    {
        _hyperArmor.Set(timer);
    }

    public void StopHyperArmor()
    {
        _hyperArmor.Unset();
    }

    public void InterruptStunIfNotActive()
    {
        if(_timer.Value && _timer.TimeFraction > settings.activeStunDurationFraction)
            _timer.Deactivate();
    }
    
    protected void Update()
    {
        if (_healthController.IsDead)
        {
            _animationLayerController.SetLayerWeight(1f);
            if (!_isBeingFinished && _canBeFinished && !_timer)
            {
                RestoreDeathSpeed();
            }
        }
        else
        {
            if (!CanGetStunned)
            {
                _timer.Deactivate();
            }

            _animationLayerController.SetLayerWeight(_transitionState.GetFraction(_timer.Value));
        }
    }

    private void LateUpdate()
    {
        if (_needRotate)
        {
            _needRotate = false;
            var dir =  _rotateTarget - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                transform.rotation =  Quaternion.LookRotation(dir);
            }
        }
    }

    public void Stun(float poiseDamage, bool haveParried)
    {
        if (_timer.TimeFraction < settings.stunDurationFractionToGetStunnedAgain || !CanGetStunned || _hyperArmor.Value) return;

        if (settings.TryStun(poiseDamage, haveParried, out var stunDuration))
        {
            settings.SetStunAnimation(stunDuration, _animationLayerController, ref _lastClip);
            _timer.Activate(stunDuration);
        }
    }
    
    private void RestoreDeathSpeed()
    {
        settings.RestoreDeathAnimationSpeed(_animationLayerController, ref _lastClip);
        _canBeFinished = false;
    }

    public void GetStabbedDuringDeath(Vector3 stabSource)
    {
        if (_isBeingFinished)
        {
            _needRotate = true;
            _rotateTarget = stabSource;
            settings.GetStabbed(_animationLayerController, ref _lastClip);
        }
    }
    
    public void Death()
    {
        _canBeFinished = settings.shouldSlowDeath;
        settings.SetDeathAnimation(_animationLayerController, ref _lastClip, _timer);
    }
    
    public void Revive()
    {
        settings.UnsetDeathAnimation(_animationLayerController, ref _lastClip);
    }

}
