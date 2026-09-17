using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Pool;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

/// <summary>
/// Since these all are mutable structs, they are unsafe to work with.
/// </summary>
public static class UtilityStructures
{
    public struct ChangeableFractionValueReference : UtilityTimers.IFractionTimer<bool>
    {
        private UtilityTimers.IFractionTimer<bool> _holder;
        private bool _alwaysOn;
        private int _setCounter;
        
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
    
    public struct BoolCounter
    {
        private int _counter;
        
        public bool Value => _counter > 0;
        public int Count => _counter;
        
        public void Set()
        {
            ++_counter;
        }

        public void Unset()
        {
            --_counter;
            if (_counter < 0)
            {
                _counter = 0;
                Debug.LogError("[BoolCounter] Counter went below zero. Check your set-unset operations.");
            }
        }
        
        public static implicit operator bool(BoolCounter holder)
        {
            return holder.Value;
        }
        
        public static implicit operator BoolCounter(int count)
        {
            return new BoolCounter { _counter = count };
        }
    }

    public struct ToggleableState
    {
        public bool IsActive { get; private set; }
        private readonly Action _onActivate;
        private readonly Action _onDeactivate;

        public ToggleableState(Action onActivate, Action onDeactivate, bool isActive = false)
        {
            _onActivate = onActivate;
            _onDeactivate = onDeactivate;
            IsActive = isActive;
        }

        public void Activate()
        {
            if (!IsActive)
            {
                IsActive = true;
                _onActivate?.Invoke();
            }
        }

        public void Deactivate()
        {
            if (IsActive)
            {
                IsActive = false;
                _onDeactivate?.Invoke();
            }
        }
    }
}
