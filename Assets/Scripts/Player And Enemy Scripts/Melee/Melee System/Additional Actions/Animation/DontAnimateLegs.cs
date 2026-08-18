using UnityEngine;
using System.Collections.Generic;

namespace MeleeSystem
{
    [CreateAssetMenu(fileName = "DontAnimateLegs", menuName = "Melee/MeleeAdditionalActions/Animation/DontAnimateLegs")]
    public class DontAnimateLegs : MeleeAdditionalActionWithTimeFractionSerialized
    {
        public override void InterruptAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overhead,
            bool wasActive)
        {
            meleeController.StopDontAnimateLegs();
        }

        public override void StartAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overhead,
            UtilityTimers.FractionTemporaryValue<bool> thisActivityTimer)
        {
            meleeController.ActivateDontAnimateLegs(thisActivityTimer);
        }
    }
}