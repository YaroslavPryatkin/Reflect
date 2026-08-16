using UnityEngine;
using System.Collections.Generic;
using OverHeadDictionary = System.Collections.Generic.Dictionary<
    MeleeSystem.MeleeAdditionalAction,
    MeleeSystem.MeleeAdditionalActionOverhead>;

namespace MeleeSystem
{
   [CreateAssetMenu(fileName = "LookAtTarget", menuName = "Melee/MeleeAdditionalActions/Transform/LookAtTarget")]
    public class LookAtTarget : MeleeAdditionalActionWithTimeFractionSerialized
    {
        [Header("Rotation")] [SerializeField] private float rotateByDegrees = 30f;
        [SerializeField] private bool forceLookToTarget = false;
        [SerializeField] private bool standInPlace = true;
        [SerializeField, HideInInspector] private float rotateByRad;

        public void OnValidate()
        {
            rotateByRad = rotateByDegrees * Mathf.Deg2Rad;
        }

        private class LookAtTargetOverhead : MeleeAdditionalActionOverhead
        {
            public readonly float RotationSpeed;

            public LookAtTargetOverhead(float thisStateDuration, LookAtTarget action) :
                base(thisStateDuration, action)
            {
                if (Duration < 0.001f)
                {
                    RotationSpeed = 0f;
                }
                else
                {
                    RotationSpeed = action.rotateByRad / Duration;
                }
            }
        }


        public override MeleeAdditionalActionOverhead  Initialize(
            MeleePlayablePart playablePart,
            float thisStateDuration)
        {
            return new LookAtTargetOverhead(thisStateDuration, this);
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

            var overhead = (LookAtTargetOverhead)overheadRaw;
            meleeController.TransformController.ActivateStanding(overhead.RotationSpeed, forceLookToTarget,
                standInPlace, thisActivityTimer);
        }
    }
}