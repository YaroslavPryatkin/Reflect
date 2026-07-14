using UnityEngine;

public class PlayerHealthController : HealthController
{
    
    [Header("Regeneration")]
    [SerializeField] private float regenerationRate = 20f;
    [SerializeField] private float canNotHealAfterTakingDamageTime = 2f;
    
    private Utility.TemporaryValue<bool> canRegenerate = new(true, false);
    
    
    protected override void OnDamageTaken(float damage)
    {
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
