using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Assemblies;
using BaseActionTransitionsEnum = Utility.BaseActionTransitionsEnum;

public class CurrentPlayableAnimationLayerController : AnimationLayerController
{

    public CurrentPlayableAnimationLayerController(PlayableGraph graph, AnimationLayerMixerPlayable layerMixer, 
        uint destinationLayerPort, HashSet<AnimationClip> uniqueClips, string name, bool additive) : base( graph,  layerMixer, 
        destinationLayerPort,  uniqueClips, name, additive)
    { 
        CurrentPort = new() { Port = -1, Weight = 1f };
    }
    
    public CurrentPlayableAnimationLayerController(PlayableGraph graph, AnimationLayerMixerPlayable layerMixer, 
        uint destinationLayerPort, HashSet<AnimationClip> uniqueClips,  AvatarMask avatarMask, string name, bool additive) : base( graph,  layerMixer, 
        destinationLayerPort,  uniqueClips, avatarMask, name, additive)
    { 
        CurrentPort = new() { Port = -1, Weight = 1f };
    }
    
    public override string MakeDebugInfo()
    {
        var res = "--| " + Name + " => " + LayerWeight.ToString("0.00") +" |--\n" +
                  "Current port: " + CurrentPort.ToString();
        if (_previousPortsCount != 0)
        {
            res += ", Previous ports: ";
            for (var i = 0; i < _previousPortsCount; ++i)
            {
                res += _previousPorts[i].ToString() + (i == _previousPortsCount ? "" : ", ");
            }
        }

        return res;
    }
    
    protected struct PortAndWeight
    {
        public int Port { get; set; }
        public float Weight{ get; set; }

        public override string ToString()
        {
            return "[ " + Port + " => " + Weight.ToString("0.00") + " ]";
        }
    }
    
    protected PortAndWeight CurrentPort;
    private const int PreviousPortsCapacity = 4;
    private int _previousPortsCount = 0;
    private readonly PortAndWeight[] _previousPorts = new PortAndWeight[PreviousPortsCapacity];

    protected void InternalChangeCurrentPort(int newPort)
    {
        if (CurrentPort.Port != -1)
        {
            var isPortMerged = false;

            for (var i = 0; i < _previousPortsCount; ++i)
            {
                _previousPorts[i].Weight *= 1 - CurrentPort.Weight;

                if (_previousPorts[i].Port == CurrentPort.Port)
                {
                    _previousPorts[i].Weight += CurrentPort.Weight;
                    isPortMerged = true;
                }
            }

            if (!isPortMerged)
            {
                if (_previousPortsCount < PreviousPortsCapacity)
                {
                    _previousPorts[_previousPortsCount] = CurrentPort;
                    _previousPortsCount++;
                }
                else
                {
                    AnimationMixer.SetInputWeight(_previousPorts[0].Port, 0f);

                    for (var i = 1; i < PreviousPortsCapacity; ++i)
                    {
                        _previousPorts[i - 1] = _previousPorts[i];
                    }

                    _previousPorts[PreviousPortsCapacity - 1] = CurrentPort;
                }
            }
        }

        CurrentPort = new(){Port = newPort, Weight = 0f};
    }

    protected void InternalChangeCurrentPlayableSpeedAndTime(float clipSpeed, bool updateTime)
    {
        if (CurrentPort.Port != -1)
        {
            var currentPlayable = AnimationMixer.GetInput(CurrentPort.Port);
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
        if (input == null || input.ClipIsNull) return;

        if (ClipToPort.TryGetValue(input.Clip, out var newPort))
        {
            if (newPort != CurrentPort.Port)
            {
                InternalChangeCurrentPort(newPort);
                InternalChangeCurrentPlayableSpeedAndTime(input.ClipSpeed, true);
            }
            else
            {
                InternalChangeCurrentPlayableSpeedAndTime(input.ClipSpeed, input.ShouldUpdateCurrentPlayableAnyway);
            }
        }
        else
        {
            Debug.LogError("Didn't register the clip: " + input.Clip);
        }
    }
    
    public void UpdateCurrentPlayableTransitioningWeight(float weight)
    {
        if (_previousPortsCount == 0)
        {
            AnimationMixer.SetInputWeight(CurrentPort.Port, 1f);
            return;
        }
        
        weight = Mathf.Clamp01(weight);
        
        var realCurrentWeight = weight;
        
        for (var i = 0; i < _previousPortsCount; ++i)
        {
            if (_previousPorts[i].Port == CurrentPort.Port)
            {
                realCurrentWeight += _previousPorts[i].Weight * (1 - weight);
            }
            else
            {
                AnimationMixer.SetInputWeight(
                    _previousPorts[i].Port, _previousPorts[i].Weight * (1 - weight));
            }
        }

        CurrentPort.Weight = weight;
        AnimationMixer.SetInputWeight(CurrentPort.Port, realCurrentWeight);
    }

    public void FinishTransitioningToCurrentPlayable()
    {
        for (var i = 0; i < _previousPortsCount; ++i)
        {
            AnimationMixer.SetInputWeight(
                _previousPorts[i].Port, 0f);
        }

        _previousPortsCount = 0;

        if (CurrentPort.Port == -1)
        {
            Debug.LogError("Current port == -1");
            return;
        }

        CurrentPort.Weight = 1f;
        AnimationMixer.SetInputWeight(CurrentPort.Port, 1f);
    }
    
    
}
