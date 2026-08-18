using System;
using UnityEngine;
using System.Collections.Generic;
using CustomAttributes;
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
    [SerializeField] private float deathDuration = 3f;
    [SerializeField] private List<AnimationClip> deathClips;
    [SerializeField] private AvatarMask deathAvatarMask;
    [SerializeField] public bool shouldSlowDeath = false;
    [SerializeField, EnableIf("shouldSlowDeath")] 
    private float slowDeathDuration = 0.4f;
    [SerializeField, EnableIf("shouldSlowDeath")] 
    private float slowDeathSpeed = 0.1f;
    [Header("Getting stabbed")]
    [SerializeField] private AnimationClip getStabbedClip;
    [SerializeField] private float gettingStubbedClipDuration = 0.07f;
        
    [SerializeField, HideInInspector] private float durationForPoiseFraction;
    [SerializeField, HideInInspector] private float gettingStubbedClipSpeed;

    private int _lastRandomIndexStun = 0;
    private int _lastRandomIndexDeath = 0;
    
    private void OnValidate()
    {
        durationForPoiseFraction = (maximalPoiseDamageToGetStunned - minimalPoiseDamageToGetStunned) /
                                    (maximalStunDuration - minimalStunDuration);
        
        if(getStabbedClip!=null)
            gettingStubbedClipSpeed = UtilityFunctions.GetAnimationSpeed(getStabbedClip, gettingStubbedClipDuration);
    }

    public void InitializeController(AnimationAndRigManager controller, out AnimationLayerController animationLayerController, out UtilityClasses.BaseActionAutomaticTransition transitionState, out AnimationClip lastClip)
    {
        if (gettingHitClips.Count == 0 || deathClips.Count == 0)
        {
            Debug.LogError("No animation clips!", this);
            animationLayerController = null;
            transitionState = null;
            lastClip = null;
            return;
        }

        
        lastClip = gettingHitClips[0];
        
        
        var uniqueClips = new HashSet<AnimationClip>();
        foreach (var clip in gettingHitClips)
            uniqueClips.Add(clip);
        
        foreach (var clip in deathClips)
            uniqueClips.Add(clip);

        uniqueClips.Add(getStabbedClip);
        
        animationLayerController = controller.GetAnimationLayer(uniqueClips, 5, gettingHitAvatarMask, "Getting hit");
        
        transitionState = new(crossFadeDuration,weightMultiplier);
    }
    
    ///<summary>
    /// Sets random stun animation
    /// </summary>
    public void SetStunAnimation(float stunDuration, AnimationLayerController animationLayerController, ref AnimationClip lastClip)
    {
        animationLayerController.SetPlayableWeight(lastClip, 0f);
        
        lastClip = gettingHitClips.GetRandomElement(ref _lastRandomIndexStun);
        
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
            durationForPoiseFraction, minimalStunDuration, maximalStunDuration);
    }

    /// <summary>
    /// Sets random death animation
    /// </summary>
    public void SetDeathAnimation(AnimationLayerController animationLayerController, ref AnimationClip lastClip, UtilityTimers.FractionTemporaryValue<bool> timer)
    {
        animationLayerController.SetPlayableWeight(getStabbedClip, 0f);
        animationLayerController.SetPlayableWeight(lastClip, 0f);
        
        lastClip = deathClips.GetRandomElement(ref _lastRandomIndexDeath);
        
        animationLayerController.SetAvatarMask(deathAvatarMask);
        
        animationLayerController.SetPlayableSpeedAndWeightAndResetTime(
            lastClip, shouldSlowDeath ? slowDeathSpeed :  
                UtilityFunctions.GetAnimationSpeed(lastClip, deathDuration), 1f);
        
        if(shouldSlowDeath)
            timer.Activate(slowDeathDuration);
    }

    public void GetStabbed(AnimationLayerController animationLayerController, ref AnimationClip lastClip)
    {
        animationLayerController.SetPlayableWeight(lastClip, 0f);
        animationLayerController.SetPlayableSpeedAndWeightAndResetTime(getStabbedClip, gettingStubbedClipSpeed, 1f);
    }
    
    public void RestoreDeathAnimationSpeed(
        AnimationLayerController animationLayerController, ref AnimationClip lastClip)
    {
        animationLayerController.SetPlayableWeight(getStabbedClip, 0f);
        animationLayerController.SetPlayableWeight(lastClip, 1f);
        animationLayerController.SetPlayableSpeed(
            lastClip, UtilityFunctions.GetAnimationSpeed(lastClip, deathDuration));
    }

    public void UnsetDeathAnimation(AnimationLayerController animationLayerController, ref AnimationClip lastClip)
    {
        animationLayerController.SetPlayableWeight(getStabbedClip, 0f);
        animationLayerController.SetPlayableWeight(lastClip, 0f);
        animationLayerController.SetAvatarMask(gettingHitAvatarMask);
    }
}
