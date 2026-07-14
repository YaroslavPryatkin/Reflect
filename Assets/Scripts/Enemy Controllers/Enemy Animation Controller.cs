using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using Debug = UnityEngine.Debug;
using BaseActionTransitionsEnum = Utility.BaseActionTransitionsEnum;

public class EnemyAnimationController : MonoBehaviour
{
    
    [Header("Target")]
    [SerializeField] private GameObject body;
    
    [Header("Movement Clips")]
    [SerializeField] private AnimationClip idleClip;
    
    private EnemySensors _sensors;
    private AnimationLayerController _animationLayerController;
    
    private void Awake()
    {if (!TryGetComponent(out AnimationController controller))
        {
            Debug.LogError("No AnimationController found!", this);
            enabled = false;
            return;
        }
        
        var clips = new HashSet<AnimationClip>();
        clips.Add(idleClip);
        _animationLayerController = controller.GetAnimationLayer(clips, 0);
        _animationLayerController.SetPlayableWeight(idleClip, 1f);
        _animationLayerController.SetLayerWeight(1f);
        _sensors = GetComponent<EnemySensors>();
    }
}
