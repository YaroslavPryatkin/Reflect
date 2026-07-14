using CustomAttributes;
using UnityEngine;
using BaseActionTransitionsEnum = Utility.BaseActionTransitionsEnum;
public abstract class GunController : MonoBehaviour
{
    [Header("This")]
    [SerializeField] protected Transform shoulderPoint;
    [SerializeField] private float gunFromShoulderDistance = 0.4f;
    [Header("Bullet")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] protected float bulletSpeed = 10f;
    [SerializeField] private float bulletDamage = 40f;
    [SerializeField] private float bulletPoiseDamage = 40f;
    [Header("Shotgun mode")]
    [SerializeField] private float amountOfBulletsPerShot = 1;
    [SerializeField] private float shootingConeAngle = 0f;
    [SerializeField] [Range(1f, 5f)] private float centerBias = 2f;
    [SerializeField] [Range(0f, 1f)] private float verticalScale = 0.3f;
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
    

    public BaseActionTransitionsEnum GunStateValue => GunState.Value;
    
    public int CurrentAmountOfBullets => virtualCurrentAmountOfBullets + (isRechargingBullet ? 0 : 1);
    public float RechargeFraction => isRechargingBullet ? isRechargingBullet.TimeFraction : 0;
    public bool IsRechargingBullet => isRechargingBullet.Value;

    public float TargetAngle { get; private set; } = 0f;

    public  Utility.FractionBlockingValueTimer<BaseActionTransitionsEnum> GunState { get;  } = BaseActionTransitionsEnum.Base;
    
    protected Sensors _sensors;


    private Utility.FractionTemporaryValue<bool> isRechargingBullet = new(false, true);
    private Utility.FractionTemporaryValue<bool> canShootAfterPreviouseShot = new(true, false);
    private bool hasChangedTime = false;
    
    private int virtualMagazineCapacity;
    private int virtualCurrentAmountOfBullets = -1;
    
    protected int layerMask;
    
    protected bool isAiming  = false;
    public Vector3 WantedTargetPoint { get; protected set; } = Vector3.zero;
    
    public  Vector3 TargetPoint { get; private set; } = Vector3.zero;
    public Vector3 GunPosition { get; private set; } = Vector3.zero;
    public Vector3 TargetDirection { get; private set; } = Vector3.zero;
    
    public Vector3 TargetHeadDirection { get; private set; } = Vector3.zero;

    protected virtual void Awake()
    {
        if (!TryGetComponent(out _sensors))
        {
            Debug.Log("No Sensors found!");
        }
        
        layerMask = _sensors.IgnoreMyLayerMask;
        virtualMagazineCapacity = magazineCapacity - 1;
        if (leftGunHorizontalAngle > rightGunHorizontalAngle)
        {
            Debug.Log("Left gun horizontal angle is greater than right");
        }
    }

    protected abstract void SetWantedTargetPoint();
    protected abstract void ChangeIsAiming();
    protected abstract bool ShouldInterruptAiming();
    
    private void CalculateTarget()
    {
        var shoulderPos = shoulderPoint.position;
        var wantedDirection = (WantedTargetPoint - shoulderPos).normalized;
        
        var localDir = transform.InverseTransformDirection(wantedDirection);
        var horizontalDistance = Mathf.Sqrt(localDir.x * localDir.x + localDir.z * localDir.z);
        var angleYRad = Mathf.Atan2(localDir.x, localDir.z);

        var armAngle = Mathf.Clamp(angleYRad, leftArmHorizontalAngle * Mathf.Deg2Rad, rightArmHorizontalAngle * Mathf.Deg2Rad);
        
        
        
        localDir.x = horizontalDistance * Mathf.Sin(armAngle);
        localDir.z = horizontalDistance * Mathf.Cos(armAngle);
        GunPosition = shoulderPos + transform.TransformDirection(localDir) * gunFromShoulderDistance;
        
        TargetAngle = Mathf.Clamp(angleYRad, leftGunHorizontalAngle * Mathf.Deg2Rad, rightGunHorizontalAngle * Mathf.Deg2Rad);
        localDir.x = horizontalDistance * Mathf.Sin(TargetAngle);
        localDir.z = horizontalDistance * Mathf.Cos(TargetAngle);
        TargetDirection = transform.TransformDirection(localDir).normalized;
        
        var headAngle = Mathf.Clamp(TargetAngle, -headHorizontalAngle * Mathf.Deg2Rad, headHorizontalAngle * Mathf.Deg2Rad);
        localDir.x = horizontalDistance * Mathf.Sin(headAngle);
        localDir.z = horizontalDistance * Mathf.Cos(headAngle);
        TargetHeadDirection = transform.TransformDirection(localDir).normalized;

        
        if (Physics.Raycast(GunPosition, TargetDirection, out var hit2, targetDistanceIfNotFound, layerMask))
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
        if (GunStateValue == BaseActionTransitionsEnum.Action && canShootAfterPreviouseShot && CurrentAmountOfBullets > 0)
        {
            canShootAfterPreviouseShot.Activate(timeBetweenShots);
            SpendBullet();
            for (var i = 0; i < amountOfBulletsPerShot; ++i)
            {
                var bullet = Instantiate(bulletPrefab, GunPosition, GetRandomShotgunDirection());

                if (bullet.TryGetComponent(out BulletController bulletScript))
                {
                    bulletScript.Initialize(bulletSpeed, layerMask, _sensors.EnemyLayer,
                        bulletDamage, bulletPoiseDamage, transform);
                }
            }

            return 1;
        }

        return 0;
    }
    
    public Quaternion GetRandomShotgunDirection()
    {
        var randomAngle = Random.Range(0f, Mathf.PI * 2f);
        var radius = Mathf.Pow(Random.value, centerBias);
        var localX = Mathf.Cos(randomAngle) * radius;
        var localY = Mathf.Sin(randomAngle) * radius * verticalScale;
        var maxHalfAngle = shootingConeAngle * 0.5f;
        var yaw = localX * maxHalfAngle; 
        var pitch = localY * maxHalfAngle;
        var spreadRotation = Quaternion.Euler(-pitch, yaw, 0f);
        var targetRotation = Quaternion.LookRotation(TargetDirection);
        return targetRotation * spreadRotation;
    }

    private void SpendBullet()
    {
        if (isRechargingBullet)
        {
            virtualCurrentAmountOfBullets--;
        }
        isRechargingBullet.Activate(rechargeTime);
    }
    
    private void RechargeBullet()
    {
        if (virtualCurrentAmountOfBullets < virtualMagazineCapacity && !isRechargingBullet)
        {
            isRechargingBullet.Activate(rechargeTime);
            virtualCurrentAmountOfBullets++;
        }
    }

    private void InterruptAiming()
    {
        if(GunState.Value!=BaseActionTransitionsEnum.Base)
            GunState.SetForce(BaseActionTransitionsEnum.Base, 0);
        if (hasChangedTime && useSloMo)
        {
            GlobalTimeScaleController.ReturnTimePace(this);
            hasChangedTime = false;
        }
    }

    private void ChangeState()
    {
        switch (GunState.Value)
        {
            case BaseActionTransitionsEnum.Base:
                if (isAiming)
                    GunState.SetForce(BaseActionTransitionsEnum.BaseToAction, startAimTime);
                break;
            case BaseActionTransitionsEnum.BaseToAction:
                if (isAiming)
                {
                    if (!hasChangedTime && useSloMo)
                    {
                        GlobalTimeScaleController.ChangeTimePace(this, sloMoCoefficient);
                        hasChangedTime = true;
                    }
                    if(GunState.CanBeChanged)
                        GunState.SetForce(BaseActionTransitionsEnum.Action, 0);
                }
                else
                {
                    GunState.SetForce(BaseActionTransitionsEnum.ActionToBase, finishAimTime, 1-GunState.TimeFraction);
                }

                break;
            case BaseActionTransitionsEnum.Action:
                if(!isAiming)
                {
                    if (hasChangedTime && useSloMo)
                    {
                        GlobalTimeScaleController.ReturnTimePace(this);
                        hasChangedTime = false;
                    }

                    GunState.SetForce(BaseActionTransitionsEnum.ActionToBase, finishAimTime);
                }
                break;
            case BaseActionTransitionsEnum.ActionToBase:
                if (isAiming)
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
        ChangeIsAiming();
        
        ChangeState();
        
        if (GunState.Value != BaseActionTransitionsEnum.Base && isAiming)
        {
            if(ShouldInterruptAiming())
                InterruptAiming();
            SetWantedTargetPoint();
            CalculateTarget();
        }

        RechargeBullet();
    }

}
