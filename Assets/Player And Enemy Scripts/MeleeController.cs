using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using MeleeComponents;
using MeleeComponents.Presets;
using SubclassSelector;

public abstract class MeleeController : MonoBehaviour
{
    [Header("Sheathe")] 
    [SerializeReference] private MeleeMultiComponent unSheath = new StandardUnSheath();
    [SerializeReference] private MeleeMultiComponent sheath = new StandardSheath();

    [Header("Holding")]
    [SerializeReference] private MeleeMultiComponent hold = new StandardTransitionLoop();

    [Header("Combos")] 
    [SerializeReference, SelectSubclass] private List<MeleeMultiComponent> combos = new (2);
    
    [Header("Weapon objects")] 
    [SerializeField] private GameObject sheathedWeapon;
    [SerializeField] private GameObject handWeapon;
    
    [Header("Settings")]
    [SerializeField] private AvatarMask meleeAvatarMask;
    [SerializeField] private MeleeWeaponHitboxController meleeWeaponHitboxController;
    [SerializeField] private float holdAnimationOverrideFraction = 0.5f;

    
    protected DamageController _damageController;
    
    private Utility.TemporaryValue<bool> _parrying = new(false, true);
    private MeleeMultiComponent _onSuccessfulParry;
    public bool Parrying => _parrying.Value;

    public void ActivateParrying(float duration, MeleeMultiComponent onSuccessfulParry)
    {
        _parrying.Activate(duration);
        _onSuccessfulParry = onSuccessfulParry;
        OnParry();
    } 
    public void DeactivateParrying(float parryStateAfterSuccessfulParryTime) => _parrying.Activate(parryStateAfterSuccessfulParryTime);
    
    public void OnSuccessfulParry()
    {
        if (Parrying)
        {
            ChangeCurrentMeleePlayable(_onSuccessfulParry);
        }
    }

    public void ActivateAttack(float damage)
    {
        meleeWeaponHitboxController.StartSwing(damage);
        OnAttack();
    }

    public void DeactivateAttack() => meleeWeaponHitboxController.FinishSwing();
   
    

    protected virtual void Awake()
    {
        _damageController = GetComponent<DamageController>();
        meleeWeaponHitboxController.SetTargetLayers(_damageController.EnemyLayer);
        
        sheath.Awake(this);
        unSheath.Awake(this);
        hold.Awake(this);
        foreach (var combo in combos)
        {
            combo.Awake(this);
        }
    }
    /// <returns>
    /// Must return -1 if no combo should be played
    /// </returns>
    protected abstract int WhatComboToPlay();
    protected abstract bool ShouldHold();
    protected abstract bool ShouldInterrupt();
    protected abstract bool ShouldSwitchStateToNon();
    
    protected virtual void OnAttack(){}
    protected virtual void OnParry(){}
    
    public enum MeleeStateEnum
    {
        Non, UnSheath, Sheathe, Hold, Combo
    }

    public MeleeStateEnum State { get; private set; } = MeleeStateEnum.Non;
    public int CurrentCombo { get; private set; } = -1;

    public bool CanBeSafelyInterrupted => State == MeleeStateEnum.Non || _currentMeleePlayable.CanBeSafelyInterrupted;
    
    private IMeleePlayable _currentMeleePlayable;

    private void ChangeCurrentMeleePlayable(IMeleePlayable meleePlayable)
    {
        if (_currentMeleePlayable != null)
            _currentMeleePlayable.Interrupt();
        meleePlayable.Start(0f);
        
        _currentMeleePlayable = meleePlayable;
    }
    private void ChangeCurrentMeleePlayableAntiFraction(IMeleePlayable meleePlayable, bool lengthSourceIsNew)
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
        
        _layerMixer.SetLayerMaskFromAvatarMask(destinationLayerPort, meleeAvatarMask);
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
                    weight = _currentMeleePlayable.CurrentStateFraction;
                else
                    weight = 1f;
                break;
            case MeleeStateEnum.Sheathe:
                if(_currentMeleePlayable.LengthFraction >= _currentMeleePlayable.Length - 1f)
                    weight = 1 - _currentMeleePlayable.CurrentStateFraction;
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
        
        
        
        _layerMixer.SetInputWeight(_destinationLayerPort, weight);
    }

    private int currentPort = -1;
    private int previousPort = -1;
    // private float currentWeight;
    // private float previousWeight;
    
    private void UpdateAnimationMixer()
    {
        if (_currentMeleePlayable == null) return;
        
        if (_currentMeleePlayable.ClipIsNotNull)
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
                    //Debug.Log("Changed clip");
                }
            }

            
            var currentPlayable = _animationMixer.GetInput(currentPort);
            if (currentPlayable.IsValid())
            {
                if (_currentMeleePlayable.ShouldResetAnimationTime)
                {
                    //Debug.Log("reseted clip " + );
                    currentPlayable.SetTime(0f);
                }

                currentPlayable.SetSpeed(_currentMeleePlayable.ClipSpeed);
            }
            
        }
        
        if (_currentMeleePlayable.IsTransitioning && State!=MeleeStateEnum.UnSheath)
        {
            var weight = Mathf.Clamp01(_currentMeleePlayable.CurrentStateFraction);
        
            if(previousPort!=-1)
                _animationMixer.SetInputWeight(previousPort, 1f - weight);
            _animationMixer.SetInputWeight(currentPort, weight);
            // currentWeight = weight;
            // previousWeight = 1 - weight;
        }
        else
        {
            if(previousPort!=-1)
                _animationMixer.SetInputWeight(previousPort, 0f);
            _animationMixer.SetInputWeight(currentPort, 1f);
            // currentWeight = 1;
            // previousWeight = 0;
        }
        
        //Debug.Log( "cur weight = " + currentWeight + ", prev weight = " + previousWeight +", cur length" +_currentMeleePlayable.Length+ ", cur length fraction = " + _currentMeleePlayable.LengthFraction);
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
