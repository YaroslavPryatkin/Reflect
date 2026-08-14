using System;
using UnityEngine;

public class HealthController : MonoBehaviour
{
    [SerializeField] private float maxHealth = 120f;
    [SerializeField] private float damageFractionWhileStunned = 0.7f;
    
    [Header("Kill plain")]
    [SerializeField] private float killPlainActivationSuppressDuration = 0.1f;

    [Header("Deflect bullet")] 
    [SerializeField] private float deflectBulletAngle = 80f;
    [SerializeField] private float deflectBulletDamageReduction = 2f;
    
    public float CurrentHealth{get; private set;}
    public bool IsDead{get; private set;}
    public bool IsFullHealth => HealthFraction >= 0.99f;
    public float MaxHealth => maxHealth;
    public float HealthFraction => CurrentHealth / maxHealth;

    private MeleeController _meleeController;
    private GettingHitController _gettingHitController;
    private GunController _gunController;
    private bool _haveGunController;
    private bool _haveGettingHitController = false;
    private bool _haveMelee;
    private Sensors _sensors;
    
    protected ArenaController ArenaController;
    protected bool HaveArenaController = false;

    private readonly UtilityClasses.TemporaryValue<bool> _canKillPlain =
        new (true, false);
    public bool CanBeKilledByPlain => _canKillPlain.Value;
    
    public void SetArenaController(ArenaController arenaController)
    {
        HaveArenaController = true;
        ArenaController = arenaController;
    }

    public void RemoveArenaController(ArenaController arenaController)
    {
        if (arenaController == ArenaController)
        {
            HaveArenaController = false;
            ArenaController=null;
        }
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
        Bullet, Melee, Hazard
    }
    
    protected virtual void Awake()
    {
        CurrentHealth = maxHealth;
        _sensors =  GetComponent<Sensors>();
        _haveMelee = TryGetComponent(out _meleeController);
        _haveGettingHitController = TryGetComponent(out _gettingHitController);
        _haveGunController = TryGetComponent(out _gunController);
    }
    
    protected virtual void OnRevive()
    {
        _gettingHitController.Revive();
    }

    public void OnArenaReset()
    {
        CurrentHealth = maxHealth;
        
        if(IsDead)
            OnRevive();
        
        IsDead = false;
        
        if(_haveGunController)
            _gunController.SetBulletsToMaximum();
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

    public void DoDeflectDamage(float damage, float poiseDamage, float bulletRechargeFraction, DamageDealer damageDealer)
    {
        if (IsDead) return;
    
        _meleeController.OnSuccessfulParry();
        if (_haveGunController)
            _gunController.EarnBullet(bulletRechargeFraction);

        if (_iFrameSourcesCount > 0) return;

        if (_haveGettingHitController)
            _gettingHitController.Stun(poiseDamage, true);
        
    }

    public void DoNormalDamage(float damage, float poiseDamage, DamageDealer damageDealer)
    {
        if (IsDead || _iFrameSourcesCount > 0) return;
        
        if (_haveGettingHitController)
        {
            if (_gettingHitController.IsStunned)
                ChangeHealth(-damage * damageFractionWhileStunned);
            else
                ChangeHealth(-damage);
        }
        else
            ChangeHealth(-damage);
        

        if (!IsDead)
        {
            if(_haveGettingHitController)
                _gettingHitController.Stun(poiseDamage, false);
            
            OnDamageTaken(damage, damageDealer);
        }
    }


    protected virtual void OnDamageTaken(float damage, DamageDealer damageDealer)
    {
    }

    protected virtual void OnDeath()
    {
        _gettingHitController.Death();
    }

    public void FellOffTheMap()
    {
        if (_canKillPlain)
        {
            _canKillPlain.Activate(killPlainActivationSuppressDuration);
            Die();
        }
    }
    public virtual void OnHazardEntered()
    {
    }
    public void Die()
    {
        IsDead = true;
        CurrentHealth = 0f;
        OnDeath();
    }
    
    public bool TryDeflecting(Vector3 attackDirection)
    {
        return _haveMelee && GetAngleToBullet(attackDirection) <= deflectBulletAngle && _meleeController.Parrying;
    }

    public void GetNewEnemyLayerMask(out int destructiveLayer, out int enemyLayer)
    {
        enemyLayer = _sensors.EnemyLayer;
        destructiveLayer = _sensors.IgnoreMyLayerMask;
    }

    public Transform GetBackTarget() => transform;

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
