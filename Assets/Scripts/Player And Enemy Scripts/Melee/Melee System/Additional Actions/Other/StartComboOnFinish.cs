using UnityEngine;
using System.Collections.Generic;
using OverHeadDictionary = System.Collections.Generic.Dictionary<
    MeleeSystem.MeleeAdditionalAction,
    MeleeSystem.MeleeAdditionalActionOverhead>;

namespace MeleeSystem
{

    [CreateAssetMenu(fileName = "StartComboOnFinish", menuName = "Melee/MeleeAdditionalActions/Other/StartComboOnFinish")]
    public class StartComboOnFinish : MeleeAdditionalAction
    {
        public override float DurationFraction => 1f;
        [SerializeReference] private MeleePlayableSource sourceToSwitchTo;

        private class StartComboOnFinishOverhead : MeleeAdditionalActionOverhead
        {
            public readonly int Index;
            public StartComboOnFinishOverhead(int index, float thisStateDuration, StartComboOnFinish action) :
                base(thisStateDuration, action)
            {
                Index = index;
            }
        }
        
        public override MeleeAdditionalActionOverhead  Initialize(
            MeleeController meleeController,
            float thisStateDuration)
        {
            var index = meleeController.GetSourceIndex(sourceToSwitchTo);
            return new StartComboOnFinishOverhead(index, thisStateDuration, this);
        }
        
        public override void FinishAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw)
        {
            var overhead = (StartComboOnFinishOverhead)overheadRaw;
            meleePlayable.StartingOtherComboOnFinish();

            meleeController.PlayCombo(overhead.Index);
        }
    }
}