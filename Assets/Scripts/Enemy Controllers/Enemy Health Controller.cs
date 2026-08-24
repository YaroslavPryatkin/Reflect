using UnityEngine;

public class EnemyHealthController : HealthController
{
    [Header("Player health regen upon death")]
    [SerializeField] private float playerHpRegen=0f;
    [SerializeField] private float playerBulletRegen = 1f;
    
    private EnemyDodgeController _enemyDodgeController;
    private EnemyAI _enemyAI;
    private EnemySensors _enemySensors;
    private bool _haveDodgeController;

    private LayerMask _wasExcludeLayers;
    
    private Collider _collider;
    
    public bool CanBeFinished => IsDead && GettingHitController.CanBeFinished;
    
    public float PlayerRegenHpAmount => playerHpRegen;
    public float PlayerBulletRegenAmount => playerBulletRegen;
    
        
    private ArenaController _arenaController;
    
    public void SetArenaController(ArenaController arenaController)
    {
        _arenaController = arenaController;
    }
    
    
    
    protected override void Awake()
    {
        base.Awake();
        _enemyAI = GetComponent<EnemyAI>();
        _collider = GetComponent<Collider>();
        _enemySensors = GetComponent<EnemySensors>();
        _haveDodgeController = TryGetComponent(out _enemyDodgeController);
    }

    protected override void OnRevive()
    {
        _arenaController.EnemyRevived();
        TurnOnCollisionWithPlayer();
        base.OnRevive();
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        _enemyAI.Deactivate();
        _arenaController.EnemyDied();
        TurnOffCollisionWithPlayer();
    }

    public override void OnArenaReset()
    {
        base.OnArenaReset();
        _enemySensors.ResetHasSeenPlayer();
    }

    private void TurnOnCollisionWithPlayer()
    {
        //_collider.excludeLayers = _wasExcludeLayers;
        _collider.isTrigger=false;
    }

    private void TurnOffCollisionWithPlayer()
    {
        // _wasExcludeLayers = _collider.excludeLayers;
        // _collider.excludeLayers = _wasExcludeLayers | Sensors.EnemyLayer;
        _collider.isTrigger=true;
    }

    protected override void OnDamageTaken(bool triggerReaction, float damage, DamageDealer damageDealer)
    {
        if(triggerReaction && _haveDodgeController)
            _enemyDodgeController.GettingHit(damageDealer);
    }
}
