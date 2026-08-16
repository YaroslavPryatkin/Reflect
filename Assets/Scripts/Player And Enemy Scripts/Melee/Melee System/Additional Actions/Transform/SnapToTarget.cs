using UnityEngine;
using System.Collections.Generic;
using OverHeadDictionary = System.Collections.Generic.Dictionary<
    MeleeSystem.MeleeAdditionalAction,
    MeleeSystem.MeleeAdditionalActionOverhead>;

namespace MeleeSystem
{
    [CreateAssetMenu(fileName = "SnapToTarget", menuName = "Melee/MeleeAdditionalActions/Transform/SnapToTarget")]
    public class SnapToTarget : MeleeAdditionalActionWithTimeFractionSerialized
    {
        [SerializeField] private bool forceLookToTarget = true;
        [SerializeField] private float distanceToTravel = 0f;

        [SerializeField] private AnimationCurve speedCurve = new AnimationCurve(new Keyframe(0f, 0f),
            new Keyframe(0.1f, 1f), new Keyframe(1f, 1f));

        [SerializeField, HideInInspector] private AnimationCurve distanceCurve;

        private void OnValidate()
        {
            if (speedCurve != null && speedCurve.keys.Length > 0)
            {
                distanceCurve = UtilityFunctions.BakeDistanceCurve(speedCurve, distanceToTravel);
            }
        }

        
        public override MeleeAdditionalActionOverhead Initialize(
            MeleePlayablePart playablePart,
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
            if(overhead.ShouldTurnOff)
                meleeController.TransformController.StopMovingAndClearReferences();
        }


        public override void StartAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw,
            UtilityClasses.FractionTemporaryValue<bool> thisActivityTimer)
        {
            var overhead = (ShouldTurnOffOverhead)overheadRaw;
            overhead.ShouldTurnOff = meleeController.TransformController.ActivateSnapping(
                forceLookToTarget,
                distanceCurve,
                thisActivityTimer);
        }
    }
}