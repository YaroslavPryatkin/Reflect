using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Playables;
using UnityEngine.Animations;
using SubclassSelector;

namespace MeleeComponents
{
    public interface IMeleeState
    {
        float CurrentStateFraction { get; }
        AnimationClip Clip { get; }
        bool ClipIsNotNull { get; }
        float ClipSpeed { get; }

        bool IsActive { get; }

        bool CanBeSafelyInterrupted { get; }

        bool IsTransitioning { get; }
        
        bool ShouldResetAnimationTime { get; }
        
        bool ShouldMoveNextNotNull { get; }

        void Awake(MeleeController meleeController);

        void Interrupt();
        void Update();
        void Start(float startingFraction);

        void AddClipsToSet(HashSet<AnimationClip> uniqueClips);
    }

    public interface IMeleePlayable : IMeleeState
    {

        /// <returns>
        /// current part + time fraction of the current part
        /// </returns>
        float LengthFraction { get; }
        
        float Length { get; }
    }


    [Serializable, VisibleSubclass]
    public class MeleeMultiComponent : IMeleePlayable
    {
        [SerializeField] private bool loop = false;
        [SerializeReference, SelectSubclass] private List<IMeleeState> parts = new();
        
        public MeleeMultiComponent() { }

        protected MeleeMultiComponent(bool loop, List<IMeleeState> parts)
        {
            this.loop = loop;
            this.parts = parts;
        }
        

        private int _currentPart = -1;
        private int _nextPartWithClip; // == parts.Count means there is no next part with clip

        private bool _hasClip;
        
        private void MoveNextNotNullClip()
        {
            if (!_hasClip) return;

            if (loop)
            {
                while (!parts[_nextPartWithClip].ClipIsNotNull)
                    _nextPartWithClip = (_nextPartWithClip + 1) % parts.Count;
            }
            else
            {
                while (_nextPartWithClip < parts.Count && !parts[_nextPartWithClip].ClipIsNotNull)
                    ++_nextPartWithClip;
            }
            
           
        }

        private void UpdateNextNotNullClip()
        {
            if (_hasClip)
            {
                _nextPartWithClip = _currentPart;
                MoveNextNotNullClip();
            }
        }

        public bool IsActive => _currentPart != -1;

        public void Awake(MeleeController meleeController)
        {
            if (parts == null) return;
            _hasClip = false;
            foreach (var part in parts)
            {
                part.Awake(meleeController);
                _hasClip = _hasClip || part.ClipIsNotNull;
            }

            if (_hasClip)
            {
                _nextPartWithClip = 0;
                MoveNextNotNullClip();
            }
            else
            {
                _nextPartWithClip = parts.Count;
            }
        }

        public void AddClipsToSet(HashSet<AnimationClip> uniqueClips)
        {
            foreach (var part in parts)
            {
                part.AddClipsToSet(uniqueClips);
            }
        }

        public bool CanBeSafelyInterrupted => _currentPart == -1 || parts[_currentPart].CanBeSafelyInterrupted;

        public bool IsTransitioning => _currentPart != -1 && parts[_currentPart].IsTransitioning;
        
        public bool ShouldResetAnimationTime => _currentPart != -1 && parts[_currentPart].ShouldResetAnimationTime;

        public bool ShouldMoveNextNotNull { get; private set; } = false;

        public float LengthFraction
        {
            get
            {
                float res = parts.Count;
                if (_currentPart != -1)
                {
                    res = _currentPart + parts[_currentPart].CurrentStateFraction;
                }

                return res;
            }
        }

        public float Length => parts.Count;

        public void Interrupt()
        {
            ShouldMoveNextNotNull = false;
            if (_currentPart != -1)
            {
                parts[_currentPart].Interrupt();
                _currentPart = -1;
            }
        }

        public void Update()
        {
            ShouldMoveNextNotNull = false;
            if (_currentPart != -1)
            {
                parts[_currentPart].Update();
                
                ShouldMoveNextNotNull = parts[_currentPart].ShouldMoveNextNotNull;
                
                // var old = _currentPart;
                // var oldNext = _nextPartWithClip;
                // var showDebug = false;
                
                if (!parts[_currentPart].IsActive)
                {
                   // showDebug = true;
                    

                    if (_currentPart >= parts.Count - 1)
                    {
                        if (loop)
                        {
                            _currentPart = 0;
                        }
                        else
                        {
                            _currentPart = -1;
                            return;
                        }
                    }
                    else
                    {
                        ++_currentPart;
                        //Debug.Log("Next part = " + _currentPart + ", next clip part = " + _nextPartWithClip + ", length = " + Length);
                    }
                    
                    

                    parts[_currentPart].Start(0f);
                }

                if (ShouldMoveNextNotNull)
                {
                    //showDebug = true;
                    UpdateNextNotNullClip();
                }
                
                // if(showDebug)
                //     Debug.Log("Changing current, length = " + Length +
                //           ", old current = "+ old + ", new current = "+ _currentPart +", old next not null = " + oldNext + 
                //           ", next not null = " + _nextPartWithClip + ", should move = " + ShouldMoveNextNotNull +
                //           ",\n old current clip = "+ 
                //           (parts[old].ClipIsNotNull ? parts[old].Clip.name : "null")
                //           +", new current clip = " + 
                //           (parts[_currentPart].ClipIsNotNull ? parts[_currentPart].Clip.name : "null") + 
                //           ", old next not null clip = " + 
                //           (parts[oldNext].ClipIsNotNull ? parts[oldNext].Clip.name : "null") +
                //           ", next not null clip = " + 
                //           (parts[_nextPartWithClip].ClipIsNotNull ? parts[_nextPartWithClip].Clip.name : "null"));
            }
        }

        public void Start(float fraction)
        {
            if (parts.Count == 0) return;

            if (_currentPart != -1)
                parts[_currentPart].Interrupt();

            var floor = (int)Mathf.Floor(fraction);
            if (floor >= parts.Count) return;
            _currentPart = floor;
            parts[_currentPart].Start(fraction - floor);
            UpdateNextNotNullClip();
            //
            // Debug.Log("Starting current, length = " + Length +
            //           ", new current = "+ _currentPart +", next not null = " + 
            //           _nextPartWithClip
            //           +", new current clip = " + 
            //           (parts[_currentPart].ClipIsNotNull ? parts[_currentPart].Clip.name : "null") + 
            //           ", next not null clip = " + 
            //           (parts[_nextPartWithClip].ClipIsNotNull ? parts[_nextPartWithClip].Clip.name : "null"));
        }

        public float CurrentStateFraction => _currentPart == -1 ? 1f : parts[_currentPart].CurrentStateFraction;

        public AnimationClip Clip => parts[_nextPartWithClip].Clip;


        public bool ClipIsNotNull => _nextPartWithClip != parts.Count;

        public float ClipSpeed => _currentPart == -1 ? 0f : parts[_currentPart].ClipSpeed;
    }

    [Serializable, VisibleSubclass]
    public class MeleeAction : IMeleeState
    {
        [SerializeField] private bool canBeInterrupted = false;
        [SerializeField] protected float duration = 0.1f;
        [SerializeField] private AnimationClip clip;

        public MeleeAction()
        {
        }

        public MeleeAction(bool canBeInterrupted)
        {
            this.canBeInterrupted = canBeInterrupted;
        }

        private float _clipSpeed;

        public enum ActionStateEnum
        {
            Non,
            Action
        }

        protected Utility.FractionBlockingValueTimer<ActionStateEnum> ActionState;

        public float CurrentStateFraction =>
            ActionState.Value == ActionStateEnum.Action ? Mathf.Clamp01(ActionState.TimeFraction) : 1f;

        public AnimationClip Clip => clip;
        public bool ClipIsNotNull { get; private set; }

        public float ClipSpeed => ActionState.Value == ActionStateEnum.Action ? _clipSpeed : 0f;

        public bool IsActive => ActionState.Value == ActionStateEnum.Action;

        public bool CanBeSafelyInterrupted => canBeInterrupted || ActionState.Value == ActionStateEnum.Non;

        public bool IsTransitioning => false;

        public bool ShouldResetAnimationTime { get; protected set; } = false;

        public bool ShouldMoveNextNotNull => ActionState.Value == ActionStateEnum.Non;

        public virtual void Awake(MeleeController meleeController)
        {
            _clipSpeed = clip != null ? AnimationUtility.GetAnimationSpeed(clip, duration) : 1f;
            ClipIsNotNull = clip != null;
        }

        public virtual void AddClipsToSet(HashSet<AnimationClip> uniqueClips)
        {
            if (clip != null)
                uniqueClips.Add(clip);
        }

        public virtual void Interrupt()
        {
            ActionState.SetForce(ActionStateEnum.Non);
            ShouldResetAnimationTime = false;
        }

        public virtual void Update()
        {
            ShouldResetAnimationTime = false;
            if (ActionState.CanBeChanged && ActionState.Value == ActionStateEnum.Action)
            {
                ActionState.SetForce(ActionStateEnum.Non);
            }
        }

        public virtual void Start(float fraction)
        {
            if (ActionState.Value == ActionStateEnum.Non && fraction <= 1f)
            {
                ShouldResetAnimationTime = true;
                ActionState.SetForce(ActionStateEnum.Action, duration, fraction);
            }
        }
    }

    [Serializable, VisibleSubclass]
    public class MeleeAttackAction : MeleeAction
    {
        [SerializeField] private float damage = 0f;

        private MeleeController _meleeController;

        public override void Awake(MeleeController meleeController)
        {
            base.Awake(meleeController);
            _meleeController = meleeController;
        }

        public override void Interrupt()
        {
            ShouldResetAnimationTime = false;
            if (ActionState.Value == ActionStateEnum.Action)
            {
                _meleeController.DeactivateAttack();
                ActionState.SetForce(ActionStateEnum.Non);
            }
        }

        public override void Update()
        {
            ShouldResetAnimationTime = false;
            if (ActionState.CanBeChanged && ActionState.Value == ActionStateEnum.Action)
            {
                _meleeController.DeactivateAttack();
                ActionState.SetForce(ActionStateEnum.Non);
            }
        }

        public override void Start(float fraction)
        {
            if (ActionState.Value == ActionStateEnum.Non && fraction <= 1f)
            {
                ActionState.SetForce(ActionStateEnum.Action, duration, fraction);
                _meleeController.ActivateAttack(damage);
                ShouldResetAnimationTime = true;
            }
        }
    }

    [Serializable, VisibleSubclass]
    public class MeleeParryAction : MeleeAction
    {
        [Header("Parry settings")] 
        [SerializeField] private float parryTimeReductionForConsecutiveParries;
        [SerializeField] private float consecutiveParriesRechargeTime;
        [SerializeField] private float minimalParryTime;

        [Header("After successful parry")] 
        [SerializeField] private float parryStateAfterSuccessfulParryTime;
        [SerializeReference, SelectSubclass] private MeleeMultiComponent afterSuccessfulParry;

        public MeleeParryAction() { }

        public MeleeParryAction(bool canBeInterrupted, MeleeMultiComponent afterSuccessfulParry) : base(canBeInterrupted)
        {
            this.afterSuccessfulParry = afterSuccessfulParry;
        }

        private MeleeController _meleeController;
        private Utility.MultipleTemporaryValue<bool> _consecutiveParriesCounter;
        private bool hasParryTimeChanged = false;

        public override void Awake(MeleeController meleeController)
        {
            base.Awake(meleeController);
            afterSuccessfulParry.Awake(meleeController);
            _meleeController = meleeController;
            hasParryTimeChanged = parryTimeReductionForConsecutiveParries > 0.001f;
            if (!hasParryTimeChanged) return;
            
            var consecitiveSlots =
                Math.Clamp((int)Math.Floor((duration - minimalParryTime) / parryTimeReductionForConsecutiveParries), 1, 10);
            _consecutiveParriesCounter = new(false, true, consecutiveParriesRechargeTime, consecitiveSlots);
        }

        public override void AddClipsToSet(HashSet<AnimationClip> uniqueClips)
        {
            base.AddClipsToSet(uniqueClips);
            afterSuccessfulParry.AddClipsToSet(uniqueClips);
        }

        public override void Interrupt()
        {
            ShouldResetAnimationTime = false;
            if (ActionState.Value == ActionStateEnum.Action)
            {
                _meleeController.DeactivateParrying(parryStateAfterSuccessfulParryTime);
                ActionState.SetForce(ActionStateEnum.Non);
                if (hasParryTimeChanged)
                {
                    _consecutiveParriesCounter.Deactivate();
                }
            }
        }

        public override void Update()
        {
            ShouldResetAnimationTime = false;
            if (ActionState.CanBeChanged && ActionState.Value == ActionStateEnum.Action)
            {
                _meleeController.DeactivateParrying(parryStateAfterSuccessfulParryTime);
                ActionState.SetForce(ActionStateEnum.Non);
            }
        }

        public override void Start(float fraction)
        {
            if (ActionState.Value == ActionStateEnum.Non && fraction <= 1f)
            {
                ActionState.SetForce(ActionStateEnum.Action, duration, fraction);
                
                var parryDuration = duration;
                if (hasParryTimeChanged)
                {
                    parryDuration = Mathf.Max(duration - parryTimeReductionForConsecutiveParries *
                        _consecutiveParriesCounter.AmountOfActive, 0);
                    _consecutiveParriesCounter.Activate();
                }

                _meleeController.ActivateParrying(parryDuration, afterSuccessfulParry);
                ShouldResetAnimationTime = true;
            }
        }
    }

    [Serializable, VisibleSubclass]
    public class Transition : IMeleeState
    {
        [SerializeField] protected float duration = 0.1f;
        [SerializeField] private bool canBeInterrupted = false;

        public Transition() { }

        public Transition(bool canBeInterrupted)
        {
            this.canBeInterrupted = canBeInterrupted;
        }

        [Flags]
        public enum ActionStateEnum
        {
            Non,
            Active
        }

        protected Utility.FractionBlockingValueTimer<ActionStateEnum> ActionState = ActionStateEnum.Non;

        public virtual float CurrentStateFraction => ActionState.Value == ActionStateEnum.Active
            ? Mathf.Clamp01(ActionState.TimeFraction)
            : 1f;

        public AnimationClip Clip => null;
        public bool ClipIsNotNull => false;
        public float ClipSpeed => 0f;

        public bool IsActive => ActionState.Value != ActionStateEnum.Non;

        public bool ShouldResetAnimationTime { get; protected set; } = false;
        public bool ShouldMoveNextNotNull => false;

        public void Awake(MeleeController meleeController)
        {
        }

        public void AddClipsToSet(HashSet<AnimationClip> uniqueClips)
        {
        }

        public bool CanBeSafelyInterrupted => canBeInterrupted || ActionState.Value == ActionStateEnum.Non;
        public virtual bool IsTransitioning => ActionState.Value == ActionStateEnum.Active;

        public virtual void Interrupt()
        {
            ShouldResetAnimationTime = false;
            ActionState.SetForce(ActionStateEnum.Non);
        }

        public virtual void Update()
        {
            ShouldResetAnimationTime = false;
            if (ActionState.CanBeChanged && ActionState.Value == ActionStateEnum.Active)
            {
                ActionState.SetForce(ActionStateEnum.Non);
            }
        }

        public virtual void Start(float fraction)
        {
            if (ActionState.Value == ActionStateEnum.Non && fraction <= 1f)
            {
                ActionState.SetForce(ActionStateEnum.Active, duration, fraction);
                ShouldResetAnimationTime = true;
            }
        }
    }
    
    [Serializable, VisibleSubclass]
    public class Retain : Transition
    {
        public override bool IsTransitioning => false;
        
        public Retain() { }

        public Retain(bool canBeInterrupted) : base(canBeInterrupted) { }
        
        public override void Interrupt()
        {
            ActionState.SetForce(ActionStateEnum.Non);
        }

        public override void Update()
        {
            if (ActionState.CanBeChanged && ActionState.Value == ActionStateEnum.Active)
            {
                ActionState.SetForce(ActionStateEnum.Non);
            }
        }

        public override void Start(float fraction)
        {
            ShouldResetAnimationTime = false;
            if (ActionState.Value == ActionStateEnum.Non && fraction <= 1f)
            {
                ActionState.SetForce(ActionStateEnum.Active, duration, fraction);
            }
        }
        
    }
    
    
    namespace Presets
    {
        [Serializable]
        public class StandardSheath : MeleeMultiComponent
        {
            public StandardSheath() : base(
                loop: false,
                parts: new List<IMeleeState>
                {
                    new InterruptableTransition(),
                    new InterruptableMeleeAction(),
                    new InterruptableRetain()
                })
            { }
        }
        
        [Serializable]
        public class StandardUnSheath : MeleeMultiComponent
        {
            public StandardUnSheath() : base(
                loop: false,
                parts: new List<IMeleeState>
                {
                    new InterruptableTransition(),
                    new InterruptableMeleeAction()
                })
            { }
        }

        [Serializable, VisibleSubclass]
        public class StandardCombo : MeleeMultiComponent
        {
            public StandardCombo() : base(
                loop: true,
                parts: new List<IMeleeState>
                {
                    new StandardAttack(),
                    new StandardAttack(),
                    new StandardAttack(),
                    new StandardAttack(),
                })
            { }
        }
        
        [Serializable, VisibleSubclass]
        public class StandardAttack : MeleeMultiComponent
        {
            public StandardAttack() : base(
                loop: false,
                parts: new List<IMeleeState>
                {
                    new InterruptableTransition(),
                    new MeleeAttackAction(),
                    new MeleeAction()
                })
            { }
        }
        

        
        [Serializable, VisibleSubclass]
        public class StandardParry : MeleeMultiComponent
        {
            public StandardParry() : base(
                loop: false,
                parts: new List<IMeleeState>
                {
                    new Transition(),
                    new StandardParryAction(),
                    new MeleeAction()
                })
            { }
        }
        
        

        [Serializable]
        public class StandardTransitionLoop : MeleeMultiComponent
        {
            public StandardTransitionLoop() : base(
                loop: false,
                parts: new List<IMeleeState>
                {
                    new InterruptableTransition(),
                    new StandardLoop() 
                })
            { }
        }
    
        [Serializable]
        public class StandardLoop : MeleeMultiComponent
        {
            public StandardLoop() : base(
                loop: true,
                parts: new List<IMeleeState>
                {
                    new InterruptableMeleeAction()
                })
            { }
        }

        [Serializable]
        public class InterruptableTransition : Transition
        {
            public InterruptableTransition() : base(canBeInterrupted: true) { }
        }
        
        [Serializable]
        public class InterruptableRetain : Transition
        {
            public InterruptableRetain() : base(canBeInterrupted: true) { }
        }
        
        [Serializable]
        public class InterruptableMeleeAction : MeleeAction
        {
            public InterruptableMeleeAction() : base(canBeInterrupted: true) { }
        }
        
        [Serializable]
        public class StandardParryAction : MeleeParryAction
        {
            public StandardParryAction() : base(
                canBeInterrupted: false,
                afterSuccessfulParry : new StandardAfterSuccessfulParry ()
                ) 
            { }
        }
        
        [Serializable]
        public class StandardAfterSuccessfulParry : MeleeMultiComponent
        {
            public StandardAfterSuccessfulParry() : base(
                loop: false,
                parts: new List<IMeleeState>
                {
                    new Transition(),
                    new MeleeAction(),
                    new MeleeAction()
                })
            { }
        }
        
    }
}
