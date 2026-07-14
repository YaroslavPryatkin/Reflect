using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Assemblies;
using BaseActionTransitionsEnum = Utility.BaseActionTransitionsEnum;

public class AutomaticAnimationLayerController : AnimationLayerController
{
    public AutomaticAnimationLayerController(PlayableGraph graph, AnimationLayerMixerPlayable layerMixer, 
        uint destinationLayerPort, HashSet<AnimationClip> uniqueClips, bool additive) : base( graph,  layerMixer, 
        destinationLayerPort,  uniqueClips, additive)
    { }
    
    public AutomaticAnimationLayerController(PlayableGraph graph, AnimationLayerMixerPlayable layerMixer, 
        uint destinationLayerPort, HashSet<AnimationClip> uniqueClips,  AvatarMask avatarMask, bool additive) : base( graph,  layerMixer, 
        destinationLayerPort,  uniqueClips, avatarMask, additive)
    { }
    
    private readonly Utility.FractionTemporaryValue<bool> _isTransitioning = new (false, true);

    public void AutomaticUpdateCurrentPlayable(AnimationClip clip, float clipSpeed, float crossFadeDuration)
    {
        var newPort = ClipToPort.GetValueOrDefault(clip, -1);
        var updateTime = false;

        if (newPort != CurrentPort)
        {
            InternalChangeCurrentPort(newPort);
            _isTransitioning.Activate(crossFadeDuration);
            updateTime = true;
        }
        InternalChangeCurrentPlayableSpeedAndTime(clipSpeed,  updateTime);

        var weight = 1f;
        if (_isTransitioning)
        {
            UpdateCurrentPlayableWeight(_isTransitioning.TimeFraction);
            weight = _isTransitioning.TimeFraction;
        }
        else
        {
            UpdateCurrentPlayableWeight(1f);
        }
    }
}
