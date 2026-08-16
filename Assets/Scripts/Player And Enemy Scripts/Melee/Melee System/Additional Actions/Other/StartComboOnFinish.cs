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
        [SerializeField] private int combo;
        
        public override void FinishAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw)
        {
            meleePlayable.StartingOtherComboOnFinish();
            meleeController.PlayCombo(combo);
        }
    }
}