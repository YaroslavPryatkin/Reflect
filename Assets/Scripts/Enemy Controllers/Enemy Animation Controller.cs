using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using Debug = UnityEngine.Debug;
using BaseActionTransitionsEnum = UtilityFunctions.BaseActionTransitionsEnum;

public class EnemyAnimationController : MonoBehaviour
{
    
    [Header("Target")]
    [SerializeField] private GameObject body;
    
    [Header("Movement Clips")]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip traversalClip;
    [SerializeField] private AnimationClip aimingIdleClip;
    
    [Header("Settings")]
    [SerializeField] private float crossfadeDuration = 0.15f;
    [SerializeField] private float normalSpeedForTraversalAnimation = 9f;
    [SerializeField] private float runSpeedThreshold = 0.3f;
    
    private EnemySensors _sensors;
    private AutomaticAnimationLayerController _animationLayerController;
    private MultidirectionalMovementController _multidirController;
    private EnemyAI _enemyAI;
    
    private void Awake()
    {
        if (!TryGetComponent(out AnimationAndRigManager controller))
        {
            Debug.LogError("No AnimationController found!", this);
            enabled = false;
            return;
        }
        
        var clips = new HashSet<AnimationClip>();
        clips.Add(idleClip);
        clips.Add(traversalClip);
        clips.Add(aimingIdleClip);
        _animationLayerController = controller.GetAutomaticAnimationLayer(clips, 0, "Enemy base");
        _animationLayerController.SetLayerWeight(1f);
        
        _sensors = GetComponent<EnemySensors>();
        _multidirController = GetComponent<MultidirectionalMovementController>();
        _enemyAI = GetComponent<EnemyAI>();
    }

    private void Update()
    {
        var targetSpeed = 1f;
        var targetClip = idleClip;
        
        
        var speed = _sensors.SpeedAlignedWithGround;
        _multidirController.ShouldUseMultiDirectionalAnimation = false;

        if (_enemyAI.IsMoving &&  speed >= runSpeedThreshold)
        {
            if (_enemyAI.IsAiming)
            {
                targetClip = aimingIdleClip;
                _multidirController.ShouldUseMultiDirectionalAnimation = true;
            }
            else
            {
                targetClip = traversalClip;
                targetSpeed = speed / normalSpeedForTraversalAnimation;
            }
        }
        else
        {
            if (_enemyAI.IsAiming)
            {
                targetClip = aimingIdleClip;
            }
            else
            {
                targetClip = idleClip;
            }
        }
        
       //Debug.Log("Target clip = " + targetClip.name + ", target speed = " + targetSpeed + ", using multidir = " + _multidirController.ShouldUseMultiDirectionalAnimation);
        _animationLayerController.AutomaticUpdateCurrentPlayable(targetClip, targetSpeed, crossfadeDuration);
    }
}
