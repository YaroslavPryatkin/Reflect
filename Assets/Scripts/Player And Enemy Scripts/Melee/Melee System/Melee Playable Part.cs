using System;
using UnityEngine;
using System.Collections.Generic;

namespace MeleeSystem
{
    public enum InterruptionEnum
    {
        Never,
        ToOtherCombo,
        ToAnything
    }
    
    public class MeleePlayablePart : ScriptableObject
    {
        [SerializeField] protected float duration = 0f;
        [SerializeField] protected InterruptionEnum canBeInterruptedInto;
        [SerializeReference] protected List<MeleeAdditionalAction> additionalActions = new();

        [SerializeField, HideInInspector] private bool haveReturnToLoop = false;

        
        public float Duration => duration;
        public InterruptionEnum CanBeInterruptedInto => canBeInterruptedInto;
        public virtual float ClipSpeed => 0f;
        public virtual bool HasClip => false;
        public virtual AnimationClip Clip => null;
        public virtual bool ShouldAnimationTransition => false;
        public int AdditionalActionsCount => additionalActions.Count;
        public bool HaveReturnToLoop => haveReturnToLoop;

        private void OnValidate()
        {
            haveReturnToLoop = false;
            var usedActions = new HashSet<MeleeAdditionalAction>();
            foreach (var additionalAction in additionalActions)
            {
                if (additionalAction == null) continue;

                haveReturnToLoop = additionalAction.IsReturnToLoop || additionalAction.IsReturnToLoop;

                if (!usedActions.Add(additionalAction))
                {
                    Debug.LogError(
                        "Melee playable part contains two instances of one additional action: " + additionalAction.name,
                        this);
                }
            }
        }

        public void Initialize(List<MeleeAdditionalActionOverhead> overheadList)
        {
            overheadList.Clear();
            if (additionalActions.Count > 0)
            {
                foreach (var additionalAction in additionalActions)
                {
                    overheadList.Add(additionalAction.Initialize(this, duration));
                }
            }
        }

        public void AddClipsToSet(HashSet<AnimationClip> uniqueClips)
        {
            if (HasClip)
                uniqueClips.Add(Clip);


            foreach (var additionalAction in additionalActions)
            {
                additionalAction.AddClipsToSet(uniqueClips);
            }
        }

        public void StartAdditionalActions(
            MeleeController meleeController,
            MeleePlayable thisMeleePlayable,
            List<MeleeAdditionalActionOverhead> overheadList,
            List<UtilityClasses.FractionTemporaryValue<bool>> additionalActivityTimers)
        {
            if (additionalActions.Count > 0)
            {
                for (var i = 0; i < additionalActions.Count; ++i)
                {
                    var additionalActionDuration = overheadList[i].Duration;
                    
                    if (additionalActionDuration > 0)
                    {
                        additionalActivityTimers[i].Activate(additionalActionDuration);
                    }
                    
                    additionalActions[i].StartAction(
                        meleeController,
                        thisMeleePlayable,
                        overheadList[i],
                        additionalActivityTimers[i]);
                }
            }
        }

        public void InterruptAdditionalActions(
            MeleeController meleeController,
            MeleePlayable thisMeleePlayable,
            List<MeleeAdditionalActionOverhead> overheadList,
            List<UtilityClasses.FractionTemporaryValue<bool>> additionalActivityTimers)
        {
            if (additionalActions.Count > 0)
            {
                for (var i = 0; i < additionalActions.Count; ++i)
                {
                    additionalActions[i].InterruptAction(
                        meleeController,
                        thisMeleePlayable,
                        overheadList[i],
                        additionalActivityTimers[i].Value);
                    additionalActivityTimers[i].Deactivate();
                }
            }
        }

        public void FinishAdditionalActions(
            MeleeController meleeController,
            MeleePlayable thisMeleePlayable,
            List<MeleeAdditionalActionOverhead> overheadList,
            List<UtilityClasses.FractionTemporaryValue<bool>> additionalActivityTimers)
        {
            if (additionalActions.Count > 0)
            {
                for (var i = 0; i < additionalActions.Count; ++i)
                {
                    additionalActions[i].FinishAction(
                        meleeController,
                        thisMeleePlayable,
                        overheadList[i]);
                    additionalActivityTimers[i].Deactivate();
                }
            }
        }
    }
}