using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Playables;
using UnityEngine.Animations;
using BaseActionTransitionsEnum = Utility.BaseActionTransitionsEnum;

public class MultidirectionalMovementController : MonoBehaviour
{
    [SerializeField] private List<MultidirectionalMovement> multidirectionalMovement;
    [SerializeField] private float normalSpeedForForwardMovement;
    [SerializeField] private float crossFadeDuration;

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
        if (!TryGetComponent(out AnimationController controller))
        {
            Debug.LogError("No AnimationController found!", this);
            enabled = false;
            return;
        }
        
        _sensors = GetComponent<Sensors>();
        
        var animationClips = new HashSet<AnimationClip>();
        
        foreach (var multidirectional in multidirectionalMovement)
        {
            animationClips.Add(multidirectional.animationClip);
        }
        
        _animationLayerController = controller.GetAnimationLayer(animationClips, 1);

        _directionsCount = multidirectionalMovement.Count;
        _multipliers = new float[_directionsCount];
        _multipliers[0] = 1f;
        for (var i = 1; i < _directionsCount; ++i)
        {
            _multipliers[i] = Utility.GetSpeedFraction(multidirectionalMovement[i].animationClip, multidirectionalMovement[0].animationClip);
        }
    }
    
    private readonly Utility.FractionBlockingValueTimer<BaseActionTransitionsEnum> _transitionState = 
        BaseActionTransitionsEnum.Base;


    private int _currentIndex1;
    private int _currentIndex2;
    
    private void UpdateWeights()
    {
        var targetSpeed = _sensors.SpeedAlignedWithGround/normalSpeedForForwardMovement;
        
       
        for (var i = 0; i < _directionsCount; i++)
        {
            _animationLayerController.SetPlayableSpeed(
                multidirectionalMovement[i].animationClip, targetSpeed * _multipliers[i]);
        }
        
        var velocity = _sensors.NormalizedHorizontalVelocity;
        velocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
        velocity = transform.InverseTransformDirection(velocity);
        
        
        if ((velocity.z * velocity.z + velocity.x * velocity.x) < 0.0001f)
        {
            return;
        }
        
        
        _animationLayerController.SetPlayableWeight(multidirectionalMovement[_currentIndex1].animationClip, 0f);
        _animationLayerController.SetPlayableWeight(multidirectionalMovement[_currentIndex2].animationClip, 0f);

        var theta = (float)(Mathf.Atan2(velocity.x, velocity.z) * (180.0 / Mathf.PI));

    
        var idxPos = -1;
        var idxNeg = -1;
        var minPosDiff = 360f;
        var minNegDiff = -360f;

        for (int i = 0; i < _directionsCount; i++)
        {
            var diff = (theta - multidirectionalMovement[i].angle) % 360f;
            if (diff > 180f) diff -= 360f;
            else if (diff <= -180f) diff += 360f;

            if (Mathf.Abs(diff) < 0.001f)
            {
                _currentIndex1 = i;
                _animationLayerController.SetPlayableWeight(multidirectionalMovement[i].animationClip, 1f);
                return;
            }

            if (diff > 0 && diff < minPosDiff)
            {
                minPosDiff = diff;
                idxPos = i;
            }
            else if (diff < 0 && diff > minNegDiff)
            {
                minNegDiff = diff;
                idxNeg = i;
            }
        }

        if (idxPos != -1 && idxNeg != -1)
        {
            float span = minPosDiff - minNegDiff;
            _currentIndex1 = idxPos;
            _animationLayerController.SetPlayableWeight(
                multidirectionalMovement[_currentIndex1].animationClip, -minNegDiff / span);
            _currentIndex2 = idxNeg;
            _animationLayerController.SetPlayableWeight(
                multidirectionalMovement[_currentIndex2].animationClip, minPosDiff / span);
           // Debug.Log("both, pos = " + idxPos + ", weight = "+ (-minNegDiff / span)+", neg = " + idxNeg + ", weight = " + (minPosDiff / span) + ", transition = " + _transitionState.Value);
        }
        else if (idxPos != -1)
        {
            _currentIndex1 = idxPos;
            _animationLayerController.SetPlayableWeight(
                multidirectionalMovement[_currentIndex1].animationClip, 1f);
        }
        else if (idxNeg != -1)
        {
            _currentIndex2 = idxNeg;
            _animationLayerController.SetPlayableWeight(
                multidirectionalMovement[_currentIndex2].animationClip, 1f);
        }
    }


    private void Update()
    {
        if (_transitionState != BaseActionTransitionsEnum.Base)
        {
            UpdateWeights();
        }
        
        _animationLayerController.SetLayerWeightAndChangeState(
            _transitionState, ShouldUseMultiDirectionalAnimation, crossFadeDuration);
    }
}
