using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Playables;
using UnityEngine.Animations;
using BaseActionTransitionsEnum = UtilityFunctions.BaseActionTransitionsEnum;

public class AnimationLayerController
{
    protected readonly AnimationAndRigManager Manager;
    protected AnimationMixerPlayable AnimationMixer;
    private readonly int _destinationLayerPort;
    private readonly uint _destinationLayerPortU;
    protected readonly Dictionary<AnimationClip, int> ClipToPort;
    public float LayerWeight { get; private set; }
    public string Name { get; private set; }
    
    public AnimationLayerController(AnimationAndRigManager manager, 
        uint destinationLayerPort, HashSet<AnimationClip> uniqueClips, string name,  bool additive)
    {
        Name = name;
        Manager = manager;
        _destinationLayerPortU = destinationLayerPort;
        _destinationLayerPort = (int)destinationLayerPort;
        
        
        ClipToPort = new Dictionary<AnimationClip, int>();
        var portIdx = 0;
        foreach (var clip in uniqueClips) {
            ClipToPort[clip] = portIdx++;
        }

        AnimationMixer = AnimationMixerPlayable.Create(Manager.Graph, ClipToPort.Count);
    
        foreach (var pair in ClipToPort) {
            var clipPlayable = AnimationClipPlayable.Create(Manager.Graph, pair.Key);
            Manager.Graph.Connect(clipPlayable, 0, AnimationMixer, pair.Value);
            AnimationMixer.SetInputWeight(pair.Value, 0f);
        }
        Manager.Graph.Connect(AnimationMixer, 0, Manager.LayerMixer, _destinationLayerPort);
        Manager.LayerMixer.SetInputWeight(_destinationLayerPort, 0f);
        Manager.LayerMixer.SetLayerAdditive(_destinationLayerPortU, additive);
        LayerWeight = 0f;
    }
    
    public AnimationLayerController(AnimationAndRigManager manager, uint destinationLayerPort, 
        HashSet<AnimationClip> uniqueClips,  AvatarMask avatarMask, string name,bool additive) : 
        this(manager, destinationLayerPort,  uniqueClips,  name, additive)
    {
        Manager.LayerMixer.SetLayerMaskFromAvatarMask(_destinationLayerPortU, avatarMask);
    }


    public virtual string MakeDebugInfo()
    {
        return "--| " + Name + " => " + LayerWeight.ToString("0.00") + " |--";
    }
    
    public void SetLayerWeight(float weight)
    {
        weight = Mathf.Clamp01(weight);
        LayerWeight = weight;
        Manager.LayerMixer.SetInputWeight(_destinationLayerPort, weight);
    }
    
    
    public void SetPlayableWeight(AnimationClip clip, float weight)
    {
        if (ClipToPort.TryGetValue(clip, out var port))
        {
            weight= Mathf.Clamp01(weight);
            AnimationMixer.SetInputWeight(port, weight);
        }
    }
    
    public void SetPlayableSpeed(AnimationClip clip, float clipSpeed)
    {
        if (ClipToPort.TryGetValue(clip, out var port))
        {
            var playable = AnimationMixer.GetInput(port);
            if (playable.IsValid())
            {
                playable.SetSpeed(clipSpeed);
            }
        }
    }

    public void SetPlayableSpeedAndWeightAndResetTime(AnimationClip clip, float clipSpeed, float weight)
    {
        if (ClipToPort.TryGetValue(clip, out var port))
        {
            var playable = AnimationMixer.GetInput(port);
            if (playable.IsValid())
            {
                playable.SetSpeed(clipSpeed);
                playable.SetTime(0f);
            }
            weight= Mathf.Clamp01(weight);
            AnimationMixer.SetInputWeight(port, weight);
        }
    }

    public void SetPlayableTime(AnimationClip clip, float timeInSeconds=0f)
    {
        if (ClipToPort.TryGetValue(clip, out var port))
        {
            var playable = AnimationMixer.GetInput(port);
            if(playable.IsValid()) playable.SetTime(timeInSeconds);
        }
    }
    

    public void SetAvatarMask(AvatarMask mask)
    {
        Manager.LayerMixer.SetLayerMaskFromAvatarMask(_destinationLayerPortU, mask);
    }
    
    
}
