using System;
using CustomAttributes;
using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class BarrelController : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private bool setMaxDistanceToDistanceToTarget = false;
    [SerializeField] private float initialSpeed = 30f;
    [SerializeField] private float acceleration = 0f;
    [SerializeField] private BulletController.CanBeParriedEnum canBeParried = BulletController.CanBeParriedEnum.YesWithReturn;
    [SerializeField] private bool destroyAtInpact = true;
    [Header("Bullet Damages")]
    [SerializeField] private float bulletDamage = 40f;
    [SerializeField] private float bulletPoiseDamage = 40f;
    [SerializeField] private float bulletRechargeOnParryFraction = 0f;
    [Header("Noob mod: damage multiplier")]
    [SerializeField] private BoolSettingValue settingValue;
    [SerializeField] private float noobMultiplier = 2f;
    
    [Header("Spread")]
    [SerializeField] [Range(1f, 5f)] 
    private float centerBias = 2f;
    [SerializeField] [Range(0f, 1f)]
    private float verticalScale = 0.3f;
    [SerializeField] private float shootingConeAngle = 0f;
    
    [Header("Shotgun mode")]
    [SerializeField] private bool useShotgun=false;
    [SerializeField, EnableIf("useShotgun")] 
    private float amountOfBulletsPerShot = 1;

    [Header("Fly outward")]
    [SerializeField] 
    public bool horizontal = false;
    [SerializeField] 
    public bool vertical = false;
    [SerializeField, EnableIf("horizontal || vertical")]
    public bool lookAtMoveDirection = true;
    [Header("Center position")]
    [SerializeField, EnableIf("horizontal")]
    private Vector3 horizontalPosition=new (0f,0f,1f);
    [SerializeField, EnableIf("vertical")]
    private Vector3 verticalPosition=new (0f,0f,1f);
    [Header("Movement Speed")]
    [SerializeField, EnableIf("horizontal")] 
    private bool scaleSpeedHorizontal = true;
    [SerializeField, EnableIf("vertical")] 
    private bool scaleSpeedVertical = true;
    [Header("Scale")] 
    [SerializeField, EnableIf("horizontal")] 
    private bool scaleSizeHorizontal= true;
    [SerializeField, EnableIf("vertical")] 
    private bool scaleSizeVertical= true;

    public bool ScaleSpeedHorizontal => scaleSpeedHorizontal;
    public bool ScaleSpeedVertical => scaleSpeedVertical;

    public bool ScaleSizeHorizontal => scaleSizeHorizontal;
    public bool ScaleSizeVertical => scaleSizeVertical;
    
    public Vector3 HorizontalCenterPosition => _root.TransformPoint(horizontalPosition);
    public Vector3 VerticalCenterPosition => _root.TransformPoint(verticalPosition);

    public Transform RootTransform => _root;
    
    private Vector3 HorizontalCenterPositionGizmos => transform.TransformPoint(horizontalPosition);
    private Vector3 VerticalCenterPositionGizmos => transform.TransformPoint(verticalPosition); 
    
    public float BulletInitialSpeed => initialSpeed;
    public float BulletAcceleration => acceleration;
    
   
    private readonly List<AutoPool<BulletController>> _bulletPools = new();

    private SparkSpawner[] _shootSparkSpawners;

    private IObjectPool<AlreadyHitTargets> _alreadyHitTargetsPool;

    public class AlreadyHitTargets
    {
        private readonly HashSet<HealthController> _set;
        private int _references;
        private readonly IObjectPool<AlreadyHitTargets> _pool;

        public AlreadyHitTargets(IObjectPool<AlreadyHitTargets> pool)
        {
            _pool = pool;
            _set = new();
            _references = 0;
        }

        public bool TryHitTarget(HealthController target)
        {
            return _set.Add(target);
        }
        
        public void RecordProjectile()
        {
            ++_references;
        }

        public void RemoveRecord()
        {
            --_references;
            if (_references <=0)
            {
                _references = 0;
                _set.Clear();
                if(_pool != null)
                    _pool.Release(this);
            }
        }
        
    }
    
    
    private int _destructionLayerMask;
    private int _targetLayerMask;

    private Transform _root;

    private bool _haveNoobMod = false;
    private float BulletDamage => _haveNoobMod && settingValue.value ? bulletDamage * noobMultiplier : bulletDamage;
    
    public void AwakeBarrel(int magazineCapacity, int destructionLayerMask, int targetLayerMask)
    {
        _haveNoobMod = settingValue != null;
        
        _destructionLayerMask = destructionLayerMask;
        _targetLayerMask = targetLayerMask;

        if (!horizontal && !vertical)
            lookAtMoveDirection = false;

        if (!horizontal)
        {
            scaleSpeedHorizontal = false;
            scaleSizeHorizontal = false;
        }

        if (!vertical)
        {
            scaleSpeedVertical = false;
            scaleSizeVertical = false;
        }


        _alreadyHitTargetsPool = new ObjectPool<AlreadyHitTargets>(
            createFunc: () => new AlreadyHitTargets(_alreadyHitTargetsPool),
            collectionCheck: false,
            defaultCapacity: magazineCapacity,
            maxSize: Mathf.Max(10, magazineCapacity * 2)
        );

        
        var allChildren = GetComponentsInChildren<Transform>();

        var shootSparksRoot = UtilityFunctions.MakeEmptyObjectOrphan("Shoot sparks", transform);
        
        var poolsRoot = UtilityFunctions.MakeEmptyObjectOrphan("Pools for " + gameObject.name, transform);

        _root = UtilityFunctions.MakeEmptyObjectOrphan("All projectiles prefabs", transform);
        
        

        var barrelDirHorizontal = transform.position - HorizontalCenterPosition;
        var barrelDirVertical = transform.position - VerticalCenterPosition;
        
        foreach (var child in allChildren)
        {
            if (child.TryGetComponent(out BulletController controller))
            {
                child.SetParent(_root,true);
                controller.AwakeBullet(
                    this, 
                    canBeParried,
                    destroyAtInpact, 
                    in barrelDirHorizontal,
                    in barrelDirVertical, 
                    poolsRoot
                    );
                _bulletPools.Add(new AutoPool<BulletController>(controller, magazineCapacity, Mathf.Max(10, magazineCapacity * 2), poolsRoot));
                child.gameObject.SetActive(false);
            }
        }

        foreach (var spark in poolsRoot.GetComponentsInChildren<SparkSpawner>())
        {
            spark.Initialize(magazineCapacity, Mathf.Max(10, magazineCapacity * 2), poolsRoot);
        }
        
        _shootSparkSpawners = GetComponentsInChildren<SparkSpawner>();

        foreach (var spark in _shootSparkSpawners)
        {
            spark.Initialize(magazineCapacity, Mathf.Max(10, magazineCapacity * 2), poolsRoot);
            spark.transform.SetParent(shootSparksRoot, true);
        }
        
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        
        _root.SetParent(transform, true);
        shootSparksRoot.SetParent(transform,true);  
        
        if (!useShotgun)
        {
            amountOfBulletsPerShot = 1;
        }
    }

    
    public void Shoot(float aimAngleIncrease, Transform baskTarget, ref Vector3 target)
    {
        var dist = setMaxDistanceToDistanceToTarget
            ? Mathf.Min(maxDistance, Vector3.Distance(RootTransform.position, target))
            : maxDistance;        
        for (var i = 0; i < amountOfBulletsPerShot; ++i)
        {
            var alreadyHitTargets = _alreadyHitTargetsPool.Get();
            _root.rotation = GetBulletDirection(aimAngleIncrease);
            foreach (var pool in _bulletPools)
            {
                var bullet = pool.Get();
                bullet.Initialize(
                    this, _destructionLayerMask, _targetLayerMask,
                    BulletDamage, bulletPoiseDamage, bulletRechargeOnParryFraction,
                    dist,initialSpeed,acceleration, 
                    baskTarget, alreadyHitTargets);
            }
            
        }

        foreach (var spark in _shootSparkSpawners)
        {
            spark.SpawnSparks();
        }
    }
    
    private Quaternion GetBulletDirection(float aimAngleIncrease)
    {
        var randomAngle = Random.Range(0f, Mathf.PI * 2f);
        var radius = Mathf.Pow(Random.value, centerBias);
        var localX = Mathf.Cos(randomAngle) * radius;
        var localY = Mathf.Sin(randomAngle) * radius * verticalScale;
        var maxHalfAngle = (shootingConeAngle+aimAngleIncrease) * 0.5f;
        var yaw = localX * maxHalfAngle; 
        var pitch = localY * maxHalfAngle;
        var spreadRotation = Quaternion.Euler(-pitch, yaw, 0f);
        var targetRotation = Quaternion.LookRotation(transform.forward);
        return targetRotation * spreadRotation;
    }

    private void OnDrawGizmosSelected()
    {
        if (horizontal)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(HorizontalCenterPositionGizmos, 0.06f);
        }

        if (vertical)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(VerticalCenterPositionGizmos, 0.04f);
        }
    }
}
