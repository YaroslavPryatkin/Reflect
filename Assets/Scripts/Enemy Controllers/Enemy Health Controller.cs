using UnityEngine;

public class EnemyHealthController : HealthController
{
    private EnemyDodgeController _enemyDodgeController;
    private bool _haveDodgeController;

    protected override void Awake()
    {
        base.Awake();

        _haveDodgeController = TryGetComponent(out _enemyDodgeController);
    }
    
    protected override void OnDeath()
    {
        gameObject.SetActive(false);
    }

    protected override void OnDamageTaken(float damage, DamageDealer damageDealer)
    {
        if(_haveDodgeController)
            _enemyDodgeController.GettingHit(damageDealer);
    }
}
