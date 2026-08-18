using UnityEngine;
using System.Collections.Generic;
using OverHeadDictionary = System.Collections.Generic.Dictionary<
    MeleeSystem.MeleeAdditionalAction,
    MeleeSystem.MeleeAdditionalActionOverhead>;

namespace MeleeSystem
{
   [CreateAssetMenu(fileName = "MoveThrough", menuName = "Melee/MeleeAdditionalActions/Transform/MoveThrough")]
    public class MoveThrough : MeleeAdditionalActionWithTimeFractionSerialized
    {
        
        [Header("Rotation")] 
        [SerializeField] private float rotateByDegrees = 0f;
        [SerializeField] private float horizontalAngleShift = 0f;
        [SerializeField] private bool forceLookToTarget = false;

        [Header("Movement")] 
        [SerializeField] private float distanceToTravel = 0f;
        [SerializeField] private float overshootDistanceToStop = 2f;

        [SerializeField] private AnimationCurve speedCurve = new (new Keyframe(0f, 0f),
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

        private class MoveThroughOverhead : MeleeAdditionalActionOverhead
        {
            public readonly float SpeedMultiplier;
            public readonly float RotationSpeed;

            public MoveThroughOverhead(float thisStateDuration, MoveThrough action) :
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
            UtilityTimers.FractionTemporaryValue<bool> thisActivityTimer)
        {
            var overhead = (MoveThroughOverhead)overheadRaw;
            meleeController.TransformController.ActivateMoveThrough(
                overhead.SpeedMultiplier,
                speedCurve,
                overhead.RotationSpeed,
                horizontalAngleShift,
                overshootDistanceToStop,
                forceLookToTarget,
                thisActivityTimer);
        }

        public override MeleeAdditionalActionOverhead  Initialize(
            MeleeController meleeController,
            float thisStateDuration)
        {
            return new MoveThroughOverhead(thisStateDuration, this);
        }
    }
}