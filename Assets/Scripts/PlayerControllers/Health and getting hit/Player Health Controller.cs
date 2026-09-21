using CustomAttributes;
using UnityEngine;

public class PlayerHealthController : HealthController
{
    
    [Header("Regeneration")]
    [SerializeField] private float regenerationRate = 20f;
    [SerializeField] private float canNotHealAfterTakingDamageTime = 2f;
    [Header("Noob mod: taken damage")]
    [SerializeField] private BoolSettingValue settingValue;
    [SerializeField] private float noobMultiplier = 2f;
    
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
        GameSavings.IncreaseAmountOfDeaths(gameObject.scene.name);
        base.OnDeath();
        UIManager.ShowDeath();
    }

    public override void OnArenaReset()
    {
        base.OnArenaReset();
        _playerTargetLockController.UnlockEverything();
    }

    protected override void ChangeTakenDamage(ref float takenDamage)
    {
        if (settingValue.value)
            takenDamage *= noobMultiplier;
    }
    
    protected override void OnDamageTaken(bool triggerReaction, float damage, DamageDealer damageDealer)
    {
        _playerInputController.ClearAllBuffers();
        _canRegenerate.Activate(canNotHealAfterTakingDamageTime);
    }
    
    public override void OnHazardEntered()
    {
        LevelController.TryReturnPlayerToSpawnPoint(true);
    }

    protected void Update()
    {
        if (LevelController.CanRegenerateOnArena && _canRegenerate.Value && !IsFullHealth)
        {
            ChangeHealth(regenerationRate * Time.deltaTime);
        }
    }
}
