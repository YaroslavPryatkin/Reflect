using UnityEngine;

public class EnemyHealthController : HealthController
{

    private void Update()
    {
        if(IsDead)
            gameObject.SetActive(false);
    }
    
}
