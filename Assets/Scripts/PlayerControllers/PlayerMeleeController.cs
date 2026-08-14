using Unity.VisualScripting;
using UnityEngine;

public class PlayerMeleeController : MeleeController
{
    private PlayerInputController _playerInputController;
    private PlayerManager _playerManager;
    private PlayerTargetLockController _playerTargetLockController;
    private PlayerSensors _playerSensors;
    private PlayerHealthController _playerHealthController;

    public bool ShouldBlockChangeTargetLock => _currentAction == 2 || _currentAction == 7;
    
    private int _currentAction = -1;
    
    public void ResetCurrentAction(){_currentAction = -1;}
    
    private void ChangeFlags()
    {
        if (_playerManager.CanNotUseSword)
        {
            _currentAction = -1;
            return;
        }


        if (_playerSensors.IsGrounded)
        {
            if (_playerInputController.IsParryBufferActive)
            {
                _currentAction = 0;
                return;
            }
            if (_playerInputController.IsHeavyAttackCharging)
            {
                _currentAction = 2;
                return;
            }
            if (_playerInputController.IsAttackBufferActive)
            {
                _currentAction = 3;
                return;
            }
        }
        else
        {
            if (_playerInputController.IsParryBufferActive)
            {
                _currentAction = 5;
                return;
            }
            if (_playerInputController.IsHeavyAttackCharging &&
                (_playerTargetLockController.IsLocked || _playerTargetLockController.IsMeleeLocked) && 
                _playerTargetLockController.TargetPosition.y < transform.position.y && 
                Vector3.Distance(transform.position, _playerTargetLockController.TargetPosition) <= 11f)
            {
                _currentAction = 7;
                return;
            }
            if (_playerInputController.IsAttackBufferActive)
            {
                _currentAction = 8;
                return;
            }
        }
        
        if (_currentAction == 2 || _currentAction == 7)
            _currentAction = -1;
    }

    protected override void Awake()
    {
        base.Awake();
        _playerInputController = GetComponent<PlayerInputController>();
        _playerManager= GetComponent<PlayerManager>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>();
        _playerSensors = GetComponent<PlayerSensors>();
        _playerHealthController = GetComponent<PlayerHealthController>();
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

    public override void OnAttackEnd()
    {
        _playerInputController.ConsumeAttack();
        _currentAction = -1;
        ChangeFlags();
    }

    public override void OnParryEnd()
    {
        _playerInputController.ConsumeParry();
        _currentAction = -1;
        ChangeFlags();
    }

    protected override bool ShouldHold()
    {
        return _playerTargetLockController.IsLocked || _playerHealthController.ShouldHoldSwordOnArena;
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
