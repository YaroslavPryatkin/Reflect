using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

[DefaultExecutionOrder(-60)]
public class AnimationAndRigManager : MonoBehaviour
{
    [SerializeField] private GameObject body;
    [SerializeField] private int inputCount = 7;
    
    [Header("Debug")]
    [SerializeField] private bool doDebug=false;
    
    private PlayableGraph _graph;
    private AnimationLayerMixerPlayable _layerMixer;
    private readonly SortedSet<uint> _usedLayers  = new ();
    private Animator _animator;
    
    private readonly Dictionary<int, IkRigTargetController> _rigs = new ();
    
    private AnimationLayerController[]  _controllers;
    private TextMeshProUGUI  _debugText;
    
    
    private void Update()
    {
        if (doDebug)
        {
            var res = Time.timeScale + "\nLayers: ";
            foreach (var index in _usedLayers)
            {
                res += index + " : "+_controllers[index].MakeDebugInfo() + "\n";
            }
            _debugText.text = res;
        }
    }

    private void Awake()
    {
        _animator = body.GetComponent<Animator>();
        InitPlayableGraph();

        if (doDebug)
        {
            _controllers = new AnimationLayerController[inputCount];
            _debugText = GlobalUIManager.DebugText;
        }
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
        
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        
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
        _graph.Play();

        if (TryGetComponent(out RigBuilder rigBuilder))
        {
            rigBuilder.Build();
        }
        
        
    }

    public AnimationLayerController GetAnimationLayer(HashSet<AnimationClip> uniqueClips, uint destinationPort, string name, bool additive = false)
    {
        AddNewPortToSet(destinationPort);
        var res =new AnimationLayerController(_graph, _layerMixer, destinationPort, uniqueClips, name, additive);
        if (doDebug)
        {
            _controllers[destinationPort] = res;
        }

        return res;
    }
    
    public AnimationLayerController GetAnimationLayer(HashSet<AnimationClip> uniqueClips, uint destinationPort, AvatarMask avatarMask,string name, bool additive = false)
    {
        AddNewPortToSet(destinationPort);
        var res = new AnimationLayerController(_graph, _layerMixer, destinationPort, uniqueClips, avatarMask, name, additive);
        if (doDebug)
        {
            _controllers[destinationPort] = res;
        }

        return res;
    }

    public CurrentPlayableAnimationLayerController GetCurrentPlayableAnimationLayer(HashSet<AnimationClip> uniqueClips, uint destinationPort, string name, bool additive = false)
    {
        AddNewPortToSet(destinationPort);
        var res =new CurrentPlayableAnimationLayerController(_graph, _layerMixer, destinationPort, uniqueClips, name, additive);
        if (doDebug)
        {
            _controllers[destinationPort] = res;
        }

        return res;
    }
    
    public CurrentPlayableAnimationLayerController GetCurrentPlayableAnimationLayer(HashSet<AnimationClip> uniqueClips, uint destinationPort, AvatarMask avatarMask,string name, bool additive = false)
    {
        AddNewPortToSet(destinationPort);
        var res = new CurrentPlayableAnimationLayerController(_graph, _layerMixer, destinationPort, uniqueClips, avatarMask, name, additive);
        if (doDebug)
        {
            _controllers[destinationPort] = res;
        }

        return res;
    }
    
    public AutomaticAnimationLayerController GetAutomaticAnimationLayer(HashSet<AnimationClip> uniqueClips, uint destinationPort, string name, bool additive = false)
    {
        AddNewPortToSet(destinationPort);
        var res = new AutomaticAnimationLayerController(_graph, _layerMixer, destinationPort, uniqueClips,name,  additive);
        if (doDebug)
        {
            _controllers[destinationPort] = res;
        }

        return res;
    }
    
    public AutomaticAnimationLayerController GetAutomaticAnimationLayer(HashSet<AnimationClip> uniqueClips, uint destinationPort, AvatarMask avatarMask,string name, bool additive = false)
    {
        AddNewPortToSet(destinationPort);
        var res = new AutomaticAnimationLayerController(_graph, _layerMixer, destinationPort, uniqueClips,avatarMask, name, additive);
        
        if (doDebug)
        {
            _controllers[destinationPort] = res;
        }

        return res;
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
