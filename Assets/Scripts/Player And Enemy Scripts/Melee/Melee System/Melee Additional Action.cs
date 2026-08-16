using System;
using UnityEngine;
using System.Collections.Generic;

namespace MeleeSystem
{
    public class MeleeAdditionalActionOverhead
    {
        public readonly float Duration;

        public MeleeAdditionalActionOverhead(float thisStateDuration, MeleeAdditionalAction action)
        {
            Duration = thisStateDuration * Mathf.Clamp01(action.DurationFraction);
        }
    }
    
    public class ShouldTurnOffOverhead : MeleeAdditionalActionOverhead
    {
        public bool ShouldTurnOff = false;

        public ShouldTurnOffOverhead(float thisStateDuration, MeleeAdditionalAction action) :
            base(thisStateDuration, action)
        {
        }
    }
    
    public abstract class MeleeAdditionalAction : ScriptableObject
    {
        public abstract float DurationFraction { get; }

        public virtual bool IsReturnToLoop => false;

        public void AddClipsToSet(HashSet<AnimationClip> uniqueClips)
        {
        }

        public virtual MeleeAdditionalActionOverhead Initialize(
            MeleePlayablePart playablePart,
            float thisStateDuration
        )
        {
            return new MeleeAdditionalActionOverhead(thisStateDuration, this);
        }

        public virtual void StartAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overhead,
            UtilityClasses.FractionTemporaryValue<bool> thisActivityTimer)
        {
            
        }



        public virtual void FinishAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overhead)
        {
            InterruptAction(meleeController, meleePlayable, overhead, false);
        }

        public virtual void InterruptAction(
            MeleeController meleeController,
            MeleePlayable meleePlayable,
            MeleeAdditionalActionOverhead overhead,
            bool wasActive)
        {
            
        }
    }
    
    public abstract class MeleeAdditionalActionWithTimeFractionSerialized : MeleeAdditionalAction
    {
        [SerializeField] protected float durationFraction = 1f;
        public override float DurationFraction => durationFraction; 
    } 
}