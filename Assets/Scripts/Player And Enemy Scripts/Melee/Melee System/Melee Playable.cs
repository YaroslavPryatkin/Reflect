using System;
using UnityEngine;
using System.Collections.Generic;

namespace MeleeSystem
{
    public abstract class MeleePlayableSource : ScriptableObject
    {
        public abstract List<MeleePlayablePart> Parts { get; }
    }

    public class MeleePlayable : UtilityFunctions.IPlayable
    {
        private readonly MeleePlayableSource _source;
        private readonly MeleeController _meleeController;

        private readonly List<UtilityClasses.FractionTemporaryValue<bool>> _additionalActionsActivityTimers;
        private readonly UtilityClasses.FractionBlockingValueTimer<int> _currentPart;
        private readonly List<List<MeleeAdditionalActionOverhead>> _overheads = new();

        public int Length { get; private set; }
        public float LengthFraction => _currentPart.Value + _currentPart.TimeFraction;
        public bool IsActive => _currentPart < Length;

        public InterruptionEnum CanBeInterruptedInto =>
            _currentPart == Length ? InterruptionEnum.ToAnything : _source.Parts[_currentPart].CanBeInterruptedInto;

        public bool ClipIsNull => _nextPartWithClip == Length;
        public AnimationClip Clip => _source.Parts[_nextPartWithClip].Clip;
        public float ClipSpeed => _source.Parts[_currentPart].ClipSpeed;

        public float CurrentClipTimeFraction => _source.Parts[_currentPart].ShouldAnimationTransition
            ? _currentPart.TimeFraction
            : 1f;

        public bool ShouldAnimationTransition => _source.Parts[_currentPart].ShouldAnimationTransition;
        public bool ShouldResetTimeOfPlayableAnyway { get; private set; } = false;

        private int _loopMark = 0;
        private bool _shouldReturnToLoopMark = false;
        private bool _startingOtherComboOnFinish = false;

        private readonly bool _hasClip = false;
        private readonly bool _hasLoop = false;
        private int _nextPartWithClip;
        private bool _stillHasClip = false;

        public MeleePlayable(MeleeController meleeController, MeleePlayableSource source)
        {
            _source = source;
            _meleeController = meleeController;

            Length = _source.Parts.Count;
            int maxAddition = 0;
            for (var i = 0; i < _source.Parts.Count; ++i)
            {
                _overheads.Add(new List<MeleeAdditionalActionOverhead>());
                _source.Parts[i].Initialize(_overheads[^1]);

                int otherMax = _source.Parts[i].AdditionalActionsCount;
                if (otherMax > maxAddition)
                    maxAddition = otherMax;

                _hasClip = _hasClip || _source.Parts[i].HasClip;

                if (_source.Parts[i].HaveReturnToLoop)
                {
                    Length = i + 1;
                    _hasLoop = true;
                    break;
                }
            }

            _currentPart = Length;
            _nextPartWithClip = Length;
            _stillHasClip = _hasClip;

            _additionalActionsActivityTimers = new List<UtilityClasses.FractionTemporaryValue<bool>>(maxAddition);

            for (var i = 0; i < maxAddition; ++i)
            {
                _additionalActionsActivityTimers.Add(new(false, true));
            }
        }

        private void MoveNextPartWithClip()
        {
            if (!_stillHasClip) return;

            _nextPartWithClip = _currentPart;

            if (_hasLoop)
            {
                int safetyCounter = 0;
                while (!_source.Parts[_nextPartWithClip].HasClip)
                {
                    if (_nextPartWithClip < Length - 1)
                        _nextPartWithClip++;
                    else
                    {
                        if (++safetyCounter > 1)
                        {
                            _stillHasClip = false;
                            _nextPartWithClip = Length;
                            return;
                        }

                        _nextPartWithClip = _loopMark;
                    }
                }
            }
            else
            {
                while (_nextPartWithClip < Length && !_source.Parts[_nextPartWithClip].HasClip)
                    _nextPartWithClip++;
            }
        }

        private void UpdateCurrentPart(int newValue, float newFraction = 0f)
        {
            if (newValue < Length)
            {
                var duration = _source.Parts[newValue].Duration;
                _currentPart.SetForce(newValue, duration, newFraction);
                MoveNextPartWithClip();
                var index = _currentPart.Value;
                _source.Parts[index].StartAdditionalActions(_meleeController, this, _overheads[index],
                    _additionalActionsActivityTimers);
            }
            else
            {
                _currentPart.SetForce(Length);
                _nextPartWithClip = Length;
            }
        }

        public void SetLoopMark()
        {
            _loopMark = _currentPart;
        }

        public void ReturnToLoopMark()
        {
            _shouldReturnToLoopMark = true;
        }

        public void StartingOtherComboOnFinish()
        {
            _startingOtherComboOnFinish = true;
        }

        public void AddClipsToSet(HashSet<AnimationClip> uniqueClips)
        {
            foreach (var part in _source.Parts)
            {
                part.AddClipsToSet(uniqueClips);
            }
        }

        public void Start(float fraction)
        {
            _stillHasClip = _hasClip;
            ShouldResetTimeOfPlayableAnyway = true;
            var startPos = (int)Mathf.Floor(fraction);
            var startFraction = fraction - startPos;
            startPos = Mathf.Clamp(startPos, 0, Length);


            UpdateCurrentPart(startPos, startFraction);
        }

        public void Update()
        {
            ShouldResetTimeOfPlayableAnyway = false;
            if (IsActive && _currentPart.CanBeChanged)
            {
                var index = _currentPart.Value;
                _source.Parts[index].FinishAdditionalActions(_meleeController, this, _overheads[index],
                    _additionalActionsActivityTimers);

                int newValue;
                if (_startingOtherComboOnFinish)
                {
                    _startingOtherComboOnFinish = false;
                    newValue = Length;
                }
                else if (_shouldReturnToLoopMark)
                {
                    newValue = _loopMark;
                    _shouldReturnToLoopMark = false;
                    ShouldResetTimeOfPlayableAnyway = true;
                }
                else
                {
                    newValue = _currentPart + 1;
                }

                UpdateCurrentPart(newValue);
            }
        }

        public void Interrupt()
        {
            ShouldResetTimeOfPlayableAnyway = false;
            if (IsActive && !_startingOtherComboOnFinish)
            {
                var index = _currentPart.Value;
                _source.Parts[index].InterruptAdditionalActions(_meleeController, this, _overheads[index],
                    _additionalActionsActivityTimers);
            }
        }
    }
}