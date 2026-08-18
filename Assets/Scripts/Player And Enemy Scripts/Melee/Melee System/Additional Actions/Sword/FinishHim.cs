using UnityEngine;
using System.Collections.Generic;

namespace MeleeSystem
{
    [CreateAssetMenu(fileName = "FinishHim", menuName = "Melee/MeleeAdditionalActions/Sword/FinishHim")]
    public class FinishHim : MeleeAdditionalAction
    {
        private enum TypeEnum
        {
            Start, Continue, Finish
        }
        
        [SerializeField] private TypeEnum type = TypeEnum.Continue;
        
        public override float DurationFraction => 1f;


        public override void StartAction(MeleeController meleeController, MeleePlayable meleePlayable, MeleeAdditionalActionOverhead overhead, UtilityTimers.FractionTemporaryValue<bool> thisActivityTimer)
        {
            if (type == TypeEnum.Start)
                meleeController.OnFinishHimStart();
        }

        public override void InterruptAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overhead,
            bool wasActive)
        {
            meleeController.OnFinishHimEnd();
        }

        public override void FinishAction(
            MeleeController meleeController, 
            MeleePlayable meleePlayable, 
            MeleeAdditionalActionOverhead overhead)
        {
            if(type == TypeEnum.Finish)
                meleeController.OnFinishHimEnd();
        }
    }
}