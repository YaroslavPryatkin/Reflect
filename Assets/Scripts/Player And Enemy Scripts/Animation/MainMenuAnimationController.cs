using System;
using UnityEngine;
using System.Collections.Generic;

public class MainMenuAnimationController : MonoBehaviour
{
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private float idleDuration;
    [SerializeField] private int handRigLayerIndex;
    [SerializeField] private Transform handRigSource;
    
    private AutomaticAnimationLayerController _animationLayerController;
    
    
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
        _animationLayerController = controller.GetAutomaticAnimationLayer(clips, 0, "Player base");
        _animationLayerController.SetLayerWeight(1f);
        var clipSpeed = UtilityFunctions.GetAnimationSpeed(idleClip, idleDuration);
        _animationLayerController.AutomaticUpdateCurrentPlayable(idleClip, clipSpeed, 0.15f);

        var handRig = controller.GetRig(handRigLayerIndex);

        var source = new IkRigTargetController.TransformSource(handRigSource);
        source.Weight = 1f;
        handRig.SetSource(source);
    }
}