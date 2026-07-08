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
                var currentPlayable = mixer.GetInput(currentPort);
                if (currentPlayable.IsValid()) currentPlayable.SetSpeed(targetSpeed);
                break;
            case TransitionStateEnum.Switching:
                var newPort = 1 - currentPort;
                var weight = Mathf.Clamp01(transitionState.TimeFraction);
            
                weights[newPort] = weight;
                weights[currentPort] = 1f - weight;
                if(transitionState.CanBeChanged)
                    transitionState.SetForce(TransitionStateEnum.Finished);
                break;
            case TransitionStateEnum.Finished:
                var previousPlayable = mixer.GetInput(currentPort);
                if (previousPlayable.IsValid()) graph.DestroySubgraph(previousPlayable);
                
                currentPort = 1 - currentPort;
                transitionState.SetForce(TransitionStateEnum.Current);
                break;
        }
    }
    
    public static void UpdateMultiDirectionalAnimation(
        Sensors sensors,
        Transform transform,
        AnimationMixerPlayable mixer,
        ref float[] weights,
        ref float[] baseSpeeds,
        bool shouldUseMultiDirectionalAnimation,
        float targetSpeed,
        float crossfadeDur,
        ref float leftRightCurrent,
        ref float leftRightSmoothVelocity,
        ref Utility.FractionBlockingValueTimer<BaseActionTransitionsEnum> transitionState, 
        out float rotation)
    {
        rotation = 0f;
        if (transitionState.Value != BaseActionTransitionsEnum.Base)
        {
            var velocity = sensors.NormalizedHorizontalVelocity;
            velocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            velocity = transform.InverseTransformDirection(velocity);

            leftRightCurrent = Mathf.SmoothDamp(leftRightCurrent, velocity.x, ref leftRightSmoothVelocity, crossfadeDur);
            
            var wForward = Mathf.Max(0f, velocity.z);
            var wBackward = Mathf.Max(0f, -velocity.z);
            var wLeft = Mathf.Max(0f, -leftRightCurrent);
            var wRight = Mathf.Max(0f, leftRightCurrent);
            
            rotation = Mathf.Atan2(leftRightCurrent, velocity.z) * Mathf.Rad2Deg;
            if (Mathf.Abs(rotation) > 90f)
            {
                rotation = (rotation - Mathf.Sign(rotation) * 90f)/3f;
            }
            else
            {
                rotation /= 2f;
            }
            
           
            var totalWeight = wForward + wBackward + wLeft + wRight;
            if (totalWeight > 0.001f)
            {
                weights[2] = wForward / totalWeight; 
                weights[3] = wBackward / totalWeight; 
                weights[4] = wLeft / totalWeight; 
                weights[5] = wRight / totalWeight;
            }
            else
            {
                weights[2] = 1f;
                weights[3] = 0f;
                weights[4] = 0f;
                weights[5] = 0f;
            }
            
            
            for (var i = 2; i <= 5; i++)
            {
                var p = mixer.GetInput(i);
                if (p.IsValid()) p.SetSpeed(targetSpeed * baseSpeeds[i-2]);
            }
        }
        
        switch (transitionState.Value)
        {
            case BaseActionTransitionsEnum.Base:
                weights[2] *= 0f;
                weights[3] *= 0f;
                weights[4] *= 0f;
                weights[5] *= 0f;
                if(shouldUseMultiDirectionalAnimation)
                    transitionState.SetForce(BaseActionTransitionsEnum.BaseToAction, crossfadeDur);
                break;
            case BaseActionTransitionsEnum.BaseToAction:
                var timeFraction = Mathf.Clamp01(transitionState.TimeFraction);
                weights[0] *= 1 - timeFraction;
                weights[1] *= 1 - timeFraction;
                weights[2] *= timeFraction;
                weights[3] *= timeFraction;
                weights[4] *= timeFraction;
                weights[5] *= timeFraction;
                if(!shouldUseMultiDirectionalAnimation)
                    transitionState.SetForce(BaseActionTransitionsEnum.ActionToBase, crossfadeDur, 1-timeFraction);
                if(transitionState.CanBeChanged)
                    transitionState.SetForce(BaseActionTransitionsEnum.Action);
                break;
            case BaseActionTransitionsEnum.Action:
                weights[0] *= 0f;
                weights[1] *= 0f;
                weights[2] *= 1f;
                weights[3] *= 1f;
                weights[4] *= 1f;
                weights[5] *= 1f;
                if(!shouldUseMultiDirectionalAnimation)
                    transitionState.SetForce(BaseActionTransitionsEnum.ActionToBase, crossfadeDur);
                break;
            case BaseActionTransitionsEnum.ActionToBase:
                timeFraction = Mathf.Clamp01(transitionState.TimeFraction);
                weights[0] *= timeFraction;
                weights[1] *= timeFraction;
                weights[2] *= 1 - timeFraction;
                weights[3] *= 1 - timeFraction;
                weights[4] *= 1 - timeFraction;
                weights[5] *= 1 - timeFraction;
                if(shouldUseMultiDirectionalAnimation)
                    transitionState.SetForce(BaseActionTransitionsEnum.BaseToAction, crossfadeDur, 1-timeFraction);
                if(transitionState.CanBeChanged)
                    transitionState.SetForce(BaseActionTransitionsEnum.Base);
                break;
        }
    }

    public static void UseWeights(
        AnimationMixerPlayable mixer,
        ref float[] weights,
        int mixerSize)
    {
       // string res = "";
        for (int i = 0; i < mixerSize; i++)
        {
            mixer.SetInputWeight(i, weights[i]);
            //res += "[ " + i + ", weight = " + weights[i];
            var p = mixer.GetInput(i);
            // if (p.IsValid()) 
            // {
            //     res += ", speed = " + p.GetSpeed();
            // }
            //
            // res += " ] ";
        }
        //Debug.Log(res);
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
