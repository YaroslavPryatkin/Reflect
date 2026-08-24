using System;
using UnityEngine;
using System.Collections.Generic;
using CustomAttributes;


[CreateAssetMenu(fileName = "MultidirectionalMovementSettings", menuName = "Scriptable Objects/MultidirectionalMovementSettings")]
public class MultidirectionalMovementSettings : ScriptableObject
{
    [SerializeField] private List<MultidirectionalMovement> movementClips;
    [SerializeField] private float normalSpeedForForwardMovement;
    [SerializeField] public float crossFadeDuration;
    
    [Serializable]
    private struct MultidirectionalMovement
    {
        public AnimationClip animationClip;
        public float angle;
    }
    
    [SerializeField, HideInInspector] private int directionsCount;
    [SerializeField, HideInInspector] private float[] multipliers;


    private void OnValidate()
    {
        directionsCount = movementClips.Count;
        if (directionsCount > 0)
        {
            multipliers = new float[directionsCount];
            multipliers[0] = 1f;
            for (var i = 1; i < directionsCount; ++i)
            {
                multipliers[i] =
                    UtilityFunctions.GetSpeedFraction(movementClips[i].animationClip, movementClips[0].animationClip);
            }
        }
    }

    public void Initialize(AnimationAndRigManager manager, 
        out AnimationLayerController  animationLayerController)
    {
        var animationClips = new HashSet<AnimationClip>();
        
        foreach (var multidirectional in movementClips)
        {
            animationClips.Add(multidirectional.animationClip);
        }
        
        animationLayerController = manager.GetAnimationLayer(animationClips, 1, "Multidir movement");
    }
    
    
    
    public void UpdateWeights(
            AnimationLayerController  animationLayerController, 
            Sensors sensors, 
            ref int currentIndex1, ref int currentIndex2)
    {
        var targetSpeed = sensors.SpeedAlignedWithGround/normalSpeedForForwardMovement;
        
       
        for (var i = 0; i < directionsCount; i++)
        {
            animationLayerController.SetPlayableSpeed(
                movementClips[i].animationClip, targetSpeed * multipliers[i]);
        }
        
        var velocity = sensors.NormalizedHorizontalVelocity;
        velocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
        velocity = sensors.transform.InverseTransformDirection(velocity);
        
        
        if ((velocity.z * velocity.z + velocity.x * velocity.x) < 0.0001f)
        {
            return;
        }
        
        
        animationLayerController.SetPlayableWeight(movementClips[currentIndex1].animationClip, 0f);
        animationLayerController.SetPlayableWeight(movementClips[currentIndex2].animationClip, 0f);

        var theta = (float)(Mathf.Atan2(velocity.x, velocity.z) * (180.0 / Mathf.PI));

    
        var idxPos = -1;
        var idxNeg = -1;
        var minPosDiff = 360f;
        var minNegDiff = -360f;

        for (int i = 0; i < directionsCount; i++)
        {
            var diff = (theta - movementClips[i].angle) % 360f;
            if (diff > 180f) diff -= 360f;
            else if (diff <= -180f) diff += 360f;

            if (Mathf.Abs(diff) < 0.001f)
            {
                currentIndex1 = i;
                animationLayerController.SetPlayableWeight(movementClips[i].animationClip, 1f);
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
            currentIndex1 = idxPos;
            animationLayerController.SetPlayableWeight(
                movementClips[currentIndex1].animationClip, -minNegDiff / span);
            currentIndex2 = idxNeg;
            animationLayerController.SetPlayableWeight(
                movementClips[currentIndex2].animationClip, minPosDiff / span);
           // Debug.Log("both, pos = " + idxPos + ", weight = "+ (-minNegDiff / span)+", neg = " + idxNeg + ", weight = " + (minPosDiff / span) + ", transition = " + _transitionState.Value);
        }
        else if (idxPos != -1)
        {
            currentIndex1 = idxPos;
            animationLayerController.SetPlayableWeight(
                movementClips[currentIndex1].animationClip, 1f);
        }
        else if (idxNeg != -1)
        {
            currentIndex2 = idxNeg;
            animationLayerController.SetPlayableWeight(
                movementClips[currentIndex2].animationClip, 1f);
        }
    }
    
}