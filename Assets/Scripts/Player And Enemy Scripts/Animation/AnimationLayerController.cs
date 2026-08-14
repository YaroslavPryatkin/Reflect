using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Playables;
using UnityEngine.Animations;
using BaseActionTransitionsEnum = UtilityFunctions.BaseActionTransitionsEnum;

public class AnimationLayerController
{
    private readonly AnimationLayerMixerPlayable _layerMixer;
    protected readonly AnimationMixerPlayable AnimationMixer;
    private readonly int _destinationLayerPort;
    private readonly uint _destinationLayerPortU;
    protected readonly Dictionary<AnimationClip, int> ClipToPort;
    public float LayerWeight { get; private set; }
    public string Name { get; private set; }
    
    public AnimationLayerController(PlayableGraph graph, AnimationLayerMixerPlayable layerMixer, 
        uint destinationLayerPort, HashSet<AnimationClip> uniqueClips, string name,  bool additive)
    {
        Name = name;
        _layerMixer = layerMixer;
        _destinationLayerPortU = destinationLayerPort;
        _destinationLayerPort = (int)destinationLayerPort;
        
        ClipToPort = new Dictionary<AnimationClip, int>();
        var portIdx = 0;
        foreach (var clip in uniqueClips) {
            ClipToPort[clip] = portIdx++;
        }

        AnimationMixer = AnimationMixerPlayable.Create(graph, ClipToPort.Count);
    
        foreach (var pair in ClipToPort) {
            var clipPlayable = AnimationClipPlayable.Create(graph, pair.Key);
            graph.Connect(clipPlayable, 0, AnimationMixer, pair.Value);
            AnimationMixer.SetInputWeight(pair.Value, 0f);
        }
        graph.Connect(AnimationMixer, 0, _layerMixer, _destinationLayerPort);
        _layerMixer.SetInputWeight(_destinationLayerPort, 0f);
        _layerMixer.SetLayerAdditive(_destinationLayerPortU, additive);
        LayerWeight = 0f;
    }
    
    public AnimationLayerController(PlayableGraph graph, AnimationLayerMixerPlayable layerMixer, 
        uint destinationLayerPort, HashSet<AnimationClip> uniqueClips,  AvatarMask avatarMask, string name,bool additive) : this( graph,  layerMixer, 
         destinationLayerPort,  uniqueClips,  name, additive)
    {
        _layerMixer.SetLayerMaskFromAvatarMask(_destinationLayerPortU, avatarMask);
    }


    public virtual string MakeDebugInfo()
    {
        return "--| " + Name + " => " + LayerWeight.ToString("0.00") + " |--";
    }
    
    public void SetLayerWeight(float weight)
    {
        weight = Mathf.Clamp01(weight);
        LayerWeight = weight;
        _layerMixer.SetInputWeight(_destinationLayerPort, weight);
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
        _layerMixer.SetLayerMaskFromAvatarMask(_destinationLayerPortU, mask);
    }
    
    
}
