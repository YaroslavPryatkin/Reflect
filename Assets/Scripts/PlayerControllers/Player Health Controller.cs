using UnityEngine;

public class PlayerHealthController : HealthController
{
    
    [Header("Regeneration")]
    [SerializeField] private float regenerationRate = 20f;
    [SerializeField] private float canNotHealAfterTakingDamageTime = 2f;
    
    private Utility.TemporaryValue<bool> canRegenerate = new(true, false);

    private PlayerInputController _playerInputController;

    protected override void Awake()
    {
        base.Awake();
        _playerInputController = GetComponent<PlayerInputController>();
    }
    
    protected override void OnDeath()
    {
        if (HaveArenaController)
        {
            ArenaController.PlayerDied();
        }
    }

    protected override void OnDamageTaken(float damage, DamageDealer damageDealer)
    {
        _playerInputController.ClearAllBuffers();
        canRegenerate.Activate(canNotHealAfterTakingDamageTime);
    }

    private void Update()
    {
        if (canRegenerate && !IsFullHealth)
        {
            ChangeHealth(regenerationRate * Time.deltaTime);
        }
    }
}
