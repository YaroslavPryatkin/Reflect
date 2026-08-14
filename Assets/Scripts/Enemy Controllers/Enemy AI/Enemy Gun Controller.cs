using CustomAttributes;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyGunController : GunController
{

    [Header("Conditions to open fire")] 
    [SerializeField] private int minimalCurrentBulletsToOpenFire = 1;
    [SerializeField] private float angleLeftToOpenFire = -45;
    [SerializeField] private float angleRightToOpenFire = 45;

    
    [Header("Getting hit")] 
    [SerializeField] private bool interruptTelegraphingWhenStunned = true;
    
    [Header("Telegraph")]
    [SerializeField] private float telegraphTime = 1f;

    [Header("Bursts")] 
    [SerializeField] private bool shootInBursts = false;
    [SerializeField, EnableIf("shootInBursts")] private int bulletsInOneBurst = 1;
    [SerializeField, EnableIf("shootInBursts")] private float timeBetweenBursts = 0.3f;
    [Header("Auto aim")]
    [SerializeField] private bool autoAim = true;
    [SerializeField] private float playerMaxSpeedToAutoAim = 9f;


    private GettingHitController _gettingHitController;
    
    private enum FireStateEnum{Non, Telegraph, Shooting}
    private readonly UtilityClasses.FractionBlockingValueTimer<FireStateEnum> _fireState = FireStateEnum.Non;
    private int _bulletsShotInThisBurst = 0;
    
    public bool IsStateNon => _fireState.Value == FireStateEnum.Non;
    public bool IsTelegraphingAttack => _fireState.Value ==  FireStateEnum.Telegraph;
    public float TelegraphFraction => _fireState.TimeFraction;

    private float _openFireLeftRad;
    private float _openFireRightRad;

    private int _interruptAimingCounter = 0;
    
    public void InterruptAiming()
    {
        ++_interruptAimingCounter;
    }

    public void StopInterruptingAiming()
    {
        --_interruptAimingCounter;
    }


    private readonly UtilityClasses.TemporaryValue<bool> _useFastTelegraphTime = new(false, true);
    public bool IsUsingFastTelegraphTime => _useFastTelegraphTime.Value;
    private float _fastTelegraphTime;
    private float _fastTelegraphAngleIncrease;

    public void UseFastTelegraphTime(float duration, float telegraphTime,  float angleIncrease)
    {
        _fastTelegraphAngleIncrease = angleIncrease;
        _useFastTelegraphTime.Activate(duration);
        _fastTelegraphTime=telegraphTime;
        _fireState.SetForce(FireStateEnum.Non);
        SetBulletsAtLeast(minimalCurrentBulletsToOpenFire);
    }

    private EnemySensors _enemySensors;
    private EnemyAI _enemyAI;
    
    protected override void Awake()
    {
        base.Awake();
        _enemySensors = (EnemySensors) Sensors;
        _enemyAI = GetComponent<EnemyAI>();
        _openFireLeftRad = angleLeftToOpenFire * Mathf.Deg2Rad;
        _openFireRightRad = angleRightToOpenFire * Mathf.Deg2Rad;
        if(interruptTelegraphingWhenStunned)
            _gettingHitController = GetComponent<GettingHitController>();
    }

    // protected override void SetWantedTargetPoint()
    // {
    //     if (!autoAim)
    //     {
    //         WantedTargetPoint = _enemySensors.PlayerPosition;
    //         return;
    //     }
    //
    //
    //     var playerPos = _enemySensors.PlayerPosition;
    //     var rawVelocity = _enemySensors.PlayerVelocity;
    //
    //     var clampedVelocity = Vector3.ClampMagnitude(rawVelocity, playerMaxSpeedToAutoAim);
    //
    //     var distanceVector = playerPos - GunPosition;
    //
    //     var a = clampedVelocity.sqrMagnitude - _sqrBulletSpeed;
    //     var b = 2f * Vector3.Dot(distanceVector, clampedVelocity);
    //     var c = distanceVector.sqrMagnitude;
    //     var discriminant = b * b - 4f * a * c;
    //
    //     if (discriminant < 0f)
    //     {
    //         WantedTargetPoint = playerPos;
    //         return;
    //     }
    //
    //     var sqrtDiscriminant = Mathf.Sqrt(discriminant);
    //     var t1 = (-b - sqrtDiscriminant) / (2f * a);
    //     var t2 = (-b + sqrtDiscriminant) / (2f * a);
    //
    //     var timeToIntercept = 0f;
    //
    //     if (t1 > 0f && t2 > 0f)
    //         timeToIntercept = Mathf.Min(t1, t2);
    //     else if (t1 > 0f)
    //         timeToIntercept = t1;
    //     else if (t2 > 0f)
    //         timeToIntercept = t2;
    //     else
    //     {
    //         WantedTargetPoint = playerPos;
    //         return;
    //     }
    //
    //     WantedTargetPoint = playerPos + (clampedVelocity * timeToIntercept);
    //     
    // }
    
    
    protected override void SetWantedTargetPoint(out Vector3 wantedTargetPoint)
    {
        if (!autoAim)
        {
            wantedTargetPoint = _enemySensors.PlayerPosition;
            return; 
        }

        var playerPos = _enemySensors.PlayerPosition;
        var rawVelocity = _enemySensors.PlayerVelocity;
        var clampedVelocity = Vector3.ClampMagnitude(rawVelocity, playerMaxSpeedToAutoAim);
        var distanceVector = playerPos - GunPosition;

        var v0 = barrelController.BulletInitialSpeed;
        var a = barrelController.BulletAcceleration;

        // C4*t^4 + C3*t^3 + C2*t^2 + C1*t + C0 = 0
        var c4 = 0.25f * a * a;
        var c3 = v0 * a;
        var c2 = (v0 * v0) - clampedVelocity.sqrMagnitude;
        var c1 = -2f * Vector3.Dot(distanceVector, clampedVelocity);
        var c0 = -distanceVector.sqrMagnitude;

        var t = distanceVector.magnitude / Mathf.Max(v0, 0.1f);

        for (var i = 0; i < 5; i++)
        {
            var f = c4 * (t * t * t * t) + c3 * (t * t * t) + c2 * (t * t) + c1 * t + c0;
            
            var df = 4f * c4 * (t * t * t) + 3f * c3 * (t * t) + 2f * c2 * t + c1;

            if (Mathf.Abs(df) < 0.0001f)
                break;

            t = t - (f / df);

            if (t < 0f)
                t = 0f;
        }

        if (t <= 0.05f || float.IsNaN(t))
        {
            wantedTargetPoint = playerPos;
            return;
        }

        wantedTargetPoint = playerPos + (clampedVelocity * t);
    }

    protected override void SetIsAimingAndAimingAngleIncrease(out bool isAiming, out float aimAngleIncrease)
    {
        isAiming = _enemyAI.IsAiming;
        
        aimAngleIncrease = _useFastTelegraphTime ?_fastTelegraphAngleIncrease : _enemyAI.ShootingConeAngleIncrease;
    }

    protected override bool ShouldInterruptAiming()
    {
        return _interruptAimingCounter>0;
    }

    protected override void Update()
    {
        base.Update();
        
        switch (_fireState.Value)
        {
            case FireStateEnum.Non:
                if (_fireState.CanBeChanged && CanOpenFire() && _enemyAI.IsAiming)
                {
                    if (_useFastTelegraphTime)
                    {
                        _fireState.SetForce(FireStateEnum.Telegraph, _fastTelegraphTime);
                    }
                    else{
                        _fireState.SetForce(FireStateEnum.Telegraph, telegraphTime);
                    }
                }

                break;
            case FireStateEnum.Telegraph:
                if (!CanOpenFire() || !_enemyAI.IsAiming)
                {
                    _fireState.SetForce(FireStateEnum.Non, 0);
                }
                else if (_fireState.CanBeChanged)
                {
                    _bulletsShotInThisBurst = 0;
                    _fireState.SetForce(FireStateEnum.Shooting, 0);
                }
                break;
            case FireStateEnum.Shooting:
                
                if (shootInBursts)
                {
                    _bulletsShotInThisBurst += Shoot();
                    if (_bulletsShotInThisBurst >= bulletsInOneBurst || CurrentAmountOfBullets <= 0 || !_enemyAI.IsAiming)
                    {
                        _useFastTelegraphTime.Deactivate();
                        _fireState.SetForce(FireStateEnum.Non, timeBetweenBursts);
                    }
                }
                else
                {
                    if (Shoot() > 0)
                    {
                        _useFastTelegraphTime.Deactivate();
                        _fireState.SetForce(FireStateEnum.Non, 0f);
                    }
                }
                break;
        }
        
    }

    private bool CanOpenFire()
    {
        var res = CanShootAfterPreviousShot &&
                  (!interruptTelegraphingWhenStunned || !_gettingHitController.IsActivelyStunned) &&
                  RawTargetAngle >= _openFireLeftRad && 
                  RawTargetAngle <= _openFireRightRad && 
                  CurrentAmountOfBullets >= minimalCurrentBulletsToOpenFire;
        //Debug.Log("Can start burst = " + res + ", target angle = " + TargetAngle);
        return res;
    }
    
}
