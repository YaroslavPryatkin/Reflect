using UnityEngine;
using System.Collections.Generic;
using MeleeSystem;

public class PlayerMeleeController : MeleeController
{
    [SerializeField] private List<MeleePlayableSource> finishHimSources = new();
    [Header("Noob mod: full parry window")]
    [SerializeField] private BoolSettingValue settingValue;
    
    private PlayerInputController _playerInputController;
    private PlayerManager _playerManager;
    private PlayerTargetLockController _playerTargetLockController;
    private PlayerSensors _playerSensors;
    private PlayerHealthController _playerHealthController;
    public bool ShouldBlockChangeTargetLock => CurrentCombo == 2 || CurrentCombo == 7;
    

    private readonly List<int> _finishHimIndexes = new();
    private int _lastRandomIndexFinishHim = 0;

    public override bool ShouldUseMaximumParryDuration => settingValue.value;

    protected override void Awake()
    {
        _playerInputController = GetComponent<PlayerInputController>();
        _playerManager= GetComponent<PlayerManager>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>();
        _playerSensors = GetComponent<PlayerSensors>();
        _playerHealthController = GetComponent<PlayerHealthController>();

        
        foreach (var source in finishHimSources)
        {
            _finishHimIndexes.Add(playableSources.Count);
            playableSources.Add(source);
        }
        
        base.Awake();
    }
    
    protected override bool WhatComboToPlay(ref int nextComboToPlay)
    {
        if (_playerManager.CanNotUseSword)
        {
            nextComboToPlay = -1;
            return true;
        }

        if (_playerTargetLockController.IsFinishHimLocked)
        {
            return false;
        }
        
        if (nextComboToPlay == -1 && 
            _playerInputController.IsAttackBufferActive &&
            _playerTargetLockController.HaveFinishHimTarget)
        {
            nextComboToPlay = _finishHimIndexes.GetRandomElement(ref _lastRandomIndexFinishHim);
            return false;
        }
        
        if (_playerSensors.IsGrounded)
        {
            if (_playerInputController.IsParryBufferActive)
            {
                nextComboToPlay = 0;
                return false;
            }
            if (_playerInputController.IsHeavyAttackCharging)
            {
                nextComboToPlay = 2;
                return true;
            }
            if (_playerInputController.IsAttackBufferActive)
            {
                nextComboToPlay = 3;
                return true;
            }
            if (_playerInputController.IsWeaponArtCharging)
            {
                nextComboToPlay = 9;
                return true;
            }
        }
        else
        {
            if (_playerInputController.IsParryBufferActive)
            {
                nextComboToPlay = 5;
                return false;
            }
            if (_playerInputController.IsHeavyAttackCharging &&
                (_playerTargetLockController.IsLocked || _playerTargetLockController.IsMeleeLocked) && 
                _playerTargetLockController.TargetPosition.y < transform.position.y && 
                Vector3.Distance(transform.position, _playerTargetLockController.TargetPosition) <= 11f)
            {
                nextComboToPlay = 7;
                return true;
            }
            if (_playerInputController.IsAttackBufferActive)
            {
                nextComboToPlay = 8;
                return true;
            }
        }
        
        if (nextComboToPlay == 2 || nextComboToPlay == 7 || nextComboToPlay == 9)
            nextComboToPlay = -1;
        
        return true;
    }

    public override void OnAttackEnd()
    {
        _playerInputController.ConsumeAttack();
        ResetNextComboToPlay();
    }

    public override void OnParryEnd()
    {
        _playerInputController.ConsumeParry();
        ResetNextComboToPlay();
    }
    
    public void OnFinishHimEvent()
    {
        if (_playerTargetLockController.IsFinishHimLocked)
        {
            _playerTargetLockController.UnlockFinishHimSuccessful();
            TransformController.InterruptMove();
            _playerInputController.ConsumeAttack();
        }
    }
    
    public override void OnFinishHimStart()
    {
        _playerTargetLockController.LockFinishHimTarget();
    }

    public override void OnFinishHimEnd()
    {
        if (_playerTargetLockController.IsFinishHimLocked)
        {
            _playerTargetLockController.UnlockFinishHimUnsuccessful();
        }
    }
    
    protected override bool ShouldHold()
    {
        return _playerTargetLockController.IsLocked || 
               LevelController.ShouldHoldSwordOnArena || 
               _playerTargetLockController.HaveFinishHimTarget;
    }

    protected override bool ShouldInterrupt()
    {
        return _playerManager.CanNotUseSword;
    }
    
    protected override bool ShouldSwitchStateToNon()
    {
        return !_playerManager.CanHoldSword;
    }


}
