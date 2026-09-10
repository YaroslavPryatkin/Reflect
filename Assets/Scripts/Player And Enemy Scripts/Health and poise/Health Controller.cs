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
    public GettingHitController GettingHitController { get; private set; }
    private GunController _gunController;
    private bool _haveGunController;
    private bool _haveMelee;
    protected Sensors Sensors;
    
    private readonly UtilityTimers.TemporaryValue<bool> _canKillPlain =
        new (true, false);
    public bool CanBeKilledByPlain => _canKillPlain.Value;
    
    private readonly UtilityClasses.ChangeableFractionValueReference _iFrames = new();

    public void ActivateIFrames(UtilityTimers.IFractionTimer<bool> timer)
    {
        _iFrames.Set(timer);
    }
    
    public void ActivateIFrames()
    {
        _iFrames.Set();
    }

    public void StopIFrames()
    {
        _iFrames.Unset();
    }

    public enum DamageDealer
    {
        Bullet, Melee, Hazard
    }
    
    protected virtual void Awake()
    {
        CurrentHealth = maxHealth;
        Sensors =  GetComponent<Sensors>();
        _haveMelee = TryGetComponent(out _meleeController);
        GettingHitController = GetComponent<GettingHitController>();
        _haveGunController = TryGetComponent(out _gunController);
    }
    
    protected virtual void OnRevive()
    {
        GettingHitController.Revive();
    }

    public virtual void OnArenaReset()
    {
        CurrentHealth = maxHealth;
        
        if(IsDead)
            OnRevive();
        
        IsDead = false;
        
        if(_haveGunController)
            _gunController.BeAbleToShootImmediately();
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

    public void DoDeflectDamage(bool triggerReaction, float damage, float poiseDamage, float bulletRechargeFraction, DamageDealer damageDealer)
    {
        if (IsDead) return;
    
        _meleeController.OnSuccessfulParry();
        if (_haveGunController)
            _gunController.EarnBullet(bulletRechargeFraction);

        if (_iFrames.Value) return;

            GettingHitController.Stun(poiseDamage, true);
        
    }

    public void DoNormalDamage(bool triggerReaction, float damage, float poiseDamage, DamageDealer damageDealer)
    {
        if (IsDead || _iFrames.Value) return;
        
        if (GettingHitController.IsStunned)
            ChangeHealth(-damage * damageFractionWhileStunned);
        else
            ChangeHealth(-damage);
        

        if (!IsDead)
        {
            GettingHitController.Stun(poiseDamage, false);
            
            OnDamageTaken(triggerReaction, damage, damageDealer);
        }
    }


    protected virtual void OnDamageTaken(bool triggerReaction, float damage, DamageDealer damageDealer)
    {
    }

    protected virtual void OnDeath()
    {
        GettingHitController.Death();
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
        enemyLayer = Sensors.EnemyLayer;
        destructiveLayer = Sensors.IgnoreMyLayerMask;
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
