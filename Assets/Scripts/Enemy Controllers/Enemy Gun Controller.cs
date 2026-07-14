using Unity.VisualScripting;
using UnityEngine;

public class EnemyGunController : GunController
{

    [Header("Conditions to open fire")] 
    [SerializeField] private float shootingDistance=20f;
    [SerializeField] private int minimalCurrentBulletsToOpenFire = 1;
    [SerializeField] private float angleLeftToOpenFire = -45;
    [SerializeField] private float angleRightToOpenFire = 45;
    
    [Header("Bursts")]
    [SerializeField] private int bulletsInOneBurst = 1;
    [SerializeField] private float timeBetweenBursts = 0.3f;
    [SerializeField] private float burstTelegraphTime = 0.3f;
    
    [Header("Auto aim")]
    [SerializeField] private bool autoAim = true;
    [SerializeField] private float playerMaxSpeedToAutoAim = 9f;
    
    
    private enum FireStateEnum{Non, Telegraph, Burst}
    private Utility.FractionBlockingValueTimer<FireStateEnum> fireState = FireStateEnum.Non;
    private int bulletsShotInThisBurst = 0;
    
    public bool IsTelegraphingAttack => fireState.Value ==  FireStateEnum.Telegraph;
    public float TelegraphFraction => fireState.TimeFraction;
    
    private EnemySensors _enemySensors;
    
    protected override void Awake()
    {
        base.Awake();
        _enemySensors = (EnemySensors) _sensors;
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

    protected override void ChangeIsAiming()
    {
        isAiming = _enemySensors.IsSeeingPlayer && Vector3.Distance(transform.position, _enemySensors.PlayerPosition) <= shootingDistance;
    }

    protected override bool ShouldInterruptAiming()
    {
        return false;
    }

    protected override void Update()
    {
        base.Update();
        
        switch (fireState.Value)
        {
            case FireStateEnum.Non:
                if(fireState.CanBeChanged && CanStartBurst() && isAiming)
                    fireState.SetForce(FireStateEnum.Telegraph, burstTelegraphTime);
                break;
            case FireStateEnum.Telegraph:
                if (!CanStartBurst()  || !isAiming)
                    fireState.SetForce(FireStateEnum.Non, 0);
                
                
                if (fireState.CanBeChanged)
                {
                    bulletsShotInThisBurst = 0;
                    fireState.SetForce(FireStateEnum.Burst, 0);
                }
                break;
            case FireStateEnum.Burst:
                bulletsShotInThisBurst += Shoot();
                if(bulletsShotInThisBurst >= bulletsInOneBurst || CurrentAmountOfBullets <=0 || !isAiming)
                    fireState.SetForce(FireStateEnum.Non, timeBetweenBursts);
                break;
        }
        
    }

    private bool CanStartBurst()
    {
        return TargetAngle >= angleLeftToOpenFire && 
               TargetAngle <= angleRightToOpenFire && 
               CurrentAmountOfBullets >= minimalCurrentBulletsToOpenFire;
    }
    
}
