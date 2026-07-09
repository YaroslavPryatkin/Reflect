using UnityEngine;

public class PlayerHealthController : HealthController
{
    
    [Header("Regeneration")]
    [SerializeField] private float regenerationRate = 20f;
    [SerializeField] private float canNotHealAfterTakingDamageTime = 2f;

    [Header("Deflect / block bullet")] 
    [SerializeField] private float blockBulletAngle = 120f;
    [SerializeField] private float deflectBulletAngle = 80f;
    [SerializeField] private float deflectBulletDamageReduction = 2f;

    private Utility.TemporaryValue<bool> canRegenerate = new(true, false);
    private PlayerSwordController _playerSwordController;

    protected override void Awake()
    {
        base.Awake();
        _playerSwordController = GetComponent<PlayerSwordController>();
    }
    
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
    
    public override bool ShouldBeDeflected(Vector3 bulletForward)
    {
        return GetAngleToBullet(bulletForward) <= deflectBulletAngle && _playerSwordController.Parrying;
    }

    public override bool ShouldBeBlocked(Vector3 bulletForward)
    {
        
        return GetAngleToBullet(bulletForward) <= blockBulletAngle && _playerSwordController.Parrying;
    }

    private float GetAngleToBullet(Vector3 bulletForward)
    {
        var forwardHorizontal = new Vector3(transform.forward.x, 0f, transform.forward.z);
        var bulletHorizontal = new Vector3(-bulletForward.x, 0f, -bulletForward.z);

        return Vector3.Angle(forwardHorizontal, bulletHorizontal);
    }

    public override void GetNewBulletDamage(ref float damage)
    {
        damage /= deflectBulletDamageReduction;
    }
}
