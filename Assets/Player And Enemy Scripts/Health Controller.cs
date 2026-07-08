using UnityEngine;

public class HealthController : MonoBehaviour
{
    [SerializeField] private float maxHealth = 120f;
    [SerializeField] private float blockDamageReduction = 2f;
    
    public float CurrentHealth{get; private set;}
    public bool IsDead{get; private set;}
    public bool IsFullHealth => HealthFraction >= 0.99f;
    public float MaxHealth => maxHealth;
    public float HealthFraction => CurrentHealth / maxHealth;

    protected DamageController _damageController;
    protected Sensors _sensors;
    
    protected virtual void Awake()
    {
        CurrentHealth = maxHealth;
        _damageController =  GetComponent<DamageController>();
        _sensors =  GetComponent<Sensors>();
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

    public virtual bool ShouldBeDeflected(Vector3 bulletForward)
    {
        return false;
    }

    public virtual bool ShouldBeBlocked(Vector3 bulletForward)
    {
        return false;
    }

    public void GetNewEnemyLayerMask(out int destructiveLayer, out int enemyLayer)
    {
        enemyLayer = _damageController.EnemyLayer;
        destructiveLayer = _sensors.IgnoreMyLayerMask;
    }

    public virtual void GetNewBulletDamage(ref float damage)
    {
        damage /= 2f;
    }
}
