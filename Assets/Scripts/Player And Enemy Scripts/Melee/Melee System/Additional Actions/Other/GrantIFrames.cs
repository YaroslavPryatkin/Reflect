using UnityEngine;
using System.Collections.Generic;
using OverHeadDictionary = System.Collections.Generic.Dictionary<
    MeleeSystem.MeleeAdditionalAction,
    MeleeSystem.MeleeAdditionalActionOverhead>;

namespace MeleeSystem
{
    [CreateAssetMenu(fileName = "GrantIFrames", menuName = "Melee/MeleeAdditionalActions/Other/GrantIFrames")]
    public class GrantIFrames : MeleeAdditionalActionWithTimeFractionSerialized
    {
        public override void InterruptAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw,
            bool wasActive)
        {
            meleeController.HealthController.StopIFrames();
        }

        public override void StartAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw,
            UtilityClasses.FractionTemporaryValue<bool> thisActivityTimer)
        {
            meleeController.HealthController.ActivateIFrames(thisActivityTimer);
        }
    }
}