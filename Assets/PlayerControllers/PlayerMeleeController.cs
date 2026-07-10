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
    public bool CanUseSword { get; private set; } = false;
    
    private void ChangeFlags()
    {
        CanUseSword = _playerMovementController.WallRunningState == 0 && !_playerForwardJumpingController.IsForwardJumping &&
                      _playerLandingController.CanBeInterrupted;
        
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
        if (_playerInputController.IsParryBufferActive)
            return 1;
        if (_playerInputController.IsAttackBufferActive)
            return 0;
        
        return -1;
    }

    protected override void OnAttack()
    {
        _playerInputController.ConsumeAttack();
        
    }

    protected override void OnParry()
    {
        _playerInputController.ConsumeParry();
    }

    protected override bool ShouldHold()
    {
        return _playerTargetLockController.IsLocked;
    }

    protected override bool ShouldInterrupt()
    {
        return false;
    }
    
    protected override bool ShouldSwitchStateToNon()
    {
        return !CanUseSword;
    }
}
