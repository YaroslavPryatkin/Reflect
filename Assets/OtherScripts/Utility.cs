using System;
using NUnit.Framework;
using UnityEngine;

public static class Utility
{
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
    
    public static Vector3 GetCapsuleRayCastPointOld(Vector3 origin, Vector3 centerToTop, float capsuleRadius, Vector3 direction, float distance, LayerMask ignoreLayers)
    {
        direction.Normalize();
        RaycastHit hit;
        
        if (Physics.CapsuleCast(origin - centerToTop, origin + centerToTop, capsuleRadius, direction, out hit, distance, ~ignoreLayers))
        {
            return  origin + direction * hit.distance;
        }
    
        return origin + direction * distance;
    }
    
    public static Vector3 GetCapsuleRayCastPoint(Vector3 origin, Vector3 centerToTop, float capsuleRadius, Vector3 direction, float distance, LayerMask ignoreLayers)
    {
        direction.Normalize();
        LayerMask targetMask = ~ignoreLayers;
        
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
                        direction, out RaycastHit hit, distMid, targetMask))
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
                        targetMask))
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
    
    
    public struct Timer
    {
        private float targetTime;

        public void Start(float duration) => targetTime = Time.time + duration;
        public bool IsReady => Time.time >= targetTime;
    }
    
    ///<summary>
    /// Blocks changing the value for the specified duration
    ///</summary>
    public struct ValueTimer<T>
    {
        private float targetTime;
        public T Value { get; private set; }

        public ValueTimer(T initialVal)
        {
            Value = initialVal;
            targetTime = 0f;
        }
        
        public bool Set(T value, float duration = 0)
        {
            if (Time.time < targetTime) return false;
            
            SetForce(value, duration);
            return true;
        }

        public void SetForce(T value, float duration = 0)
        {
            targetTime = Time.time + duration;
            Value = value;
        }

        public bool CanBeChanged => Time.time >= targetTime;
        
        public static implicit operator ValueTimer<T>(T value)
        {
            return new ValueTimer<T>(value);
        }
        
        public static implicit operator T(ValueTimer<T> timer)
        {
            return timer.Value;
        }
    }

    ///<summary>
    /// Delays changing the value for the specified delay
    ///</summary>
    public struct DelayedValueTimer<T>
    {
        private float targetTime;
        private T lastValue;
        private T value;

        public DelayedValueTimer(T value)
        {
            this.value = value;
            lastValue = value;
            targetTime = 0f;
        }

        public T Value => Time.time < targetTime ? lastValue : value;

        public T RealValue => value;
        
        public void Set(T value, float delay = 0)
        {
            lastValue = Value; 
            targetTime = Time.time + delay;
            this.value = value;
        }
        
        public static implicit operator DelayedValueTimer<T>(T value)
        {
            return new DelayedValueTimer<T>(value);
        }
        
        public static implicit operator T(DelayedValueTimer<T> timer)
        {
            return timer.Value;
        }
    }
    
    ///<summary>
    /// Delays changing the value for the specified delay
    ///</summary>
    public struct TemporaryValue<T>
    {
        private float targetTime;
        public T BaseValue{get; set;}
        public T ActiveValue{get; private set;}

        public TemporaryValue(T baseValue, T activeValue)
        {
            BaseValue = baseValue;
            ActiveValue = activeValue;
            targetTime = 0f;
        }
        public TemporaryValue(T value)
        {
            BaseValue = value;
            ActiveValue = value;
            targetTime = 0f;
        }

        public T Value => Time.time < targetTime ? ActiveValue : BaseValue;
        
        
        public void Activate(T activeValue, float duration)
        {
            ActiveValue = activeValue; 
            targetTime = Time.time + duration;
        }

        public void Activate(float duration)
        {
            targetTime = Time.time + duration;
        }

        public void Deactivate()
        {
            targetTime = 0f;
        }
        
        public static implicit operator TemporaryValue<T>(T value)
        {
            return new TemporaryValue<T>(value);
        }
        
        public static implicit operator T(TemporaryValue<T> tmpValue)
        {
            return tmpValue.Value;
        }
    }
    
    public struct DelayDurationValueTimer<T>
    {
        private float delayTime;
        private float durationTime;
        private T lastValue;
        private T value;

        public DelayDurationValueTimer(T value)
        {
            this.value = value;
            lastValue = value;
            delayTime = 0f;
            durationTime = 0f;
        }

        public T Value => Time.time < delayTime ? lastValue : value;

        public T RealValue => value;
        
        public float DelayTime => Math.Max(delayTime - Time.time, 0f);
        public float DurationTime => Math.Max(durationTime - Time.time, 0f);

        public bool Set(T value, float delay = 0, float duration = 0)
        {
            if (Time.time < durationTime) return false;
            
            SetForce(value, delay, duration);
            return true;
        }
        
        public void SetForce(T value, float delay = 0, float duration = 0)
        {
            lastValue = Value; 
            delayTime = Time.time + delay;
            durationTime = Time.time + duration;
            //Debug.Log("Current time = " + Time.time + ", delay time = " + delayTime + ", duration time = " + durationTime + ", new value = " + value);
            this.value = value;
        }

        public bool CanBeChanged => Time.time >= durationTime;
        
        public static implicit operator DelayDurationValueTimer<T>(T value)
        {
            return new DelayDurationValueTimer<T>(value);
        }
        
        public static implicit operator T(DelayDurationValueTimer<T> timer)
        {
            return timer.Value;
        }
    }
    
}
