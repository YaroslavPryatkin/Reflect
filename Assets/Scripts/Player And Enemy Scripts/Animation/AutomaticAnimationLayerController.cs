using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Assemblies;
using BaseActionTransitionsEnum = UtilityFunctions.BaseActionTransitionsEnum;

public class AutomaticAnimationLayerController : CurrentPlayableAnimationLayerController
{
    public AutomaticAnimationLayerController(AnimationAndRigManager manager, 
        uint destinationLayerPort, HashSet<AnimationClip> uniqueClips,string name, bool additive) : base(manager, 
        destinationLayerPort,  uniqueClips,name, additive)
    { }
    
    public AutomaticAnimationLayerController(AnimationAndRigManager manager, 
        uint destinationLayerPort, HashSet<AnimationClip> uniqueClips,  AvatarMask avatarMask,string name, bool additive) : base(manager, 
        destinationLayerPort,  uniqueClips, avatarMask, name, additive)
    { }
    
    private readonly UtilityClasses.FractionTemporaryValue<bool> _isTransitioning = new (false, true);

    public void AutomaticUpdateCurrentPlayable(AnimationClip clip, float clipSpeed, float crossFadeDuration)
    {
        if (!ClipToPort.TryGetValue(clip, out var newPort))
        {
            Debug.LogError("Didn't register the clip: " + clip.name);
            return;
        }

        if (newPort != CurrentPort.Port)
        {
            InternalChangeCurrentPort(newPort);
            _isTransitioning.Activate(crossFadeDuration);
            InternalChangeCurrentPlayableSpeedAndTime(clipSpeed,  true);
        }
        else
        {
            InternalChangeCurrentPlayableSpeedAndTime(clipSpeed,  false);
        }

        if (_isTransitioning)
        {
            UpdateCurrentPlayableTransitioningWeight(_isTransitioning.TimeFraction);
        }
        else
        {
            FinishTransitioningToCurrentPlayable();
        }
    }
}
