using UnityEngine;
using System.Collections.Generic;
using OverHeadDictionary = System.Collections.Generic.Dictionary<
    MeleeSystem.MeleeAdditionalAction,
    MeleeSystem.MeleeAdditionalActionOverhead>;

namespace MeleeSystem
{
   [CreateAssetMenu(fileName = "MoveToTarget", menuName = "Melee/MeleeAdditionalActions/Transform/MoveToTarget")]
    public class MoveToTarget : MeleeAdditionalActionWithTimeFractionSerialized
    {
        [Header("Rotation")] [SerializeField] private float rotateByDegrees = 0f;
        [SerializeField] private bool forceLookToTarget = false;

        [Header("Movement")] [SerializeField] private float distanceToTravel = 0f;

        [SerializeField] private AnimationCurve speedCurve = new AnimationCurve(new Keyframe(0f, 0f),
            new Keyframe(0.1f, 1f), new Keyframe(0.9f, 1f), new Keyframe(1f, 0f));

        [SerializeField, HideInInspector] private float distanceByAverage;
        [SerializeField, HideInInspector] private float rotateByRad;

        private void OnValidate()
        {
            if (speedCurve != null && speedCurve.keys.Length > 0)
            {
                distanceByAverage = distanceToTravel / UtilityFunctions.EvaluateCurveAverage(speedCurve);
            }

            rotateByRad = rotateByDegrees * Mathf.Deg2Rad;
        }

        private class MoveToTargetOverhead : MeleeAdditionalActionOverhead
        {
            public readonly float SpeedMultiplier;
            public readonly float RotationSpeed;

            public MoveToTargetOverhead(float thisStateDuration, MoveToTarget action) :
                base(thisStateDuration, action)
            {
                if (Duration < 0.001f)
                {
                    SpeedMultiplier = 0f;
                    RotationSpeed = 0f;
                }
                else
                {
                    SpeedMultiplier = action.distanceByAverage / Duration;
                    RotationSpeed = action.rotateByRad / Duration;
                }
            }
        }

        public override void InterruptAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw,
            bool wasActive)
        {
            meleeController.TransformController.StopMovingAndClearReferences();
        }


        public override void StartAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overheadRaw,
            UtilityClasses.FractionTemporaryValue<bool> thisActivityTimer)
        {
            var overhead = (MoveToTargetOverhead)overheadRaw;
            meleeController.TransformController.ActivateMove(
                overhead.SpeedMultiplier,
                speedCurve,
                overhead.RotationSpeed,
                forceLookToTarget,
                thisActivityTimer);
        }

        public override MeleeAdditionalActionOverhead  Initialize(
            MeleePlayablePart playablePart,
            float thisStateDuration)
        {
            return  new MoveToTargetOverhead(thisStateDuration, this);
        }
    }
}