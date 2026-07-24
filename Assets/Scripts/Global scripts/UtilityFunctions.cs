using UnityEngine;
using System.Collections.Generic;
using System;


public static class UtilityFunctions 
{
    public interface IPlayable
    {
        public float ClipSpeed => 0f;   
        public bool ClipIsNull  => true;
        public AnimationClip Clip => null;
        public bool ShouldUpdateCurrentPlayableAnyway => false;
    }

    public enum BaseActionTransitionsEnum { Base, BaseToAction, Action, ActionToBase }
    
    
    
    public static float GetTransitionFraction(UtilityClasses.IFractionTimer<BaseActionTransitionsEnum> timer, float weightMultiplier = 1f)
    {
        return timer.Value switch
        {
            BaseActionTransitionsEnum.Action => weightMultiplier,
            BaseActionTransitionsEnum.BaseToAction => Mathf.Clamp01(timer.TimeFraction) * weightMultiplier,
            BaseActionTransitionsEnum.ActionToBase => (1f - Mathf.Clamp01(timer.TimeFraction)) * weightMultiplier,
            _ => 0f
        };
    }
    public static float GetAnimationSpeed(AnimationClip animationClip, float targetTime)
    {
        return animationClip.length / Mathf.Max(0.001f, targetTime);
    }
    
    public static float GetAnimationSpeed(AnimationClip animationClip, float targetTime, float crossFadeDuration)
    {
        return animationClip.length / Mathf.Max(0.001f, animationClip.isLooping ? targetTime : targetTime + crossFadeDuration);
    }

    public static float GetSpeedFraction(AnimationClip target, AnimationClip origin)
    {
        return origin.length / Mathf.Max(0.001f, target.length);
    }
    
    
    public static Vector3 ProjectPointOnLine(Transform point, Transform line)
    {
        var toPoint = point.position - line.position;
    
        var t = Vector3.Dot(toPoint, line.forward);
        
        return line.position + line.forward * t;
    }
    
    
    private static float distancePrecision = 0.01f;
    private static float fractionPrecision = 0.01f;
    private static float stepFromHitPoint = 0.05f;
    
    public static Vector3 GetSphereRayCastPoint(Vector3 origin, Vector3 direction, float distance, float cameraRadius, LayerMask ignoreLayers)
    {
        direction.Normalize();
        RaycastHit hit;
        if (Physics.SphereCast(origin, cameraRadius, direction, out hit, distance, ~ignoreLayers))
        {
            return  origin + direction * hit.distance;
        }
    
        return origin + direction * distance;
    }
    
    public static bool HasLineOfSight(Vector3 origin, Collider target, int layerMask)
    {
        
        if (Physics.Linecast(origin, target.bounds.center, out var hit, layerMask))
        {
            if (hit.collider == target)
            {
                return true;
            }
        }
        return false;
    }
    
    private readonly static Collider[] _colliders = new Collider[6];
    public static bool HasLineOfSight(Vector3 origin, Vector3 target, int layerMask, int targetLayerMask)
    {
        var count = Physics.OverlapSphereNonAlloc(origin, 0.1f, _colliders, layerMask);
        for(var i=0;i<count;++i)
        {
            if (((1<<_colliders[i].gameObject.layer) & targetLayerMask) == 0) 
            {
                return false;
            }
        }
        
        if (Physics.Linecast(origin, target, out var hit, layerMask))
        {
            if (((1<<hit.collider.gameObject.layer) & targetLayerMask) != 0) 
            {
                return true;
            }
        }
        return false;
    }


    private const int CurveEvaluatingResolution = 100;
    public static float EvaluateCurveAverage(AnimationCurve curve)
    {
        var sum = 0f;
        for (var i = 0; i < CurveEvaluatingResolution; i++)
        {
            var t = (float)i / (CurveEvaluatingResolution - 1);
            sum += curve.Evaluate(t);
        }
        return sum / CurveEvaluatingResolution;
    }

    public static AnimationCurve BakeIntegralCurve(AnimationCurve curve)
    {
        var res = new AnimationCurve();
        res.AddKey(0f, 0f);
        var accum = 0f;
        var dt = 1f / CurveEvaluatingResolution;
        var prevVal = curve.Evaluate(0f);
        for (var i = 1; i <= CurveEvaluatingResolution; i++)
        {
            var t = i * dt;
            var currVal = curve.Evaluate(t);
            
            accum += (prevVal + currVal) * 0.5f * dt; 
            res.AddKey(t, accum);
            prevVal = currVal;
        }

        return res;
    }
    
    public static AnimationCurve BakeDistanceCurve(AnimationCurve speedCurve, float desiredDistance)
    {
        var values = new float[CurveEvaluatingResolution + 1];
        var dt = 1f / CurveEvaluatingResolution;
        var accum = 0f;
        var prevVal = speedCurve.Evaluate(0f);
        values[0] = 0f;

        for (var i = 1; i <= CurveEvaluatingResolution; i++)
        {
            var currVal = speedCurve.Evaluate(i * dt);
            accum += (prevVal + currVal) * 0.5f * dt;
            values[i] = accum;
            prevVal = currVal;
        }

        var res = new AnimationCurve();

        if (accum <= 0.001f)
        {
            res.AddKey(0f, 0f);
            res.AddKey(1f, desiredDistance);
            return res;
        }

        var scale = desiredDistance / accum;
        for (var i = 0; i <= CurveEvaluatingResolution; i++)
        {
            res.AddKey(i * dt, values[i] * scale);
        }

        return res;
    }
    
    public static Vector3 GetCapsuleRayCastPoint(Vector3 origin, Vector3 centerToTop, float capsuleRadius, Vector3 direction, float distance, LayerMask layerMask)
    {
        direction.Normalize();
        
        int iterations = 70;
        var capsuleUp = centerToTop.normalized;

        Vector3 result = origin;
        //String resultCouse = "Origin";

        var binSearchResPoint = Vector3.zero;
        var binSearchResNormal = Vector3.zero;
        
        float distLeft = 0;
        float distRight = distance;
        while (distRight - distLeft > distancePrecision)
        {
            var distMid = (distLeft + distRight) / 2;


            bool found = false;
            //this internal binary search projects the point origin + direction * distMid onto the closest surface touched by capsule
            float fracLeft = 0f;
            float fracRight = 1f;
            while (fracRight - fracLeft > fractionPrecision)
            {
                iterations--;
                if (iterations <= 0)
                    return result;
                
                
                var fracMid = (fracRight + fracLeft) / 2;
                var midCenterToTop = centerToTop * fracMid;
                var midRadius = capsuleRadius * fracMid;


                if (Physics.CapsuleCast(origin - midCenterToTop, origin + midCenterToTop, midRadius,
                        direction, out RaycastHit hit, distMid, layerMask))
                {
                    //Debug.Log("Fraction checking: " + fracMid + " for dist mid = " + distMid + " === success");
                    fracRight = fracMid;
                    binSearchResPoint = hit.point;
                    binSearchResNormal = hit.normal;
                    found = true;
                }
                else
                {
                    //Debug.Log("Fraction checking: " + fracMid + " for dist mid = " + distMid + " === fail");
                    fracLeft = fracMid;
                }
            }

            if (found)
            {
                //some capsule touched some surface
                Vector3 targetPos;

                float dot = Vector3.Dot(binSearchResNormal, capsuleUp);

                targetPos = binSearchResPoint + binSearchResNormal * (capsuleRadius + stepFromHitPoint);
                if (dot > 0.01f)
                {
                    targetPos += centerToTop;
                }
                else if (dot < -0.01f)
                {
                    targetPos -= centerToTop;
                }

                if (!Physics.CheckCapsule(targetPos - centerToTop, targetPos + centerToTop, capsuleRadius,
                        layerMask))
                {
                    //resultCouse = "Found by checking, ";
                    result = targetPos;
                    distLeft = distMid;
                }
                else
                {
                    //Debug.Log("Distance checking = " + distMid + " === making less");
                    distRight = distMid;
                }
            }
            else
            {
                //we are free to move to that position since no capsule touched anything
                //Debug.Log("Distance checking = " + distMid + " === making more by free to move");
                //resultCouse = "Free place";
                result = origin + direction * distMid;
                distLeft = distMid;
            }

        }
        //Debug.Log(resultCouse);
        return result;
    }
    
    public static Vector3 FromLocalToGlobalByZX(Vector3 forward, Vector3 localVector)
    {
        var lookDirXZ = new Vector3(forward.x, 0f, forward.z).normalized;

        if (lookDirXZ == Vector3.zero) 
            return localVector;

        var cameraGroundedRotation = Quaternion.LookRotation(lookDirXZ, Vector3.up);
        
        return cameraGroundedRotation * localVector;
    }

    public static float ChangeMeasurementScale(float valueA, float minA, float maxA, float minB, float maxB)
    {
        return Math.Clamp((valueA - minA) *
            (maxB-minB)/(maxA-minA) + minB,
            minB, maxB);
    }
    
    ///<summary>
    /// Fraction = (maxB-minB)/(maxA-minA)
    ///</summary>
    public static float ChangeMeasurementScaleFraction(float valueA, float minA, float fraction, float minB, float maxB)
    {
        return Math.Clamp((valueA - minA) *
            fraction + minB,
            minB, maxB);
    }

}
