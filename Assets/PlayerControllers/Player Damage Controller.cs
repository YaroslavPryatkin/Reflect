using System;
using UnityEngine;

public class PlayerDamageController : DamageController
{
    [SerializeField] private float swordDamage = 60f;
    [SerializeField] private MeleWeaponHitboxController swordHitboxController;
    public float SwordDamage => swordDamage;


    private void Awake()
    {
        swordHitboxController.SetTargetLayers(EnemyLayer);
    }

    public void StartSwordSwing()
    {
        swordHitboxController.StartSwing(swordDamage);
    }

    public void FinishSwordSwing()
    {
        swordHitboxController.FinishSwing();
    }
    
}
