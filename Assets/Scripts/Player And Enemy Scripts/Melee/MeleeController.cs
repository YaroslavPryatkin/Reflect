using UnityEngine;
using System.Collections.Generic;
using CustomAttributes;
using InterruptionEnum = MeleeSystem.InterruptionEnum;
using MeleeSystem;

public abstract class MeleeController : MonoBehaviour
{
    [Header("Sheathe")] 
    [SerializeField] private Unsheathe unSheathSource;
    [SerializeField] private Sheathe sheathSource;

    [Header("Holding")] 
    [SerializeField] private Hold holdSource;

    [Header("Playables")] 
    [SerializeReference] protected List<MeleePlayableSource> playableSources = new ();
    
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
    
    
    private Sensors _sensors;
    public MeleeTransformController TransformController { get; private set; }
    public MeleeSecondHandController SecondHandController { get; private set; }
    public GettingHitController GettingHitController { get; private set; }
    public HealthController HealthController { get; private set; }
    public MeleeWeaponHitboxController MeleeHitboxController => meleeWeaponHitboxController;
    

    private int _onSuccessfulParryIndex = -1;
    private UtilityTimers.TemporaryValue<bool> _parrying;
    private UtilityTimers.FractionTemporaryValue<bool> _waitingForOnSuccessfulParry;
    private bool _hasParrying = false;
    public bool Parrying=> _hasParrying && _parrying.Value;


    private readonly UtilityClasses.ChangeableFractionValueReference _dontAnimateLegs = new();
    
    private readonly UtilityClasses.ChangeableFractionValueReference _interruptIfNotOnGround = new();
        
    private CurrentPlayableAnimationLayerController  _handAnimationLayer;
    private CurrentPlayableAnimationLayerController _legsAnimationLayer;
    private UtilityClasses.BaseActionAutomaticTransition _legsTransition;
    
    public enum MeleeStateEnum
    {
        Non, UnSheath, Sheathe, Hold, Combo
    }

    public MeleeStateEnum State { get; private set; } = MeleeStateEnum.Non;
    public int CurrentCombo { get; private set; } = -1;

    private int _nextComboToPlay = -1;
    private bool _canChangeNextComboToPlay = true;
    
    public bool CanBeInterruptedToAnything => State == MeleeStateEnum.Non ||
                                              _currentMeleePlayable.CanBeInterruptedInto == InterruptionEnum.ToAnything;
    
    private MeleePlayable _currentMeleePlayable;
    private readonly List<MeleePlayable> _playables=new();
    private MeleePlayable _sheath;
    private MeleePlayable _unSheath;
    private MeleePlayable _hold;

    public void ActivateParrying(UtilityTimers.TemporaryValue<bool> parrying, 
        UtilityTimers.FractionTemporaryValue<bool> waitingForOnSuccessfulParry, 
        int onSuccessfulParryIndex)
    {
        _onSuccessfulParryIndex = onSuccessfulParryIndex;
        
        _parrying = parrying;
        _waitingForOnSuccessfulParry = waitingForOnSuccessfulParry;
        _hasParrying = true;
    }

    public virtual bool ShouldUseMaximumParryDuration => false;

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
            _onSuccessfulParryIndex = -1;
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

    public void ActivateDontAnimateLegs(UtilityTimers.FractionTemporaryValue<bool> timer)
    {
        _dontAnimateLegs.Set(timer);
    }

    public void StopDontAnimateLegs()
    {
        _dontAnimateLegs.Unset();
    }

    public void ActivateInterruptIfNotOnGround(UtilityTimers.FractionTemporaryValue<bool> timer)
    {
        _interruptIfNotOnGround.Set(timer);
    }

    public void StopInterruptIfNotOnGround()
    {
        _interruptIfNotOnGround.Unset();
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
        HealthController = GetComponent<HealthController>();
        _sensors = GetComponent<Sensors>();
        meleeWeaponHitboxController.SetTargetLayers(sensors.EnemyLayer);

        _sheath = new MeleePlayable(this, sheathSource);
        _unSheath = new MeleePlayable(this, unSheathSource);
        _hold = new MeleePlayable(this, holdSource);
        
        foreach (var source in playableSources)
        {
            _playables.Add(new MeleePlayable(this,  source));
        }
        
        var uniqueClips = new HashSet<AnimationClip>();
    
        _unSheath.AddClipsToSet(uniqueClips);
        _sheath.AddClipsToSet(uniqueClips);
        _hold.AddClipsToSet(uniqueClips);
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
        foreach (var combo in _playables)
        {
            combo.AddClipsToSet(uniqueClips);
        }
    }
    
    public void ResetNextComboToPlay()
    {
        _nextComboToPlay = -1;
        _canChangeNextComboToPlay = true;
    }

    private void ResetAllCombos()
    {
        CurrentCombo = -1;
        ResetNextComboToPlay();
    }
    
    /// <returns>
    /// If nextComboToPlayCanBeChanged
    /// </returns>
    protected abstract bool WhatComboToPlay(ref int nextComboToPlay);
    protected abstract bool ShouldHold();
    protected abstract bool ShouldInterrupt();
    protected abstract bool ShouldSwitchStateToNon();

    public virtual void OnAttackEnd(){}
    public virtual void OnParryEnd(){}

    public virtual void OnFinishHimStart() {}
    public virtual void OnFinishHimEnd(){}

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
        else
        {
            meleePlayable.Start(0f);
        }
        _currentMeleePlayable = meleePlayable;
    }

    public int GetSourceIndex(MeleePlayableSource source)
    {
        return playableSources.IndexOf(source);
    }
    
    public void PlayCombo(int index)
    {
        if (index != CurrentCombo || !_currentMeleePlayable.IsActive)
        {
            //Debug.Log("CurrentCombo: " + CurrentCombo + ", index: " + index);
            CurrentCombo = index;
            State = MeleeStateEnum.Combo;
            //Debug.Log("Starting combo " + index + ", frame = " + Time.frameCount);
            ChangeCurrentMeleePlayable(_playables[index]);
        }
    }

    private void DoSomethingBasedOnInput()
    {
        if (_currentMeleePlayable.CanBeInterruptedInto == InterruptionEnum.Never) return;
        
        if (_nextComboToPlay != -1)
        {
            PlayCombo(_nextComboToPlay);
            return;
        }

        if (_currentMeleePlayable.CanBeInterruptedInto != InterruptionEnum.ToAnything) return;
        
        if (ShouldHold())
        {
            CurrentCombo = -1;
            if (State != MeleeStateEnum.Hold || !_currentMeleePlayable.IsActive)
            {
                State = MeleeStateEnum.Hold;
                ChangeCurrentMeleePlayable(_hold);
            }
        }
        else
        {
            if (State != MeleeStateEnum.Sheathe)
            {
                CurrentCombo = -1;
                State = MeleeStateEnum.Sheathe;
                ChangeCurrentMeleePlayable(_sheath);
            }
        }
    }
    
    private void UpdateMeleeState()
    {
        if (ShouldSwitchStateToNon())
        {
            State = MeleeStateEnum.Non;
            if(_currentMeleePlayable != null)
                _currentMeleePlayable.Interrupt();
            _currentMeleePlayable = null;
            
            ResetAllCombos();
            return;
        }

        if (ShouldInterrupt() || GettingHitController.IsActivelyStunned || (_interruptIfNotOnGround.Value && !_sensors.IsGrounded))
        {
            if (State == MeleeStateEnum.Non || State == MeleeStateEnum.Sheathe)
            {
                State = MeleeStateEnum.Non;
                if(_currentMeleePlayable != null)
                    _currentMeleePlayable.Interrupt();
                _currentMeleePlayable = null;
            }
            else
            {
                CurrentCombo = -1;
                if (State != MeleeStateEnum.Hold || !_currentMeleePlayable.IsActive)
                {
                    State = MeleeStateEnum.Hold;
                    ChangeCurrentMeleePlayable(_hold);
                }
            }
            ResetAllCombos();
            return;
        }
        

        if (_currentMeleePlayable != null)
        {
            _currentMeleePlayable.Update();
            if (!_currentMeleePlayable.IsActive)
            {
                ResetAllCombos();
            }
        }
        
        if (_canChangeNextComboToPlay)
        {
            _canChangeNextComboToPlay = WhatComboToPlay(ref _nextComboToPlay);
        }

        if (_nextComboToPlay == -1)
        {
            ResetAllCombos();
        }
        
        switch (State)
        {
            case MeleeStateEnum.Non:
                if (ShouldHold() || _nextComboToPlay != -1)
                {
                    State = MeleeStateEnum.UnSheath;
                    ChangeCurrentMeleePlayable(_unSheath);
                }
                break;
            case MeleeStateEnum.UnSheath:
                if (ShouldHold() || _nextComboToPlay != -1)
                {
                    if (!_currentMeleePlayable.IsActive)
                    {
                        DoSomethingBasedOnInput();
                    }
                }
                else
                {
                    State = MeleeStateEnum.Sheathe;
                    ChangeCurrentMeleePlayableAntiFraction(_sheath, true);
                }
                break;
            case MeleeStateEnum.Hold or MeleeStateEnum.Combo:
                DoSomethingBasedOnInput();
                break;
            case MeleeStateEnum.Sheathe:
                if (ShouldHold() || _nextComboToPlay != -1)
                {
                    if (_currentMeleePlayable.LengthFraction < 1f)
                    {
                        DoSomethingBasedOnInput();
                    }
                    else
                    {
                        //actively putting sword into sheath
                        State = MeleeStateEnum.UnSheath;
                        ChangeCurrentMeleePlayableAntiFraction(_unSheath, false);
                    }
                }
                break;
        }
        

        if (State!=MeleeStateEnum.Non && !_currentMeleePlayable.IsActive)
        {
            State = MeleeStateEnum.Non;
            
            if(_currentMeleePlayable!=null)
                _currentMeleePlayable.Interrupt();

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



        float weight;
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
                GettingHitController.InterruptStunIfNotActive();
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
