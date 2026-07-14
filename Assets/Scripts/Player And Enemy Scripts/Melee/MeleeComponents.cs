using UnityEngine;
using System.Collections.Generic;
using System;
using CustomAttributes;

namespace MeleeComponents
{
    [Serializable]
    public abstract class MeleeStateInformation
    {
        [SerializeField] protected float duration = 0f;
        [SerializeField] protected bool canBeSafelyInterrupted;

        public MeleeStateInformation()
        { }

        public MeleeStateInformation(bool canBeSafelyInterrupted)
        {
            this.canBeSafelyInterrupted = canBeSafelyInterrupted;
        }

        public float Duration => duration;
        public bool CanBeSafelyInterrupted => canBeSafelyInterrupted;
        public float ClipSpeed { get; protected set; }= 0f;
        public bool HasClip { get; protected set; } = false;
        public virtual AnimationClip Clip => null;
        public virtual bool ShouldAnimationTransition => false;

        public virtual void Awake()
        { }

        public void AddClipsToSet(HashSet<AnimationClip> uniqueClips)
        {
            if(HasClip)
                uniqueClips.Add(Clip);
        }
    }
    
    [Serializable, VisibleSubclass]
    public class MeleeAnimationStateInformation : MeleeStateInformation
    {
        [SerializeField] private AnimationClip clip;
        public override AnimationClip Clip => clip;
        
        public MeleeAnimationStateInformation()
        {
        }

        public MeleeAnimationStateInformation(bool canBeSafelyInterrupted) : base(canBeSafelyInterrupted)
        {
        }
        
        public override void Awake()
        {
            HasClip = clip != null;
            if(HasClip)
                ClipSpeed = Utility.GetAnimationSpeed(clip, Duration);
        }
    }
    
    [Serializable, VisibleSubclass]
    public class MeleeTransitionStateInformation : MeleeStateInformation
    {
        public override bool ShouldAnimationTransition => true;

        public MeleeTransitionStateInformation()
        {
        }

        public MeleeTransitionStateInformation(bool canBeSafelyInterrupted) : base(canBeSafelyInterrupted)
        {
        }
    }
    
    [Serializable, VisibleSubclass]
    public class MeleeRemainStateInformation : MeleeStateInformation
    {
        public MeleeRemainStateInformation()
        {
        }

        public MeleeRemainStateInformation(bool canBeSafelyInterrupted) : base(canBeSafelyInterrupted)
        {
        }
    }
    
    [Serializable]
    public class MeleePlayablePart
    {
        [SerializeReference, SelectSubclass] protected MeleeStateInformation settings;
        [SerializeReference, SelectSubclass] protected List<MeleeAdditionalAction> additionalActions = new();
        
        public MeleeStateInformation Settings =>  settings;
        public int AdditionalActionsCount => additionalActions.Count;
        public bool HaveReturnToLoop { get; private set; } = false;

        public MeleePlayablePart() { }

        public MeleePlayablePart(MeleeStateInformation settings, List<MeleeAdditionalAction> additionalActions)
        {
            this.settings = settings;
            this.additionalActions=additionalActions;
        }

        // public MeleePlayablePart Clone()
        // {
        //     var clone = (MeleePlayablePart)Activator.CreateInstance(GetType());
        //     clone.settings = settings; 
        //     clone.additionalActions = additionalActions.Select(a => a.Clone()).ToList();
        //     return clone;
        // }
        

        public void Awake(MeleeController meleeController, MeleePlayable thisMeleePlayable)
        {
            settings.Awake();
            foreach (var additionalAction in additionalActions)
            {
                HaveReturnToLoop = HaveReturnToLoop || additionalAction.Awake(meleeController, thisMeleePlayable, settings.Duration);
            }
        }

        public void AddClipsToSet(HashSet<AnimationClip> uniqueClips)
        {
            settings.AddClipsToSet(uniqueClips);
            foreach (var additionalAction in additionalActions)
            {
                additionalAction.AddClipsToSet(uniqueClips);
            }
        }

        public void StartAdditionalActions(List<Utility.FractionTemporaryValue<bool>> additionalActivityTimers)
        {
            for (var i = 0; i < additionalActions.Count; ++i)
            {
                var duration = settings.Duration;
                
                if(additionalActions[i].UseSpecificDuration)
                    duration = additionalActions[i].Duration;

                if (duration > 0)
                {
                    additionalActivityTimers[i].Activate(duration);
                }
                additionalActions[i].Start(additionalActivityTimers[i]);
            }
        }
        
        public void InterruptAdditionalActions(List<Utility.FractionTemporaryValue<bool>> additionalActivityTimers)
        {
            for (var i = 0; i < additionalActions.Count; ++i)
            {
                additionalActions[i].Interrupt(additionalActivityTimers[i].Value);
                additionalActivityTimers[i].Deactivate();
            }
        }
        
        public void FinishAdditionalActions(List<Utility.FractionTemporaryValue<bool>> additionalActivityTimers)
        {
            for (var i = 0; i < additionalActions.Count; ++i)
            {
                additionalActions[i].Finish();
                additionalActivityTimers[i].Deactivate();
            }
        }
    }
    
    [Serializable]
    public class MeleePlayable : Utility.IPlayable
    {
        [SerializeField] protected List<MeleePlayablePart> parts = new();

        private List<Utility.FractionTemporaryValue<bool>> _additionalActionsActivityTimers;
        private Utility.FractionBlockingValueTimer<int> _currentPart ;

        public MeleePlayable()
        {
        }

        public MeleePlayable(List<MeleePlayablePart> parts)
        {
            this.parts = parts;
        }

        // public MeleePlayable Clone()
        // {
        //     var clone = (MeleePlayable)Activator.CreateInstance(GetType());
        //     clone.parts = parts.Select(p => p.Clone()).ToList();
        //     return clone;
        // }

        public int Length { get; private set; }
        public float LengthFraction => _currentPart.Value + _currentPart.TimeFraction;
        public bool IsActive => _currentPart < Length;
        public bool CanBeSafelyInterrupted =>
            _currentPart == Length || parts[_currentPart].Settings.CanBeSafelyInterrupted;
        public bool ClipIsNull => _nextPartWithClip == Length;
        public AnimationClip Clip => parts[_nextPartWithClip].Settings.Clip;
        public float ClipSpeed => parts[_currentPart].Settings.ClipSpeed;
        public float CurrentClipWeight => parts[_currentPart].Settings.ShouldAnimationTransition
            ? _currentPart.TimeFraction
            :  1f;
        
        
        
        
        
        
        private int _loopMark = 0;
        private bool _shouldReturnToLoopMark = false;

        private bool _hasClip = false;
        private bool _hasLoop = false;
        private int _nextPartWithClip;
        
        

        private void MoveNextPartWithClip()
        {
            if (!_hasClip) return;
            
            _nextPartWithClip = _currentPart;
            
            if (_hasLoop)
            {
                int safetyCounter = 0;
                while (!parts[_nextPartWithClip].Settings.HasClip)
                {
                    if (_nextPartWithClip < Length - 1)
                        _nextPartWithClip++;
                    else
                    {
                        if (++safetyCounter > 1)
                        {
                            _hasClip = false;
                            _nextPartWithClip = Length;
                            return;
                        }
                        _nextPartWithClip = _loopMark;
                    }
                }
            }
            else
            {
                while (_nextPartWithClip < Length && !parts[_nextPartWithClip].Settings.HasClip)
                    _nextPartWithClip++;
            }
        }
        
        
        private void UpdateCurrentPart(int newValue, float newFraction = 0f)
        {
            if (newValue<Length)
            {
                var duration = parts[newValue].Settings.Duration;
                _currentPart.SetForce(newValue, duration, newFraction);
                MoveNextPartWithClip();
                parts[_currentPart].StartAdditionalActions(_additionalActionsActivityTimers);
            }
            else
            {
                _currentPart.SetForce(Length);
                _nextPartWithClip = Length;
            }
        }
        
        public void SetLoopMark()
        {
            _loopMark = _currentPart;
        }

        public void ReturnToLoopMark()
        {
            _shouldReturnToLoopMark = true;
        }

        public void Awake(MeleeController meleeController)
        {
            Length = parts.Count;


            int maxAddition = 0;
            for(var i=0;i<parts.Count;++i)
            {
                parts[i].Awake(meleeController, this);
                int otherMax = parts[i].AdditionalActionsCount;
                if (otherMax > maxAddition)
                    maxAddition = otherMax;

                _hasClip = _hasClip || parts[i].Settings.HasClip;
                
                if (parts[i].HaveReturnToLoop)
                {
                    Length = i + 1;
                    _hasLoop = true;
                    break;
                }
            }
            
            _currentPart = Length;
            _nextPartWithClip = Length;

            _additionalActionsActivityTimers = new List<Utility.FractionTemporaryValue<bool>>(maxAddition);

            for (var i = 0; i < maxAddition; ++i)
            {
                _additionalActionsActivityTimers.Add(new(false, true));
            }
        }

        public void AddClipsToSet(HashSet<AnimationClip> uniqueClips)
        {
            foreach (var part in parts)
            {
                part.AddClipsToSet(uniqueClips);

            }
        }


        public void Start(float fraction)
        {
            var startPos = (int)Mathf.Floor(fraction);
            var startFraction = fraction - startPos;
            startPos = Math.Clamp(startPos, 0, Length);
            
            
            UpdateCurrentPart(startPos, startFraction);
        }

        public void Update()
        {
            
            if (IsActive && _currentPart.CanBeChanged)
            {
                parts[_currentPart].FinishAdditionalActions(_additionalActionsActivityTimers);
                int newValue;
                if (_shouldReturnToLoopMark)
                {
                    newValue = _loopMark;
                    _shouldReturnToLoopMark = false;
                }
                else
                {
                    newValue = _currentPart + 1;
                }
                
                UpdateCurrentPart(newValue);
            }
        }

        public void Interrupt()
        {
            if (IsActive)
            {
                parts[_currentPart].InterruptAdditionalActions(_additionalActionsActivityTimers);
            }
        }
    }
    
    [Serializable]
    public abstract class MeleeAdditionalStateInformation
    {
        [SerializeField] protected bool useSpecificDuration = false;
        [SerializeField, EnableIf("useSpecificDuration")] protected float duration = -1f;
        public float Duration => duration;
        public bool UseSpecificDuration => useSpecificDuration;
        
        public virtual void Awake()
        { }
    }

    [Serializable]
    public class AdditionalStateInformationParry : MeleeAdditionalStateInformation
    {
        
        [SerializeField] private float parryTimeReductionForConsecutiveParries;
        [SerializeField] private float consecutiveParriesRechargeTime;
        [SerializeField] private float minimalParryTime;
        [SerializeField] private float parryStateAfterSuccessfulParryTime;

        public bool ParryTimeChanges { get;private set; }
        
        public float ParryStateAfterSuccessfulParryTime => parryStateAfterSuccessfulParryTime;
        public float ParryTimeReductionForConsecutiveParries => parryTimeReductionForConsecutiveParries;
        public float ConsecutiveParriesRechargeTime => consecutiveParriesRechargeTime;
        public float MinimalParryTime => minimalParryTime;

        public override void Awake()
        {
            ParryTimeChanges = parryTimeReductionForConsecutiveParries > 0.001f;
        }
    }
    
    [Serializable]
    public class AdditionalStateInformationAttack : MeleeAdditionalStateInformation
    {
        [SerializeField] private float damage;

        public float Damage => damage;
    }
    
    [Serializable]
    public class AdditionalStateInformationMask : MeleeAdditionalStateInformation
    {
        [SerializeField] private AvatarMask mask;

        public AvatarMask Mask => mask;
    }
    
    [Serializable]
    public class AdditionalStateInformationMoveToTarget : MeleeAdditionalStateInformation
    {
        [SerializeField] private float distanceToTravel = 0f;
        [SerializeField] private float targetDistance = 0f;
        [SerializeField] private int animationClipIndex = 0;
        public float TargetDistance => targetDistance;
        public float DistanceToTravel => distanceToTravel;
        public int AnimationClipIndex => animationClipIndex;
    }
    
    [Serializable]
    public class AdditionalStateInformationSecondHand : MeleeAdditionalStateInformation
    {
        [SerializeField] private Utility.BaseActionTransitionsEnum type =  Utility.BaseActionTransitionsEnum.Base;
        public Utility.BaseActionTransitionsEnum Type => type;

        public AdditionalStateInformationSecondHand()
        {
        }

        public AdditionalStateInformationSecondHand(Utility.BaseActionTransitionsEnum type)
        {
            this.type = type;
        }
    }
    
    public class AdditionalStateInformationDummy : MeleeAdditionalStateInformation
    {
        public AdditionalStateInformationDummy()
        {
            useSpecificDuration = false;
            duration = -1f;
        }
    }
    
    [Serializable]
    public abstract class MeleeAdditionalAction
    {
        protected abstract MeleeAdditionalStateInformation StateInformation { get; }
        protected MeleeController MeleeController;
        protected MeleePlayable ThisMeleePlayable;
        
        public float Duration => StateInformation.Duration;
        public bool UseSpecificDuration => StateInformation.UseSpecificDuration;
        
        public virtual void AddClipsToSet(HashSet<AnimationClip> uniqueClips){}

        public virtual bool Awake(MeleeController meleeController, MeleePlayable thisMeleePlayable, float thisStateDuration)
        {
            MeleeController = meleeController;
            ThisMeleePlayable = thisMeleePlayable;
            StateInformation.Awake();
            return false;
        }

        public abstract void Start(Utility.FractionTemporaryValue<bool> thisActivityTimer);

        public virtual void Finish()
        {
            Interrupt(false);
        }

        public abstract void Interrupt(bool wasActive);
        //
        // public abstract MeleeAdditionalAction Clone();
    }
    
    
    [Serializable, VisibleSubclass]
    public class Parry : MeleeAdditionalAction
    {
        [SerializeField] private AdditionalStateInformationParry settings;
        [SerializeReference, SelectSubclass] private MeleePlayable onSuccessfulParry;
        protected override MeleeAdditionalStateInformation StateInformation => settings;
        
        private float _realDuration;
        
        public Parry() { }

        public Parry(MeleePlayable onSuccessfulParry)
        {
            this.onSuccessfulParry = onSuccessfulParry;
        }
        
        private Utility.MultipleTemporaryValue<bool> _consecutiveParriesCounter;
        private Utility.TemporaryValue<bool> _parrying = new(false, true);


        public override bool Awake(MeleeController meleeController, MeleePlayable thisMeleePlayable, float thisStateDuration)
        {
            base.Awake(meleeController, thisMeleePlayable, thisStateDuration);
            onSuccessfulParry.Awake(meleeController);

            _realDuration = settings.UseSpecificDuration ? settings.Duration : thisStateDuration;
            
            if (!settings.ParryTimeChanges) return false;
            
            var consecitiveSlots =
                Math.Clamp((int)Math.Floor((_realDuration - settings.MinimalParryTime) / 
                                           settings.ParryTimeReductionForConsecutiveParries), 1, 10);
            _consecutiveParriesCounter = new(false, true, settings.ConsecutiveParriesRechargeTime, consecitiveSlots);
            return false;
        }
        
        public override void AddClipsToSet(HashSet<AnimationClip> uniqueClips)
        {
            onSuccessfulParry.AddClipsToSet(uniqueClips);
        }

        public override void Interrupt(bool wasActive)
        {
            MeleeController.ClearParryingReferences();
            if (wasActive)
            {
                _parrying.Activate(settings.ParryStateAfterSuccessfulParryTime);
            }
            else
            {
                _parrying.Deactivate();
            }
            
            if (settings.ParryTimeChanges)
            {
                _consecutiveParriesCounter.Deactivate();
            }
        }

        public override void Finish()
        {
            _parrying.Deactivate();
            MeleeController.ClearParryingReferences();
        }
        

        public override void Start(Utility.FractionTemporaryValue<bool> thisActivityTimer)
        {
            var parryDuration = _realDuration;
            if (settings.ParryTimeChanges)
            {
                parryDuration = Mathf.Max(parryDuration - settings.ParryTimeReductionForConsecutiveParries *
                    _consecutiveParriesCounter.AmountOfActive, 0);
                _consecutiveParriesCounter.Activate();
            }
            //Debug.Log("Parying for " + parryDuration);
            _parrying.Activate(parryDuration);
            MeleeController.ActivateParrying(_parrying, thisActivityTimer, onSuccessfulParry);
            MeleeController.OnParry();
        }
        
        // public override MeleeAdditionalAction Clone()
        // {
        //     var clone = new Parry();
        //     clone.settings = settings;
        //     clone.onSuccessfulParry = onSuccessfulParry?.Clone();
        //     return clone;
        // }
    }
    
    [Serializable, VisibleSubclass]
    public class Attack : MeleeAdditionalAction
    {
        [SerializeField] private AdditionalStateInformationAttack settings;
        protected override MeleeAdditionalStateInformation StateInformation => settings;
        
        public override void Interrupt(bool wasActive)
        {
            MeleeController.MeleeHitboxController.FinishSwing();
        }

        public override void Start(Utility.FractionTemporaryValue<bool> thisActivityTimer)
        {
            MeleeController.MeleeHitboxController.StartSwing(settings.Damage);
            MeleeController.OnAttack();
        }
        // public override MeleeAdditionalAction Clone()
        // {
        //     var clone = new Attack();
        //     clone.settings = settings;
        //     return clone;
        // }
    }
    
    [Serializable, VisibleSubclass]
    public class MoveToTarget : MeleeAdditionalAction
    {
        [SerializeField] private AdditionalStateInformationMoveToTarget settings;
        protected override MeleeAdditionalStateInformation StateInformation => settings;
        private float _duration;
        private float _speed;
        
        
        public override void Interrupt(bool wasActive)
        {
            MeleeController.ToTargetMoveController.StopMovingAndClearReferences();
        }
        

        public override void Start(Utility.FractionTemporaryValue<bool> thisActivityTimer)
        {
            MeleeController.ToTargetMoveController.ActivateMoving(
                settings.TargetDistance, _speed, _duration, settings.AnimationClipIndex, thisActivityTimer);
        }

        public override bool Awake(MeleeController meleeController, MeleePlayable thisMeleePlayable, float thisStateDuration)
        {
            base.Awake(meleeController, thisMeleePlayable, thisStateDuration);

            _duration = thisStateDuration;
            if (settings.UseSpecificDuration)
                _duration = settings.Duration;
            _speed = settings.DistanceToTravel/_duration;
            return false;
        }

        // public override MeleeAdditionalAction Clone()
        // {
        //     var clone = new MoveToTarget();
        //     clone.settings = settings;
        //     return clone;
        // }
    }
    
    [Serializable, VisibleSubclass]
    public class UseSecondHand : MeleeAdditionalAction
    {
        [SerializeField] private AdditionalStateInformationSecondHand settings;
        protected override MeleeAdditionalStateInformation StateInformation => settings;

        public UseSecondHand()
        {
        }

        public UseSecondHand(Utility.BaseActionTransitionsEnum type)
        {
            settings = new  AdditionalStateInformationSecondHand(type);
        }

        public override void Interrupt(bool wasActive)
        {
            MeleeController.SecondHandController.ClearSecondHandReferences();
        }
        

        public override void Start(Utility.FractionTemporaryValue<bool> thisActivityTimer)
        {
            MeleeController.SecondHandController.SetSecondHand(settings.Type, thisActivityTimer);
        }
        // public override MeleeAdditionalAction Clone()
        // {
        //     var clone = new UseSecondHand();
        //     clone.settings = settings;
        //     return clone;
        // }
    }
    
    [Serializable, VisibleSubclass]
    public class OtherAvatarMask : MeleeAdditionalAction
    {
        [SerializeField] private AdditionalStateInformationMask settings;
        protected override MeleeAdditionalStateInformation StateInformation => settings;

        public override void Interrupt(bool wasActive)
        {
            MeleeController.ClearOtherMaskReferences();
        }
        

        public override void Start(Utility.FractionTemporaryValue<bool> thisActivityTimer)
        {
            MeleeController.SetOtherMask(settings.Mask);
        }

    }

    [Serializable, VisibleSubclass]
    public class LoopMark : MeleeAdditionalAction
    {
        private AdditionalStateInformationDummy _settings = new();
        protected override MeleeAdditionalStateInformation StateInformation => _settings;
        
        public override void Interrupt(bool wasActive)
        {
            //do nothing
        }

        public override void Start(Utility.FractionTemporaryValue<bool> thisActivityTimer)
        {
            ThisMeleePlayable.SetLoopMark();
        }
        
        // public override MeleeAdditionalAction Clone()
        // {
        //     var clone = new LoopMark();
        //     clone.settings = settings;
        //     return clone;
        // }
    }
    
    [Serializable, VisibleSubclass]
    public class ReturnToLoopMark : MeleeAdditionalAction
    {
        private AdditionalStateInformationDummy _settings = new();
        protected override MeleeAdditionalStateInformation StateInformation => _settings;


        public override void Interrupt(bool wasActive)
        {
            //do nothing
        }

        public override void Finish()
        {
            ThisMeleePlayable.ReturnToLoopMark();
        }

        public override void Start(Utility.FractionTemporaryValue<bool> thisActivityTimer)
        {
            
        }

        public override bool Awake(MeleeController meleeController, MeleePlayable thisMeleePlayable, float thisStateDuration)
        {
            base.Awake(meleeController, thisMeleePlayable, thisStateDuration);
            return true;
        }
        
        // public override MeleeAdditionalAction Clone()
        // {
        //     var clone = new ReturnToLoopMark();
        //     clone.settings = settings;
        //     return clone;
        // }
    }

    
    
    
    namespace Presets
    {
        [Serializable]
        public class InterruptableAnimationInformation : MeleeAnimationStateInformation
        {
            public InterruptableAnimationInformation() : base(true)
            { }
        }
        
        [Serializable]
        public class InterruptableTransitionInformation : MeleeTransitionStateInformation
        {
            public InterruptableTransitionInformation() : base(true)
            { }
        }
        
        
        
        
        [Serializable]
        public class StandardAnimation : MeleePlayablePart
        {
            public StandardAnimation() : base(
                settings: new MeleeAnimationStateInformation(),
                additionalActions: new())
            { }
        }
        
        [Serializable]
        public class StandardInterruptableAnimation : MeleePlayablePart
        {
            public StandardInterruptableAnimation() : base(
                settings: new InterruptableAnimationInformation(),
                additionalActions: new())
            { }
        }
        
        
        [Serializable]
        public class StandardTransition : MeleePlayablePart
        {
            public StandardTransition() : base(
                settings: new MeleeTransitionStateInformation(),
                additionalActions: new())
            { }
        }
        
        [Serializable]
        public class StandardInterruptableTransition : MeleePlayablePart
        {
            public StandardInterruptableTransition() : base(
                settings: new InterruptableTransitionInformation(),
                additionalActions: new())
            { }
        }
        
        
        [Serializable]
        public class StandardRemain : MeleePlayablePart
        {
            public StandardRemain() : base(
                settings: new MeleeRemainStateInformation(),
                additionalActions: new())
            { }
        }
        
        
        [Serializable]
        public class StandardInterruptableAnimationLoop : MeleePlayablePart
        {
            public StandardInterruptableAnimationLoop() : base(
                settings: new InterruptableAnimationInformation(),
                additionalActions: new List<MeleeAdditionalAction>
                {
                    new LoopMark(),
                    new ReturnToLoopMark()
                })
            { }
        }
        
        
        [Serializable]
        public class StandardAttackPart : MeleePlayablePart
        {
            public StandardAttackPart() : base(
                settings: new MeleeAnimationStateInformation(),
                additionalActions: new List<MeleeAdditionalAction>
                {
                    new Attack(),
                    new MoveToTarget()
                })
            { }
        }
        
        [Serializable]
        public class StandardTransitionToTwoHandedPart : MeleePlayablePart
        {
            public StandardTransitionToTwoHandedPart() : base(
                settings: new InterruptableTransitionInformation(),
                additionalActions: new List<MeleeAdditionalAction>
                {
                    new UseSecondHand(Utility.BaseActionTransitionsEnum.BaseToAction)
                })
            { }
        }
        
        [Serializable]
        public class StandardTwoHandedAttackPart : MeleePlayablePart
        {
            public StandardTwoHandedAttackPart() : base(
                settings: new MeleeAnimationStateInformation(),
                additionalActions: new List<MeleeAdditionalAction>
                {
                    new Attack(),
                    new MoveToTarget(),
                    new UseSecondHand(Utility.BaseActionTransitionsEnum.Action)
                })
            { }
        }
        
        [Serializable]
        public class StandardRecoveryTwoHandedPart : MeleePlayablePart
        {
            public StandardRecoveryTwoHandedPart() : base(
                settings: new MeleeAnimationStateInformation(),
                additionalActions: new List<MeleeAdditionalAction>
                {
                    new UseSecondHand(Utility.BaseActionTransitionsEnum.ActionToBase)
                })
            { }
        }

        [Serializable]
        public class StandardParryPart : MeleePlayablePart
        {
            public StandardParryPart() : base(
                settings: new MeleeAnimationStateInformation(),
                additionalActions: new List<MeleeAdditionalAction>
                {
                    new Parry(new StandardOnSuccessfulParry())
                })
            { }
        }
        
        

        [Serializable]
        public class StandardUnSheath : MeleePlayable
        {
            public StandardUnSheath() : base(new List<MeleePlayablePart>
                {
                    new StandardInterruptableTransition(),
                    new StandardInterruptableAnimation()
                }
            )
            { }
        }
        [Serializable]
        public class StandardSheath : MeleePlayable
        {
            public StandardSheath() : base(new List<MeleePlayablePart>
                {
                    new StandardInterruptableTransition(),
                    new StandardInterruptableAnimation(),
                    new StandardInterruptableTransition()
                }
            )
            { }
        }
        [Serializable]
        public class StandardHold : MeleePlayable
        {
            public StandardHold() : base(new List<MeleePlayablePart>
                {
                    new StandardInterruptableTransition(),
                    new StandardInterruptableAnimationLoop()
                }
            )
            { }
        }
        [Serializable, VisibleSubclass]
        public class StandardOnSuccessfulParry : MeleePlayable
        {
            public StandardOnSuccessfulParry() : base(new List<MeleePlayablePart>
                {
                    new StandardTransition(),
                    new StandardAnimation(),
                    new StandardInterruptableAnimation()
                }
            )
            { }
        }
        
        
        
        [Serializable, VisibleSubclass]
        public class StandardParry : MeleePlayable
        {
            public StandardParry() : base(new List<MeleePlayablePart>
                {
                    new StandardTransition(),
                    new StandardParryPart(),
                    new StandardAnimation()
                }
            )
            { }
        }
        [Serializable, VisibleSubclass]
        public class StandardTwoHandedAttack : MeleePlayable
        {
            public StandardTwoHandedAttack() : base(new List<MeleePlayablePart>
                {
                    new StandardTransitionToTwoHandedPart(),
                    new StandardTwoHandedAttackPart(),
                    new StandardRecoveryTwoHandedPart()
                }
            )
            { }
        }
        [Serializable, VisibleSubclass]
        public class StandardTwoHandedCombo2 : MeleePlayable
        {
            public StandardTwoHandedCombo2() : base(new List<MeleePlayablePart>
                {
                    new StandardTransitionToTwoHandedPart(),
                    new StandardTwoHandedAttackPart(),
                    new StandardRecoveryTwoHandedPart(),
                    new StandardTransitionToTwoHandedPart(),
                    new StandardTwoHandedAttackPart(),
                    new StandardRecoveryTwoHandedPart()
                }
            )
            { }
        }
        [Serializable, VisibleSubclass]
        public class StandardTwoHandedCombo3 : MeleePlayable
        {
            public StandardTwoHandedCombo3() : base(new List<MeleePlayablePart>
                {
                    new StandardTransitionToTwoHandedPart(),
                    new StandardTwoHandedAttackPart(),
                    new StandardRecoveryTwoHandedPart(),
                    new StandardTransitionToTwoHandedPart(),
                    new StandardTwoHandedAttackPart(),
                    new StandardRecoveryTwoHandedPart(),
                    new StandardTransitionToTwoHandedPart(),
                    new StandardTwoHandedAttackPart(),
                    new StandardRecoveryTwoHandedPart()
                }
            )
            { }
        }
        [Serializable, VisibleSubclass]
        public class StandardAttack : MeleePlayable
        {
            public StandardAttack() : base(new List<MeleePlayablePart>
                {
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                    new StandardAnimation()
                }
            )
            { }
        }
        [Serializable, VisibleSubclass]
        public class StandardCombo2 : MeleePlayable
        {
            public StandardCombo2() : base(new List<MeleePlayablePart>
                {
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                    new StandardAnimation(),
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                    new StandardAnimation()
                }
            )
            { }
        }
        [Serializable, VisibleSubclass]
        public class StandardCombo3 : MeleePlayable
        {
            public StandardCombo3() : base(new List<MeleePlayablePart>
                {
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                    new StandardAnimation(),
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                    new StandardAnimation(),
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                    new StandardAnimation()
                }
            )
            { }
        }
        [Serializable, VisibleSubclass]
        public class StandardCombo4 : MeleePlayable
        {
            public StandardCombo4() : base(new List<MeleePlayablePart>
                {
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                    new StandardAnimation(),
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                    new StandardAnimation(),
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                    new StandardAnimation(),
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                    new StandardAnimation()
                }
            )
            { }
        }
        [Serializable, VisibleSubclass]
        public class StandardCombo5 : MeleePlayable
        {
            public StandardCombo5() : base(new List<MeleePlayablePart>
                {
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                    new StandardAnimation(),
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                    new StandardAnimation(),
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                    new StandardAnimation(),
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                    new StandardAnimation(),
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                    new StandardAnimation()
                }
            )
            { }
        }
    }
}
