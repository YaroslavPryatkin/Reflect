using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using Debug = UnityEngine.Debug;
using BaseActionTransitionsEnum = AnimationUtility.BaseActionTransitionsEnum;
using TransitionStateEnum =  AnimationUtility.TransitionStateEnum;

public class PlayerSwordAnimationController : MonoBehaviour
{

    [Header("Sheathe")]
    [SerializeField] private AnimationCurve unsheatheClip;
    [SerializeField] private AnimationCurve sheatheClip;
    
    [Header("Sheathe to overhead")]
    [SerializeField] private AnimationCurve unsheatheToOverheadClip;
    [SerializeField] private AnimationCurve overheadToSheatheClip;

    [Header("Combo")]
    public AnimationClip[] comboClips;
        
    
}
