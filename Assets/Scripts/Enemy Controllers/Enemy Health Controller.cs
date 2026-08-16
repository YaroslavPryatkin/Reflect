using UnityEngine;

public class EnemyHealthController : HealthController
{
    [Header("Player health regen upon death")]
    [SerializeField] protected float playerHpRegen=0f;
    
    private EnemyDodgeController _enemyDodgeController;
    private EnemyAI _enemyAI;
    private bool _haveDodgeController;

    private LayerMask _wasExcludeLayers;
    
    private Collider _collider;
    
    protected override void Awake()
    {
        base.Awake();
        _enemyAI = GetComponent<EnemyAI>();
        _collider = GetComponent<Collider>();
        _haveDodgeController = TryGetComponent(out _enemyDodgeController);
    }

    protected override void OnRevive()
    {
        ArenaController.EnemyRevived();
        TurnOnCollisionWithPlayer();
        base.OnRevive();
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        _enemyAI.Deactivate();
        ArenaController.EnemyDied();
        TurnOffCollisionWithPlayer();
    }

    public bool CanBeFinished => IsDead && GettingHitController.CanBeFinished;

    public void StartBeingFinished()
    {
        GettingHitController.IsBeingFinished = true;
    }
    
    public void StopBeingFinished(bool shouldRegen)
    {
        GettingHitController.IsBeingFinished = false;
        
        if(shouldRegen)
            ArenaController.RegenPlayerHp(playerHpRegen);
    }
    
    private void TurnOnCollisionWithPlayer()
    {
        _collider.excludeLayers = _wasExcludeLayers;
    }

    private void TurnOffCollisionWithPlayer()
    {
        _wasExcludeLayers = _collider.excludeLayers;
        _collider.excludeLayers = _wasExcludeLayers | Sensors.EnemyLayer;
    }

    protected override void OnDamageTaken(float damage, DamageDealer damageDealer)
    {
        if(_haveDodgeController)
            _enemyDodgeController.GettingHit(damageDealer);
    }
}
