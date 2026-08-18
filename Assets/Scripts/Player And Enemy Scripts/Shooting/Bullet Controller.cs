using CustomAttributes;
using UnityEngine;

public class BulletController : Poolable<BulletController>
{
    [SerializeField] private bool useBoxCast = false;
    [SerializeField, EnableIf("useBoxCast")] private Vector2 halfBoxSizes;
    [SerializeField] private bool useExplosiveArea = false;
    [SerializeField, EnableIf("useExplosiveArea")]
    private float radius = 0f;
    
    [SerializeField, HideInInspector] private Vector3 startScale;
    [SerializeField, HideInInspector] private Vector3 scaleAtUnitDistance;
    [SerializeField, HideInInspector] private Vector2 boxSizesAtUnitDistance;
    [SerializeField, HideInInspector] private bool hasOffsetHorizontal=false;
    [SerializeField, HideInInspector] private bool hasOffsetVertical=false;
    [SerializeField, HideInInspector] private float speedMultiplier;
    [SerializeField, HideInInspector] private CanBeParriedEnum canBeParried;
    [SerializeField, HideInInspector] private bool destroyAtInpact;
    [SerializeField, HideInInspector] private bool increaseScaleWithDistance;
    [SerializeField, HideInInspector] private float rateOverTimeAtUnitDistanceMultiplier;
    
    private bool _initialized = false;
    
    private Vector3 _moveDirection;

    public enum CanBeParriedEnum
    {
        No, YesWithReturn, YesWithoutReturn
    }
    
    
    private float _damage;
    private float _poiseDamage;
    private float _bulletRechargeOnParryFraction;
    private float _maxDistance;
    private float _speed;
    private float _acceleration;
    
    private int _destructionLayerMask;
    private int _enemyLayerMask;
    
    private float _traveledDistance;

    private Transform _backTarget;
    
    private BarrelController.AlreadyHitTargets _alreadyHitTargets;

    private bool _haveParticles = false;
    private ParticleSystem _particles;
    private float _startingRateOverTime;

    [SerializeField, HideInInspector] private SparkSpawner[] hitSparks;
    
    private void Awake()
    {
        _particles = GetComponentInChildren<ParticleSystem>();
        _haveParticles = _particles != null;
        if (_haveParticles)
        {
            _startingRateOverTime = _particles.emission.rateOverTimeMultiplier;
        }
    }
    public void AwakeBullet(
        BarrelController barrel, 
        CanBeParriedEnum canBeParried, 
        bool destroyAtInpact,
        in Vector3 barrelDirHorizontal, 
        in Vector3 barrelDirVertical,
        Transform hitSparkRoot
        )
    {

        hitSparks = GetComponentsInChildren<SparkSpawner>();
        foreach (var spark in hitSparks)
        {
            spark.transform.SetParent(hitSparkRoot, true);
        }
        
        this.canBeParried = canBeParried;
        this.destroyAtInpact = destroyAtInpact;

        var dirHorizontal = barrel.RootTransform.InverseTransformDirection(transform.position-barrel.HorizontalCenterPosition);
        var dirVertical = barrel.RootTransform.InverseTransformDirection(transform.position-barrel.VerticalCenterPosition);
        hasOffsetHorizontal = Mathf.Abs(dirHorizontal.x) > 0.001f && Mathf.Abs(dirHorizontal.z) > 0.001f;
        hasOffsetVertical = Mathf.Abs(dirVertical.y) > 0.001f && Mathf.Abs(dirVertical.z) > 0.001f;
        
        CalculateMoveDirection(barrel);
        
        var distHorizontal = DistanceToPlaneByLine(
            transform.position, _moveDirection, 
            barrel.HorizontalCenterPosition, 
            hasOffsetHorizontal ? barrel.RootTransform.right : barrel.RootTransform.forward);
        var distVertical = DistanceToPlaneByLine(
            transform.position, _moveDirection,
            barrel.VerticalCenterPosition,
            hasOffsetVertical ? barrel.RootTransform.up : barrel.RootTransform.forward);
        
        
        var barrelMoveDir = barrel.RootTransform.InverseTransformDirection(_moveDirection);
        var baseMagnitude = barrelMoveDir.magnitude;
        // barrelMoveDir.x *= hasOffsetHorizontal && barrel.ScaleSpeedHorizontal ? 0f : distHorizontal / dirHorizontal.magnitude;
        // barrelMoveDir.y *= hasOffsetVertical && barrel.ScaleSpeedVertical ? 0f : distVertical / dirVertical.magnitude;
        barrelMoveDir.x *= hasOffsetHorizontal && barrel.ScaleSpeedHorizontal ? 0f : 1f;
        barrelMoveDir.y *= hasOffsetVertical && barrel.ScaleSpeedVertical ? 0f : 1f;
        speedMultiplier = Mathf.Abs(baseMagnitude / barrelMoveDir.magnitude);
        
        increaseScaleWithDistance = barrel.ScaleSizeHorizontal || 
                                    barrel.ScaleSizeVertical;
        
        if (increaseScaleWithDistance)
        {
            var scaleMultiplierX = 1f;
            var scaleMultiplierY = 1f;
            if (barrel.ScaleSizeHorizontal)
            {
                var dist = barrel.ScaleSpeedHorizontal ? distHorizontal : barrelDirHorizontal.magnitude;
                scaleMultiplierX = (1f + dist) / dist;
            }

            if (barrel.ScaleSizeVertical)
            {
                var dist = barrel.ScaleSpeedVertical  ? distVertical : barrelDirVertical.magnitude;
                scaleMultiplierY = (1f + dist) / dist;
            }

            if (_haveParticles)
            {
                rateOverTimeAtUnitDistanceMultiplier = scaleMultiplierX * scaleMultiplierY;
            }
            
            startScale = transform.localScale;
            scaleAtUnitDistance = startScale;
            scaleAtUnitDistance.x *= scaleMultiplierX;
            scaleAtUnitDistance.y *= scaleMultiplierY;
            if (useBoxCast)
            {
                boxSizesAtUnitDistance = halfBoxSizes;
                boxSizesAtUnitDistance.x *= scaleMultiplierX;
                boxSizesAtUnitDistance.y *= scaleMultiplierY;
            }
        }
    }

    private float DistanceToPlaneByLine(Vector3 point, Vector3 lineDirection, Vector3 planePoint, Vector3 planeNormal)
    {
        var denominator = Vector3.Dot(lineDirection, planeNormal);
        var originToPoint = point - planePoint;
        var numerator = Vector3.Dot(originToPoint, planeNormal);
        return Mathf.Abs(numerator / denominator);
    }
    
    
    private void CalculateMoveDirection(BarrelController barrel, bool changeTransformDirection=false)
    {
        _moveDirection = Vector3.forward;
        var _lookDirection = Vector3.forward;
        if (hasOffsetHorizontal && barrel.horizontal)
        {
            var targetDir = (transform.position - barrel.HorizontalCenterPosition).normalized;
            targetDir = transform.InverseTransformDirection(targetDir);
            _moveDirection.x = targetDir.x / targetDir.z;
            if (!barrel.ScaleSpeedHorizontal)
            {
                _lookDirection.x = _moveDirection.x;
            }
        }
        if (hasOffsetVertical && barrel.vertical)
        {
            var targetDir = (transform.position - barrel.VerticalCenterPosition).normalized;
            targetDir = transform.InverseTransformDirection(targetDir);
            _moveDirection.y = targetDir.y / targetDir.z;
            if (!barrel.ScaleSpeedVertical)
            {
                _lookDirection.y = _moveDirection.y;
            }
        }
        _moveDirection = _moveDirection.normalized;
        _moveDirection = transform.TransformDirection(_moveDirection);
        if (changeTransformDirection)
        {
            _lookDirection = transform.TransformDirection(_lookDirection);
            transform.rotation = Quaternion.LookRotation(_lookDirection, barrel.RootTransform.up);
        }
        // var tmp = Quaternion.Euler(targetPitch, targetYaw, 0f) * Vector3.forward;
        // Debug.Log(targetPitch + ", "+targetYaw + " => "+tmp.y/tmp.z);
        // _moveDirection = transform.TransformDirection(Quaternion.Euler(targetPitch, targetYaw, 0f) * Vector3.forward);

    }
    
    public void Initialize(BarrelController barrel, 
        int destructionLayerMask, int targetLayerMask, 
        float damage, float poiseDamage, float bulletRechargeOnParryFraction,
        float maxDistance, float speed, float acceleration,
        Transform backTarget, BarrelController.AlreadyHitTargets alreadyHitTargets)
    {
        _initialized = true;

        _alreadyHitTargets = alreadyHitTargets;
        _alreadyHitTargets.RecordProjectile();
        
        CalculateMoveDirection(barrel, barrel.lookAtMoveDirection);
        
        _destructionLayerMask = destructionLayerMask | targetLayerMask;
        _enemyLayerMask = targetLayerMask;
        _damage = damage;
        _backTarget = backTarget;
        _poiseDamage = poiseDamage;
        _bulletRechargeOnParryFraction = bulletRechargeOnParryFraction;
        _traveledDistance = 0f;
        _speed = speed * speedMultiplier;
        _acceleration = acceleration * speedMultiplier;
        _maxDistance = maxDistance * speedMultiplier;
        
        if (increaseScaleWithDistance)
        {
            transform.localScale = 
                Vector3.Lerp(startScale, scaleAtUnitDistance, _traveledDistance);
        }
        
        var candidates = Physics.OverlapSphere(transform.position, 0.1f,  _destructionLayerMask, Sensors.QueryTriggerInteractionShooting);
        foreach (var c in candidates)
        {
            if (CheckCollision(c.gameObject))
            {
                
                SpawnSparks(c.transform.position);
                DoExplosiveDamage(c.transform.position);
                ReturnToPool();
                return;
            }
        }
    }
    
    private bool CheckCollision(GameObject obj)
    {
        if ((_enemyLayerMask & (1 << obj.layer)) != 0)
        {
            if (obj.TryGetComponent<HealthController>(out var healthController))
            {
                if (canBeParried != CanBeParriedEnum.No && healthController.TryDeflecting(transform.forward))
                {
                    if(_alreadyHitTargets.TryHitTarget(healthController))
                        healthController.DoDeflectDamage(true, _damage, _poiseDamage,_bulletRechargeOnParryFraction, HealthController.DamageDealer.Bullet);
                   
                    if (canBeParried == CanBeParriedEnum.YesWithReturn)
                    {
                        healthController.GetNewEnemyLayerMask(out _destructionLayerMask, out _enemyLayerMask);
                        healthController.GetNewBulletDamage(ref _damage);
                        _destructionLayerMask |= _enemyLayerMask;
                        transform.LookAt(_backTarget);
                        _moveDirection = _backTarget.position - transform.position;
                        _moveDirection = _moveDirection.normalized;
                        _backTarget = healthController.GetBackTarget();

                        if (!increaseScaleWithDistance)
                            _traveledDistance = 0f;
                        return false;
                    }
                }
                else
                {
                    if (_alreadyHitTargets.TryHitTarget(healthController))
                    {
                        
                        healthController.DoNormalDamage(true, _damage, _poiseDamage, HealthController.DamageDealer.Bullet);
                    }
                }
            }
            else
            {
                Debug.Log("No health controller on target object " + obj.name);
            }
        }

        
        return destroyAtInpact;
    }
    
    
    private readonly Collider[] _castResults = new Collider[20];

    private bool CheckBox(in Vector3 halfSizes)
    {
        var res = Physics.OverlapBoxNonAlloc(transform.position, halfSizes, _castResults, transform.rotation,
            _destructionLayerMask, Sensors.QueryTriggerInteractionShooting);

        var shouldDestroy = false;
        var minDist = 0f;
        var pos = Vector3.zero;
        while (res-- > 0)
        {
            var obj = _castResults[res].gameObject;
            if (CheckCollision(obj))
            {
                if (!shouldDestroy)
                {
                    shouldDestroy = true;
                    minDist = Vector3.Distance(obj.transform.position, transform.position);
                    pos =  obj.transform.position;
                }
                else
                {
                    var dist = Vector3.Distance(obj.transform.position, transform.position);
                    if (dist < minDist)
                    {
                        pos =  obj.transform.position;
                        minDist = dist;
                    }
                }
            }
        }

        if (shouldDestroy)
        {
            SpawnSparks(in pos);
            DoExplosiveDamage(in pos);
            ReturnToPool();
            return true;
        }
        return false;
    }
    
    private void Update()
    {
        if (!_initialized) return;
        
        
        var step = _speed * Time.deltaTime;
        _traveledDistance += step;

        if (useBoxCast)
        {
            if (increaseScaleWithDistance)
            {
                var curHalf = Vector3.LerpUnclamped(halfBoxSizes, boxSizesAtUnitDistance, _traveledDistance);
                curHalf.z = step;
                if (CheckBox(curHalf)) return;
            }
            else
            {
                Vector3 curHalf = halfBoxSizes;
                curHalf.z = step;
                if (CheckBox(curHalf)) return;
            }
        }
        else
        {
            if (Physics.Raycast(transform.position, _moveDirection, out var hit, step, _destructionLayerMask, Sensors.QueryTriggerInteractionShooting))
            {
                var obj = hit.collider.gameObject;
                if (CheckCollision(obj))
                {
                    SpawnSparks(hit.point);
                    DoExplosiveDamage(hit.point);
                    ReturnToPool();
                    return;
                }
            }
        }


        if (increaseScaleWithDistance)
        {
            transform.localScale = 
                Vector3.LerpUnclamped(startScale, scaleAtUnitDistance, _traveledDistance);
            if (_haveParticles)
            {
                var emission =_particles.emission;
                var res = _startingRateOverTime * Mathf.LerpUnclamped(1f,
                    rateOverTimeAtUnitDistanceMultiplier, _traveledDistance);
                emission.rateOverTimeMultiplier = res;
            }
        }
        
        transform.position += _moveDirection * step;
        _speed += _acceleration * Time.deltaTime;
        if (_traveledDistance >= _maxDistance)
        {
            SpawnSparks(transform.position);
            DoExplosiveDamage(transform.position);
            ReturnToPool();
        }
    }

    protected override void OnReturnToPool()
    {
        _initialized = false;
        _alreadyHitTargets.RemoveRecord();
    }

    private void SpawnSparks(in Vector3 position)
    {
        foreach (var spark in hitSparks)
        {
            spark.SpawnSparks(position, transform.rotation);
        }
    }

    private void DoExplosiveDamage(in Vector3 position)
    {
        var candidates = Physics.OverlapSphere(position, radius,  _enemyLayerMask);
        foreach (var c in candidates)
        {
            if (c.TryGetComponent<HealthController>(out var healthController))
            {
                if (UtilityFunctions.HasLineOfSight(position, c, _destructionLayerMask))
                {
                    if (_alreadyHitTargets.TryHitTarget(healthController))
                    {
                        healthController.DoNormalDamage(true, _damage, _poiseDamage, HealthController.DamageDealer.Bullet);
                    }
                }
            }
            else
            {
                Debug.Log("No health controller on target object " + c.gameObject.name);
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (useBoxCast)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.color = Color.green;
            if (increaseScaleWithDistance)
            {
                Gizmos.DrawWireCube(Vector3.zero, Vector3.LerpUnclamped(halfBoxSizes, boxSizesAtUnitDistance, _traveledDistance)*2);
            }
            else
            {
                Gizmos.DrawWireCube(Vector3.zero, halfBoxSizes * 2);
            }
            Gizmos.matrix = Matrix4x4.identity;
        }
        if (useExplosiveArea)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
