using UnityEngine;
using System.Collections.Generic;
using OverHeadDictionary = System.Collections.Generic.Dictionary<
    MeleeSystem.MeleeAdditionalAction,
    MeleeSystem.MeleeAdditionalActionOverhead>;

namespace MeleeSystem
{
    [CreateAssetMenu(fileName = "LoopMark", menuName = "Melee/MeleeAdditionalActions/Loop/LoopMark")]
    public class LoopMark : MeleeAdditionalAction
    {
        public override float DurationFraction => 1f;
        
        public override void StartAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw,
            UtilityTimers.FractionTemporaryValue<bool> thisActivityTimer)
        {
            meleePlayable.SetLoopMark();
        }
    }
}