using System;
using UnityEngine;

public class PlayerGunController : GunController
{
    [Header("Auto Aim")] 
    [SerializeField] private bool autoAimWhileTargetLock = false;
    
    private PlayerInputController _playerInputController;
    private PlayerTargetLockController _playerTargetLockController;
    private PlayerManager _playerManager;
    private PlayerHealthController _playerHealthController;

    private float _bulletEarnFraction=0f;
    
    public override float RechargeFraction => CanRechargeBulletsWithTime ? base.RechargeFraction : _bulletEarnFraction;

    public override bool IsRechargingBullet => CanRechargeBulletsWithTime ? base.IsRechargingBullet : _bulletEarnFraction > 0;

    protected override void Awake()
    {
        base.Awake();
        _playerInputController = GetComponent<PlayerInputController>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>();
        _playerManager = GetComponent<PlayerManager>();
        _playerHealthController = GetComponent<PlayerHealthController>();
    }
    
    protected override void SetWantedTargetPoint(out Vector3 wantedTargetPoint)
    {
        if (autoAimWhileTargetLock && _playerTargetLockController.IsLocked)
        {
            wantedTargetPoint = _playerTargetLockController.TargetPosition;
        }
        else
        {
            if (Physics.Raycast(GlobalCameraManager.GetPlayerCameraPosition(),
                    GlobalCameraManager.GetPlayerCameraForward(), out var hit,
                    maximumTargetDistance, DestructionLayerMask, Sensors.QueryTriggerInteractionShooting))
            {
                wantedTargetPoint = hit.point;
            }
            else
            {
                wantedTargetPoint = GlobalCameraManager.GetPlayerCameraPosition() +
                                    GlobalCameraManager.GetPlayerCameraForward() * targetDistanceIfNotFound;
            }
        }
    }

    protected override void SetIsAimingAndAimingAngleIncrease(out bool isAiming, out float aimingAngleIncrease)
    {
        isAiming = _playerInputController.IsAimPressed;
        aimingAngleIncrease = 0f;
    }

    protected override bool ShouldInterruptAiming()
    {
        return !_playerManager.CanAim;
    }

    public override void EarnBullet(float fraction)
    {
        if (CanRechargeBulletsWithTime || CurrentAmountOfBullets >= MagazineCapacity)
        {
            return;
        }

        _bulletEarnFraction += fraction;
        if (_bulletEarnFraction >= 1f)
        {
            int toRecharge = Mathf.FloorToInt(_bulletEarnFraction);
            _bulletEarnFraction -= toRecharge;
            SetBulletsAtLeast(CurrentAmountOfBullets + toRecharge);
        }

        if (CurrentAmountOfBullets >= MagazineCapacity)
            _bulletEarnFraction = 0;
    }
    
    

    protected override void Update()
    {
        CanRechargeBulletsWithTime = _playerHealthController.CanRegenerateOnArena;
        if (CanRechargeBulletsWithTime || CurrentAmountOfBullets >= MagazineCapacity)
            _bulletEarnFraction = 0f;
        base.Update();
    }
}
