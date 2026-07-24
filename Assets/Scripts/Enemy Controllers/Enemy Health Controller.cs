using UnityEngine;

public class EnemyHealthController : HealthController
{
    private EnemyDodgeController _enemyDodgeController;
    private EnemyAI _enemyAI;
    private bool _haveDodgeController;

    protected override void Awake()
    {
        base.Awake();
        _enemyAI = GetComponent<EnemyAI>();
        _haveDodgeController = TryGetComponent(out _enemyDodgeController);
    }

    protected override void OnRevive()
    {
        GlobalEnemyComputingTimeOptimizer.AddEnemy(_enemyAI);
    }
    
    protected override void OnDeath()
    {
        GlobalEnemyComputingTimeOptimizer.DeleteEnemy(_enemyAI.Index);
        gameObject.SetActive(false);
    }

    protected override void OnDamageTaken(float damage, DamageDealer damageDealer)
    {
        if(_haveDodgeController)
            _enemyDodgeController.GettingHit(damageDealer);
    }
}
