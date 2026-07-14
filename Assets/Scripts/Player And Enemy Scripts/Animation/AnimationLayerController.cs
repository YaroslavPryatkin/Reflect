using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Playables;
using UnityEngine.Animations;
using BaseActionTransitionsEnum = Utility.BaseActionTransitionsEnum;

public class AnimationLayerController
{
    private readonly AnimationLayerMixerPlayable _layerMixer;
    private readonly AnimationMixerPlayable _animationMixer;
    protected readonly int _destinationLayerPort;
    private readonly uint _destinationLayerPortU;
    protected readonly Dictionary<AnimationClip, int> ClipToPort;
    
    public AnimationLayerController(PlayableGraph graph, AnimationLayerMixerPlayable layerMixer, 
        uint destinationLayerPort, HashSet<AnimationClip> uniqueClips, bool additive)
    {
        _layerMixer = layerMixer;
        _destinationLayerPortU = destinationLayerPort;
        _destinationLayerPort = (int)destinationLayerPort;
        
        ClipToPort = new Dictionary<AnimationClip, int>();
        var portIdx = 0;
        foreach (var clip in uniqueClips) {
            ClipToPort[clip] = portIdx++;
        }

        _animationMixer = AnimationMixerPlayable.Create(graph, ClipToPort.Count);
    
        foreach (var pair in ClipToPort) {
            var clipPlayable = AnimationClipPlayable.Create(graph, pair.Key);
            graph.Connect(clipPlayable, 0, _animationMixer, pair.Value);
            _animationMixer.SetInputWeight(pair.Value, 0f);
        }
        graph.Connect(_animationMixer, 0, _layerMixer, _destinationLayerPort);
        _layerMixer.SetInputWeight(_destinationLayerPort, 0f);
        _layerMixer.SetLayerAdditive(_destinationLayerPortU, additive);
    }
    
    public AnimationLayerController(PlayableGraph graph, AnimationLayerMixerPlayable layerMixer, 
        uint destinationLayerPort, HashSet<AnimationClip> uniqueClips,  AvatarMask avatarMask, bool additive) : this( graph,  layerMixer, 
         destinationLayerPort,  uniqueClips, additive)
    {
        _layerMixer.SetLayerMaskFromAvatarMask(_destinationLayerPortU, avatarMask);
    }
    
    protected int CurrentPort { get; private set; }= -1;
    private int _previousPort = -1;

    protected void InternalChangeCurrentPort(int newPort)
    {
        if (_previousPort != -1)
            _animationMixer.SetInputWeight(_previousPort, 0f);
        _previousPort = CurrentPort;
        CurrentPort = newPort;
    }

    protected void InternalChangeCurrentPlayableSpeedAndTime(float clipSpeed, bool updateTime)
    {
        if (CurrentPort != -1)
        {
            var currentPlayable = _animationMixer.GetInput(CurrentPort);
            if (currentPlayable.IsValid())
            {
                if (updateTime)
                {
                    currentPlayable.SetTime(0f);
                }

                currentPlayable.SetSpeed(clipSpeed);
            }
        }
    }

    public void UpdateCurrentPlayable(Utility.IPlayable input)
    {
        if (input == null) return;

        if (!input.ClipIsNull && ClipToPort.TryGetValue(input.Clip, out var newPort))
        {
            var updateTime = false;

            if (newPort != CurrentPort)
            {
                InternalChangeCurrentPort(newPort);
                updateTime = true;
            }


            InternalChangeCurrentPlayableSpeedAndTime(input.ClipSpeed, updateTime);
        }
    }

    public void ResetCurrentPlayable()
    {
        if(-1!=CurrentPort)
            InternalChangeCurrentPort(-1);
    }
    
    public void UpdateCurrentPlayableWeight(float weight)
    {
        weight = Mathf.Clamp01(weight);
        if (_previousPort != -1)
            _animationMixer.SetInputWeight(_previousPort, 1f - weight);
        if (CurrentPort != -1)
            _animationMixer.SetInputWeight(CurrentPort, weight);
    }
    
    public void SetLayerWeight(float weight)
    {
        weight = Mathf.Clamp01(weight);
        _layerMixer.SetInputWeight(_destinationLayerPort, weight);
    }
    
    public void SetLayerWeight(Utility.IFractionTimer<BaseActionTransitionsEnum> fractionTimer, float weightMultiplier = 1f)
    {
        _layerMixer.SetInputWeight(_destinationLayerPort, weightMultiplier * Utility.GetTransitionFraction(fractionTimer));
    }

    public void SetLayerWeightAndChangeState(Utility.FractionBlockingValueTimer<BaseActionTransitionsEnum> state,
        bool shouldBeActive, float crossFadeDuration, float weightMultiplier = 1f)
    {
        switch (state.Value)
        {
            case BaseActionTransitionsEnum.Base:
                if(shouldBeActive)
                    state.SetForce(BaseActionTransitionsEnum.BaseToAction, crossFadeDuration);
                break;
            case BaseActionTransitionsEnum.BaseToAction:
                if(!shouldBeActive)
                    state.SetForce(BaseActionTransitionsEnum.ActionToBase, crossFadeDuration, 1-state.TimeFraction);
                if(state.CanBeChanged)
                    state.SetForce(BaseActionTransitionsEnum.Action);
                break;
            case BaseActionTransitionsEnum.Action:
                if(!shouldBeActive)
                    state.SetForce(BaseActionTransitionsEnum.ActionToBase, crossFadeDuration);
                break;
            case BaseActionTransitionsEnum.ActionToBase:
                if(shouldBeActive)
                    state.SetForce(BaseActionTransitionsEnum.BaseToAction, crossFadeDuration, 1-state.TimeFraction);
                if(state.CanBeChanged)
                    state.SetForce(BaseActionTransitionsEnum.Base);
                break;
        }
        SetLayerWeight(state, weightMultiplier);
    }
    
    public void SetPlayableWeight(AnimationClip clip, float weight)
    {
        if (ClipToPort.TryGetValue(clip, out var port))
        {
            weight= Mathf.Clamp01(weight);
            _animationMixer.SetInputWeight(port, weight);
        }
    }
    
    public void SetPlayableSpeed(AnimationClip clip, float clipSpeed)
    {
        if (ClipToPort.TryGetValue(clip, out var port))
        {
            var playable = _animationMixer.GetInput(port);
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
            var playable = _animationMixer.GetInput(port);
            if (playable.IsValid())
            {
                playable.SetSpeed(clipSpeed);
                playable.SetTime(0f);
            }
            weight= Mathf.Clamp01(weight);
            _animationMixer.SetInputWeight(port, weight);
        }
    }

    public void SetAvatarMask(AvatarMask mask)
    {
        _layerMixer.SetLayerMaskFromAvatarMask(_destinationLayerPortU, mask);
    }
}
