using UnityEngine;
using System.Collections.Generic;
using OverHeadDictionary = System.Collections.Generic.Dictionary<
    MeleeSystem.MeleeAdditionalAction,
    MeleeSystem.MeleeAdditionalActionOverhead>;

namespace MeleeSystem
{
    [CreateAssetMenu(fileName = "InterruptIfNotOnGround", menuName = "Melee/MeleeAdditionalActions/Other/InterruptIfNotOnGround")]
    public class InterruptIfNotOnGround : MeleeAdditionalActionWithTimeFractionSerialized
    {
        public override void InterruptAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw,
            bool wasActive)
        {
            meleeController.StopInterruptIfNotOnGround();
        }

        public override void StartAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw,
            UtilityTimers.FractionTemporaryValue<bool> thisActivityTimer)
        {
            meleeController.ActivateInterruptIfNotOnGround(thisActivityTimer);
        }
    }
}