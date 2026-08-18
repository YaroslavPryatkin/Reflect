using System;
using UnityEngine;

public static class UtilityTimers
{
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

    public class FractionBlockingValueTimerUnscaled<T> : IFractionTimer<T>
    {
        private float targetTime;
        private float startTime;
        public T Value { get; private set; }

        public FractionBlockingValueTimerUnscaled(T initialVal)
        {
            Value = initialVal;
            targetTime = 0f;
            startTime = 0f;
        }
        
        public bool TrySet(T value, float duration = 0)
        {
            if (Time.unscaledTime < targetTime) return false;
            
            SetForce(value, duration);
            return true;
        }

        public void SetForce(T value, float duration = 0, float fraction = 0)
        {
            Value = value;
            startTime = Time.unscaledTime - duration * fraction;
            targetTime = Time.unscaledTime + duration * (1-fraction);
        }
        
        public bool CanBeChanged => Time.unscaledTime >= targetTime;
        public float TimeFraction => Mathf.Abs(targetTime - startTime) <= 0.0001f ? 1 : (Time.unscaledTime-startTime)/(targetTime - startTime);
        
        public static implicit operator FractionBlockingValueTimerUnscaled<T>(T value)
        {
            return new FractionBlockingValueTimerUnscaled<T>(value);
        }
        
        public static implicit operator T(FractionBlockingValueTimerUnscaled<T> timer)
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

    public class FractionDelayedValueTimer<T> : IFractionTimer<T>
    {
        private float targetTime;
        private float startTime;
        private T lastValue;
        private T value;

        public FractionDelayedValueTimer(T value)
        {
            this.value = value;
            lastValue = value;
            startTime = 0f;
            targetTime = 0f;
        }

        public T Value => Time.time < targetTime ? lastValue : value;

        public float TimeFraction => Mathf.Abs(targetTime - startTime) <= 0.0001f ? 1 : (Time.time-startTime)/(targetTime - startTime);

        public bool CanBeChanged => Time.time >= targetTime;
        
        public T RealValue => value;
        
        public void Set(T value, float delay = 0, float fraction=0)
        {
            lastValue = Value;
            startTime = Time.time - delay * fraction;
            targetTime = Time.time + delay * (1-fraction);
            this.value = value;
        }

        public void ResetTime(float delay = 0, float fraction = 0)
        {
            startTime = Time.time - delay * fraction;
            targetTime = Time.time + delay * (1-fraction);
        }
        
        public static implicit operator FractionDelayedValueTimer<T>(T value)
        {
            return new FractionDelayedValueTimer<T>(value);
        }
        
        public static implicit operator T(FractionDelayedValueTimer<T> timer)
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

    public class FractionTimer
    {
        private float lastTime;
        private float unitTime;
        private float startTime;
        
        
        public float TimeFraction => GetTimeFraction(Time.time);
        public int IntegersSinceLastTime => (int)GetTimeFraction(Time.time) - (int)GetTimeFraction(lastTime);
        public void UpdateLastTime() => lastTime = Time.time;
        public float TimeFractionSinceLastTime => (Time.time - lastTime) / unitTime;
        
        public void Set(float unitDuration, float timeShift=0f, float fraction = 0f)
        {
            lastTime = Time.time;
            startTime = Time.time - unitDuration * fraction + timeShift;
            unitTime = unitDuration;
            if (Mathf.Abs(unitTime) <= 0.0001f)
            {
                Debug.LogError("UnitTime is zero");
                unitTime = 1f;
            }
        }

        private float GetTimeFraction(float time)
        {
            return (time - startTime) / unitTime;
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

    public class FractionDelayDurationValueTimer<T>
    {
        private float startTime;
        private float delayTime;
        private float durationTime;
        private T lastValue;
        private T value;

        public FractionDelayDurationValueTimer(T value)
        {
            this.value = value;
            lastValue = value;
            delayTime = 0f;
            durationTime = 0f;
        }

        public T Value => Time.time < delayTime ? lastValue : value;

        public T RealValue => value;

        public float DelayTimeFraction => 
            Mathf.Abs(delayTime - startTime) <= 0.0001f
                ? 1
                : (Time.time - startTime) / (delayTime - startTime);
        
        public float DurationTimeFraction => 
            Mathf.Abs(durationTime - startTime) <= 0.0001f
                ? 1
                : (Time.time - startTime) / (durationTime - startTime);

        public bool TrySet(T value, float delay = 0, float duration = 0)
        {
            if (Time.time < durationTime) return false;
            
            SetForce(value, delay, duration);
            return true;
        }
        
        public void SetForce(T value, float delay = 0, float duration = 0)
        {
            lastValue = Value;
            startTime = Time.time;
            delayTime = Time.time + delay;
            durationTime = Time.time + duration;
            this.value = value;
        }

        public bool CanBeChanged => Time.time >= durationTime;
        
        public static implicit operator FractionDelayDurationValueTimer<T>(T value)
        {
            return new FractionDelayDurationValueTimer<T>(value);
        }
        
        public static implicit operator T(FractionDelayDurationValueTimer<T> timer)
        {
            return timer.Value;
        }
    }
}