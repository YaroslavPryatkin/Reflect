using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Playables;
using UnityEngine.Animations;
using BaseActionTransitionsEnum = UtilityFunctions.BaseActionTransitionsEnum;

public class MultidirectionalMovementController : MonoBehaviour
{
    [SerializeField] private MultidirectionalMovementSettings settings;

    public bool ShouldUseMultiDirectionalAnimation { get; set; } = false;

    [Serializable]
    private struct MultidirectionalMovement
    {
        public AnimationClip animationClip;
        public float angle;
    }

    private AnimationLayerController  _animationLayerController;
    private Sensors _sensors;
    
    private float[] _multipliers;
    private int _directionsCount;
    
    
    private void Awake()
    {
        if (!TryGetComponent(out AnimationAndRigManager controller))
        {
            Debug.LogError("No AnimationController found!", this);
            enabled = false;
            return;
        }
        
        _sensors = GetComponent<Sensors>();
        
        settings.Initialize(controller, out _animationLayerController);
        _transitionState = settings.crossFadeDuration;
    }
    
    private UtilityClasses.BaseActionAutomaticTransition _transitionState;


    private int _currentIndex1;
    private int _currentIndex2;
    
    private void Update()
    {
        if (_transitionState != BaseActionTransitionsEnum.Base)
        {
            settings.UpdateWeights(_animationLayerController, _sensors, ref _currentIndex1, ref _currentIndex2);
        }

        _animationLayerController.SetLayerWeight(_transitionState.GetFraction(ShouldUseMultiDirectionalAnimation));
    }
}
