using UnityEngine;

public class HealthController : MonoBehaviour
{
    [SerializeField] private float maxHealth = 120f;
    [SerializeField] private float damageFractionWhileStunned = 0.7f;
    
    [Header("Deflect bullet")] 
    [SerializeField] private float deflectBulletAngle = 80f;
    [SerializeField] private float deflectBulletDamageReduction = 2f;
    
    public float CurrentHealth{get; private set;}
    public bool IsDead{get; private set;}
    public bool IsFullHealth => HealthFraction >= 0.99f;
    public float MaxHealth => maxHealth;
    public float HealthFraction => CurrentHealth / maxHealth;

    protected MeleeController _meleeController;
    private GettingHitController _gettingHitController;
    private bool _haveGettingHitController = false;
    protected bool haveMelee;
    protected Sensors _sensors;
    
    protected ArenaController ArenaController;
    protected bool HaveArenaController = false;

    public void SetArenaController(ArenaController arenaController)
    {
        HaveArenaController = true;
        ArenaController = arenaController;
    }

    public void RemoveArenaController()
    {
        HaveArenaController = false;
    }
    

    private int _iFrameSourcesCount = 0;

    public void GrantIFrames()
    {
        ++_iFrameSourcesCount;
    }

    public void TakeIFrames()
    {
        --_iFrameSourcesCount;
    }

    public enum DamageDealer
    {
        Bullet, Melee
    }
    
    protected virtual void Awake()
    {
        CurrentHealth = maxHealth;
        _sensors =  GetComponent<Sensors>();
        haveMelee = TryGetComponent(out _meleeController);
        _haveGettingHitController = TryGetComponent(out _gettingHitController);
    }

    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
        IsDead = false;
    }
    
    public void ChangeHealth(float change)
    {
        CurrentHealth += change;
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

    public void DoDeflectDamage(float damage, float poiseDamage, DamageDealer  damageDealer)
    {
        _meleeController.OnSuccessfulParry();
        
        if (_iFrameSourcesCount > 0) return;
        if (_haveGettingHitController)
            _gettingHitController.Stun(poiseDamage, true);
    }

    public void DoNormalDamage(float damage, float poiseDamage, DamageDealer damageDealer)
    {
        if (_iFrameSourcesCount > 0) return;
        
        if (_haveGettingHitController)
        {
            if(_gettingHitController.IsStunned)
                ChangeHealth(-damage * damageFractionWhileStunned);
            else
                ChangeHealth(-damage);

            _gettingHitController.Stun(poiseDamage, false);
        }
        else
            ChangeHealth(-damage);

        OnDamageTaken(damage, damageDealer);
    }

    protected virtual void OnDamageTaken(float damage, DamageDealer damageDealer)
    {
        
    }

    protected virtual void OnDeath()
    {
        
    }

    public bool TryDeflecting(Vector3 attackDirection)
    {
        return haveMelee && GetAngleToBullet(attackDirection) <= deflectBulletAngle && _meleeController.Parrying;
    }

    public void GetNewEnemyLayerMask(out int destructiveLayer, out int enemyLayer)
    {
        enemyLayer = _sensors.EnemyLayer;
        destructiveLayer = _sensors.IgnoreMyLayerMask;
    }

    private float GetAngleToBullet(Vector3 attackDirection)
    {
        var forwardHorizontal = new Vector3(transform.forward.x, 0f, transform.forward.z);
        var bulletHorizontal = new Vector3(-attackDirection.x, 0f, -attackDirection.z);

        return Vector3.Angle(forwardHorizontal, bulletHorizontal);
    }

    public void GetNewBulletDamage(ref float damage)
    {
        damage /= deflectBulletDamageReduction;
    }
}
