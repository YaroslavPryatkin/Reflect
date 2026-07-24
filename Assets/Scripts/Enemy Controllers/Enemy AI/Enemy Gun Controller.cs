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
    [SerializeField] private bool interruptAimingWhenStunned = true;
    
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
    private UtilityClasses.FractionBlockingValueTimer<FireStateEnum> fireState = FireStateEnum.Non;
    private int bulletsShotInThisBurst = 0;
    
    public bool IsTelegraphingAttack => fireState.Value ==  FireStateEnum.Telegraph;
    public float TelegraphFraction => fireState.TimeFraction;

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


    private UtilityClasses.TemporaryValue<bool> _useFastTelegraphTime = new(false, true);
    private float _fastTelegraphTime;
    private float _fastTelegraphAngleIncrease;

    public void UseFastTelegraphTime(float duration, float telegraphTime,  float angleIncrease)
    {
        _fastTelegraphAngleIncrease = angleIncrease;
        _useFastTelegraphTime.Activate(duration);
        _fastTelegraphTime=telegraphTime;
        fireState.SetForce(FireStateEnum.Non);
        GetImmediatelyReadyToShot(minimalCurrentBulletsToOpenFire);
    }

    private EnemySensors _enemySensors;
    private EnemyAI _enemyAI;
    
    protected override void Awake()
    {
        base.Awake();
        _enemySensors = (EnemySensors) _sensors;
        _enemyAI = GetComponent<EnemyAI>();
        _openFireLeftRad = angleLeftToOpenFire * Mathf.Deg2Rad;
        _openFireRightRad = angleRightToOpenFire * Mathf.Deg2Rad;
        if(interruptAimingWhenStunned)
            _gettingHitController = GetComponent<GettingHitController>();
    }

    protected override void SetWantedTargetPoint()
    {
        if (!autoAim)
        {
            WantedTargetPoint = _enemySensors.PlayerPosition;
        }


        Vector3 playerPos = _enemySensors.PlayerPosition;
        Vector3 rawVelocity = _enemySensors.PlayerVelocity;

        Vector3 clampedVelocity = Vector3.ClampMagnitude(rawVelocity, playerMaxSpeedToAutoAim);

        Vector3 distanceVector = playerPos - GunPosition;

        float a = clampedVelocity.sqrMagnitude - (bulletSpeed * bulletSpeed);
        float b = 2f * Vector3.Dot(distanceVector, clampedVelocity);
        float c = distanceVector.sqrMagnitude;
        float discriminant = b * b - 4f * a * c;

        if (discriminant < 0f)
        {
            WantedTargetPoint = playerPos;
            return;
        }

        float sqrtDiscriminant = Mathf.Sqrt(discriminant);
        float t1 = (-b - sqrtDiscriminant) / (2f * a);
        float t2 = (-b + sqrtDiscriminant) / (2f * a);

        float timeToIntercept = 0f;

        if (t1 > 0f && t2 > 0f)
            timeToIntercept = Mathf.Min(t1, t2);
        else if (t1 > 0f)
            timeToIntercept = t1;
        else if (t2 > 0f)
            timeToIntercept = t2;
        else
        {
            WantedTargetPoint = playerPos;
            return;
        }

        WantedTargetPoint = playerPos + (clampedVelocity * timeToIntercept);
        
    }

    protected override void ChangeIsAimingAndAimingAngleIncrease()
    {
        IsAiming = _enemyAI.IsAiming;
        
        AimAngleIncrease = _useFastTelegraphTime ?_fastTelegraphAngleIncrease : _enemyAI.ShootingConeAngleIncrease;
    }

    protected override bool ShouldInterruptAiming()
    {
        return _interruptAimingCounter>0;
    }

    protected override void Update()
    {
        base.Update();
        
        switch (fireState.Value)
        {
            case FireStateEnum.Non:
                if (fireState.CanBeChanged && CanOpenFire() && IsAiming)
                {
                    if (_useFastTelegraphTime)
                    {
                        fireState.SetForce(FireStateEnum.Telegraph, _fastTelegraphTime);
                    }
                    else{
                        fireState.SetForce(FireStateEnum.Telegraph, telegraphTime);
                    }
                }

                break;
            case FireStateEnum.Telegraph:
                if (!CanOpenFire() || !IsAiming)
                {
                    fireState.SetForce(FireStateEnum.Non, 0);
                }
                else if (fireState.CanBeChanged)
                {
                    bulletsShotInThisBurst = 0;
                    fireState.SetForce(FireStateEnum.Shooting, 0);
                }
                break;
            case FireStateEnum.Shooting:
                
                if (shootInBursts)
                {
                    bulletsShotInThisBurst += Shoot();
                    if (bulletsShotInThisBurst >= bulletsInOneBurst || CurrentAmountOfBullets <= 0 || !IsAiming)
                        fireState.SetForce(FireStateEnum.Non, timeBetweenBursts);
                }
                else
                {
                    if(Shoot()>0)
                        fireState.SetForce(FireStateEnum.Non, 0f);
                }
                break;
        }
        
    }

    private bool CanOpenFire()
    {
        var res = CanShootAfterPreviousShot &&
                  (!interruptAimingWhenStunned || !_gettingHitController.IsActivelyStunned) &&
                  RawTargetAngle >= _openFireLeftRad && 
                  RawTargetAngle <= _openFireRightRad && 
                  CurrentAmountOfBullets >= minimalCurrentBulletsToOpenFire;
        //Debug.Log("Can start burst = " + res + ", target angle = " + TargetAngle);
        return res;
    }
    
}
