using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public static class UtilityClasses
{
    public interface IFractionTimer<T>
    {
        public T Value { get; }
        public float TimeFraction { get; }
    }

    public struct ParabolaCurve
    {
        public Vector3 StartPos;
        public Vector3 EndPos;
        
        public float MidPointX;
        public float MidPointY;
        public float Scale;

        public float Length;
        
        public void MakeObstacleParabola(float obstacleHeight, float jumpHeigh)
        {
            MidPointY = Mathf.Max(StartPos.y, EndPos.y, obstacleHeight) + jumpHeigh;
            var startToMidX = Mathf.Sqrt(MidPointY - StartPos.y);
            var midToEndX = Mathf.Sqrt(MidPointY - EndPos.y);
            var totalX = startToMidX + midToEndX;
            MidPointX = startToMidX / totalX; 
            Scale= totalX * totalX;
        }

        
        public void MakeStartAngleParabola(float startAngleUp)
        {
            var dir = EndPos - StartPos;
            var horizDist = new Vector2(dir.x, dir.z).magnitude;
            horizDist = Mathf.Max(horizDist, 0.0001f);
            var deltaY = dir.y;
            
            var baseAngle = Mathf.Atan2(deltaY, horizDist);
            var totalAngle = baseAngle + startAngleUp * Mathf.Deg2Rad;
            totalAngle = Mathf.Min(totalAngle, 89.9f * Mathf.Deg2Rad);
            
            var k0 = Mathf.Tan(totalAngle);
            Scale = k0 * horizDist - deltaY;
            
            MidPointY = StartPos.y + (k0 * k0 * horizDist * horizDist) / (4f * Scale);
            MidPointX = k0 * horizDist / (2f * Scale);
        }
        
        public float GetParabolaY(float fraction)
        {
            var x = fraction - MidPointX; 
            return MidPointY - Scale * x * x;
        }

        public Vector3 GetPosition(float fraction)
        {
            var p = Vector3.Lerp(StartPos, EndPos, fraction);
            p.y = GetParabolaY(fraction);
            return p;
        }
        
        public void CalculateTotalLength()
        {
            var dir = EndPos - StartPos;
            var horizDist = Mathf.Max(new Vector2(dir.x, dir.z).magnitude, 0.0001f);

            if (Scale <= 0.0001f) 
                Length = Vector3.Distance(StartPos, EndPos);

            var factor = (2f * Scale) / horizDist;
            var u0 = -MidPointX * factor;
            var u1 = (1f - MidPointX) * factor;

            var coeff = (horizDist * horizDist) / (2f * Scale);
            Length = coeff * (H(u1) - H(u0));
        }

        private static float H(float u)
        {
            var sqrt = Mathf.Sqrt(1f + u * u);
            return 0.5f * (u * sqrt + Mathf.Log(u + sqrt));
        }
        
        public Vector3 GetPositionByTraveledDistance(float distance)
        {
            var fraction = Mathf.Clamp01(distance / Length);
            var p = Vector3.Lerp(StartPos, EndPos, fraction);
            p.y = GetParabolaY(fraction);
            return p;
        }
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
        
        public bool TrySet(T value, float duration = 0)
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
        
        public bool TrySet(T value, float duration = 0)
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
    
    public class TemporaryValue<T> : IFractionTimer<T>
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

        public float TimeFraction => throw new InvalidOperationException();

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

    public class BaseActionAutomaticTransition
    {
        private readonly FractionBlockingValueTimer<UtilityFunctions.BaseActionTransitionsEnum> _timer;
        public float CrossFadeDuration{get; set; }
        public float WeightMultiplier { get; set; }

        public UtilityFunctions.BaseActionTransitionsEnum Value => _timer.Value;
        
        public BaseActionAutomaticTransition(UtilityFunctions.BaseActionTransitionsEnum startingValue, float crossFadeDuration, float weightMultiplier = 1f)
        {
            _timer = startingValue;
            CrossFadeDuration = crossFadeDuration;
            WeightMultiplier =  weightMultiplier;
        }

        public BaseActionAutomaticTransition(float crossFadeDuration, float weightMultiplier = 1f)
        {
            _timer = UtilityFunctions.BaseActionTransitionsEnum.Base;
            CrossFadeDuration = crossFadeDuration;
            WeightMultiplier =  weightMultiplier;
        }
        
        public float GetFraction(bool shouldBeActive){
            switch (_timer.Value)
            {
                case UtilityFunctions.BaseActionTransitionsEnum.Base:
                    if(shouldBeActive)
                        _timer.SetForce(UtilityFunctions.BaseActionTransitionsEnum.BaseToAction, CrossFadeDuration);
                    break;
                case UtilityFunctions.BaseActionTransitionsEnum.BaseToAction:
                    if(!shouldBeActive)
                        _timer.SetForce(UtilityFunctions.BaseActionTransitionsEnum.ActionToBase, CrossFadeDuration, 1-_timer.TimeFraction);
                    if(_timer.CanBeChanged)
                        _timer.SetForce(UtilityFunctions.BaseActionTransitionsEnum.Action);
                    break;
                case UtilityFunctions.BaseActionTransitionsEnum.Action:
                    if(!shouldBeActive)
                        _timer.SetForce(UtilityFunctions.BaseActionTransitionsEnum.ActionToBase, CrossFadeDuration);
                    break;
                case UtilityFunctions.BaseActionTransitionsEnum.ActionToBase:
                    if(shouldBeActive)
                        _timer.SetForce(UtilityFunctions.BaseActionTransitionsEnum.BaseToAction, CrossFadeDuration, 1-_timer.TimeFraction);
                    if(_timer.CanBeChanged)
                        _timer.SetForce(UtilityFunctions.BaseActionTransitionsEnum.Base);
                    break;
            }
            return UtilityFunctions.GetTransitionFraction(_timer, WeightMultiplier);
        }

        public float GetRemainCurrentStateFraction()
        {
            switch (_timer.Value)
            {
                case UtilityFunctions.BaseActionTransitionsEnum.BaseToAction:
                    if(_timer.CanBeChanged)
                        _timer.SetForce(UtilityFunctions.BaseActionTransitionsEnum.Action);
                    break;
                case UtilityFunctions.BaseActionTransitionsEnum.ActionToBase:
                    if(_timer.CanBeChanged)
                        _timer.SetForce(UtilityFunctions.BaseActionTransitionsEnum.Base);
                    break;
            }
            return UtilityFunctions.GetTransitionFraction(_timer, WeightMultiplier);
        }
        
        public static implicit operator BaseActionAutomaticTransition(float crossFadeDuration)
        {
            return new BaseActionAutomaticTransition(crossFadeDuration);
        }
        
        public static implicit operator UtilityFunctions.BaseActionTransitionsEnum(BaseActionAutomaticTransition transition)
        {
            return transition.Value;
        }
    }

    public class ChangeableFractionValue : IFractionTimer<bool>
    {
        private IFractionTimer<bool> _holder;
        private int _setCounter = 0;
        
        public bool Value => _setCounter > 0 && _holder.Value;
        public float TimeFraction => _setCounter > 0 ? _holder.TimeFraction : 1f;

        public int Count => _setCounter;
        
        public void Set(IFractionTimer<bool> holder)
        {
            _holder = holder;
            ++_setCounter;
        }

        public void Unset()
        {
            --_setCounter;
        }
        
        public static implicit operator bool(ChangeableFractionValue holder)
        {
            return holder.Value;
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
