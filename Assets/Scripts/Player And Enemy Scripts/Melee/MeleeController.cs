using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using MeleeComponents;
using MeleeComponents.Presets;
using CustomAttributes;
using InterruptionEnum = MeleeComponents.MeleeStateInformation.InterruptionEnum;

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
    [SerializeField] private AvatarMask handsAvatarMask;
    [SerializeField] private MeleeWeaponHitboxController meleeWeaponHitboxController;
    [SerializeField] private float holdAnimationOverrideFraction = 0.5f;
    
    [Header("Legs")]
    [SerializeField] private AvatarMask legsAvatarMask;
    [SerializeField] private float legsCrossFadeDuration = 0.15f;
    
    
    
    public MeleeTransformController TransformController { get; private set; }
    public MeleeSecondHandController SecondHandController { get; private set; }
    
    public MeleeWeaponHitboxController MeleeHitboxController => meleeWeaponHitboxController;
    

    private int _onSuccessfulParryIndex = -1;
    private Utility.TemporaryValue<bool> _parrying;
    private Utility.FractionTemporaryValue<bool> _waitingForOnSuccessfulParry;
    private bool _hasParrying = false;
    public bool Parrying=> _hasParrying && _parrying.Value;

    public GettingHitController GettingHitController { get; private set; }

    private readonly Utility.ChangeableFractionValue _dontAnimateLegs = new();

        
    private CurrentPlayableAnimationLayerController  _handAnimationLayer;
    
    private CurrentPlayableAnimationLayerController _legsAnimationLayer;
    private Utility.BaseActionAutomaticTransition _legsTransition;
    
    public enum MeleeStateEnum
    {
        Non, UnSheath, Sheathe, Hold, Combo
    }

    public MeleeStateEnum State { get; private set; } = MeleeStateEnum.Non;
    public int CurrentCombo { get; private set; } = -1;
    
    public bool CanBeInterruptedToAnything => State == MeleeStateEnum.Non ||
                                              _currentMeleePlayable.CanBeInterruptedInto == InterruptionEnum.ToAnything;
    
    private MeleePlayable _currentMeleePlayable;

    public void ActivateParrying(Utility.TemporaryValue<bool> parrying, Utility.FractionTemporaryValue<bool> waitingForOnSuccessfulParry, int onSuccessfulParryIndex)
    {
        _onSuccessfulParryIndex = onSuccessfulParryIndex;
        _parrying = parrying;
        _waitingForOnSuccessfulParry = waitingForOnSuccessfulParry;
        _hasParrying = true;
    }

    public void ClearParryingReferences()
    {
        _onSuccessfulParryIndex = -1;
    }
    
    public void OnSuccessfulParry()
    {
        if (_onSuccessfulParryIndex!=-1 && _waitingForOnSuccessfulParry.Value)
        {
            PlayCombo(_onSuccessfulParryIndex);
            MeleeHitboxController.SpawnSparks();
        }
    }
    
    public void ActivateOtherMask(AvatarMask mask)
    {
        _handAnimationLayer.SetAvatarMask(mask);
    }

    public void StopOtherMask()
    {
        _handAnimationLayer.SetAvatarMask(handsAvatarMask);
    }

    public void ActivateDontAnimateLegs(Utility.FractionTemporaryValue<bool> timer)
    {
        _dontAnimateLegs.Set(timer);
    }

    public void StopDontAnimateLegs()
    {
        _dontAnimateLegs.Unset();
    }
    



    protected virtual void Awake()
    {
        if (!TryGetComponent(out AnimationAndRigManager controller))
        {
            Debug.LogError("No AnimationController found!", this);
            enabled = false;
            return;
        }
        
        var sensors = GetComponent<Sensors>();
        
        TransformController = GetComponent<MeleeTransformController>();
        GettingHitController = GetComponent<GettingHitController>();
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
        AddComboClipsToSet(uniqueClips);
       
        _handAnimationLayer = controller.GetCurrentPlayableAnimationLayer(uniqueClips, 2, handsAvatarMask, "melee hands");
        
        // var comboClips =  new HashSet<AnimationClip>();
        // AddComboClipsToSet(comboClips);

        _legsAnimationLayer = controller.GetCurrentPlayableAnimationLayer(uniqueClips, 4, legsAvatarMask, "melee legs");
        _legsTransition = legsCrossFadeDuration;
        
        if (TryGetComponent(out MeleeSecondHandController secondHandController))
        {
            SecondHandController = secondHandController;
        }
    }

    public void AddComboClipsToSet(HashSet<AnimationClip> uniqueClips)
    {
        foreach (var combo in playables)
        {
            combo.AddClipsToSet(uniqueClips);
        }
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

    public void PlayCombo(int index)
    {
        if (index != CurrentCombo || !_currentMeleePlayable.IsActive)
        {
            CurrentCombo = index;
            State = MeleeStateEnum.Combo;
            ChangeCurrentMeleePlayable(playables[index]);
        }
    }
    
    private bool CheckInterrupt()
    {
        if (ShouldInterrupt() || GettingHitController.IsStunned)
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
        if (_currentMeleePlayable.CanBeInterruptedInto == InterruptionEnum.Never) return;
        
        var combo = WhatComboToPlay();
        if (combo != -1)
        {
            PlayCombo(combo);
            return;
        }

        if (_currentMeleePlayable.CanBeInterruptedInto != InterruptionEnum.ToAnything) return;
        
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
        _legsAnimationLayer.SetLayerWeight(
            _legsTransition.GetFraction(State == MeleeStateEnum.Combo && !_dontAnimateLegs));

        
        
        var weight = 0f;
        switch (State)
        {
            case MeleeStateEnum.Non:
                weight = 0f;
                break;
            case  MeleeStateEnum.UnSheath:
                if (_currentMeleePlayable.LengthFraction < 1f)
                    weight = _currentMeleePlayable.CurrentClipTimeFraction;
                else
                    weight = 1f;
                break;
            case MeleeStateEnum.Sheathe:
                if (_currentMeleePlayable.LengthFraction >= _currentMeleePlayable.Length - 1f)
                {
                    weight = 1 - _currentMeleePlayable.CurrentClipTimeFraction;
                }
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

        _handAnimationLayer.SetLayerWeight(weight);
    }
    
    private void UpdateAnimationMixer()
    {
        if (_currentMeleePlayable == null) return;
        

        _handAnimationLayer.UpdateCurrentPlayable(_currentMeleePlayable);
        _legsAnimationLayer.UpdateCurrentPlayable(_currentMeleePlayable);
        
        
        if (State == MeleeStateEnum.Sheathe &&
            _currentMeleePlayable.LengthFraction >= _currentMeleePlayable.Length - 1f)
        {
            _handAnimationLayer.FinishTransitioningToCurrentPlayable();
            _legsAnimationLayer.FinishTransitioningToCurrentPlayable();
        }
        else if (State==MeleeStateEnum.UnSheath && _currentMeleePlayable.LengthFraction < 1f)
        {
            _handAnimationLayer.FinishTransitioningToCurrentPlayable();
            _legsAnimationLayer.FinishTransitioningToCurrentPlayable();
        }
        else
        {
            if (_currentMeleePlayable.ShouldAnimationTransition)
            {
                var weight = _currentMeleePlayable.CurrentClipTimeFraction;
                _handAnimationLayer.UpdateCurrentPlayableTransitioningWeight(weight);
                _legsAnimationLayer.UpdateCurrentPlayableTransitioningWeight(weight);
            }
            else
            {
                _handAnimationLayer.FinishTransitioningToCurrentPlayable();
                _legsAnimationLayer.FinishTransitioningToCurrentPlayable();
            }
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
