using UnityEngine;

public class EnemyHealthController : HealthController
{
    protected override void OnDeath()
    {
        gameObject.SetActive(false);
    }
}
