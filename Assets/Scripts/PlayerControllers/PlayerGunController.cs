using System;
using MeleeComponents;
using UnityEngine;

public class PlayerGunController : GunController
{
    [Header("Auto Aim")] 
    [SerializeField] private bool autoAimWhileTargetLock = false;
    
    private PlayerInputController _playerInputController;
    private PlayerTargetLockController _playerTargetLockController;
    private PlayerManager _playerManager;

    protected override void Awake()
    {
        base.Awake();
        _playerInputController = GetComponent<PlayerInputController>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>();
        _playerManager = GetComponent<PlayerManager>();
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

    protected override void ChangeIsAimingAndAimingAngleIncrease()
    {
        IsAiming = _playerInputController.IsAimPressed;
    }

    protected override bool ShouldInterruptAiming()
    {
        return !_playerManager.CanAim;
    }
}
