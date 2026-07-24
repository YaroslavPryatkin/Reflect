using UnityEngine;
using UnityEngine.Pool;

public class BulletController : MonoBehaviour
{
    private float _maxDistance;
    private float _speed;
    private float _traveledDistance;
    private int _destructionLayerMask;
    private int _enemyLayerMask;
    private float _damage;
    private float _poiseDamage;

    private Transform _backTarget;
    
    private IObjectPool<BulletController> _pool;
    
    public void SetPool(IObjectPool<BulletController> pool)
    {
        _pool = pool;
    }
    
    public void Initialize(float maxDistance, float speed, int destructionLayerMask, int targetLayerMask, float damage, float poiseDamage, Transform backTarget)
    {
        _maxDistance = maxDistance;
        _speed = speed;
        _destructionLayerMask = destructionLayerMask | targetLayerMask;
        _enemyLayerMask = targetLayerMask;
        _damage = damage;
        _backTarget =  backTarget;
        _poiseDamage = poiseDamage;
        _traveledDistance = 0f;
        
        var candidates = Physics.OverlapSphere(transform.position, 0.1f,  _destructionLayerMask);
        foreach (var c in candidates)
        {
            CheckCollision(c.gameObject);
            return;
        }
    }

    private void CheckCollision(GameObject obj)
    {
        if ((_enemyLayerMask & (1 << obj.layer)) != 0)
        {
            if (obj.TryGetComponent<HealthController>(out var healthController))
            {
                if (healthController.TryDeflecting(transform.forward))
                {
                    healthController.DoDeflectDamage(_damage, _poiseDamage, HealthController.DamageDealer.Bullet);
                    healthController.GetNewEnemyLayerMask(out _destructionLayerMask, out _enemyLayerMask);
                    healthController.GetNewBulletDamage(ref _damage);
                    _destructionLayerMask |= _enemyLayerMask;
                    transform.LookAt(_backTarget);
                    _traveledDistance = 0f;
                    return;
                }
                else
                {
                    healthController.DoNormalDamage(_damage, _poiseDamage, HealthController.DamageDealer.Bullet);
                }
            }
            else
            {
                Debug.Log("No health controller on target object " + obj.name);
            }
        }
        ReturnToPool();
    }
    
    private void Update()
    {
        var step = _speed * Time.deltaTime;
        
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, step, _destructionLayerMask))
        {
            var obj = hit.collider.gameObject;
            CheckCollision(obj);
            return;
        }

        transform.position += transform.forward * step;
        _traveledDistance += step;

        if (_traveledDistance >= _maxDistance)
        {
            ReturnToPool();
        }
    }
    
    private void ReturnToPool()
    {
        if (_pool != null)
        {
            _pool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
