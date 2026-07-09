using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

public abstract class MeleeController : MonoBehaviour
{
    [Header("Sheathe")] 
    [SerializeField] private TwoPartAction unSheath;
    [SerializeField] private TwoPartAction sheath;
    [SerializeField] private RemainLastPosition positionAfterSheath;

    [Header("Holding")]
    [SerializeField] private TwoPartAction hold;
    
    [Header("Combos")]
    [SerializeField] private List<Combo> combos;
    
    [Header("Parry")] 
    [SerializeField] private FourPartAction parry;
    [SerializeField] private FourPartAction afterSuccessfulParry;

    [Header("Weapon objects")] 
    [SerializeField] private GameObject sheathedWeapon;
    [SerializeField] private GameObject handWeapon;
    
    [Header("Settings")]
    [SerializeField] private AvatarMask meleAvatarMask;

    
    private interface IMeleeEvent
    {
        public event Action OnActionStarted;
        public event Action OnActionEnded;
    }
    private interface IMeleePlayable
    {
        float CurrentStateFraction { get; }
        AnimationClip Clip { get; }
        bool IsClipNull { get; }
        float ClipSpeed { get; }
        
        bool IsActive { get; }
        
        bool CanBeSafelyInterrupted { get; }
        
        bool IsPositioning { get; }

        void Awake(int stateMask);
        
        /// <returns>
        /// Time fraction of current phase
        /// </returns>
        float Interrupt();
        void Update();
        void Start(float startingFraction, bool skipPositioning);

        void AddClipsToSet(HashSet<AnimationClip> uniqueClips);
    }
    
    [Serializable]
    public class Combo : IMeleePlayable, IMeleeEvent
    {
        [SerializeField] private List<FourPartAction> parts;

        public event Action OnActionStarted;
        public event Action OnActionEnded;
        
        
        private void HandleActionStarted() => OnActionStarted?.Invoke();
        private void HandleActionEnded() => OnActionEnded?.Invoke();
        
        private int _currentPart = -1;
        
        public bool IsActive => _currentPart != -1;
        
        public void Awake(int stateMask)
        {
            if (parts == null) return;

            foreach (var part in parts)
            {
                part.OnActionStarted -= HandleActionStarted;
                part.OnActionEnded -= HandleActionEnded;
                part.OnActionStarted += HandleActionStarted;
                part.OnActionEnded += HandleActionEnded;
                part.Awake(stateMask);
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

        public bool IsPositioning => _currentPart != -1 && parts[_currentPart].IsPositioning;

        public float Interrupt()
        {
            float res = 1f;
            if (_currentPart != -1)
            {
                res = parts[_currentPart].Interrupt();
                _currentPart = -1;
            }

            return res;
        }

        public void Update()
        {
            if (_currentPart != -1)
            {
                parts[_currentPart].Update();
                if (!parts[_currentPart].IsActive)
                {
                    if (_currentPart >= parts.Count - 1)
                    {
                        _currentPart = -1;
                    }
                    else
                    {
                        _currentPart++;
                        parts[_currentPart].Start(0f, false);
                    }
                }
            }
        }

        public void Start(float fraction, bool skipPositioning)
        {
            if (parts.Count == 0) return;
            
            if(_currentPart != -1)
                parts[_currentPart].Interrupt();
            _currentPart = 0;
            parts[_currentPart].Start(fraction, skipPositioning);
        }
        
        public float CurrentStateFraction => _currentPart == -1 ? 0f : parts[_currentPart].CurrentStateFraction;
        
        public AnimationClip Clip => _currentPart == -1 ? null : parts[_currentPart].Clip;
        
        
        public bool IsClipNull => _currentPart == -1 || parts[_currentPart].IsClipNull;
        
        public float ClipSpeed => _currentPart == -1 ? 0f : parts[_currentPart].ClipSpeed;
    }
    
    [Serializable]
    public class FourPartAction : IMeleePlayable, IMeleeEvent
    {
        [SerializeField] private float positionTime = 0.1f;
        [SerializeField] private bool hasPreparation = false;
        [SerializeField] private float prepareTime = 0.1f;
        [SerializeField] private AnimationClip prepareClip;
        [SerializeField] private float actionTime = 0.1f;
        [SerializeField] private AnimationClip actionClip;
        [SerializeField] private float recoveryTime = 0.1f;
        [SerializeField] private AnimationClip recoveryClip;
        
        public event Action OnActionStarted;
        public event Action OnActionEnded;
        
        private float _prepareSpeed;
        private float _actionSpeed;
        private float _recoverySpeed;
        
        
        [Flags]
        public enum ActionStateEnum{
            Non = 0,
            Position = 1, 
            Prepare = 1<<1, 
            Action = 1<<2, 
            Recovery = 1<<3
            
        }
        
        private Utility.FractionBlockingValueTimer<ActionStateEnum> _actionState = ActionStateEnum.Non;

        public bool IsActive => _actionState.Value != ActionStateEnum.Non;

        private ActionStateEnum _canBeSafelyInterruptedMask;

        public void Awake(int stateMask)
        {
            _canBeSafelyInterruptedMask = (ActionStateEnum)~stateMask;
            _prepareSpeed = prepareClip != null ? AnimationUtility.GetAnimationSpeed(prepareClip, prepareTime) : 1f;
            _actionSpeed = actionClip != null ? AnimationUtility.GetAnimationSpeed(actionClip, actionTime) : 1f;
            _recoverySpeed = recoveryClip != null ? AnimationUtility.GetAnimationSpeed(recoveryClip, recoveryTime) : 1f;
        }
        
        public void AddClipsToSet(HashSet<AnimationClip> uniqueClips)
        {
            if(prepareClip != null)
                uniqueClips.Add(prepareClip);
            if(actionClip != null)
                uniqueClips.Add(actionClip);
            if(recoveryClip != null)
                uniqueClips.Add(recoveryClip);
        }
        
        public bool CanBeSafelyInterrupted => (_actionState.Value & _canBeSafelyInterruptedMask)==0;
        
        public bool IsPositioning =>  _actionState.Value == ActionStateEnum.Position;
        
        public void Update()
        {
            if (_actionState.CanBeChanged)
            {
                switch (_actionState.Value)
                {
                    case ActionStateEnum.Position:
                        if(hasPreparation)
                        {
                            _actionState.SetForce(ActionStateEnum.Prepare, prepareTime);
                        }
                        else
                        {
                            _actionState.SetForce(ActionStateEnum.Action, actionTime);
                            OnActionStarted?.Invoke();
                        }
                        break;
                    case ActionStateEnum.Prepare:
                        _actionState.SetForce(ActionStateEnum.Action, actionTime);
                        OnActionStarted?.Invoke();
                        break;
                    case ActionStateEnum.Action:
                        _actionState.SetForce(ActionStateEnum.Recovery, recoveryTime);
                        OnActionEnded?.Invoke();
                        break;
                    case ActionStateEnum.Recovery:
                        _actionState.SetForce(ActionStateEnum.Non);
                        break;
                }
            }
        }

        public float Interrupt()
        {
            var res = _actionState.TimeFraction;
            _actionState.SetForce(ActionStateEnum.Non);
            return res;
        }

        public void Start(float fraction, bool skipPositioning)
        {
            if(_actionState.Value == ActionStateEnum.Non)
            {
                if (skipPositioning)
                {
                    if(hasPreparation)
                        _actionState.SetForce(ActionStateEnum.Prepare, prepareTime, fraction);
                    else
                        _actionState.SetForce(ActionStateEnum.Action, actionTime, fraction);
                }
                else 
                    _actionState.SetForce(ActionStateEnum.Position, positionTime, fraction);
            }
        }

        
        public float CurrentStateFraction => _actionState.Value == ActionStateEnum.Position ? Mathf.Clamp01(_actionState.TimeFraction) : 0f;

        public AnimationClip Clip 
        {
            get
            {
                switch (_actionState.Value)
                {
                    case ActionStateEnum.Prepare:
                        return prepareClip;
                    case ActionStateEnum.Action:
                        return actionClip;
                    case ActionStateEnum.Recovery:
                        return recoveryClip;
                    case ActionStateEnum.Position:
                        return hasPreparation ? prepareClip : actionClip; 
                    default:
                        return null;
                }
            }
        }
        
        public bool IsClipNull => _actionState.Value == ActionStateEnum.Non;

        public float ClipSpeed 
        {
            get
            {
                float res = 0;
                switch (_actionState.Value)
                {
                    case ActionStateEnum.Prepare:
                        res = _prepareSpeed;
                        break;
                    case ActionStateEnum.Action:
                        res = _actionSpeed;
                        break;
                    case ActionStateEnum.Recovery:
                        res = _recoverySpeed;
                        break;
                }
                return res;
            }
        }
    }

    [Serializable]
    public class TwoPartAction : IMeleePlayable
    {
        [SerializeField] private float positionTime = 0.1f;
        [SerializeField] private float actionTime = 0.1f;
        [SerializeField] private AnimationClip actionClip;

        
        private float _actionSpeed;
        
        
        [Flags]
        public enum ActionStateEnum{
            Non = 0, 
            Position = 1, 
            Action = 1<<1
        }
        
        private Utility.FractionBlockingValueTimer<ActionStateEnum> _actionState = ActionStateEnum.Non;
        
        public float CurrentStateFraction => _actionState.Value == ActionStateEnum.Position ? Mathf.Clamp01(_actionState.TimeFraction) : 0f;

        public AnimationClip Clip => _actionState.Value == ActionStateEnum.Non ? null :  actionClip;
        public bool IsClipNull => _actionState.Value == ActionStateEnum.Non;

        public float ClipSpeed => _actionState.Value == ActionStateEnum.Action ? _actionSpeed : 0f;
        
        public bool IsActive => _actionState.Value != ActionStateEnum.Non;

        private ActionStateEnum _canBeSafelyInterruptedMask;

        public void Awake(int stateMask)
        {
            _canBeSafelyInterruptedMask = (ActionStateEnum)~stateMask;
            _actionSpeed = actionClip != null ? AnimationUtility.GetAnimationSpeed(actionClip, actionTime) : 1f;
        }
        
        public void AddClipsToSet(HashSet<AnimationClip> uniqueClips)
        {
            if(actionClip != null)
                uniqueClips.Add(actionClip);
        }
        
        public bool CanBeSafelyInterrupted => (_actionState.Value & _canBeSafelyInterruptedMask)==0 ;
        
        public bool IsPositioning =>  _actionState.Value == ActionStateEnum.Position;
        
        public float Interrupt()
        {
            var res = _actionState.TimeFraction;
            _actionState.SetForce(ActionStateEnum.Non);
            return res;
        }

        public void Update()
        {
            if (_actionState.CanBeChanged)
            {
                switch (_actionState.Value)
                {
                    case ActionStateEnum.Position:
                        _actionState.SetForce(ActionStateEnum.Action, actionTime);
                        break;
                    case ActionStateEnum.Action:
                        _actionState.SetForce(ActionStateEnum.Non);
                        break;
                }
            }
        }

        public void Start(float fraction, bool skipPositioning)
        {
            if (_actionState.Value == ActionStateEnum.Non)
            {
                if(skipPositioning)
                    _actionState.SetForce(ActionStateEnum.Action, actionTime, fraction);
                else
                    _actionState.SetForce(ActionStateEnum.Position, positionTime, fraction);
            }
        }
        
        public ActionStateEnum State =>  _actionState.Value;
    }

    [Serializable]
    public class RemainLastPosition : IMeleePlayable
    {
        [SerializeField] private float duration = 0.1f;


        [Flags]
        public enum ActionStateEnum
        {
            Non = 0,
            Position = 1
        }

        private Utility.FractionBlockingValueTimer<ActionStateEnum> _actionState = ActionStateEnum.Non;

        public float CurrentStateFraction => _actionState.Value == ActionStateEnum.Position
            ? Mathf.Clamp01(_actionState.TimeFraction)
            : 0f;

        public AnimationClip Clip => null;
        public bool IsClipNull => true;

        public float ClipSpeed => 0f;

        public bool IsActive => _actionState.Value != ActionStateEnum.Non;

        private ActionStateEnum _canBeSafelyInterruptedMask;

        public void Awake(int stateMask)
        {
            _canBeSafelyInterruptedMask = (ActionStateEnum)~stateMask;
        }

        public void AddClipsToSet(HashSet<AnimationClip> uniqueClips)
        {
        }

        public bool CanBeSafelyInterrupted => (_actionState.Value & _canBeSafelyInterruptedMask)==0 ;
        public bool IsPositioning =>  _actionState.Value == ActionStateEnum.Position;
        
        public float Interrupt()
        {
            var res = _actionState.TimeFraction;
            _actionState.SetForce(ActionStateEnum.Non);
            return res;
        }

        public void Update()
        {
            if (_actionState.CanBeChanged && _actionState.Value ==  ActionStateEnum.Position)
            {
                _actionState.SetForce(ActionStateEnum.Non);
            }
        }

        public void Start(float fraction, bool skipPositioning)
        {
            if (_actionState.Value == ActionStateEnum.Non)
            {
                if(!skipPositioning)
                    _actionState.SetForce(ActionStateEnum.Position, duration, fraction);
            }
        }
    }
    

    protected virtual void Awake()
    {
        parry.OnActionStarted -= OnParryStart;
        parry.OnActionEnded -= OnParryEnd;
        parry.OnActionStarted += OnParryStart;
        parry.OnActionEnded += OnParryEnd;
        var alwaysInterruptableTwoActionMask = 1 | 1 << 1;
        sheath.Awake(alwaysInterruptableTwoActionMask);
        unSheath.Awake(alwaysInterruptableTwoActionMask);
        hold.Awake(alwaysInterruptableTwoActionMask);
            
        positionAfterSheath.Awake(1);

        var comboInterruptMask = 1 | 1 << 1;
        foreach (var combo in combos)
        {
            combo.OnActionStarted -= OnAttackStart;
            combo.OnActionEnded -= OnAttackEnd;
            combo.OnActionStarted += OnAttackStart;
            combo.OnActionEnded += OnAttackEnd;
            combo.Awake(comboInterruptMask);
        }
        
        afterSuccessfulParry.Awake(0);
        parry.Awake(0);
    }


    protected abstract void OnAttackStart();
    protected abstract void OnAttackEnd();
    protected abstract void OnParryStart();
    protected abstract void OnParryEnd();
    
    public void OnSuccessfulParry()
    {
        if (State != MeleeStateEnum.Parry) return;
        
        State = MeleeStateEnum.AfterSuccessfulParry;
        ChangeCurrentMeleePlayable(afterSuccessfulParry);
    }
    
    /// <returns>
    /// Must return -1 if no combo should be played
    /// </returns>
    protected abstract int WhatComboToPlay();
    protected abstract bool ShouldParry();
    protected abstract bool ShouldHold();
    protected abstract bool ShouldInterrupt();

    protected abstract bool ShouldSwitchStateToNon();
    
    public enum MeleeStateEnum
    {
        Non, UnSheath, Sheathe, PositioningAfterSheath, Hold,
        Combo, Parry, AfterSuccessfulParry
    }

    public MeleeStateEnum State { get; private set; } = MeleeStateEnum.Non;
    public int CurrentCombo { get; private set; } = -1;

    public bool CanBeSafelyInterrupted => State == MeleeStateEnum.Non || _currentMeleePlayable.CanBeSafelyInterrupted;
    
    private IMeleePlayable _currentMeleePlayable;

    private void ChangeCurrentMeleePlayable(IMeleePlayable meleePlayable, bool makeNewFractionOneMinus = false, bool skipPositioning = false)
    {
        if (makeNewFractionOneMinus && _currentMeleePlayable != null)
        {
            meleePlayable.Start(_currentMeleePlayable.Interrupt(), skipPositioning);
        }
        else
        {
            if (_currentMeleePlayable != null)
                _currentMeleePlayable.Interrupt();
            meleePlayable.Start(0f, skipPositioning);
        }
        _currentMeleePlayable = meleePlayable;
    }

    private void DoSomethingBasedOnInput()
    {
        if (ShouldInterrupt())
        {
            if (ShouldHold())
            {
                CurrentCombo = -1;
                if (State != MeleeStateEnum.Hold || !_currentMeleePlayable.IsActive)
                {
                    State = MeleeStateEnum.Hold;
                    ChangeCurrentMeleePlayable(hold);
                }
            }
            else
            {
                CurrentCombo = -1;
                State = MeleeStateEnum.Sheathe;
                ChangeCurrentMeleePlayable(sheath);
            }

            return;
        }

        if (!_currentMeleePlayable.CanBeSafelyInterrupted) return;
        
        if (ShouldParry())
        {
            CurrentCombo = -1;
            if (State != MeleeStateEnum.Parry || !_currentMeleePlayable.IsActive)
            {
                State = MeleeStateEnum.Parry;
                ChangeCurrentMeleePlayable(parry);
            }
            return;
        }
                    
        var combo = WhatComboToPlay();
        if (combo != -1)
        {
            if (combo != CurrentCombo || !_currentMeleePlayable.IsActive)
            {
                CurrentCombo = combo;
                State = MeleeStateEnum.Combo;
                ChangeCurrentMeleePlayable(combos[combo]);
            }
        }
        else if (ShouldHold())
        {
            CurrentCombo = -1;
            if (State != MeleeStateEnum.Hold || !_currentMeleePlayable.IsActive)
            {
                State = MeleeStateEnum.Hold;
                ChangeCurrentMeleePlayable(hold);
            }
        }
        else
        {
            CurrentCombo = -1;
            State = MeleeStateEnum.Sheathe;
            ChangeCurrentMeleePlayable(sheath);
        }
    }
    
    private void UpdateMeleeState()
    {
        if (ShouldSwitchStateToNon())
        {
            State = MeleeStateEnum.Non;
            return;
        }
        
        if (State == MeleeStateEnum.Non)
        {
            if (ShouldHold() || ShouldParry() || WhatComboToPlay() != -1)
            {
                State = MeleeStateEnum.UnSheath;
                ChangeCurrentMeleePlayable(unSheath);
            }
            return;
        }
        
        
        _currentMeleePlayable.Update();
        

        switch (State)
        {
            case MeleeStateEnum.UnSheath:
                if (ShouldHold() || ShouldParry() || WhatComboToPlay() != -1)
                {
                    if (!_currentMeleePlayable.IsActive)
                    {
                        DoSomethingBasedOnInput();
                    }
                }
                else
                {
                    if (_currentMeleePlayable.IsPositioning)
                    {
                        State = MeleeStateEnum.PositioningAfterSheath;
                        ChangeCurrentMeleePlayable(positionAfterSheath, true);
                    }
                    else
                    {
                        //actively putting sword out of sheath
                        State = MeleeStateEnum.Sheathe;
                        ChangeCurrentMeleePlayable(sheath, true, true);
                    }
                }
                break;
            case MeleeStateEnum.Hold or MeleeStateEnum.Parry or MeleeStateEnum.Combo or MeleeStateEnum.AfterSuccessfulParry:
                DoSomethingBasedOnInput();

                break;
            case MeleeStateEnum.Sheathe:
                if (ShouldHold() || ShouldParry() || WhatComboToPlay() != -1)
                {
                    if (_currentMeleePlayable.IsPositioning)
                    {
                        DoSomethingBasedOnInput();
                    }
                    else
                    {
                        //actively putting sword into sheath
                        State = MeleeStateEnum.UnSheath;
                        ChangeCurrentMeleePlayable(unSheath, true, true);
                    }
                }
                break;
            case MeleeStateEnum.PositioningAfterSheath:
                if (ShouldHold() || ShouldParry() || WhatComboToPlay() != -1)
                {
                    State = MeleeStateEnum.UnSheath;
                    ChangeCurrentMeleePlayable(unSheath, true);
                }
                break;
        }
        

        if (!_currentMeleePlayable.IsActive)
        {
            State = MeleeStateEnum.Non;
            _currentMeleePlayable = null;
        }
        
    }

    private void UpdateWeaponProp()
    {
        if (State == MeleeStateEnum.Non || 
            State == MeleeStateEnum.PositioningAfterSheath || 
            (State == MeleeStateEnum.UnSheath && _currentMeleePlayable.IsPositioning)
            )
        {
            sheathedWeapon.SetActive(true);
            handWeapon.SetActive(false);
        }
        else
        {
            sheathedWeapon.SetActive(false);
            handWeapon.SetActive(true);
        }
    }
    
    private PlayableGraph _graph;
    private AnimationLayerMixerPlayable _layerMixer;
    private AnimationMixerPlayable _animationMixer;
    private int _destinationLayerPort;
    private Dictionary<AnimationClip, int> _clipToPort;
    
    
    public void InitializePlayableGraph(PlayableGraph graph, AnimationLayerMixerPlayable layerMixer, uint destinationLayerPort)
    {
        _graph = graph;
        _layerMixer = layerMixer;
        _destinationLayerPort = (int)destinationLayerPort;
        
        var uniqueClips = new HashSet<AnimationClip>();
    
        unSheath.AddClipsToSet(uniqueClips);
        sheath.AddClipsToSet(uniqueClips);
        hold.AddClipsToSet(uniqueClips);
        parry.AddClipsToSet(uniqueClips);
        afterSuccessfulParry.AddClipsToSet(uniqueClips);
        foreach (var combo in combos)
        {
            combo.AddClipsToSet(uniqueClips);
        }
        
        _clipToPort = new Dictionary<AnimationClip, int>();
        var portIdx = 0;
        foreach (var clip in uniqueClips) {
            _clipToPort[clip] = portIdx++;
        }

        _animationMixer = AnimationMixerPlayable.Create(_graph, _clipToPort.Count);
    
        foreach (var pair in _clipToPort) {
            var clipPlayable = AnimationClipPlayable.Create(_graph, pair.Key);
            _graph.Connect(clipPlayable, 0, _animationMixer, pair.Value);
            _animationMixer.SetInputWeight(pair.Value, 0f);
        }
        _graph.Connect(_animationMixer, 0, _layerMixer, _destinationLayerPort);
        _layerMixer.SetInputWeight(_destinationLayerPort, 0f);
        
        _layerMixer.SetLayerMaskFromAvatarMask(destinationLayerPort, meleAvatarMask);
    }

    private void UpdateLayerTransition()
    {
        var weight = 0f;
        switch (State)
        {
            case MeleeStateEnum.Non:
                weight = 0f;
                break;
            case  MeleeStateEnum.UnSheath:
                if (_currentMeleePlayable.IsPositioning)
                    weight = _currentMeleePlayable.CurrentStateFraction;
                else
                    weight = 1f;
                break;
            case MeleeStateEnum.PositioningAfterSheath:
                weight = _currentMeleePlayable.CurrentStateFraction;
                break;
            default:
                weight = 1f;
                break;
        }
        
        _layerMixer.SetInputWeight(_destinationLayerPort, weight);
    }

    private int currentPort = -1;
    private int previousPort = -1;

    
    private void UpdateAnimationMixer()
    {
        if (_currentMeleePlayable == null) return;
        
        if (!_currentMeleePlayable.IsClipNull)
        {
            var newClip = _currentMeleePlayable.Clip;
            if (_clipToPort.TryGetValue(newClip, out var newPort))
            {
                if (newPort != currentPort)
                {
                    if(previousPort!=-1)
                        _animationMixer.SetInputWeight(previousPort, 0f);
                    previousPort = currentPort;
                    currentPort = newPort;
                    var newPlayable = _animationMixer.GetInput(currentPort);
                    if (newPlayable.IsValid()) newPlayable.SetTime(0f);
                }
            }
        }
        
        if (_currentMeleePlayable.IsPositioning && State!=MeleeStateEnum.UnSheath)
        {
            var weight = Mathf.Clamp01(_currentMeleePlayable.CurrentStateFraction);
        
            if(previousPort!=-1)
                _animationMixer.SetInputWeight(previousPort, 1f - weight);
            _animationMixer.SetInputWeight(currentPort, weight);
        }
        else
        {
            if(previousPort!=-1)
                _animationMixer.SetInputWeight(previousPort, 0f);
            _animationMixer.SetInputWeight(currentPort, 1f);
        }

        
        var currentPlayable = _animationMixer.GetInput(currentPort);
        if (currentPlayable.IsValid()) currentPlayable.SetSpeed(_currentMeleePlayable.ClipSpeed);
       
    }

    protected virtual void Update()
    {
        UpdateMeleeState();
        UpdateWeaponProp();
        UpdateLayerTransition();
        UpdateAnimationMixer();
        
    }

    protected virtual void Start()
    {
        sheathedWeapon.SetActive(true);
        handWeapon.SetActive(false);
    }
}
