using UnityEngine;
using System.Collections.Generic;
using System;
using CustomAttributes;
using InterruptionEnum = MeleeComponents.MeleeStateInformation.InterruptionEnum;

namespace MeleeComponents
{
    [Serializable]
    public abstract class MeleeStateInformation
    {
        [SerializeField] protected float duration = 0f;
        [SerializeField] protected InterruptionEnum canBeInterruptedInto;

        public MeleeStateInformation()
        { }

        public MeleeStateInformation(InterruptionEnum canBeInterruptedInto)
        {
            this.canBeInterruptedInto = canBeInterruptedInto;
        }

        public enum InterruptionEnum
        {
            Never, ToOtherCombo, ToAnything
        }

        public float Duration => duration;
        public InterruptionEnum CanBeInterruptedInto => canBeInterruptedInto;
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

        public MeleeAnimationStateInformation(InterruptionEnum canBeInterruptedInto) : base(canBeInterruptedInto)
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

        public MeleeTransitionStateInformation(InterruptionEnum canBeInterruptedInto) : base(canBeInterruptedInto)
        {
        }
    }
    
    [Serializable, VisibleSubclass]
    public class MeleeRemainStateInformation : MeleeStateInformation
    {
        public MeleeRemainStateInformation()
        {
        }

        public MeleeRemainStateInformation(InterruptionEnum canBeInterruptedInto) : base(canBeInterruptedInto)
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
                HaveReturnToLoop = additionalAction.Awake(meleeController, thisMeleePlayable, settings.Duration) || HaveReturnToLoop;
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
                var duration = additionalActions[i].Duration;

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
    
    [Serializable, VisibleSubclass]
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
        public InterruptionEnum CanBeInterruptedInto =>
            _currentPart == Length ? InterruptionEnum.ToAnything : parts[_currentPart].Settings.CanBeInterruptedInto;
        public bool ClipIsNull => _nextPartWithClip == Length;
        public AnimationClip Clip => parts[_nextPartWithClip].Settings.Clip;
        public float ClipSpeed => parts[_currentPart].Settings.ClipSpeed;
        public float CurrentClipTimeFraction => parts[_currentPart].Settings.ShouldAnimationTransition
            ? _currentPart.TimeFraction
            :  1f;
        public bool ShouldAnimationTransition => parts[_currentPart].Settings.ShouldAnimationTransition;
        public bool ShouldUpdateCurrentPlayableAnyway { get; private set; } = false;
        
        
        
        
        
        
        private int _loopMark = 0;
        private bool _shouldReturnToLoopMark = false;
        private bool _startingOtherComboOnFinish = false;

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

        public void StartingOtherComboOnFinish()
        {
            _startingOtherComboOnFinish = true;
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
            ShouldUpdateCurrentPlayableAnyway = false;
            var startPos = (int)Mathf.Floor(fraction);
            var startFraction = fraction - startPos;
            startPos = Math.Clamp(startPos, 0, Length);
            
            
            UpdateCurrentPart(startPos, startFraction);
        }

        public void Update()
        {
            ShouldUpdateCurrentPlayableAnyway = false;
            if (IsActive && _currentPart.CanBeChanged)
            {
                parts[_currentPart].FinishAdditionalActions(_additionalActionsActivityTimers);
                
                int newValue;
                if (_startingOtherComboOnFinish)
                {
                    _startingOtherComboOnFinish = false;
                    newValue = Length;
                }
                else if (_shouldReturnToLoopMark)
                {
                    newValue = _loopMark;
                    _shouldReturnToLoopMark = false;
                    ShouldUpdateCurrentPlayableAnyway = true;
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
            ShouldUpdateCurrentPlayableAnyway = false;
            if (IsActive && !_startingOtherComboOnFinish)
            {
                parts[_currentPart].InterruptAdditionalActions(_additionalActionsActivityTimers);
            }
        }
    }
    
    [Serializable]
    public class MeleeAdditionalStateInformation
    {
        [SerializeField] protected float durationFraction = 1f;
        public float DurationFraction => durationFraction;
        
        public virtual void Awake()
        { }
    }

    [Serializable]
    public class AdditionalStateInformationParry : MeleeAdditionalStateInformation
    {
        [Header("Not successful parry")]
        [SerializeField] private float parryTimeReductionForConsecutiveParries;
        [SerializeField] private float consecutiveParriesRechargeTime;
        [SerializeField] private float minimalParryTime;
        
        [Header("Successful parry")]
        [SerializeField] private float parryStateAfterSuccessfulParryTime;
        [SerializeField] private int onSuccessfulParryPlayableIndex = 1;

        public bool ParryTimeChanges { get;private set; }
        
        public float ParryStateAfterSuccessfulParryTime => parryStateAfterSuccessfulParryTime;
        public float ParryTimeReductionForConsecutiveParries => parryTimeReductionForConsecutiveParries;
        public float ConsecutiveParriesRechargeTime => consecutiveParriesRechargeTime;
        public float MinimalParryTime => minimalParryTime;
        public int OnSuccessfulParryPlayableIndex => onSuccessfulParryPlayableIndex;

        public override void Awake()
        {
            ParryTimeChanges = parryTimeReductionForConsecutiveParries > 0.001f;
        }
    }
    
    [Serializable]
    public class AdditionalStateInformationAttack : MeleeAdditionalStateInformation
    {
        [SerializeField] private float damage;
        [SerializeField] private float poiseDamage;

        public float Damage => damage;
        public float PoiseDamage => poiseDamage;
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
        [Header("Rotation")] 
        [SerializeField] private float rotateByDegrees = 0f;
        [SerializeField] private bool forceLookToTarget = false;
        
        [Header("Movement")]
        [SerializeField] private float distanceToTravel = 0f;
        [SerializeField] private float targetDistance = 0f;
        [SerializeField] private AnimationCurve speedCurve = new AnimationCurve(new Keyframe(0f, 0f),new Keyframe(0.1f,1f),new Keyframe(0.9f,1f), new Keyframe(1f, 0f));
        public float TargetDistance => targetDistance;
        public float DistanceToTravel => distanceToTravel;
        public float CurveAverage { get;private set; }
        public AnimationCurve SpeedCurve => speedCurve;
        public float RotateByRad { get; private set; }
        public bool ForceLookToTarget => forceLookToTarget;
        
        public override void Awake()
        {
            CurveAverage = Utility.EvaluateCurveAverage(speedCurve);
            RotateByRad = rotateByDegrees * Mathf.Deg2Rad;
        }
    }
    
    [Serializable]
    public class AdditionalStateInformationLookAtTarget : MeleeAdditionalStateInformation
    {
        [Header("Rotation")] 
        [SerializeField] private float rotateByDegrees = 30f;
        [SerializeField] private bool forceLookToTarget = false;
        [SerializeField] private bool standInPlace = true;
        public float RotateByRad { get; private set; }
        public bool ForceLookToTarget => forceLookToTarget;
        public bool StandInPlace => standInPlace;
        public override void Awake()
        {
            RotateByRad = rotateByDegrees * Mathf.Deg2Rad;
        }
    }
    
    [Serializable]
    public class AdditionalStateInformationChangeToOtherPlayable
    {
        [SerializeField] private int combo;
        public int Combo => combo;
    }
    
    [Serializable]
    public class AdditionalStateInformationUseSecondHand : MeleeAdditionalStateInformation
    {
        public enum UseSecondHandEnum
        {
            Activate, RemainActive
        }

        [SerializeField] private UseSecondHandEnum behavior = UseSecondHandEnum.Activate;

        public bool Type => behavior == UseSecondHandEnum.Activate;
    }
    
    public class AdditionalStateInformationDummy : MeleeAdditionalStateInformation
    {
        public AdditionalStateInformationDummy()
        {
            durationFraction = 1f;
        }
    }
    
    [Serializable]
    public abstract class MeleeAdditionalAction
    {
        protected abstract MeleeAdditionalStateInformation StateInformation { get; }
        protected MeleeController ThisMeleeController;
        protected MeleePlayable ThisMeleePlayable;
        public float Duration { get; private set;}
        
        
        public virtual void AddClipsToSet(HashSet<AnimationClip> uniqueClips){}

        public virtual bool Awake(MeleeController meleeController, MeleePlayable thisMeleePlayable, float thisStateDuration)
        {
            ThisMeleeController = meleeController;
            ThisMeleePlayable = thisMeleePlayable;
            StateInformation.Awake();
            Duration = thisStateDuration * Mathf.Clamp01(StateInformation.DurationFraction);
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
        protected override MeleeAdditionalStateInformation StateInformation => settings;
        
        private Utility.MultipleTemporaryValue<bool> _consecutiveParriesCounter;
        private Utility.TemporaryValue<bool> _parrying = new(false, true);


        public override bool Awake(MeleeController meleeController, MeleePlayable thisMeleePlayable, float thisStateDuration)
        {
            base.Awake(meleeController, thisMeleePlayable, thisStateDuration);

            
            if (!settings.ParryTimeChanges) return false;
            
            var consecitiveSlots =
                Math.Clamp((int)Math.Floor((Duration - settings.MinimalParryTime) / 
                                           settings.ParryTimeReductionForConsecutiveParries), 1, 10);
            _consecutiveParriesCounter = new(false, true, settings.ConsecutiveParriesRechargeTime, consecitiveSlots);
            return false;
        }

        public override void Interrupt(bool wasActive)
        {
            ThisMeleeController.ClearParryingReferences();
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
            ThisMeleeController.ClearParryingReferences();
        }
        

        public override void Start(Utility.FractionTemporaryValue<bool> thisActivityTimer)
        {
            var parryDuration = Duration;
            if (settings.ParryTimeChanges)
            {
                parryDuration = Mathf.Max(parryDuration - settings.ParryTimeReductionForConsecutiveParries *
                    _consecutiveParriesCounter.AmountOfActive, 0);
                _consecutiveParriesCounter.Activate();
            }
            _parrying.Activate(parryDuration);
            ThisMeleeController.ActivateParrying(_parrying, thisActivityTimer, settings.OnSuccessfulParryPlayableIndex);
            ThisMeleeController.OnParry();
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
            ThisMeleeController.MeleeHitboxController.FinishSwing();
        }

        public override void Start(Utility.FractionTemporaryValue<bool> thisActivityTimer)
        {
            ThisMeleeController.MeleeHitboxController.StartSwing(settings.Damage, settings.PoiseDamage);
            ThisMeleeController.OnAttack();
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
        private float _speedMultiplier;
        private float _rotationSpeed;
        
        public override void Interrupt(bool wasActive)
        {
            ThisMeleeController.TransformController.StopMovingAndClearReferences();
        }
        

        public override void Start(Utility.FractionTemporaryValue<bool> thisActivityTimer)
        {
            ThisMeleeController.TransformController.ActivateMoving(
                settings.TargetDistance, 
                _speedMultiplier,
                settings.SpeedCurve,
                _rotationSpeed, 
                settings.ForceLookToTarget,
                thisActivityTimer);
        }

        public override bool Awake(MeleeController meleeController, MeleePlayable thisMeleePlayable, float thisStateDuration)
        {
            base.Awake(meleeController, thisMeleePlayable, thisStateDuration);
            if (Duration < 0.001f)
            {
                _speedMultiplier = 0f;
                _rotationSpeed = 0f;
            }
            else
            {
                _speedMultiplier = settings.DistanceToTravel / (settings.CurveAverage * Duration);
                _rotationSpeed = settings.RotateByRad / Duration;
            }

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
    public class LookAtTarget : MeleeAdditionalAction
    {
        [SerializeField] private AdditionalStateInformationLookAtTarget settings;
        protected override MeleeAdditionalStateInformation StateInformation => settings;
        private float _rotationSpeed;
        
        public override void Interrupt(bool wasActive)
        {
            ThisMeleeController.TransformController.StopMovingAndClearReferences();
        }
        

        public override void Start(Utility.FractionTemporaryValue<bool> thisActivityTimer)
        {
            ThisMeleeController.TransformController.ActivateStandingAndLooking(_rotationSpeed, settings.ForceLookToTarget, settings.StandInPlace, thisActivityTimer);
        }

        public override bool Awake(MeleeController meleeController, MeleePlayable thisMeleePlayable, float thisStateDuration)
        {
            base.Awake(meleeController, thisMeleePlayable, thisStateDuration);
            
            
            if (Duration < 0.001f)
            {
                _rotationSpeed = 0f;
            }
            else
            {
                _rotationSpeed = settings.RotateByRad / Duration;
            }

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
        [SerializeField] private AdditionalStateInformationUseSecondHand settings = new();
        protected override MeleeAdditionalStateInformation StateInformation => settings;

        private bool _shouldTurnOff = false;
        
        public override void Interrupt(bool wasActive)
        {
            if(_shouldTurnOff)
                ThisMeleeController.SecondHandController.ClearSecondHandReferences();
            _shouldTurnOff = false;
        }
        

        public override void Start(Utility.FractionTemporaryValue<bool> thisActivityTimer)
        {
            if (settings.Type)
            {
                _shouldTurnOff = true;
                ThisMeleeController.SecondHandController.SetSecondHand(thisActivityTimer);
            }
            else
            {
                _shouldTurnOff =
                    ThisMeleeController.SecondHandController.SetSecondHandIfWasSecondHand(thisActivityTimer);
            }
        }

    }
    
    [Serializable, VisibleSubclass]
    public class DontAnimateLegs : MeleeAdditionalAction
    {
        private AdditionalStateInformationDummy _settings = new();
        //[SerializeField] private AdditionalStateInformationUseSecondHand settings = new();
        protected override MeleeAdditionalStateInformation StateInformation => _settings;

        public override void Interrupt(bool wasActive)
        {
            ThisMeleeController.StopDontAnimateLegs();
        }
        

        public override void Start(Utility.FractionTemporaryValue<bool> thisActivityTimer)
        {
            ThisMeleeController.ActivateDontAnimateLegs(thisActivityTimer);
        }

    }
    
    [Serializable, VisibleSubclass]
    public class HyperArmor : MeleeAdditionalAction
    {
        [SerializeField] private MeleeAdditionalStateInformation settings = new();
        protected override MeleeAdditionalStateInformation StateInformation => settings;

        public override void Interrupt(bool wasActive)
        {
            ThisMeleeController.GettingHitController.StopHyperArmor();
        }
        

        public override void Start(Utility.FractionTemporaryValue<bool> thisActivityTimer)
        {
            ThisMeleeController.GettingHitController.ActivateHyperArmor(thisActivityTimer);
        }

    }
    
    // [Serializable, VisibleSubclass]
    // public class OtherAvatarMask : MeleeAdditionalAction
    // {
    //     [SerializeField] private AdditionalStateInformationMask settings;
    //     protected override MeleeAdditionalStateInformation StateInformation => settings;
    //
    //     public override void Interrupt(bool wasActive)
    //     {
    //         ThisMeleeController.ReturnMask();
    //     }
    //     
    //
    //     public override void Start(Utility.FractionTemporaryValue<bool> thisActivityTimer)
    //     {
    //         ThisMeleeController.SetOtherMask(settings.Mask);
    //     }
    //
    // }

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
    
    [Serializable, VisibleSubclass]
    public class StartComboOnFinish : MeleeAdditionalAction
    {
        private AdditionalStateInformationDummy _settings = new();
        [SerializeField] private AdditionalStateInformationChangeToOtherPlayable settings = new();
        protected override MeleeAdditionalStateInformation StateInformation => _settings;


        public override void Interrupt(bool wasActive)
        {
            //do nothing
        }

        public override void Finish()
        {
            ThisMeleePlayable.StartingOtherComboOnFinish();
            ThisMeleeController.PlayCombo(settings.Combo);
        }

        public override void Start(Utility.FractionTemporaryValue<bool> thisActivityTimer)
        {
            
        }
    }

    
    
    
    namespace Presets
    {
        [Serializable]
        public class InterruptableAnimationInformation : MeleeAnimationStateInformation
        {
            public InterruptableAnimationInformation() : base(InterruptionEnum.ToAnything)
            { }
        }
        
        [Serializable]
        public class InterruptableTransitionInformation : MeleeTransitionStateInformation
        {
            public InterruptableTransitionInformation() : base(InterruptionEnum.ToAnything)
            { }
        }
        
        [Serializable]
        public class InterruptableToComboAnimationInformation : MeleeAnimationStateInformation
        {
            public InterruptableToComboAnimationInformation() : base(InterruptionEnum.ToOtherCombo)
            { }
        }
        
        [Serializable]
        public class InterruptableToComboTransitionInformation : MeleeTransitionStateInformation
        {
            public InterruptableToComboTransitionInformation() : base(InterruptionEnum.ToOtherCombo)
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
        public class StandardInterruptableToComboAnimation : MeleePlayablePart
        {
            public StandardInterruptableToComboAnimation() : base(
                settings: new InterruptableToComboAnimationInformation(),
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
        public class StandardInterruptableToComboTransition : MeleePlayablePart
        {
            public StandardInterruptableToComboTransition() : base(
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
                    new UseSecondHand()
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
                    new UseSecondHand()
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
                    new Parry()
                })
            { }
        }
        
        [Serializable]
        public class StandardChargingPart : MeleePlayablePart
        {
            public StandardChargingPart() : base(
                settings: new InterruptableToComboAnimationInformation(),
                additionalActions: new List<MeleeAdditionalAction>
                {
                    new StartComboOnFinish()
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
                    new StandardInterruptableToComboAnimation(),
                    new StandardInterruptableToComboAnimation()
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
        public class StandardCharging : MeleePlayable
        {
            public StandardCharging() : base(new List<MeleePlayablePart>
                {
                    new StandardTransition(),
                    new StandardChargingPart()
                }
            )
            { }
        }
        [Serializable, VisibleSubclass]
        public class StandardAttack : MeleePlayable
        {
            public StandardAttack() : base(new List<MeleePlayablePart>
                {
                    new StandardInterruptableToComboTransition(),
                    new StandardAttackPart()
                }
            )
            { }
        }
        [Serializable, VisibleSubclass]
        public class StandardCombo2 : MeleePlayable
        {
            public StandardCombo2() : base(new List<MeleePlayablePart>
                {
                    new StandardInterruptableToComboTransition(),
                    new StandardAttackPart(),
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                }
            )
            { }
        }
        [Serializable, VisibleSubclass]
        public class StandardCombo3 : MeleePlayable
        {
            public StandardCombo3() : base(new List<MeleePlayablePart>
                {
                    new StandardInterruptableToComboTransition(),
                    new StandardAttackPart(),
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                    new StandardInterruptableTransition(),
                    new StandardAttackPart()
                }
            )
            { }
        }
        [Serializable, VisibleSubclass]
        public class StandardCombo4 : MeleePlayable
        {
            public StandardCombo4() : base(new List<MeleePlayablePart>
                {
                    new StandardInterruptableToComboTransition(),
                    new StandardAttackPart(),
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                    new StandardInterruptableTransition(),
                    new StandardAttackPart(),
                }
            )
            { }
        }
    }
}
