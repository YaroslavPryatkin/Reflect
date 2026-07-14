using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using MeleeComponents;
using MeleeComponents.Presets;
using CustomAttributes;

public abstract class MeleeController : MonoBehaviour
{
    [Header("Sheathe")] 
    [SerializeReference] private StandardUnSheath unSheath = new StandardUnSheath();
    [SerializeReference] private StandardSheath sheath = new StandardSheath();

    [Header("Holding")]
    [SerializeReference] private StandardHold hold = new StandardHold();

    [Header("Playables")] 
    [SerializeReference, SelectSubclass] private List<MeleePlayable> playables = new (2);
    
    [Header("Weapon objects")] 
    [SerializeField] private GameObject sheathedWeapon;
    [SerializeField] private GameObject handWeapon;
    
    [Header("Settings")]
    [SerializeField] private AvatarMask meleeAvatarMask;
    [SerializeField] private MeleeWeaponHitboxController meleeWeaponHitboxController;
    [SerializeField] private float holdAnimationOverrideFraction = 0.5f;
    
    
    
    public  MeleeToTargetMoveController ToTargetMoveController { get; private set; }

    private MeleePlayable _onSuccessfulParry;
    private Utility.TemporaryValue<bool> _parrying;
    private Utility.FractionTemporaryValue<bool> _waitingForOnSuccessfulParry;
    private bool _hasParrying = false;
    private bool _hasWaitingForOnSuccessfulParry = false;
    public bool Parrying=> _hasParrying && _parrying.Value;

    public MeleeWeaponHitboxController MeleeHitboxController => meleeWeaponHitboxController;
    private MeleeSecondHandController _secondHandController;
    private GettingHitController _gettingHitController;
    public MeleeSecondHandController SecondHandController => _secondHandController;

    public void ActivateParrying(Utility.TemporaryValue<bool> parrying, Utility.FractionTemporaryValue<bool> waitingForOnSuccessfulParry, MeleePlayable onSuccessfulParry)
    {
        _onSuccessfulParry = onSuccessfulParry;
        _parrying = parrying;
        _waitingForOnSuccessfulParry = waitingForOnSuccessfulParry;
        _hasWaitingForOnSuccessfulParry = true;
        _hasParrying = true;
    }

    public void ClearParryingReferences()
    {
        _hasWaitingForOnSuccessfulParry = false;
    }
    
    public void OnSuccessfulParry()
    {
        if (_hasWaitingForOnSuccessfulParry && _waitingForOnSuccessfulParry.Value)
        {
            ChangeCurrentMeleePlayable(_onSuccessfulParry);
        }
    }
    
    public void SetOtherMask(AvatarMask mask)
    {
        _animationLayerController.SetAvatarMask(mask);
    }

    public void ClearOtherMaskReferences()
    {
        _animationLayerController.SetAvatarMask(meleeAvatarMask);
    }

    
    private AnimationLayerController  _animationLayerController;

    protected virtual void Awake()
    {
        if (!TryGetComponent(out AnimationController controller))
        {
            Debug.LogError("No AnimationController found!", this);
            enabled = false;
            return;
        }
        
        var sensors = GetComponent<Sensors>();
        
        ToTargetMoveController = GetComponent<MeleeToTargetMoveController>();
        _gettingHitController = GetComponent<GettingHitController>();
        meleeWeaponHitboxController.SetTargetLayers(sensors.EnemyLayer);
        
        sheath.Awake(this);
        unSheath.Awake(this);
        hold.Awake(this);
        foreach (var combo in playables)
        {
            combo.Awake(this);
        }
        
        var uniqueClips = new HashSet<AnimationClip>();
    
        unSheath.AddClipsToSet(uniqueClips);
        sheath.AddClipsToSet(uniqueClips);
        hold.AddClipsToSet(uniqueClips);
        foreach (var combo in playables)
        {
            combo.AddClipsToSet(uniqueClips);
        }
        
       
        _animationLayerController = controller.GetAnimationLayer(uniqueClips, 2, meleeAvatarMask);
        TryGetComponent(out _secondHandController);
    }
    /// <returns>
    /// Must return -1 if no combo should be played
    /// </returns>
    protected abstract int WhatComboToPlay();
    protected abstract bool ShouldHold();
    protected abstract bool ShouldInterrupt();
    protected abstract bool ShouldSwitchStateToNon();
    
    public virtual void OnAttack(){}
    public virtual void OnParry(){}
    
    public enum MeleeStateEnum
    {
        Non, UnSheath, Sheathe, Hold, Combo
    }

    public MeleeStateEnum State { get; private set; } = MeleeStateEnum.Non;
    public int CurrentCombo { get; private set; } = -1;

    public bool CanBeSafelyInterrupted => State == MeleeStateEnum.Non || _currentMeleePlayable.CanBeSafelyInterrupted;
    
    private MeleePlayable _currentMeleePlayable;

    private void ChangeCurrentMeleePlayable(MeleePlayable meleePlayable)
    {
        if (_currentMeleePlayable != null)
            _currentMeleePlayable.Interrupt();
        meleePlayable.Start(0f);
        
        _currentMeleePlayable = meleePlayable;
    }
    private void ChangeCurrentMeleePlayableAntiFraction(MeleePlayable meleePlayable, bool lengthSourceIsNew)
    {
        if (_currentMeleePlayable != null)
        {
            float fraction;
            
            if(lengthSourceIsNew)
                 fraction = meleePlayable.Length - _currentMeleePlayable.LengthFraction;
            else
                fraction = _currentMeleePlayable.Length - _currentMeleePlayable.LengthFraction;
            
            _currentMeleePlayable.Interrupt();
            meleePlayable.Start(fraction);
        }
        _currentMeleePlayable = meleePlayable;
    }

    private bool CheckInterrupt()
    {
        if (ShouldInterrupt() || _gettingHitController.IsStunned)
        {
            if (State == MeleeStateEnum.Non || State == MeleeStateEnum.Sheathe)
            {
                State = MeleeStateEnum.Non;
            }
            else
            {
                CurrentCombo = -1;
                if (State != MeleeStateEnum.Hold || !_currentMeleePlayable.IsActive)
                {
                    State = MeleeStateEnum.Hold;
                    ChangeCurrentMeleePlayable(hold);
                }
            }
            return true;
        }
        return false;
    }
    
    private void DoSomethingBasedOnInput()
    {
        if (!_currentMeleePlayable.CanBeSafelyInterrupted) return;
        
        var combo = WhatComboToPlay();
        if (combo != -1)
        {
            if (combo != CurrentCombo || !_currentMeleePlayable.IsActive)
            {
                CurrentCombo = combo;
                State = MeleeStateEnum.Combo;
                ChangeCurrentMeleePlayable(playables[combo]);
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
            if (State != MeleeStateEnum.Sheathe)
            {
                CurrentCombo = -1;
                State = MeleeStateEnum.Sheathe;
                ChangeCurrentMeleePlayable(sheath);
            }
        }
    }
    
    private void UpdateMeleeState()
    {
        if (ShouldSwitchStateToNon())
        {
            State = MeleeStateEnum.Non;
            return;
        }

        if (CheckInterrupt()) return;
        
        if (State == MeleeStateEnum.Non)
        {
            if (ShouldHold() || WhatComboToPlay() != -1)
            {
                State = MeleeStateEnum.UnSheath;
                ChangeCurrentMeleePlayable(unSheath);
            }
            return;
        }

        // if (_currentMeleePlayable == null)
        // {
        //     Debug.Log("Something went wrong " + State + ", " + _currentMeleePlayable);
        // }
        _currentMeleePlayable.Update();
        

        switch (State)
        {
            case MeleeStateEnum.UnSheath:
                if (ShouldHold() || WhatComboToPlay() != -1)
                {
                    if (!_currentMeleePlayable.IsActive)
                    {
                        DoSomethingBasedOnInput();
                    }
                }
                else
                {
                    State = MeleeStateEnum.Sheathe;
                    ChangeCurrentMeleePlayableAntiFraction(sheath, true);
                }
                break;
            case MeleeStateEnum.Hold or MeleeStateEnum.Combo:
                DoSomethingBasedOnInput();

                break;
            case MeleeStateEnum.Sheathe:
                if (ShouldHold() || WhatComboToPlay() != -1)
                {
                    if (_currentMeleePlayable.LengthFraction < 1f)
                    {
                        DoSomethingBasedOnInput();
                    }
                    else
                    {
                        //actively putting sword into sheath
                        State = MeleeStateEnum.UnSheath;
                        ChangeCurrentMeleePlayableAntiFraction(unSheath, false);
                    }
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
            (State == MeleeStateEnum.Sheathe && _currentMeleePlayable.LengthFraction >= _currentMeleePlayable.Length - 1f) ||
            (State == MeleeStateEnum.UnSheath && _currentMeleePlayable.LengthFraction < 1f)
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

    private void UpdateLayerTransition()
    {
        var weight = 0f;
        switch (State)
        {
            case MeleeStateEnum.Non:
                weight = 0f;
                break;
            case  MeleeStateEnum.UnSheath:
                if (_currentMeleePlayable.LengthFraction < 1f)
                    weight = _currentMeleePlayable.CurrentClipWeight;
                else
                    weight = 1f;
                break;
            case MeleeStateEnum.Sheathe:
                if(_currentMeleePlayable.LengthFraction >= _currentMeleePlayable.Length - 1f)
                    weight = 1 - _currentMeleePlayable.CurrentClipWeight;
                else
                    weight = 1f;
                break;
            case MeleeStateEnum.Hold:
                weight = holdAnimationOverrideFraction;
                break;
            default:
                weight = 1f;
                break;
        }

        _animationLayerController.SetLayerWeight(weight);
    }
    
    private void UpdateAnimationMixer()
    {
        if (_currentMeleePlayable == null) return;
        

        _animationLayerController.UpdateCurrentPlayable(_currentMeleePlayable);
        
        if (State == MeleeStateEnum.Sheathe &&
            _currentMeleePlayable.LengthFraction >= _currentMeleePlayable.Length - 1f)
        {
            _animationLayerController.UpdateCurrentPlayableWeight(0f);
        }
        else if (State==MeleeStateEnum.UnSheath && _currentMeleePlayable.LengthFraction < 1f)
        {
            _animationLayerController.UpdateCurrentPlayableWeight(1f);
        }
        else
        {
            var weight = Mathf.Clamp01(_currentMeleePlayable.CurrentClipWeight);
        
            _animationLayerController.UpdateCurrentPlayableWeight(weight);
        }
        
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
