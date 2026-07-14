using System;
using UnityEngine;

public class PlayerGunController : GunController
{
    [Header("Auto Aim")] 
    [SerializeField] private bool autoAimWhileTargetLock = false;
    public bool CanAim { get; private set; } = true;
    
    private PlayerInputController _playerInputController;
    private PlayerDashController _playerDashController;
    private PlayerMovementController _playerMovementController;
    private PlayerForwardJumpingController _playerForwardJumpingController;
    private PlayerLandingController  _playerLandingController;
    private PlayerMeleeController _playerMeleeController;
    private PlayerTargetLockController _playerTargetLockController;

    protected override void Awake()
    {
        base.Awake();
        _playerInputController = GetComponent<PlayerInputController>();
        _playerDashController = GetComponent<PlayerDashController>();
        _playerMovementController = GetComponent<PlayerMovementController>();
        _playerForwardJumpingController = GetComponent<PlayerForwardJumpingController>();
        _playerLandingController = GetComponent<PlayerLandingController>();
        _playerMeleeController = GetComponent<PlayerMeleeController>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>();
    }

    
    protected override void SetWantedTargetPoint()
    {
        if (autoAimWhileTargetLock && _playerTargetLockController.IsLocked)
        {
            WantedTargetPoint = _playerTargetLockController.TargetPosition;
        }
        else
        {
            if (Physics.Raycast(GlobalCameraManager.GetPlayerCameraPosition(),
                    GlobalCameraManager.GetPlayerCameraForward(), out RaycastHit hit,
                    maximumTargetDistance, layerMask))
            {
                WantedTargetPoint = hit.point;
            }
            else
            {
                WantedTargetPoint = GlobalCameraManager.GetPlayerCameraPosition() +
                                    GlobalCameraManager.GetPlayerCameraForward() * targetDistanceIfNotFound;
            }
        }
    }

    protected override void ChangeIsAiming()
    {
        CanAim = _playerMovementController.WallRunningState == 0 && _playerMovementController.SlidingPhase == 0 &&
                 !_playerDashController.IsDashing && !_playerForwardJumpingController.IsForwardJumping &&
                 _playerLandingController.CanBeInterrupted && _playerMeleeController.CanBeSafelyInterrupted 
                 && !_playerMeleeController.Parrying;
        isAiming = _playerInputController.IsAimPressed;
    }

    protected override bool ShouldInterruptAiming()
    {
        return !CanAim;
    }
}
