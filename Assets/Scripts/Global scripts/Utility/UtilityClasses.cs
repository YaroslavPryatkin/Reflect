using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Pool;

public static class UtilityClasses
{
    public class PressAntiBuffer
    {
        private readonly UtilityTimers.TemporaryValue<bool> _antiTimer = new(true,false);
        private bool _isPressed = false;
        private readonly float _duration;

        public bool IsPressed => _isPressed && _antiTimer.Value;

        public PressAntiBuffer(float duration)
        {
            _duration = duration;
        }
        
        public void Press()
        {
            _isPressed = true;
            _antiTimer.Activate(_duration);
        }

        public void Release()
        {
            _isPressed = false;
        }
    }
    
    public class ParabolaCurve
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
        
        public Vector3 GetPositionByDistance(float distance)
        {
            var fraction = Mathf.Clamp01(distance / Length);
            var p = Vector3.Lerp(StartPos, EndPos, fraction);
            p.y = GetParabolaY(fraction);
            return p;
        }
    }

    public class FractionTimerToBoolConverterSet<T> : UtilityTimers.IFractionTimer<bool>
    {
        private readonly UtilityTimers.IFractionTimer<T> _source;
        private readonly HashSet<T> _trueValues;
        
        public bool Value => _trueValues.Contains(_source.Value);
        public float TimeFraction => _source.TimeFraction;

        public FractionTimerToBoolConverterSet(UtilityTimers.IFractionTimer<T> source, HashSet<T> trueValues)
        {
            _source = source;
            _trueValues = trueValues;
        }
    }
    
    public class FractionTimerToBoolConverter<T> : UtilityTimers.IFractionTimer<bool>
    {
        private readonly UtilityTimers.IFractionTimer<T> _source;
        private readonly T _trueValue;
        
        public bool Value => _trueValue.Equals(_source.Value);
        public float TimeFraction => _source.TimeFraction;
        
        public FractionTimerToBoolConverter(UtilityTimers.IFractionTimer<T> source, T trueValue)
        {
            _source = source;
            _trueValue = trueValue;
        }
    }
    
    public class BaseActionAutomaticTransition
    {
        private readonly UtilityTimers.FractionBlockingValueTimer<UtilityFunctions.BaseActionTransitionsEnum> _timer;
        public float FadeInTime { get; set; }
        public float FadeOutTime { get; set; }
        public float WeightMultiplier { get; set; }

        public UtilityFunctions.BaseActionTransitionsEnum Value => _timer.Value;
        
        public BaseActionAutomaticTransition(UtilityFunctions.BaseActionTransitionsEnum startingValue, float crossFadeDuration, float weightMultiplier = 1f)
        {
            _timer = startingValue;
            FadeInTime = crossFadeDuration;
            FadeOutTime = crossFadeDuration;
            WeightMultiplier =  weightMultiplier;
        }

        public BaseActionAutomaticTransition(float crossFadeDuration = 1f, float weightMultiplier = 1f)
        {
            _timer = UtilityFunctions.BaseActionTransitionsEnum.Base;
            FadeInTime = crossFadeDuration;
            FadeOutTime = crossFadeDuration;
            WeightMultiplier =  weightMultiplier;
        }

        public void SetFadeTimes(float fadeInTime, float fadeOutTime)
        {
            FadeInTime = fadeInTime;
            FadeOutTime = fadeOutTime;
        }
        
        public float GetFraction(bool shouldBeActive){
            switch (_timer.Value)
            {
                case UtilityFunctions.BaseActionTransitionsEnum.Base:
                    if(shouldBeActive)
                        _timer.SetForce(UtilityFunctions.BaseActionTransitionsEnum.BaseToAction, FadeInTime);
                    break;
                case UtilityFunctions.BaseActionTransitionsEnum.BaseToAction:
                    if(!shouldBeActive)
                        _timer.SetForce(UtilityFunctions.BaseActionTransitionsEnum.ActionToBase, FadeOutTime, 1-_timer.TimeFraction);
                    if(_timer.CanBeChanged)
                        _timer.SetForce(UtilityFunctions.BaseActionTransitionsEnum.Action);
                    break;
                case UtilityFunctions.BaseActionTransitionsEnum.Action:
                    if(!shouldBeActive)
                        _timer.SetForce(UtilityFunctions.BaseActionTransitionsEnum.ActionToBase, FadeOutTime);
                    break;
                case UtilityFunctions.BaseActionTransitionsEnum.ActionToBase:
                    if(shouldBeActive)
                        _timer.SetForce(UtilityFunctions.BaseActionTransitionsEnum.BaseToAction, FadeInTime, 1-_timer.TimeFraction);
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
    
    public class BaseActionAutomaticTransitionUnscaled
    {
        private readonly UtilityTimers.FractionBlockingValueTimerUnscaled<UtilityFunctions.BaseActionTransitionsEnum> _timer;
        public float FadeInTime { get; set; }
        public float FadeOutTime { get; set; }
        public float WeightMultiplier { get; set; }

        public UtilityFunctions.BaseActionTransitionsEnum Value => _timer.Value;
        
        public BaseActionAutomaticTransitionUnscaled(UtilityFunctions.BaseActionTransitionsEnum startingValue, float crossFadeDuration, float weightMultiplier = 1f)
        {
            _timer = startingValue;
            FadeInTime = crossFadeDuration;
            FadeOutTime = crossFadeDuration;
            WeightMultiplier =  weightMultiplier;
        }

        public BaseActionAutomaticTransitionUnscaled(float crossFadeDuration = 1f, float weightMultiplier = 1f)
        {
            _timer = UtilityFunctions.BaseActionTransitionsEnum.Base;
            FadeInTime = crossFadeDuration;
            FadeOutTime = crossFadeDuration;
            WeightMultiplier =  weightMultiplier;
        }

        public void SetFadeTimes(float fadeInTime, float fadeOutTime)
        {
            FadeInTime = fadeInTime;
            FadeOutTime = fadeOutTime;
        }
        
        public float GetFraction(bool shouldBeActive){
            switch (_timer.Value)
            {
                case UtilityFunctions.BaseActionTransitionsEnum.Base:
                    if(shouldBeActive)
                        _timer.SetForce(UtilityFunctions.BaseActionTransitionsEnum.BaseToAction, FadeInTime);
                    break;
                case UtilityFunctions.BaseActionTransitionsEnum.BaseToAction:
                    if(!shouldBeActive)
                        _timer.SetForce(UtilityFunctions.BaseActionTransitionsEnum.ActionToBase, FadeOutTime, 1-_timer.TimeFraction);
                    if(_timer.CanBeChanged)
                        _timer.SetForce(UtilityFunctions.BaseActionTransitionsEnum.Action);
                    break;
                case UtilityFunctions.BaseActionTransitionsEnum.Action:
                    if(!shouldBeActive)
                        _timer.SetForce(UtilityFunctions.BaseActionTransitionsEnum.ActionToBase, FadeOutTime);
                    break;
                case UtilityFunctions.BaseActionTransitionsEnum.ActionToBase:
                    if(shouldBeActive)
                        _timer.SetForce(UtilityFunctions.BaseActionTransitionsEnum.BaseToAction, FadeInTime, 1-_timer.TimeFraction);
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
        
        public static implicit operator BaseActionAutomaticTransitionUnscaled(float crossFadeDuration)
        {
            return new BaseActionAutomaticTransitionUnscaled(crossFadeDuration);
        }
        
        public static implicit operator UtilityFunctions.BaseActionTransitionsEnum(BaseActionAutomaticTransitionUnscaled transition)
        {
            return transition.Value;
        }
    }

    public class ChangeableFractionValueReference : UtilityTimers.IFractionTimer<bool>
    {
        private UtilityTimers.IFractionTimer<bool> _holder;
        private bool _alwaysOn = false;
        private int _setCounter = 0;
        
        public bool Value => _setCounter > 0 && (_alwaysOn || _holder.Value);
        public float TimeFraction => (_setCounter <= 0 || _alwaysOn) ?  1f : _holder.TimeFraction;
        

        public int Count => _setCounter;
        
        public void Set(UtilityTimers.IFractionTimer<bool> holder)
        {
            _alwaysOn = false;
            _holder = holder;
            ++_setCounter;
        }

        public void Set()
        {
            _alwaysOn = true;
            ++_setCounter;
        }

        public void Unset()
        {
            --_setCounter;
            if (_setCounter < 0)
            {
                _setCounter = 0;
                Debug.LogError("[ChangeableFractionValueReference] Set counter went below zero. Check your set-unset operations.");
            }
        }
        
        public static implicit operator bool(ChangeableFractionValueReference holder)
        {
            return holder.Value;
        }
    }
}
