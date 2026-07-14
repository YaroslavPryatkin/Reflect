using Unity.VisualScripting;
using UnityEngine;

public class PlayerMeleeController : MeleeController
{
    private PlayerInputController _playerInputController;
    private PlayerMovementController _playerMovementController;
    private PlayerJumpController _playerJumpController;
    private PlayerDashController _playerDashController;
    private PlayerForwardJumpingController _playerForwardJumpingController;
    private PlayerLandingController _playerLandingController;
    private PlayerSensors _playerSensors;
    private PlayerGunController _playerGunController;
    private PlayerTargetLockController _playerTargetLockController;
    
    
    private bool _canUseSword = false;
    private bool _shouldInterrupt = false;
    
    public int currentAction = -1;
    
    private void ChangeFlags()
    {
        _canUseSword = _playerMovementController.WallRunningState == 0 && !_playerForwardJumpingController.IsForwardJumping &&
                      _playerLandingController.CanBeInterrupted;
        
        _shouldInterrupt = _playerMovementController.IsActiveSlidingPhase;
        if (_shouldInterrupt)
        {
            currentAction = -1;
            return;
        }
        
        if (_playerInputController.IsParryBufferActive)
        {
            if (currentAction != 1)
            {
                _playerInputController.ConsumeParry();
                currentAction = 1;
            }
        }
        else if (_playerInputController.IsAttackBufferActive)
        {
            if (currentAction != 0)
            {
                _playerInputController.ConsumeAttack();
                currentAction = 0;
            }
        }

    }
    
    

    protected override void Awake()
    {
        base.Awake();
        _playerInputController = GetComponent<PlayerInputController>();
        _playerMovementController = GetComponent<PlayerMovementController>();
        _playerJumpController =  GetComponent<PlayerJumpController>();
        _playerDashController =  GetComponent<PlayerDashController>();
        _playerForwardJumpingController = GetComponent<PlayerForwardJumpingController>();
        _playerLandingController = GetComponent<PlayerLandingController>();
        _playerSensors = GetComponent<PlayerSensors>();
        _playerGunController = GetComponent<PlayerGunController>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>();
    }

    protected override void Update()
    {
        ChangeFlags();
        base.Update();
    }
    
    protected override int WhatComboToPlay()
    {
        return currentAction;
    }

    public override void OnAttack()
    {
        currentAction = -1;
    }

    public override void OnParry()
    {
        currentAction = -1;
    }

    protected override bool ShouldHold()
    {
        return _playerTargetLockController.IsLocked;
    }

    protected override bool ShouldInterrupt()
    {
        return _shouldInterrupt;
    }
    
    protected override bool ShouldSwitchStateToNon()
    {
        return !_canUseSword;
    }
}
