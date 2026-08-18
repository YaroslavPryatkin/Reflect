using UnityEngine;
using System.Collections.Generic;
using OverHeadDictionary = System.Collections.Generic.Dictionary<
    MeleeSystem.MeleeAdditionalAction,
    MeleeSystem.MeleeAdditionalActionOverhead>;

namespace MeleeSystem
{
    [CreateAssetMenu(fileName = "UseSecondHand", menuName = "Melee/MeleeAdditionalActions/Animation/UseSecondHand")]
    public class UseSecondHand : MeleeAdditionalActionWithTimeFractionSerialized
    {
        private enum UseSecondHandEnum
        {
            Activate,
            RemainActive
        }

        [SerializeField] private UseSecondHandEnum behavior = UseSecondHandEnum.Activate;
        
        public override MeleeAdditionalActionOverhead Initialize(
            MeleeController meleeController,
            float thisStateDuration)
        {
            return new ShouldTurnOffOverhead(thisStateDuration, this);
        }

        public override void InterruptAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw,
            bool wasActive)
        {
            var overhead = (ShouldTurnOffOverhead)overheadRaw;

            if (overhead.ShouldTurnOff)
                meleeController.SecondHandController.ClearSecondHandReferences();

            overhead.ShouldTurnOff = false;
        }

        public override void StartAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw,
            UtilityTimers.FractionTemporaryValue<bool> thisActivityTimer)
        {
            var overhead = (ShouldTurnOffOverhead)overheadRaw;
            switch (behavior)
            {
                case UseSecondHandEnum.Activate:
                    overhead.ShouldTurnOff = true;
                    meleeController.SecondHandController.SetSecondHand(thisActivityTimer);
                    break;

                default:
                    overhead.ShouldTurnOff =
                        meleeController.SecondHandController.SetSecondHandIfWasSecondHand(thisActivityTimer);
                    break;
            }
        }
    }
}