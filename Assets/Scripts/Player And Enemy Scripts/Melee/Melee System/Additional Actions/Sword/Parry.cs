using UnityEngine;
using System.Collections.Generic;
using OverHeadDictionary = System.Collections.Generic.Dictionary<
    MeleeSystem.MeleeAdditionalAction,
    MeleeSystem.MeleeAdditionalActionOverhead>;

namespace MeleeSystem
{
    [CreateAssetMenu(fileName = "Parry", menuName = "Melee/MeleeAdditionalActions/Sword/Parry")]
    public class Parry : MeleeAdditionalActionWithTimeFractionSerialized
    {
        [Header("Not successful parry")] [SerializeField]
        private float parryTimeReductionForConsecutiveParries;

        [SerializeField] private float consecutiveParriesRechargeTime;
        [SerializeField] private float minimalParryTime;

        [Header("Successful parry")] [SerializeField]
        private float parryStateAfterSuccessfulParryTime;

        [SerializeReference] private MeleePlayableSource onSuccessfulParrySource;

        [SerializeField, HideInInspector] private bool parryTimeChanges = false;

        private void OnValidate()
        {
            parryTimeChanges = parryTimeReductionForConsecutiveParries > 0.001f;
        }

        private class ParryOverhead : MeleeAdditionalActionOverhead
        {
            public readonly UtilityTimers.MultipleTemporaryValue<bool> ConsecutiveParriesCounter;
            public readonly UtilityTimers.TemporaryValue<bool> Parrying = new(false, true);
            public readonly int Index;
            
            public ParryOverhead(int index, float thisStateDuration, Parry action) :
                base(thisStateDuration, action)
            {
                Index = index;
                var consecutiveSlots =
                    Mathf.Clamp(Mathf.FloorToInt((Duration - action.minimalParryTime) /
                                                 action.parryTimeReductionForConsecutiveParries), 1, 10);
                ConsecutiveParriesCounter =
                    new(false, true, action.consecutiveParriesRechargeTime, consecutiveSlots);
            }
        }

        public override MeleeAdditionalActionOverhead  Initialize(
            MeleeController meleeController,
            float thisStateDuration)
        {
            var index = meleeController.GetSourceIndex(onSuccessfulParrySource);
            return new ParryOverhead(index, thisStateDuration, this);
        }


        public override void InterruptAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw,
            bool wasActive)
        {
            var overhead = (ParryOverhead)overheadRaw;

            meleeController.ClearParryingReferences();
            if (wasActive)
            {
                overhead.Parrying.Activate(parryStateAfterSuccessfulParryTime);
            }
            else
            {
                overhead.Parrying.Deactivate();
            }

            if (parryTimeChanges)
            {
                overhead.ConsecutiveParriesCounter.Deactivate();
            }

            meleeController.OnParryEnd();
        }

        public override void FinishAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw)
        {
            var overhead = (ParryOverhead)overheadRaw;
            overhead.Parrying.Deactivate();
            meleeController.ClearParryingReferences();
            meleeController.OnParryEnd();
        }


        public override void StartAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw,
            UtilityTimers.FractionTemporaryValue<bool> thisActivityTimer)
        {
            var overhead = (ParryOverhead)overheadRaw;

            var parryDuration = overhead.Duration;
            if (parryTimeChanges)
            {
                parryDuration = Mathf.Max(parryDuration - parryTimeReductionForConsecutiveParries *
                    overhead.ConsecutiveParriesCounter.AmountOfActive, 0);
                overhead.ConsecutiveParriesCounter.Activate();
            }

            overhead.Parrying.Activate(parryDuration);
            meleeController.ActivateParrying(overhead.Parrying, thisActivityTimer, overhead.Index);
        }
    }
}