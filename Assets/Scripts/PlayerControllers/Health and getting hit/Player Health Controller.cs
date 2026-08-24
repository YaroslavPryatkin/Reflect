using CustomAttributes;
using UnityEngine;

public class PlayerHealthController : HealthController
{
    
    [Header("Regeneration")]
    [SerializeField] private float regenerationRate = 20f;
    [SerializeField] private float canNotHealAfterTakingDamageTime = 2f;
    
    [Header("World interaction")] 
    [SerializeField] private bool resetArenaOnDeath = true;
    
    private readonly UtilityTimers.TemporaryValue<bool> _canRegenerate = new(true, false);
    

    
    private PlayerInputController _playerInputController;
    private PlayerTargetLockController _playerTargetLockController;
    private Rigidbody _rb;

    protected override void Awake()
    {
        base.Awake();
        _playerInputController = GetComponent<PlayerInputController>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>();
        _rb = GetComponent<Rigidbody>();
    }
    
    protected override void OnDeath()
    {
        _playerInputController.ClearAllBuffers();
        _playerTargetLockController.UnlockEverything();
        base.OnDeath();
        GameSavings.IncreaseAmountOfDeaths(gameObject.scene.name);
        UIManager.ShowDeath();
    }

    public void TryResetArena()
    {
        if(resetArenaOnDeath)
        {
            LevelController.TryResetArena();
        }
    }

    public override void OnArenaReset()
    {
        base.OnArenaReset();
        _playerTargetLockController.UnlockEverything();
    }

    protected override void OnDamageTaken(bool triggerReaction, float damage, DamageDealer damageDealer)
    {
        _playerInputController.ClearAllBuffers();
        _canRegenerate.Activate(canNotHealAfterTakingDamageTime);
    }
    
    public override void OnHazardEntered()
    {
        if (LevelController.TryReturnPlayerToSpawnPoint())
        {
            _rb.linearVelocity = Vector3.zero;
        }
    }

    protected void Update()
    {
        if (LevelController.CanRegenerateOnArena && _canRegenerate.Value && !IsFullHealth)
        {
            ChangeHealth(regenerationRate * Time.deltaTime);
        }
    }
}
