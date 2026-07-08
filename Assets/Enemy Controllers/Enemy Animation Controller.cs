using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using Debug = UnityEngine.Debug;
using BaseActionTransitionsEnum = AnimationUtility.BaseActionTransitionsEnum;
using TransitionStateEnum =  AnimationUtility.TransitionStateEnum;

public class EnemyAnimationController : MonoBehaviour
{
    
    [Header("Target")]
    [SerializeField] private GameObject body;
    
    [Header("Movement Clips")]
    [SerializeField] private AnimationClip idleClip;
    // [SerializeField] private AnimationClip runForwardClip;
    // [SerializeField] private AnimationClip runBackwardClip;
    // [SerializeField] private AnimationClip runLeftClip;
    // [SerializeField] private AnimationClip runRightClip;
    
    
    private Animator _animator;
    private EnemySensors _sensors;

    private PlayableGraph _graph;
    private AnimationLayerMixerPlayable _layerMixer;
    private AnimationMixerPlayable _baseMixer;
    
    private void Awake()
    {
        _animator = body.GetComponent<Animator>();
        _sensors = GetComponent<EnemySensors>();
        InitPlayableGraph();
    }

    private void Start()
    {
        AnimationUtility.ConnectPersistentClip(ref _graph, _baseMixer, 0,  idleClip, 1);
    }
    
    private void InitPlayableGraph()
    {
        _graph = _animator.playableGraph;
        if (!_graph.IsValid())
        {
            _graph = PlayableGraph.Create("DirectAnimationGraph");
        }
        
        _layerMixer = AnimationLayerMixerPlayable.Create(_graph, 3);
        
        _baseMixer = AnimationMixerPlayable.Create(_graph, 1);

        _graph.Connect(_baseMixer, 0, _layerMixer, 0);

        _layerMixer.SetInputWeight(0, 1f);
        
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
        
        var rigBuilder = body.GetComponent<RigBuilder>();
        rigBuilder.Build();
        
        _graph.Play();
    }
    
    private void OnDestroy()
    {
        if (_graph.IsValid()) _graph.Destroy();
    }
}
