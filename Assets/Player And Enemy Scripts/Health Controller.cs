using UnityEngine;

public class HealthController : MonoBehaviour
{
    [SerializeField] private float maxHealth = 120f;
    
    
    
    [Header("Block bullet")] 
    [SerializeField] private float blockBulletAngle = 120f;
    [SerializeField] private float blockDamageReduction = 2f;
    
    [Header("Deflect bullet")] 
    [SerializeField] private float deflectBulletAngle = 80f;
    [SerializeField] private float deflectBulletDamageReduction = 2f;
    
    public float CurrentHealth{get; private set;}
    public bool IsDead{get; private set;}
    public bool IsFullHealth => HealthFraction >= 0.99f;
    public float MaxHealth => maxHealth;
    public float HealthFraction => CurrentHealth / maxHealth;

    protected DamageController _damageController;
    protected MeleeController _meleeController;
    protected bool haveMelee;
    protected Sensors _sensors;
    
    protected virtual void Awake()
    {
        CurrentHealth = maxHealth;
        _damageController =  GetComponent<DamageController>();
        _sensors =  GetComponent<Sensors>();
        haveMelee = TryGetComponent(out _meleeController);
    }

    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
        IsDead = false;
    }
    
    public void ChangeHealth(float change)
    {
        CurrentHealth += change;
        if (change < 0f)
        {
            OnDamageTaken(-change);
        }
        if (CurrentHealth <= 0f)
        {
            IsDead = true;
            CurrentHealth = 0f;
            OnDeath();
        }
        else 
        { 
            if(CurrentHealth > maxHealth)
                CurrentHealth = maxHealth;
            if(CurrentHealth > 0f)
                IsDead = false;
        }
    }

    public void DoDeflectDamage(float damage)
    {
        
    }

    public void DoBlockDamage(float damage)
    {
        ChangeHealth(-damage/blockDamageReduction);
    }

    public void DoNormalDamage(float damage)
    {
        ChangeHealth(-damage);
    }

    protected virtual void OnDamageTaken(float damage)
    {
        
    }

    protected virtual void OnDeath()
    {
        
    }

    public bool TryDeflecting(Vector3 bulletForward)
    {
        if (haveMelee && GetAngleToBullet(bulletForward) <= deflectBulletAngle && _meleeController.Parrying)
        {
            _meleeController.OnSuccessfulParry();
            return true;
        }

        return false;
    }

    public bool TryBlocking(Vector3 bulletForward)
    {

        return false;
    }

    public void GetNewEnemyLayerMask(out int destructiveLayer, out int enemyLayer)
    {
        enemyLayer = _damageController.EnemyLayer;
        destructiveLayer = _sensors.IgnoreMyLayerMask;
    }

    private float GetAngleToBullet(Vector3 bulletForward)
    {
        var forwardHorizontal = new Vector3(transform.forward.x, 0f, transform.forward.z);
        var bulletHorizontal = new Vector3(-bulletForward.x, 0f, -bulletForward.z);

        return Vector3.Angle(forwardHorizontal, bulletHorizontal);
    }

    public void GetNewBulletDamage(ref float damage)
    {
        damage /= deflectBulletDamageReduction;
    }
}
