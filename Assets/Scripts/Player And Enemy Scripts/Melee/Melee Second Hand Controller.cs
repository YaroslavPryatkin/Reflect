using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using CustomAttributes;
using BaseActionTransitionsEnum = UtilityFunctions.BaseActionTransitionsEnum;

public class MeleeSecondHandController : MonoBehaviour
{
    [SerializeField] private int secondHandRigIndex;
    [SerializeField] private Transform secondHandIKTarget;
    [SerializeField] private float crossFadeDuration = 0.1f;
    
    private readonly UtilityClasses.ChangeableFractionValueReference _useSecondHand = new();
    
    private IkRigTargetController _secondHandController;
    private IkRigTargetController.TransformSource _secondHandTransformSource;

    private UtilityClasses.BaseActionAutomaticTransition _transitionState;

    public void SetSecondHand(UtilityTimers.FractionTemporaryValue<bool> secondHandTimer)
    {
        _useSecondHand.Set(secondHandTimer);
    }

    public bool SetSecondHandIfWasSecondHand(UtilityTimers.FractionTemporaryValue<bool> secondHandTimer)
    {
        if (_transitionState.Value == BaseActionTransitionsEnum.Action ||
            _transitionState.Value == BaseActionTransitionsEnum.ActionToBase)
        {
            _useSecondHand.Set(secondHandTimer);
            return true;
        }
        return false;
    }

    public void ClearSecondHandReferences()
    {
        _useSecondHand.Unset();
    }

    private void Awake()
    {
        if (!TryGetComponent(out AnimationAndRigManager controller))
        {
            Debug.LogError("No AnimationController found!", this);
            enabled = false;
            return;
        }
        _secondHandController = controller.GetRig(secondHandRigIndex);
        _secondHandTransformSource = new IkRigTargetController.TransformSource(secondHandIKTarget);
        _secondHandController.SetSource(_secondHandTransformSource);
        _transitionState = crossFadeDuration;
    }


    private void Update()
    {
        // if (_hasSecondHandTimer && _secondHandTimer)
        // {
        //     if (_type == TypeEnum.Active)
        //     {
        //         _secondHandTransformSource.Weight = 
        //             _transitionState.GetFraction(true);
        //     }
        //     else
        //     {
        //
        //         _secondHandTransformSource.Weight =
        //             _transitionState.GetRemainCurrentStateFraction();
        //     }
        // }
        // else
        // {
        //     
        //     _secondHandTransformSource.Weight = 
        //         _transitionState.GetFraction(false);
        // }
        
        _secondHandTransformSource.Weight = 
                     _transitionState.GetFraction(_useSecondHand);
    }
}
