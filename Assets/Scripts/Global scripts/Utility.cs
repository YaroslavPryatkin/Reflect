using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public static class Utility
{
    
    public interface IPlayable
    {
        public float ClipSpeed => 0f;   
        public bool ClipIsNull  => true;
        public AnimationClip Clip => null;
    }
    public enum BaseActionTransitionsEnum { Base, BaseToAction, Action, ActionToBase }

    public static float GetTransitionFraction(Utility.IFractionTimer<BaseActionTransitionsEnum> timer)
    {
        return timer.Value switch
        {
            BaseActionTransitionsEnum.Action => 1f,
            BaseActionTransitionsEnum.BaseToAction => Mathf.Clamp01(timer.TimeFraction),
            BaseActionTransitionsEnum.ActionToBase => 1f - Mathf.Clamp01(timer.TimeFraction),
            _ => 0f
        };
    }
    
    public static float GetTransitionFraction(float fraction, BaseActionTransitionsEnum state)
    {
        return state switch
        {
            BaseActionTransitionsEnum.Action => 1f,
            BaseActionTransitionsEnum.BaseToAction => Mathf.Clamp01(fraction),
            BaseActionTransitionsEnum.ActionToBase => 1f - Mathf.Clamp01(fraction),
            _ => 0f
        };
    }
    
    public static float GetAnimationSpeed(AnimationClip animationClip, float targetTime)
    {
        return animationClip.length / Mathf.Max(0.001f, targetTime);
    }

    public static float GetSpeedFraction(AnimationClip target, AnimationClip origin)
    {
        return origin.length / Mathf.Max(0.001f, target.length);
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
    
    public static bool HasLineOfSight(Vector3 origin, Transform target, int layerMask)
    {
        
        if (Physics.Linecast(origin, target.position, out var hit, layerMask))
        {
            if (hit.transform.root == target.root)
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
    
    public interface IFractionTimer<T>
    {
        public T Value { get; }
        public float TimeFraction { get; }
    }
    
    ///<summary>
    /// Blocks changing the value for the specified duration
    ///</summary>
    public class BlockingValueTimer<T>
    {
        private float targetTime;
        public T Value { get; private set; }

        public BlockingValueTimer(T initialVal)
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
        
        public static implicit operator BlockingValueTimer<T>(T value)
        {
            return new BlockingValueTimer<T>(value);
        }
        
        public static implicit operator T(BlockingValueTimer<T> timer)
        {
            return timer.Value;
        }
    }
    
    ///<summary>
    /// Blocks changing the value for the specified duration with interface for getting the time fraction
    ///</summary>
    public class FractionBlockingValueTimer<T> : IFractionTimer<T>
    {
        private float targetTime;
        private float startTime;
        public T Value { get; private set; }

        public FractionBlockingValueTimer(T initialVal)
        {
            Value = initialVal;
            targetTime = 0f;
            startTime = 0f;
        }
        
        public bool Set(T value, float duration = 0)
        {
            if (Time.time < targetTime) return false;
            
            SetForce(value, duration);
            return true;
        }

        public void SetForce(T value, float duration = 0, float fraction = 0)
        {
            Value = value;
            startTime = Time.time - duration * fraction;
            targetTime = Time.time + duration * (1-fraction);
        }

        public bool CanBeChanged => Time.time >= targetTime;
        public float TimeFraction => Mathf.Abs(targetTime - startTime) <= 0.0001f ? 1 : (Time.time-startTime)/(targetTime - startTime);
        
        public static implicit operator FractionBlockingValueTimer<T>(T value)
        {
            return new FractionBlockingValueTimer<T>(value);
        }
        
        public static implicit operator T(FractionBlockingValueTimer<T> timer)
        {
            return timer.Value;
        }
    }

    ///<summary>
    /// Delays changing the value for the specified delay
    ///</summary>
    public class DelayedValueTimer<T>
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
    
    

    public class TemporaryValue<T>
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
    
    /// <summary>
    /// Works as normal temporary value, but Deactivate removes only one Activate.
    /// </summary>
    public class MultipleTemporaryValue<T>
    {
        private readonly float[] _targetTimes;
        private readonly int _slots;
        private int _index;
        private int _endIndex;
        private readonly float _duration;
        
        private int Next(int index)
        {
            return (index + 1) % _slots;
        }
        
        private int LastActive =>  (_index - 1 + _slots) % _slots;
        private void StepForward()
        {
            if (_endIndex == _index)
            {
                _endIndex = Next(_endIndex);
            }
            _index=Next(_index);
        }

        private bool IsIndexActive(int index)
        {
            return Time.time < _targetTimes[index];
        }

        private void MoveEndIndex()
        {
            var last = LastActive;
            while(_endIndex != last && !IsIndexActive(_endIndex))
                _endIndex = Next(_endIndex);
        }
        
        public T BaseValue{get; set;}
        public T ActiveValue{get; private set;}

        public MultipleTemporaryValue(T baseValue, T activeValue, float duration, int slots = 2)
        {
            BaseValue = baseValue;
            ActiveValue = activeValue;
            _duration = duration;
            _slots = slots > 0 ? slots : 1;
            _index = 0;
            _endIndex = 0;
            _targetTimes = new float[_slots];
            for (var i = 0; i < _slots; i++)
            {
                _targetTimes[i] = 0f;
            }
        }
        public T Value => IsIndexActive(LastActive) ? ActiveValue : BaseValue;
        
        public bool IsReadyToChange
        {
            get {
                MoveEndIndex();
                return _endIndex != _index;
            }
        }
        
        public void Activate()
        {
            _targetTimes[_index] = Time.time + _duration;
            StepForward();
        }

        public void Deactivate()
        {
            MoveEndIndex();
            _targetTimes[_endIndex] = 0f;
        }

        public void DeactivateAll()
        {
            _endIndex = LastActive;
            _targetTimes[_endIndex] = 0f;
        }

        public int AmountOfActive
        {
            get
            {
                MoveEndIndex();
                if (_endIndex == LastActive)
                    return IsIndexActive(_endIndex) ? 1 : 0;
                if (_endIndex >= _index)
                    return _slots - _endIndex + _index;
                return _index - _endIndex;
            }
        }
        
        public static implicit operator T(MultipleTemporaryValue<T> tmpValue)
        {
            return tmpValue.Value;
        }
    }
    
    /// <summary>
    /// Works as normal temporary value, but Deactivate removes only one Activate.
    /// </summary>
    public class MultipleTemporaryVariedValue<T>
    {
        private struct Element
        {
            public T Value;
            public float TargetTime;

            public Element(T value)
            {
                Value = value;
                TargetTime = 0f;
            }

            public void Set(T value, float duration = 0f)
            {
                Value = value;
                TargetTime = Time.time + duration;
            }

            public void Deactivate()
            {
                TargetTime = 0f;
            }
            
            public bool ItActive => Time.time <  TargetTime;
        }
        
        private readonly Element[] _elements;
        private readonly int _slots;
        private int _index;
        private int _endIndex;
        private readonly float _duration;
        
        private int Next(int index)
        {
            return (index + 1) % _slots;
        }
        
        private int LastActive =>  (_index - 1 + _slots) % _slots;
        private void StepForward()
        {
            if (_endIndex == _index)
            {
                _endIndex = Next(_endIndex);
            }
            _index=Next(_index);
        }

        private T ValueAt(int index)
        {
            return _elements[index].ItActive ? _elements[index].Value : BaseValue;
        }
        
        private void MoveEndIndex()
        {
            var last = LastActive;
            while(_endIndex != last && !_elements[_endIndex].ItActive)
                _endIndex = Next(_endIndex);
        }
        
        public T BaseValue{get; set;}

        public MultipleTemporaryVariedValue(T baseValue, float duration, int slots = 2)
        {
            BaseValue = baseValue;
            _duration = duration;
            _slots = slots > 1 ? slots : 2;
            _index = 0;
            _endIndex = 0;
            _elements = new Element[slots];
            for (var i = 0; i < _slots; i++)
                _elements[i] = new Element(baseValue);
        }
        
        public T LastValue => ValueAt(LastActive);
        public T FirstValue {
            get{
                MoveEndIndex();
                return ValueAt(_endIndex);
            }
        }

        public bool IsReadyToChange
        {
            get {
                MoveEndIndex();
                return _endIndex != _index;
            }
        }
        
        public void Activate(T value)
        {
            _elements[_index].Set(value, _duration);
            StepForward();
        }
        
        public void Deactivate()
        {
            MoveEndIndex();
            _elements[_endIndex].Deactivate();
        }

        public void DeactivateAll()
        {
            _endIndex = LastActive;
            _elements[_endIndex].Deactivate();
        }
    }
    
    
    public class FractionTemporaryValue<T> : IFractionTimer<T>
    {
        private float targetTime;
        private float startingTime;
        public T BaseValue{get; set;}
        public T ActiveValue{get; private set;}

        public FractionTemporaryValue(T baseValue, T activeValue)
        {
            BaseValue = baseValue;
            ActiveValue = activeValue;
            targetTime = 0f;
            startingTime = 0f;
        }
        public FractionTemporaryValue(T value)
        {
            BaseValue = value;
            ActiveValue = value;
            targetTime = 0f;
            startingTime = 0f;
        }

        public T Value => Time.time < targetTime ? ActiveValue : BaseValue;
        
        public float TimeFraction => Mathf.Abs(targetTime - startingTime) <= 0.0001f ? 1 : (Time.time-startingTime)/(targetTime - startingTime);
        
        public void Activate(T activeValue, float duration)
        {
            ActiveValue = activeValue;
            startingTime = Time.time;
            targetTime = Time.time + duration;
        }

        public void Activate(float duration)
        {
            startingTime = Time.time;
            targetTime = Time.time + duration;
        }

        public void Deactivate()
        {
            startingTime = 0f;
            targetTime = 0f;
        }
        
        public static implicit operator FractionTemporaryValue<T>(T value)
        {
            return new FractionTemporaryValue<T>(value);
        }
        
        public static implicit operator T(FractionTemporaryValue<T> tmpValue)
        {
            return tmpValue.Value;
        }
    }
    
    public class DelayDurationValueTimer<T>
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
