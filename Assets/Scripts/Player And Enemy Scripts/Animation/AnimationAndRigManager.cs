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
    
    public Animator Animator { get; private set; }
    public PlayableGraph Graph{ get; private set; }
    public AnimationLayerMixerPlayable LayerMixer{ get; private set; }
    
    
    private readonly SortedSet<uint> _usedLayers  = new ();
    
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
        Animator = body.GetComponent<Animator>();
        InitPlayableGraph();

        if (doDebug)
        {
            _controllers = new AnimationLayerController[inputCount];
            _debugText = UIManager.DebugText;
        }
    }
    
    private void OnDestroy()
    {
        if (Graph.IsValid()) Graph.Destroy();
    }
    
    private void InitPlayableGraph()
    {
        Graph = Animator.playableGraph;
        if (!Graph.IsValid())
        {
            Graph = PlayableGraph.Create("DirectAnimationGraph");
        }
        
        Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        
        LayerMixer = AnimationLayerMixerPlayable.Create(Graph, inputCount);
        
        AnimationPlayableOutput output;
        if (Graph.GetOutputCount() > 0)
        {
            output = (AnimationPlayableOutput)Graph.GetOutput(0);
        }
        else
        {
            output = AnimationPlayableOutput.Create(Graph, "Animation", Animator);
        }
        
        output.SetSourcePlayable(LayerMixer);
        Graph.Play();

        if (TryGetComponent(out RigBuilder rigBuilder))
        {
            rigBuilder.Build();
        }
        
        
    }

    public AnimationLayerController GetAnimationLayer(HashSet<AnimationClip> uniqueClips, uint destinationPort, string name, bool additive = false)
    {
        AddNewPortToSet(destinationPort);
        var res =new AnimationLayerController(this, destinationPort, uniqueClips, name, additive);
        if (doDebug)
        {
            _controllers[destinationPort] = res;
        }

        return res;
    }
    
    public AnimationLayerController GetAnimationLayer(HashSet<AnimationClip> uniqueClips, uint destinationPort, AvatarMask avatarMask,string name, bool additive = false)
    {
        AddNewPortToSet(destinationPort);
        var res = new AnimationLayerController(this, destinationPort, uniqueClips, avatarMask, name, additive);
        if (doDebug)
        {
            _controllers[destinationPort] = res;
        }

        return res;
    }

    public CurrentPlayableAnimationLayerController GetCurrentPlayableAnimationLayer(HashSet<AnimationClip> uniqueClips, uint destinationPort, string name, bool additive = false)
    {
        AddNewPortToSet(destinationPort);
        var res =new CurrentPlayableAnimationLayerController(this, destinationPort, uniqueClips, name, additive);
        if (doDebug)
        {
            _controllers[destinationPort] = res;
        }

        return res;
    }
    
    public CurrentPlayableAnimationLayerController GetCurrentPlayableAnimationLayer(HashSet<AnimationClip> uniqueClips, uint destinationPort, AvatarMask avatarMask,string name, bool additive = false)
    {
        AddNewPortToSet(destinationPort);
        var res = new CurrentPlayableAnimationLayerController(this, destinationPort, uniqueClips, avatarMask, name, additive);
        if (doDebug)
        {
            _controllers[destinationPort] = res;
        }

        return res;
    }
    
    public AutomaticAnimationLayerController GetAutomaticAnimationLayer(HashSet<AnimationClip> uniqueClips, uint destinationPort, string name, bool additive = false)
    {
        AddNewPortToSet(destinationPort);
        var res = new AutomaticAnimationLayerController(this, destinationPort, uniqueClips,name,  additive);
        if (doDebug)
        {
            _controllers[destinationPort] = res;
        }

        return res;
    }
    
    public AutomaticAnimationLayerController GetAutomaticAnimationLayer(HashSet<AnimationClip> uniqueClips, uint destinationPort, AvatarMask avatarMask,string name, bool additive = false)
    {
        AddNewPortToSet(destinationPort);
        var res = new AutomaticAnimationLayerController(this, destinationPort, uniqueClips,avatarMask, name, additive);
        
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
        if(!_usedLayers.Add(destinationPort))
            throw new ArgumentException($"The port {destinationPort} is already in use");
        
    }

    public void AddRig(IkRigTargetController rig)
    {
        if (!_rigs.TryAdd(rig.Index, rig))
            throw new ArgumentException($"The rig {rig.Index} is already in use");
    }

    public IkRigTargetController GetRig(int index)
    {
        return _rigs[index];
    }
}
