using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "GettingHitAnimationPack", menuName = "Scriptable Objects/GettingHitAnimationPack")]
public class GettingHitSettings : ScriptableObject
{
    [Header("Timings")]
    [SerializeField] private float crossFadeDuration;
    [SerializeField] public float stunDurationFractionToGetStunnedAgain = 0.8f;
    [SerializeField] public float activeStunDurationFraction = 0.3f;
    
    [Header("Parry")] 
    [SerializeField] private float minimalStunDamageToGetStunnedDuringParry = 50f;
    [SerializeField] private float parryStunDamageFraction = 0.6f;
    
    [Header("Durations")]
    [SerializeField] private float minimalStunDuration;
    [SerializeField] private float minimalPoiseDamageToGetStunned = 10f;
    [SerializeField] private float maximalStunDuration;
    [SerializeField] private float maximalPoiseDamageToGetStunned = 100f;
    
    [Header("Animations")]
    [Header("Getting hit")]
    [SerializeField] private List<AnimationClip> gettingHitClips;
    [SerializeField] private AvatarMask gettingHitAvatarMask;
    [SerializeField] private float weightMultiplier = 0.5f;
    [Header("Death")] 
    [SerializeField] private List<AnimationClip> deathClips;
    [SerializeField] private AvatarMask deathAvatarMask;
    
    private float _durationForPoiseFraction;

    private void Awake()
    {
        if (gettingHitClips.Count == 0 || deathClips.Count == 0)
        {
            Debug.LogError("No animation clips!", this);
        }
        
        _durationForPoiseFraction = (maximalPoiseDamageToGetStunned - minimalPoiseDamageToGetStunned) /
                                    (maximalStunDuration - minimalStunDuration);
    }

    public void InitializeController(AnimationAndRigManager controller, out AnimationLayerController animationLayerController, out UtilityClasses.BaseActionAutomaticTransition transitionState, out AnimationClip lastClip)
    {
        lastClip = gettingHitClips[0];
        
        
        var uniqueClips = new HashSet<AnimationClip>();
        foreach (var clip in gettingHitClips)
            uniqueClips.Add(clip);
        
        foreach (var clip in deathClips)
            uniqueClips.Add(clip);
        
        
        animationLayerController = controller.GetAnimationLayer(uniqueClips, 5, gettingHitAvatarMask, "Getting hit");
        
        transitionState = new(crossFadeDuration,weightMultiplier);
    }
    
    ///<summary>
    /// Sets random stun animation
    /// </summary>
    public void SetStunAnimation(float stunDuration, AnimationLayerController animationLayerController, ref AnimationClip lastClip)
    {
        animationLayerController.SetPlayableWeight(lastClip, 0f);
        
        lastClip = gettingHitClips[Random.Range(0, gettingHitClips.Count)];
        
        var speed = UtilityFunctions.GetAnimationSpeed(lastClip, stunDuration + crossFadeDuration);
        animationLayerController.SetPlayableSpeedAndWeightAndResetTime(lastClip, speed, 1f);
    }

    public bool TryStun(float poiseDamage, bool haveParried, out float stunDuration)
    {
        stunDuration = 0f;
        if (poiseDamage < minimalPoiseDamageToGetStunned) return false;

        if (haveParried)
        {
            if (poiseDamage < minimalStunDamageToGetStunnedDuringParry) return false;

            stunDuration = GetStunDuration(poiseDamage * parryStunDamageFraction);
        }
        else
        {
            stunDuration = GetStunDuration(poiseDamage);
        }

        return true;
    }

    private float GetStunDuration(float poiseDamage)
    {
        return UtilityFunctions.ChangeMeasurementScaleFraction(poiseDamage, minimalPoiseDamageToGetStunned,
            _durationForPoiseFraction, minimalStunDuration, maximalStunDuration);
    }

    /// <summary>
    /// Sets random death animation
    /// </summary>
    public void SetDeathAnimation(AnimationLayerController animationLayerController, ref AnimationClip lastClip)
    {
        animationLayerController.SetPlayableWeight(lastClip, 0f);
        
        lastClip = deathClips[Random.Range(0, deathClips.Count)];
        
        animationLayerController.SetAvatarMask(deathAvatarMask);
        animationLayerController.SetPlayableSpeedAndWeightAndResetTime(lastClip, 1f, 1f);
    }

    public void UnsetDeathAnimation(AnimationLayerController animationLayerController, ref AnimationClip lastClip)
    {
        animationLayerController.SetPlayableWeight(lastClip, 0f);
        animationLayerController.SetAvatarMask(gettingHitAvatarMask);
    }
}
