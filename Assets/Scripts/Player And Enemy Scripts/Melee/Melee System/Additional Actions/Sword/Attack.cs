using UnityEngine;
using System.Collections.Generic;
using OverHeadDictionary = System.Collections.Generic.Dictionary<
    MeleeSystem.MeleeAdditionalAction,
    MeleeSystem.MeleeAdditionalActionOverhead>;

namespace MeleeSystem
{
    
    [CreateAssetMenu(fileName = "Attack", menuName = "Melee/MeleeAdditionalActions/Sword/Attack")]
    public class Attack : MeleeAdditionalActionWithTimeFractionSerialized
    {

        [SerializeField] private float damage;
        [SerializeField] private float poiseDamage;


        public override void InterruptAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw,
            bool wasActive)
        {
            meleeController.MeleeHitboxController.FinishSwing();
            meleeController.OnAttackEnd();
        }

        public override void StartAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw,
            UtilityClasses.FractionTemporaryValue<bool> thisActivityTimer)
        {
            meleeController.MeleeHitboxController.StartSwing(damage, poiseDamage);
        }

    }
}