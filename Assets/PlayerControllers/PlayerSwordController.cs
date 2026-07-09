using Unity.VisualScripting;
using UnityEngine;

public class PlayerSwordController : MeleeController
{
    [Header("hit box controller")]
    [SerializeField] private MeleWeaponHitboxController swordHitboxController;
    
    private PlayerInputController _playerInputController;
    private PlayerMovementController _playerMovementController;
    private PlayerJumpController _playerJumpController;
    private PlayerDashController _playerDashController;
    private PlayerForwardJumpingController _playerForwardJumpingController;
    private PlayerLandingController _playerLandingController;
    private PlayerSensors _playerSensors;
    private PlayerGunController _playerGunController;
    private PlayerDamageController _playerDamageController;
    private PlayerTargetLockController _playerTargetLockController;


    public bool Parrying { get; private set; } = false;
    
    
    public bool CanUseSword { get; private set; } = false;
    
    private void ChangeFlags()
    {
        CanUseSword = _playerMovementController.WallRunningState == 0 && _playerMovementController.SlidingPhase == 0 &&
                      !_playerDashController.IsDashing && !_playerForwardJumpingController.IsForwardJumping &&
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
        _playerDamageController = GetComponent<PlayerDamageController>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>();
    }

    protected override void Update()
    {
        ChangeFlags();
        base.Update();
    }


    protected override void OnAttackStart()
    {
        _playerInputController.ConsumeAttack();
        swordHitboxController.StartSwing(_playerDamageController.SwordDamage);
    }
    protected override void OnAttackEnd()
    {
        swordHitboxController.FinishSwing();
    }
    protected override void OnParryStart()
    {
        _playerInputController.ConsumeParry();
        Parrying = true;
    }
    protected override void OnParryEnd()
    {
        Parrying = false;
    }

    protected override int WhatComboToPlay()
    {
        if (_playerInputController.IsAttackBufferActive)
            return 0;
        else
            return -1;
    }

    protected override bool ShouldHold()
    {
        return _playerTargetLockController.IsLocked;
    }

    protected override bool ShouldInterrupt()
    {
        return false;
    }

    protected override bool ShouldParry()
    {
        return _playerInputController.IsParryBufferActive;
    }

    protected override bool ShouldSwitchStateToNon()
    {
        return !CanUseSword;
    }
}
