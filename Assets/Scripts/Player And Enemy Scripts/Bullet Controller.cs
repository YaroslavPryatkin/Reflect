using UnityEngine;

public class BulletController : MonoBehaviour
{
    private const float MaxDistance = 100f;
    private float _speed;
    private float _traveledDistance;
    private int _destructionLayerMask;
    private int _enemyLayerMask;
    private float _damage;
    private float _poiseDamage;

    private Transform _backTarget;
    

    
    public void Initialize(float speed, int destructionLayerMask, int targetLayerMask, float damage, float poiseDamage, Transform backTarget)
    {
        _speed = speed;
        _destructionLayerMask = destructionLayerMask | targetLayerMask;
        _enemyLayerMask = targetLayerMask;
        _damage = damage;
        _backTarget =  backTarget;
        _poiseDamage = poiseDamage;
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
                    healthController.DoDeflectDamage(_damage, _poiseDamage);
                    healthController.GetNewEnemyLayerMask(out _destructionLayerMask, out _enemyLayerMask);
                    healthController.GetNewBulletDamage(ref _damage);
                    _destructionLayerMask |= _enemyLayerMask;
                    transform.LookAt(_backTarget);
                    _traveledDistance = 0f;
                    return;
                }
                else
                {
                    healthController.DoNormalDamage(_damage, _poiseDamage);
                }
            }
            else
            {
                Debug.Log("No health controller on target object " + obj.name);
            }
        }
        Destroy(gameObject);
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

        if (_traveledDistance >= MaxDistance)
        {
            Destroy(gameObject);
        }
    }
}
