using UnityEngine;
using System.Collections.Generic;
using OverHeadDictionary = System.Collections.Generic.Dictionary<
    MeleeSystem.MeleeAdditionalAction,
    MeleeSystem.MeleeAdditionalActionOverhead>;

namespace MeleeSystem
{
    [CreateAssetMenu(fileName = "ReturnToLoopMark", menuName = "Melee/MeleeAdditionalActions/Loop/ReturnToLoopMark")]
    public class ReturnToLoopMark : MeleeAdditionalAction
    {
        public override float DurationFraction => 1f;
        public override bool IsReturnToLoop => true;
        
        public override void FinishAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw)
        {
            meleePlayable.ReturnToLoopMark();
        }
    }
}