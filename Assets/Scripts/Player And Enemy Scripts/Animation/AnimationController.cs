using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

[DefaultExecutionOrder(-60)]
public class AnimationController : MonoBehaviour
{
    [SerializeField] private GameObject body;
    [SerializeField] private int inputCount = 4;
    
    private PlayableGraph _graph;
    private AnimationLayerMixerPlayable _layerMixer;
    private readonly HashSet<uint> _usedLayers  = new ();
    private Animator _animator;
    
    private readonly Dictionary<int, IkRigTargetController> _rigs = new ();
    
    private void Awake()
    {
        _animator = body.GetComponent<Animator>();
        InitPlayableGraph();
    }
    
    private void OnDestroy()
    {
        if (_graph.IsValid()) _graph.Destroy();
    }
    
    private void InitPlayableGraph()
    {
        _graph = _animator.playableGraph;
        if (!_graph.IsValid())
        {
            _graph = PlayableGraph.Create("DirectAnimationGraph");
        }
        
        _layerMixer = AnimationLayerMixerPlayable.Create(_graph, inputCount);
        
        AnimationPlayableOutput output;
        if (_graph.GetOutputCount() > 0)
        {
            output = (AnimationPlayableOutput)_graph.GetOutput(0);
        }
        else
        {
            output = AnimationPlayableOutput.Create(_graph, "Animation", _animator);
        }
        
        output.SetSourcePlayable(_layerMixer);

        if (TryGetComponent(out RigBuilder rigBuilder))
        {
            rigBuilder.Build();
        }
        
        
        _graph.Play();
    }

    public AnimationLayerController GetAnimationLayer(HashSet<AnimationClip> uniqueClips, uint destinationPort, bool additive = false)
    {
        AddNewPortToSet(destinationPort);
        return new AnimationLayerController(_graph, _layerMixer, destinationPort, uniqueClips, additive);
    }
    
    public AnimationLayerController GetAnimationLayer(HashSet<AnimationClip> uniqueClips, uint destinationPort, AvatarMask avatarMask, bool additive = false)
    {
        AddNewPortToSet(destinationPort);
        return new AnimationLayerController(_graph, _layerMixer, destinationPort, uniqueClips, avatarMask, additive);
    }
    
    public AutomaticAnimationLayerController GetAutomaticAnimationLayer(HashSet<AnimationClip> uniqueClips, uint destinationPort, bool additive = false)
    {
        AddNewPortToSet(destinationPort);
        return new AutomaticAnimationLayerController(_graph, _layerMixer, destinationPort, uniqueClips, additive);
    }
    
    public AutomaticAnimationLayerController GetAutomaticAnimationLayer(HashSet<AnimationClip> uniqueClips, uint destinationPort, AvatarMask avatarMask, bool additive = false)
    {
        AddNewPortToSet(destinationPort);
        return new AutomaticAnimationLayerController(_graph, _layerMixer, destinationPort, uniqueClips,avatarMask, additive);
    }

    private void AddNewPortToSet(uint destinationPort)
    {
        if(destinationPort >= inputCount)
            throw new ArgumentException($"The port {destinationPort} does not fit into the current limit of {inputCount} ports.");
        if(_usedLayers.Contains(destinationPort))
            throw new ArgumentException($"The port {destinationPort} is already in use");
        _usedLayers.Add(destinationPort);
    }

    public void AddRig(IkRigTargetController rig)
    {
        if (_rigs.ContainsKey(rig.Index))
            throw new ArgumentException($"The rig {rig.Index} is already in use");
        
        _rigs.Add(rig.Index, rig);
    }

    public IkRigTargetController GetRig(int index)
    {
        return _rigs[index];
    }
}
