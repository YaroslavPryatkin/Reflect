using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using Debug = UnityEngine.Debug;


public static class AnimationUtility
{
    
    public enum TransitionStateEnum { Current, Switching, Finished }
    public enum BaseActionTransitionsEnum { Base, BaseToAction, Action, ActionToBase }
    
    public static void UpdateLayerAnimation(
        ref PlayableGraph graph,
        AnimationMixerPlayable mixer,
        ref float[] weights,
        ref int currentPort, 
        ref AnimationClip currentClip, 
        AnimationClip targetClip, 
        float targetSpeed, 
        float crossfadeDur, 
        ref Utility.FractionBlockingValueTimer<TransitionStateEnum> transitionState)
    {
        if (currentClip != targetClip)
        {
            int newPort = 1 - currentPort;

            var oldPlayable = mixer.GetInput(newPort);
            if (oldPlayable.IsValid()) graph.DestroySubgraph(oldPlayable);
            
            var newPlayable = AnimationClipPlayable.Create(graph, targetClip);
            newPlayable.SetSpeed(targetSpeed);
            
            graph.Connect(newPlayable, 0, mixer, newPort);

            currentClip = targetClip;

            if (Mathf.Abs(crossfadeDur) >= 0.001f)
            {
                transitionState.SetForce(TransitionStateEnum.Switching, crossfadeDur);
            }
            else
            {
                transitionState.SetForce(TransitionStateEnum.Current);
                weights[newPort] = 1f;
                weights[currentPort] = 0f;
                
                var previousPlayable = mixer.GetInput(currentPort);
                if (previousPlayable.IsValid()) graph.DestroySubgraph(previousPlayable);
                
                currentPort = newPort;
            }
        }

        switch (transitionState.Value)
        {
            case TransitionStateEnum.Current:
                weights[currentPort] = 1f;
                weights[1 - currentPort] = 0f;
                var currentPlayable = mixer.GetInput(currentPort);
                if (currentPlayable.IsValid()) currentPlayable.SetSpeed(targetSpeed);
                break;
            case TransitionStateEnum.Switching:
                var weight = Mathf.Clamp01(transitionState.TimeFraction);
            
                weights[1 - currentPort] = weight;
                weights[currentPort] = 1f - weight;
                if(transitionState.CanBeChanged)
                    transitionState.SetForce(TransitionStateEnum.Finished);
                break;
            case TransitionStateEnum.Finished:
                var previousPlayable = mixer.GetInput(currentPort);
                if (previousPlayable.IsValid()) graph.DestroySubgraph(previousPlayable);
                
                currentPort = 1 - currentPort;
                transitionState.SetForce(TransitionStateEnum.Current);
                
                weights[currentPort] = 1f;
                weights[1 - currentPort] = 0f;
                break;
        }
    }
    
    
    
    public static void UpdateMultiDirectionalAnimation(
        Sensors sensors,
        Transform transform,
        AnimationMixerPlayable mixer,
        ref float[] weights,
        ref float[] baseSpeeds,
        ref float[] angles,
        bool shouldUseMultiDirectionalAnimation,
        float targetSpeed,
        float crossfadeDur,
        // ref float leftRightCurrent,
        // ref float leftRightSmoothVelocity,
        ref Utility.FractionBlockingValueTimer<BaseActionTransitionsEnum> transitionState)
    {
        if (transitionState.Value != BaseActionTransitionsEnum.Base)
        {
            for (var i = 0; i < angles.Length; i++)
            {
                var p = mixer.GetInput(i + 2);
                if (p.IsValid()) p.SetSpeed(targetSpeed * baseSpeeds[i]);
            }
            
            
            var velocity = sensors.NormalizedHorizontalVelocity;
            velocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            velocity = transform.InverseTransformDirection(velocity);

            //leftRightCurrent = Mathf.SmoothDamp(leftRightCurrent, velocity.x, ref leftRightSmoothVelocity, crossfadeDur);
            
            SetWeightsDirectional(ref weights, ref angles, velocity.z, velocity.x, 2);
        }
        // else
        // {
        //     leftRightCurrent = 0f;
        // }
        
        switch (transitionState.Value)
        {
            case BaseActionTransitionsEnum.Base:
                MultiplyWeights(ref weights, 2, angles.Length + 2, 0);
                if(shouldUseMultiDirectionalAnimation)
                    transitionState.SetForce(BaseActionTransitionsEnum.BaseToAction, crossfadeDur);
                break;
            case BaseActionTransitionsEnum.BaseToAction:
                var timeFraction = Mathf.Clamp01(transitionState.TimeFraction);
                weights[0] *= 1 - timeFraction;
                weights[1] *= 1 - timeFraction;
                MultiplyWeights(ref weights, 2, angles.Length + 2, timeFraction);
                if(!shouldUseMultiDirectionalAnimation)
                    transitionState.SetForce(BaseActionTransitionsEnum.ActionToBase, crossfadeDur, 1-timeFraction);
                if(transitionState.CanBeChanged)
                    transitionState.SetForce(BaseActionTransitionsEnum.Action);
                break;
            case BaseActionTransitionsEnum.Action:
                weights[0] *= 0f;
                weights[1] *= 0f;
                if(!shouldUseMultiDirectionalAnimation)
                    transitionState.SetForce(BaseActionTransitionsEnum.ActionToBase, crossfadeDur);
                break;
            case BaseActionTransitionsEnum.ActionToBase:
                timeFraction = Mathf.Clamp01(transitionState.TimeFraction);
                weights[0] *= timeFraction;
                weights[1] *= timeFraction;
                MultiplyWeights(ref weights, 2, angles.Length + 2, 1 - timeFraction);
                if(shouldUseMultiDirectionalAnimation)
                    transitionState.SetForce(BaseActionTransitionsEnum.BaseToAction, crossfadeDur, 1-timeFraction);
                if(transitionState.CanBeChanged)
                    transitionState.SetForce(BaseActionTransitionsEnum.Base);
                break;
        }
    }

    private static void SetWeightsDirectional(ref float[] weights, ref float[] angles, float forwardBackwardSpeed, float leftRightSpeed, int start = 0)
{
    int numAngles = angles.Length;

    for (var i = 0; i < numAngles; i++)
    {
        weights[start + i] = 0f;
    }

    if ((forwardBackwardSpeed * forwardBackwardSpeed + leftRightSpeed * leftRightSpeed) < 0.0001f)
    {
        if (numAngles > 0)
        {
            weights[start] = 1f;
        }
        //Debug.Log("null speed");
        return;
    }

    float theta = (float)(Mathf.Atan2(leftRightSpeed, forwardBackwardSpeed) * (180.0 / Mathf.PI));

    
    var idxPos = -1;
    var idxNeg = -1;
    var minPosDiff = 360f;
    var minNegDiff = -360f;

    for (int i = 0; i < numAngles; i++)
    {
        float diff = (theta - angles[i]) % 360f;
        if (diff > 180f) diff -= 360f;
        else if (diff <= -180f) diff += 360f;

        if (Mathf.Abs(diff) < 0.001f)
        {
            weights[start + i] = 1f;
            //Debug.Log("Theta = " + theta + ", exit couse found = " + start + i);
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
    
    //Debug.Log("Theta = " + theta + ", idx neg = " + idxNeg + ", idx pos = " + idxPos);

    if (idxPos != -1 && idxNeg != -1)
    {
        float span = minPosDiff - minNegDiff;
        weights[start + idxPos] = -minNegDiff / span;
        weights[start + idxNeg] = minPosDiff / span;
    }
    else if (idxPos != -1)
    {
        weights[start + idxPos] = 1f;
    }
    else if (idxNeg != -1)
    {
        weights[start + idxNeg] = 1f;
    }
}
    
    private static void MultiplyWeights(ref float[] weights, int start, int finish, float factor)
    {
        for(var i = start;i<finish; ++i)
            weights[i] *= factor;
    }

    public static void UseWeights(
        AnimationMixerPlayable mixer,
        ref float[] weights,
        int mixerSize)
    {
        for (int i = 0; i < mixerSize; i++)
        {
            mixer.SetInputWeight(i, weights[i]);

        }
    }
    
    public static void ConnectPersistentClip(ref PlayableGraph graph, AnimationMixerPlayable mixer, int port, AnimationClip clip, float startWeight = 0f)
    {
        var clipPlayable = AnimationClipPlayable.Create(graph, clip);
        clipPlayable.SetSpeed(1f);
        graph.Connect(clipPlayable, 0, mixer, port);
        mixer.SetInputWeight(port, startWeight);
    }
    
    
    public static float GetAnimationSpeed(AnimationClip animationClip, float targetTime)
    {
        return animationClip.length / Mathf.Max(0.001f, targetTime);
    }

    public static float GetSpeedFraction(AnimationClip target, AnimationClip origin)
    {
        return origin.length / Mathf.Max(0.001f, target.length);
    }
}
