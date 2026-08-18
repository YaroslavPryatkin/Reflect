using UnityEngine;
using System.Collections.Generic;
using OverHeadDictionary = System.Collections.Generic.Dictionary<
    MeleeSystem.MeleeAdditionalAction,
    MeleeSystem.MeleeAdditionalActionOverhead>;

namespace MeleeSystem
{
    [CreateAssetMenu(fileName = "HyperArmor", menuName = "Melee/MeleeAdditionalActions/Other/HyperArmor")]
    public class HyperArmor : MeleeAdditionalActionWithTimeFractionSerialized
    {
        public override void InterruptAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw,
            bool wasActive)
        {
            meleeController.GettingHitController.StopHyperArmor();
        }

        public override void StartAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw,
            UtilityTimers.FractionTemporaryValue<bool> thisActivityTimer)
        {
            meleeController.GettingHitController.ActivateHyperArmor(thisActivityTimer);
        }
    }
}