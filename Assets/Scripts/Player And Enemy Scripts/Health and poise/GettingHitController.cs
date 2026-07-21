using System.Collections.Generic;
using UnityEngine;

public class GettingHitController : MonoBehaviour
{
    [Header("Timings")]
    [SerializeField] private float crossFadeDuration;
    [SerializeField] private float stunDurationFractionToGetStunnedAgain = 0.8f;
    
    [Header("Durations")]
    [SerializeField] private float minimalStunDuration;
    [SerializeField] private float minimalPoiseDamageToGetStunned = 10f;
    [SerializeField] private float maximalStunDuration;
    [SerializeField] private float maximalPoiseDamageToGetStunned = 100f;

    [Header("Parry")] 
    [SerializeField] private float minimalStunDamageToGetStunnedDuringParry = 50f;
    [SerializeField] private float parryStunDamageFraction = 0.6f;
    
    [Header("Animation")]
    [SerializeField] private List<AnimationClip> clips;
    [SerializeField] private AvatarMask avatarMask;
    [SerializeField] private float weightMultiplier = 0.5f;
    
    
    private AnimationLayerController _animationLayerController;
    private float _durationForPoiseFraction;

    private void Awake()
    {
        if (!TryGetComponent(out AnimationAndRigManager controller))
        {
            Debug.LogError("No AnimationController found!", this);
            enabled = false;
            return;
        }

        if (clips.Count == 0)
        {
            Debug.LogError("No animation clips!", this);
            enabled = false;
            return;
        }
        
        _durationForPoiseFraction = (maximalPoiseDamageToGetStunned - minimalPoiseDamageToGetStunned) /
                                  (maximalStunDuration - minimalStunDuration);
                                  
        
        var uniqueClips = new HashSet<AnimationClip>();
        foreach (var clip in clips)
            uniqueClips.Add(clip);
        
        _animationLayerController = controller.GetAnimationLayer(uniqueClips, 5, avatarMask, "Getting hit");
        _lastClip = clips[0];
        _transitionState = new(crossFadeDuration,weightMultiplier);
    }

    private Utility.BaseActionAutomaticTransition _transitionState;

    private Utility.FractionTemporaryValue<bool> _stun = new(false, true);
    private AnimationClip _lastClip;
    
    public bool CanGetStunned { get; set; } = true;
    
    public bool IsStunned => _stun.Value;
    
    
    private readonly Utility.ChangeableFractionValue _hyperArmor = new();
    
    public void ActivateHyperArmor(Utility.FractionTemporaryValue<bool> timer)
    {
        _hyperArmor.Set(timer);
    }

    public void StopHyperArmor()
    {
        _hyperArmor.Unset();
    }
    
    private void Update()
    {
        if (!CanGetStunned)
        {
            _stun.Deactivate();
        }
        _animationLayerController.SetLayerWeight(_transitionState.GetFraction( _stun.Value));
    }

    private void InternalStun(float poiseDamage)
    {
        _animationLayerController.SetPlayableWeight(_lastClip, 0f);


        _lastClip = clips[Random.Range(0, clips.Count)];

        var stunDuration = Utility.ChangeMeasurementScaleFraction(poiseDamage, minimalPoiseDamageToGetStunned,
            _durationForPoiseFraction, minimalStunDuration, maximalStunDuration);
        
        var speed = Utility.GetAnimationSpeed(_lastClip, stunDuration + crossFadeDuration);
        _animationLayerController.SetPlayableSpeedAndWeightAndResetTime(_lastClip, speed, 1f);
        _stun.Activate(stunDuration);
    }

    public void Stun(float poiseDamage, bool haveParried)
    {
        if (_stun.TimeFraction < stunDurationFractionToGetStunnedAgain || !CanGetStunned || !_hyperArmor) return;
        
        if (poiseDamage < minimalPoiseDamageToGetStunned) return;

        if (haveParried)
        {
            if (poiseDamage < minimalStunDamageToGetStunnedDuringParry) return;
            
            InternalStun(poiseDamage * parryStunDamageFraction);
        }
        else
        {
            InternalStun(poiseDamage);
        }
    }

}
