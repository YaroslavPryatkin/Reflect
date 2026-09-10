using System;
using CustomAttributes;
using UnityEngine;
using BaseActionTransitionsEnum = UtilityFunctions.BaseActionTransitionsEnum;
using Random = UnityEngine.Random;
using UnityEngine.Pool;

public abstract class GunController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected BarrelController barrelController;
    [Header("GunPlacement")]
    [SerializeField] protected Transform shoulderPoint;
    [SerializeField] private float gunFromShoulderDistance = 0.4f;
    [Header("Distance")]
    [SerializeField] protected float maximumTargetDistance = 100;
    [SerializeField] protected float targetDistanceIfNotFound = 100;
    [Header("Shooting cone arm")] 
    [SerializeField] private float leftArmHorizontalAngle = -110f;
    [SerializeField] private float rightArmHorizontalAngle = 110f;
    [Header("Shooting cone gun")] 
    [SerializeField] private float leftGunHorizontalAngle = -110f;
    [SerializeField] private float rightGunHorizontalAngle = 110f;
    [Header("Head rotation")] 
    [SerializeField] private float headHorizontalAngle = 100f;
    [Header("Recharge")] 
    [SerializeField] private int magazineCapacity = 3;
    [SerializeField] private float timeBetweenShots = 1f;
    [SerializeField] private float rechargeTime = 3f;
    [Header("Timings")]
    [SerializeField] private float startAimTime = 0.1f;
    [SerializeField] private float finishAimTime = 0.1f;
    [Header("Slo-mo")] 
    [SerializeField] private bool useSloMo = false;
    [SerializeField, EnableIf("useSloMo")] private float sloMoCoefficient = 0.5f;

    private HealthController _healthController;

    public BaseActionTransitionsEnum GunStateValue => GunState.Value;
    
    public int CurrentAmountOfBullets { get; private set; }
    public virtual float RechargeFraction => _rechargingBulletTimer.TimeFraction;
    public virtual bool IsRechargingBullet => _rechargingBulletTimer.RealValue;
    
    private bool _isAiming  = false;
    private float _aimAngleIncrease = 0f;
    private Vector3 _wantedTargetPoint = Vector3.zero;

    protected bool CanRechargeBulletsWithTime = true;
    
    protected int MagazineCapacity => magazineCapacity;

    public Vector3 TargetPoint { get; private set; }
    public Vector3 GunPosition { get; private set; } = Vector3.zero;
    public Vector3 TargetDirection { get; private set; } = Vector3.zero;
    
    public Vector3 TargetHeadDirection { get; private set; } = Vector3.zero;
    
    public float TargetAngle { get; private set; } = 0f;
    public float RawTargetAngle { get; private set; } = 0f;

    public  UtilityTimers.FractionBlockingValueTimer<BaseActionTransitionsEnum> GunState { get;  } = BaseActionTransitionsEnum.Base;
    
    protected Sensors Sensors;

    
    private readonly UtilityTimers.FractionDelayedValueTimer<bool> _rechargingBulletTimer = false;
    private readonly UtilityTimers.FractionTemporaryValue<bool> _canShootAfterPreviousShot = new(true, false);

    protected bool CanShootAfterPreviousShot => _canShootAfterPreviousShot.Value;
    
    
    protected int DestructionLayerMask;
    
    


    protected virtual void Awake()
    {
        if (!TryGetComponent(out Sensors))
        {
            Debug.Log("No Sensors found!");
        }
        
        if (leftGunHorizontalAngle > rightGunHorizontalAngle)
        {
            Debug.Log("Left gun horizontal angle is greater than right");
        }

        DestructionLayerMask = Sensors.IgnoreMyLayerMask;
        barrelController.AwakeBarrel(magazineCapacity, DestructionLayerMask, Sensors.EnemyLayer);

        _healthController = GetComponent<HealthController>();
    }

    private void Start()
    {
        BeAbleToShootImmediately();
    }


    protected abstract void SetWantedTargetPoint(out Vector3 wantedTargetPoint);
    protected abstract void SetIsAimingAndAimingAngleIncrease(out bool isAiming, out float aimingAngleIncrease);
    protected abstract bool ShouldInterruptAiming();
    
    
    private void CalculateTarget()
    {
        var shoulderPos = shoulderPoint.position;
        var wantedDirection = (_wantedTargetPoint - shoulderPos).normalized;
        
        var localDir = transform.InverseTransformDirection(wantedDirection);
        var horizontalDistance = Mathf.Sqrt(localDir.x * localDir.x + localDir.z * localDir.z);
        RawTargetAngle = Mathf.Atan2(localDir.x, localDir.z);

        var armAngle = Mathf.Clamp(RawTargetAngle, leftArmHorizontalAngle * Mathf.Deg2Rad, rightArmHorizontalAngle * Mathf.Deg2Rad);
        
        
        
        localDir.x = horizontalDistance * Mathf.Sin(armAngle);
        localDir.z = horizontalDistance * Mathf.Cos(armAngle);
        GunPosition = shoulderPos + transform.TransformDirection(localDir) * gunFromShoulderDistance;
        
        TargetAngle = Mathf.Clamp(RawTargetAngle, leftGunHorizontalAngle * Mathf.Deg2Rad, rightGunHorizontalAngle * Mathf.Deg2Rad);
        localDir.x = horizontalDistance * Mathf.Sin(TargetAngle);
        localDir.z = horizontalDistance * Mathf.Cos(TargetAngle);
        TargetDirection = transform.TransformDirection(localDir).normalized;
        
        var headAngle = Mathf.Clamp(TargetAngle, -headHorizontalAngle * Mathf.Deg2Rad, headHorizontalAngle * Mathf.Deg2Rad);
        localDir.x = horizontalDistance * Mathf.Sin(headAngle);
        localDir.z = horizontalDistance * Mathf.Cos(headAngle);
        TargetHeadDirection = transform.TransformDirection(localDir).normalized;

        
        if (Physics.Raycast(GunPosition, TargetDirection, out var hit2, targetDistanceIfNotFound, DestructionLayerMask))
        {
            TargetPoint = hit2.point;
        }
        else
        {
            TargetPoint = GunPosition + TargetDirection * targetDistanceIfNotFound;
        }
    }
    
    public int Shoot()
    {
        if (GunStateValue == BaseActionTransitionsEnum.Action && _canShootAfterPreviousShot && CurrentAmountOfBullets > 0)
        {
            _canShootAfterPreviousShot.Activate(timeBetweenShots);
            SpendBullet();
            barrelController.Shoot(_aimAngleIncrease, transform, ref _wantedTargetPoint);
            return 1;
        }

        return 0;
    }
    


    private void SpendBullet()
    {
        --CurrentAmountOfBullets;
        if (CanRechargeBulletsWithTime && !_rechargingBulletTimer.CanBeChanged)
        {
            _rechargingBulletTimer.ResetTime(rechargeTime);
            //Debug.Log("real = " + _rechargingBulletTimer.RealValue + ", val = " + _rechargingBulletTimer.Value);
        }
    }
    
    private void RechargeBullet()
    {
        if (!CanRechargeBulletsWithTime) return;
        
        if (_rechargingBulletTimer.Value)
        {
            ++CurrentAmountOfBullets;
            _rechargingBulletTimer.Set(false);
        }

        if (!IsRechargingBullet && CurrentAmountOfBullets < magazineCapacity)
        {
            _rechargingBulletTimer.Set(true, rechargeTime);
        }

    }

    protected void SetBulletsAtLeast(int minAmountOfBullets)
    {
        _canShootAfterPreviousShot.Deactivate();
        CurrentAmountOfBullets = Mathf.Max(CurrentAmountOfBullets, Mathf.Min(magazineCapacity, minAmountOfBullets));
    }

    public virtual void EarnBullet(float fraction)
    {
    }

    public virtual void BeAbleToShootImmediately()
    {
        SetBulletsAtLeast(magazineCapacity);
    }
    
    
    private void InterruptAiming()
    {
        if(GunState.Value!=BaseActionTransitionsEnum.Base)
            GunState.SetForce(BaseActionTransitionsEnum.Base, 0);
        
        if (useSloMo)
        {
            GlobalTimeScaleController.ReturnTimePace(this);
        }
    }

    private void ChangeState()
    {
        switch (GunState.Value)
        {
            case BaseActionTransitionsEnum.Base:
                if (_isAiming)
                    GunState.SetForce(BaseActionTransitionsEnum.BaseToAction, startAimTime);
                break;
            case BaseActionTransitionsEnum.BaseToAction:
                if (_isAiming)
                {
                    if (GunState.CanBeChanged)
                    {
                        if (useSloMo)
                        {
                            GlobalTimeScaleController.ChangeTimePace(this, sloMoCoefficient);
                        }
                        GunState.SetForce(BaseActionTransitionsEnum.Action, 0);
                    }
                }
                else
                {
                    GunState.SetForce(BaseActionTransitionsEnum.ActionToBase, finishAimTime, 1-GunState.TimeFraction);
                }

                break;
            case BaseActionTransitionsEnum.Action:
                if(!_isAiming)
                {
                    if (useSloMo)
                    {
                        GlobalTimeScaleController.ReturnTimePace(this);
                    }

                    GunState.SetForce(BaseActionTransitionsEnum.ActionToBase, finishAimTime);
                }
                break;
            case BaseActionTransitionsEnum.ActionToBase:
                if (_isAiming)
                {
                    GunState.SetForce(BaseActionTransitionsEnum.BaseToAction, startAimTime, 1-GunState.TimeFraction);
                }
                else
                {   
                    if(GunState.CanBeChanged)
                        GunState.SetForce(BaseActionTransitionsEnum.Base, 0);
                }

                break;
        }


    }
    
    protected virtual void Update()
    {
        if (_healthController.IsDead)
        {
            _isAiming = false;
            _aimAngleIncrease = 0f;
        }
        else
        {
            SetIsAimingAndAimingAngleIncrease(out _isAiming, out _aimAngleIncrease);
        }

        ChangeState();
        
        if (GunState.Value != BaseActionTransitionsEnum.Base && _isAiming)
        {
            if(ShouldInterruptAiming())
                InterruptAiming();
            //if(!_wantedTargetPointWasSetInThisFrame)
            SetWantedTargetPoint(out _wantedTargetPoint);
            CalculateTarget();
        }

        RechargeBullet();
    }
}
