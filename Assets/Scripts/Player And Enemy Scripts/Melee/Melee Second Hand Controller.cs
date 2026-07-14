using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using MeleeComponents;
using MeleeComponents.Presets;
using CustomAttributes;

public class MeleeSecondHandController : MonoBehaviour
{
    [SerializeField] private int secondHandRigIndex;

    [SerializeField] private int transformSourceRigIndex = 1;
    
    [SerializeField] private float autoFadeOutTime = 0.3f;

    
    private Utility.BaseActionTransitionsEnum _secondHandState =
        Utility.BaseActionTransitionsEnum.Base;
    private Utility.FractionTemporaryValue<bool> _secondHandTimer;
    private bool _hasSecondHandTimer = false;
    private IkRigTargetController _secondHandController;

    private Utility.FractionTemporaryValue<bool> _autoFadeOut = new(false, true);

    public void SetSecondHand(Utility.BaseActionTransitionsEnum secondHandState,Utility.FractionTemporaryValue<bool> secondHandTimer )
    {
        _secondHandState = secondHandState;
        _secondHandTimer = secondHandTimer;
        _hasSecondHandTimer = true;
    }

    public void ClearSecondHandReferences()
    {
        _hasSecondHandTimer = false;
    }

    private void Awake()
    {
        if (!TryGetComponent(out AnimationController controller))
        {
            Debug.LogError("No AnimationController found!", this);
            enabled = false;
            return;
        }
        _secondHandController = controller.GetRig(secondHandRigIndex);
    }

    private void Update()
    {
        if (_secondHandState == Utility.BaseActionTransitionsEnum.Base)
        {
            _secondHandController.SetRigWeight(0f);
        }
        else if (!_hasSecondHandTimer || !_secondHandTimer.Value)
        {
            if (_autoFadeOut.Value)
            {
                _secondHandController.SetRigWeight(
                    _autoFadeOut.TimeFraction, Utility.BaseActionTransitionsEnum.ActionToBase);
            }
            else if (_secondHandState == Utility.BaseActionTransitionsEnum.ActionToBase)
            {
                _secondHandController.SetRigWeight(0f);
                
            }
            else
            {
                _autoFadeOut.Activate(autoFadeOutTime);
                _secondHandState = Utility.BaseActionTransitionsEnum.ActionToBase;
            }
        }
        else
        {
            _secondHandController.SetTransformTarget(transformSourceRigIndex, 0);
            if (_autoFadeOut.Value)
            {
                _secondHandController.SetRigWeight(Mathf.Max(
                    Utility.GetTransitionFraction(_secondHandTimer.TimeFraction, _secondHandState),
                    1f - _autoFadeOut.TimeFraction));

            }
            else
            {
                _secondHandController.SetRigWeight(_secondHandTimer.TimeFraction, _secondHandState);
            }
        }
    }
}
