using UnityEngine;

public class EnemyHealthController : HealthController
{
    [Header("Player health regen upon death")]
    [SerializeField] protected float hpRegen=0f;
    
    
    private EnemyDodgeController _enemyDodgeController;
    private EnemyAI _enemyAI;
    private bool _haveDodgeController;

    private Collider _collider;
    private Rigidbody _rigidbody;
    
    protected override void Awake()
    {
        base.Awake();
        _enemyAI = GetComponent<EnemyAI>();
        _collider = GetComponent<Collider>();
        _rigidbody = GetComponent<Rigidbody>();
        _haveDodgeController = TryGetComponent(out _enemyDodgeController);
    }

    protected override void OnRevive()
    {
        ArenaController.EnemyRevived();
        SwitchActivity(true);
        base.OnRevive();
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        ArenaController.EnemyDied(hpRegen);
        _enemyAI.Deactivate();
        SwitchActivity(false);
    }

    private void SwitchActivity(bool isActive)
    {
        _collider.enabled = isActive;
    }

    protected override void OnDamageTaken(float damage, DamageDealer damageDealer)
    {
        if(_haveDodgeController)
            _enemyDodgeController.GettingHit(damageDealer);
    }
}
