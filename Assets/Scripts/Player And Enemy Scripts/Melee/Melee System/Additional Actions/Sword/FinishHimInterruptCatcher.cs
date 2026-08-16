using UnityEngine;
using System.Collections.Generic;

namespace MeleeSystem
{
    [CreateAssetMenu(fileName = "FinishHimInterruptCatcher", menuName = "Melee/MeleeAdditionalActions/Sword/FinishHimInterruptCatcher")]
    public class FinishHimInterruptCatcher : MeleeAdditionalActionWithTimeFractionSerialized
    {
        public override void InterruptAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overhead,
            bool wasActive)
        {
            meleeController.OnFinishHimInterrupt();
        }

        public override void FinishAction(
            MeleeController meleeController, 
            MeleePlayable meleePlayable, 
            MeleeAdditionalActionOverhead overhead)
        {
            // do nothing
        }
    }
}