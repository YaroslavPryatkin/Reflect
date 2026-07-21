using Unity.VisualScripting;
using UnityEngine;

public class PlayerMeleeController : MeleeController
{
    private PlayerInputController _playerInputController;
    private PlayerManager _playerManager;
    private PlayerTargetLockController _playerTargetLockController;
    
    
    
    private int _currentAction = -1;
    
    private void ChangeFlags()
    {
        if (_playerManager.CanUseSword)
        {
            _currentAction = -1;
            return;
        }
        
        if (_playerInputController.IsParryBufferActive)
        {
            _currentAction = 0;
        }
        else
        {
            if (_playerInputController.IsHeavyAttackCharging)
            {
                _currentAction = 2;
            }
            else if (_playerInputController.IsAttackBufferActive)
            {
                _currentAction = 3;
            }
        }

    }
    
    

    protected override void Awake()
    {
        base.Awake();
        _playerInputController = GetComponent<PlayerInputController>();
        _playerManager= GetComponent<PlayerManager>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>();
    }

    protected override void Update()
    {
        ChangeFlags();
        base.Update();
    }
    
    protected override int WhatComboToPlay()
    {
        return _currentAction;
    }

    public override void OnAttack()
    {
        _playerInputController.ConsumeAttack();
        _currentAction = -1;
    }

    public override void OnParry()
    {
        _playerInputController.ConsumeParry();
        _currentAction = -1;
    }

    protected override bool ShouldHold()
    {
        return _playerTargetLockController.IsLocked;
    }

    protected override bool ShouldInterrupt()
    {
        return _playerManager.CanUseSword;
    }
    
    protected override bool ShouldSwitchStateToNon()
    {
        return !_playerManager.CanHoldSword;
    }
}
