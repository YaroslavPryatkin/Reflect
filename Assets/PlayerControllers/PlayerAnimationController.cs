using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using Debug = UnityEngine.Debug;
using BaseActionTransitionsEnum = AnimationUtility.BaseActionTransitionsEnum;
using TransitionStateEnum =  AnimationUtility.TransitionStateEnum;

[DefaultExecutionOrder(100)]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private GameObject body;
    [SerializeField] private Transform bodyHolder;

    [Header("Movement Clips")]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip walkClip;
    [SerializeField] private AnimationClip runClip;

    [Header("Directional Run Clips")] 
    [SerializeField] private List<MultidirectionalMovement> multidirectionalMovement;
    
    [Header("Action Clips")]
    [SerializeField] private AnimationClip dashClip;
    [SerializeField] private AnimationClip fallingIdleClip;
    
    [Header("Landing Clips")]
    [SerializeField] private AnimationClip standLandingClip;
    [SerializeField] private AnimationClip runLandingClip;
    [SerializeField] private AnimationClip rollLandingClip;
    
    [Header("Jump Clips")]
    [SerializeField] private AnimationClip startingGroundStandJumpClip;
    [SerializeField] private AnimationClip continuingGroundStandJumpClip;
    [SerializeField] private AnimationClip startingGroundRunJumpClip;
    [SerializeField] private AnimationClip continuingGroundRunJumpClip;
    [SerializeField] private AnimationClip startingWallJumpClip;
    [SerializeField] private AnimationClip continuingWallJumpClip;

    [Header("Forward Jump Clips")]
    [SerializeField] private AnimationClip forwardJumpingInAirClip;
    [SerializeField] private AnimationClip forwardJumpingLandingClip;

    [Header("Wall Run Clips")]
    [SerializeField] private AnimationClip leftWallRunClip;
    [SerializeField] private AnimationClip rightWallRunClip;

    [Header("Slide Clips")]
    [SerializeField] private AnimationClip startingSlideClip;
    [SerializeField] private AnimationClip slidingClip;
    [SerializeField] private AnimationClip endingSlideClip;

    
    [Header("Gun Game Objects")]
    [SerializeField] private GameObject pocketGun;
    [SerializeField] private GameObject handGun;

    [Header("Gun Animation Clips")] 
    [SerializeField] private AnimationClip leftHandGunFingersClip;
    
    [Header("Gun Layer Settings")] 
    [SerializeField] private AvatarMask gunBodyMask;
    
    [Header("Gun Rigging Settings")]
    [SerializeField] private Rig leftHandRig;
    [SerializeField] private Transform gunBarrelPoint;
    [SerializeField] private Transform leftArmIKTarget;
    [SerializeField] private Transform leftHandBone;
    [SerializeField] private Rig headRig;
    [SerializeField] private Transform headIKTarget;
    [SerializeField] private Transform headBone;
    
    [Header("Settings & Thresholds for basic movement")]
    [SerializeField] private float walkThreshold = 0.2f;
    [SerializeField] private float normalSpeedForWalkAnimationSpeed = 3f;
    [SerializeField] private float runThreshold = 8f;
    [SerializeField] private float normalSpeedForRunAnimationSpeed = 8f;
    [SerializeField] private float normalSpeedForWallRunAnimationSpeed = 12f;
    [SerializeField] private float normalSpeedForForwardRunAnimationSpeed = 8f;

    
    [Header("Other Settings")] 
    [SerializeField] private float runLandingExitOffset = 0.2f;
    [SerializeField] private float rollLandingExitOffset = 0.05f;
    [SerializeField] private float crossfadeDuration = 0.15f;

    // --- References ---
    private Animator animator;
    private Transform bodyTransform;
    private PlayerSensors _playerSensors;
    private PlayerMovementController _playerMovementController;
    private PlayerForwardJumpingController _playerForwardJumpingController;
    private PlayerDashController _playerDashController;
    private PlayerJumpController _playerJumpController;
    private PlayerLandingController _playerLandingController;
    private PlayerMeleeController _playerMeleeController;
    private PlayerGunController _playerGunController;
    private MeleeController _meleeController;

    // --- Caches ---
    private Quaternion _defaultBodyLocalRot;
    
    private Vector3 _gunLocalPosOffset;
    private Quaternion _gunLocalRotOffset;
    
    private float _cachedStartingSlideSpeed;
    private float _cachedEndingSlideSpeed;
    
    private float _cachedForwardJumpLandingSpeed;
    private float _cachedRotationChangeSpeedForSlideStarting;
    private float _cachedRotationChangeSpeedForSlideEnding;
    
    private float _cachedStartingGroundStandJumpSpeed;
    private float _cachedContinuingGroundStandJumpSpeed;    
    private float _cachedStartingGroundRunJumpSpeed;
    private float _cachedContinuingGroundRunJumpSpeed;
    private float _cachedStartingWallJumpSpeed;
    private float _cachedContinuingWallJumpSpeed;
    
    
    private float _cachedStandLandingSpeed;
    private float _cachedRunLandingSpeed;
    private float _cachedRollLandingSpeed;
    
    private PlayableGraph _graph;
    private AnimationLayerMixerPlayable _layerMixer;
    private AnimationMixerPlayable _baseMixer;
    private AnimationMixerPlayable _gunMixer;
    private PlayerTargetLockController _playerTargetLockController;

    [Serializable]
    private struct MultidirectionalMovement
    {
        public AnimationClip animationClip;
        public float angle;
    }

    private int baseMixerSize;
    private float[] baseMixerWeights;
    
    private AnimationClip baseTargetClip;
    private float baseTargetSpeed;
    private float baseTargetCrossfadeDuration;
    
    private bool shouldUseMultiDirectionalInput;
    private Utility.FractionBlockingValueTimer<BaseActionTransitionsEnum> multiDirectionalTransitionBase =
        BaseActionTransitionsEnum.Base;
    
    

    private float targetMultidirectionalSpeed;
    private float[] multiDirectionalSpeedMultipliers;
    private float[] multidirectionalAngles;
    


    private int _currentBasePort = 0;
    private AnimationClip _currentBaseClip = null;
    
    
    private Utility.FractionBlockingValueTimer<TransitionStateEnum> isAnimationTransitionBase =
        TransitionStateEnum.Current;
    
    
    private BaseActionTransitionsEnum gunTransitionState = BaseActionTransitionsEnum.Base;
    private float gunTransitionFraction = 0f;
    
    
    private void Awake()
    {
        animator = body.GetComponent<Animator>();
        bodyTransform = body.GetComponent<Transform>();
        _playerSensors = GetComponent<PlayerSensors>();
        _playerMovementController = GetComponent<PlayerMovementController>();
        _playerForwardJumpingController = GetComponent<PlayerForwardJumpingController>();
        _playerDashController = GetComponent<PlayerDashController>();
        _playerJumpController = GetComponent<PlayerJumpController>();
        _playerLandingController = GetComponent<PlayerLandingController>();
        _playerMeleeController = GetComponent<PlayerMeleeController>();
        _playerGunController = GetComponent<PlayerGunController>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>(); 
        _meleeController = GetComponent<MeleeController>();
        
        _defaultBodyLocalRot = bodyTransform.localRotation;

        baseMixerSize = multidirectionalMovement.Count + 2;
        baseMixerWeights = new float[baseMixerSize];
        multiDirectionalSpeedMultipliers = new float[baseMixerSize-2];
        multidirectionalAngles = new float[baseMixerSize-2];
        
        InitPlayableGraph();
    }

    private void InitPlayableGraph()
    {
        _graph = animator.playableGraph;
        if (!_graph.IsValid())
        {
            _graph = PlayableGraph.Create("DirectAnimationGraph");
        }
        
        _layerMixer = AnimationLayerMixerPlayable.Create(_graph, 3);
        
        _baseMixer = AnimationMixerPlayable.Create(_graph, baseMixerSize);
        _gunMixer = AnimationMixerPlayable.Create(_graph, 1);

        _graph.Connect(_baseMixer, 0, _layerMixer, 0);
        _graph.Connect(_gunMixer, 0, _layerMixer, 2);

        _layerMixer.SetInputWeight(0, 1f);
        _layerMixer.SetInputWeight(2, 0f);
        
        _meleeController.InitializePlayableGraph(_graph, _layerMixer, 1);
        
        if (gunBodyMask != null)
        {
            _layerMixer.SetLayerMaskFromAvatarMask(2, gunBodyMask);
        }
        else
        {
            Debug.LogError("Gun Body Mask is NULL in AnimationController!");
        }
        
        AnimationPlayableOutput output;
        if (_graph.GetOutputCount() > 0)
        {
            output = (AnimationPlayableOutput)_graph.GetOutput(0);
        }
        else
        {
            output = AnimationPlayableOutput.Create(_graph, "Animation", animator);
        }
        
        output.SetSourcePlayable(_layerMixer);
        
        var rigBuilder = body.GetComponent<RigBuilder>();
        rigBuilder.Build();
        
        _graph.Play();
    }



    private void Start()
    {
        _cachedStandLandingSpeed = AnimationUtility.GetAnimationSpeed(standLandingClip, _playerLandingController.StandLandingDuration);
        _cachedRunLandingSpeed = AnimationUtility.GetAnimationSpeed(runLandingClip,  _playerLandingController.RunLandingDuration + runLandingExitOffset);
        _cachedRollLandingSpeed = AnimationUtility.GetAnimationSpeed(rollLandingClip, _playerLandingController.RollLandingDuration + rollLandingExitOffset);
        
        
        _cachedStartingSlideSpeed = AnimationUtility.GetAnimationSpeed(startingSlideClip, _playerMovementController.StartingSlideTime);
        _cachedEndingSlideSpeed = AnimationUtility.GetAnimationSpeed(endingSlideClip, _playerMovementController.EndingSlideTime);
        _cachedForwardJumpLandingSpeed = AnimationUtility.GetAnimationSpeed(forwardJumpingLandingClip, _playerForwardJumpingController.LandingTime);
        _cachedStartingGroundStandJumpSpeed = AnimationUtility.GetAnimationSpeed(startingGroundStandJumpClip, _playerJumpController.StartingGroundStandJumpTime);
        _cachedContinuingGroundStandJumpSpeed = AnimationUtility.GetAnimationSpeed(continuingGroundStandJumpClip, _playerJumpController.ContinuingGroundStandJumpTime);
        _cachedStartingGroundRunJumpSpeed = AnimationUtility.GetAnimationSpeed(startingGroundRunJumpClip, _playerJumpController.StartingGroundRunJumpTime);
        _cachedContinuingGroundRunJumpSpeed = AnimationUtility.GetAnimationSpeed(continuingGroundRunJumpClip, _playerJumpController.ContinuingGroundRunJumpTime);
        _cachedStartingWallJumpSpeed = AnimationUtility.GetAnimationSpeed(startingWallJumpClip, _playerJumpController.StartingWallJumpTime);
        _cachedContinuingWallJumpSpeed = AnimationUtility.GetAnimationSpeed(continuingWallJumpClip, _playerJumpController.ContinuingWallJumpTime);
        
        _cachedRotationChangeSpeedForSlideStarting = 90.0f / Mathf.Max(0.001f, _playerMovementController.StartingSlideTime);
        _cachedRotationChangeSpeedForSlideEnding = 90.0f / Mathf.Max(0.001f, _playerMovementController.EndingSlideTime);
        
        
        SetMultidirectionalArrays();
        
        AnimationUtility.ConnectPersistentClip(ref _graph, _gunMixer, 0, leftHandGunFingersClip, 1f);
        
        _gunLocalPosOffset = leftHandBone.InverseTransformPoint(gunBarrelPoint.position);
        _gunLocalRotOffset = Quaternion.Inverse(leftHandBone.rotation) * gunBarrelPoint.rotation;
        
        
        handGun.SetActive(false);
        pocketGun.SetActive(true);
    }

    private void SetMultidirectionalArrays()
    {
        for (int i = 2; i < baseMixerSize; ++i)
        {
            AnimationUtility.ConnectPersistentClip(ref _graph, _baseMixer, i, multidirectionalMovement[i-2].animationClip);
        }

        multiDirectionalSpeedMultipliers[0] = 1f;
        for (int i = 3; i < baseMixerSize; ++i)
        {
            multiDirectionalSpeedMultipliers[i-2] = AnimationUtility.GetSpeedFraction(multidirectionalMovement[i-2].animationClip, multidirectionalMovement[0].animationClip);
        }
        
        for (int i = 2; i < baseMixerSize; ++i)
        {
            multidirectionalAngles[i-2] = multidirectionalMovement[i-2].angle;
        }
    }
    
    private void Update()
    {
        //dummy.position = hint.position;
        CalculateBaseLayerLogic();
        CalculateGunLayerLogic();
        
        
        UpdateLayerTransitions(2, gunTransitionFraction, ref gunTransitionState);
        UpdateRig(ref leftHandRig, gunTransitionFraction, ref gunTransitionState);
        UpdateRig(ref headRig, gunTransitionFraction, ref gunTransitionState);
        UpdateProp(ref handGun, ref pocketGun, ref gunTransitionState, true);
        

        AnimationUtility.UpdateLayerAnimation(ref _graph,_baseMixer, ref baseMixerWeights, ref _currentBasePort, ref _currentBaseClip, 
            baseTargetClip, baseTargetSpeed, baseTargetCrossfadeDuration, ref isAnimationTransitionBase);
        AnimationUtility.UpdateMultiDirectionalAnimation(_playerSensors, transform,_baseMixer, ref baseMixerWeights, ref multiDirectionalSpeedMultipliers, ref multidirectionalAngles, shouldUseMultiDirectionalInput,
            targetMultidirectionalSpeed, crossfadeDuration, 
           // ref multidirectionalLeftRightCurrent, ref multidirectionalLeftRightSmoothVelocity, 
            ref multiDirectionalTransitionBase);
        AnimationUtility.UseWeights(_baseMixer, ref baseMixerWeights, baseMixerSize);
        
        UpdateBodyRotation();
    }
    
    private void LateUpdate()
    {
        if (gunTransitionState!=BaseActionTransitionsEnum.Base)
        {
            UpdateGunIKTargetTransform();
        }
    }
    
    private void UpdateGunIKTargetTransform()
    {
        var targetGunPos = _playerGunController.GunPosition;
        var targetGunRot = Quaternion.LookRotation(_playerGunController.TargetDirection);

        var targetHandRot = targetGunRot * Quaternion.Inverse(_gunLocalRotOffset);
        var targetHandPos = targetGunPos - (targetHandRot * _gunLocalPosOffset);

        leftArmIKTarget.position = targetHandPos;
        leftArmIKTarget.rotation = targetHandRot;
        
        headIKTarget.position = headBone.position + _playerGunController.TargetHeadDirection ;
    }

    private void CalculateBaseLayerLogic()
    {
        baseTargetClip = fallingIdleClip;
        baseTargetSpeed = 1f;
        targetMultidirectionalSpeed = 1f;
        baseTargetCrossfadeDuration = crossfadeDuration; 
        shouldUseMultiDirectionalInput = false;
        
        var currentSpeed = _playerSensors.SpeedAlignedWithGround;

        if (_playerDashController.IsDashing)
        {
            baseTargetClip = dashClip;
        }
        else if (_playerLandingController.LandingState != 0)
        {
            switch (_playerLandingController.LandingState)
            {
                case 1:
                    baseTargetClip = standLandingClip;
                    baseTargetSpeed = _cachedStandLandingSpeed;
                    break;
                case 2:
                    baseTargetClip = runLandingClip;
                    baseTargetSpeed = _cachedRunLandingSpeed;
                    break;
                case 3:
                    baseTargetClip = rollLandingClip;
                    baseTargetSpeed = _cachedRollLandingSpeed;
                    break;
            }
        }
        else if (_playerForwardJumpingController.IsForwardJumping)
        {
            if (_playerForwardJumpingController.IsLanding)
            {
                baseTargetClip = forwardJumpingLandingClip;
                baseTargetSpeed = _cachedForwardJumpLandingSpeed;
            }
            else
            {
                baseTargetClip = forwardJumpingInAirClip;
                baseTargetSpeed = forwardJumpingInAirClip.length / Mathf.Max(0.001f, _playerForwardJumpingController.TimeToJump); 
            }
        }
        else if (_playerJumpController.JumpState != 0)
        {
            switch (_playerJumpController.JumpState)
            {
                case 1:
                    baseTargetClip = startingGroundStandJumpClip;
                    baseTargetSpeed = _cachedStartingGroundStandJumpSpeed;
                    break;
                case 2:
                    baseTargetClip = continuingGroundStandJumpClip;
                    baseTargetSpeed = _cachedContinuingGroundStandJumpSpeed;
                    break;
                case 3:
                    baseTargetClip = startingGroundRunJumpClip;
                    baseTargetSpeed = _cachedStartingGroundRunJumpSpeed;
                    break;
                case 4:
                    baseTargetClip = continuingGroundRunJumpClip;
                    baseTargetSpeed = _cachedContinuingGroundRunJumpSpeed;
                    break;
                case 5:
                    baseTargetClip = startingWallJumpClip;
                    baseTargetSpeed = _cachedStartingWallJumpSpeed;
                    break;
                case 6:
                    baseTargetClip = continuingWallJumpClip;
                    baseTargetSpeed = _cachedContinuingWallJumpSpeed;
                    break;
            }
        }
        else if (_playerMovementController.WallRunningState != 0)
        {
            baseTargetClip = _playerMovementController.WallRunningState > 0 ? rightWallRunClip : leftWallRunClip;
            baseTargetSpeed = currentSpeed / normalSpeedForWallRunAnimationSpeed;
        }
        else if (_playerMovementController.SlidingPhase != 0)
        {
            if (_playerMovementController.SlidingPhase == 1) 
            {
                baseTargetClip = startingSlideClip;
                baseTargetSpeed = _cachedStartingSlideSpeed;
            }
            else if (_playerMovementController.SlidingPhase == 2) 
            {
                baseTargetClip = slidingClip;
            }
            else 
            {
                baseTargetClip = endingSlideClip;
                baseTargetSpeed = _cachedEndingSlideSpeed;
            }
        }
        else if (_playerSensors.IsGrounded)
        {
            if (_playerTargetLockController.IsLocked)
            {
                baseTargetClip = idleClip;
                shouldUseMultiDirectionalInput = currentSpeed > walkThreshold;
                targetMultidirectionalSpeed = currentSpeed / normalSpeedForForwardRunAnimationSpeed;
            }
            else if (currentSpeed > runThreshold)
            {
                baseTargetClip = runClip;
                baseTargetSpeed = currentSpeed / normalSpeedForRunAnimationSpeed;
            }
            else if (currentSpeed > walkThreshold)
            {
                baseTargetClip = walkClip;
                baseTargetSpeed = currentSpeed / normalSpeedForWalkAnimationSpeed;
            }
            else
            {
                baseTargetClip = idleClip;
            }
        }
        else
        {
            baseTargetClip = fallingIdleClip; 
        }
    }

    private void CalculateGunLayerLogic()
    {
        switch (_playerGunController.GunState)
        {
            case PlayerGunController.GunStateEnum.Non:
                gunTransitionFraction = 0;
                gunTransitionState = BaseActionTransitionsEnum.Base;
                break;
            case PlayerGunController.GunStateEnum.Starting:
                gunTransitionFraction = _playerGunController.CurrentBlendFraction;
                gunTransitionState = BaseActionTransitionsEnum.BaseToAction;
                break;
            case PlayerGunController.GunStateEnum.Aiming:
                gunTransitionFraction = 1;
                gunTransitionState = BaseActionTransitionsEnum.Action;
                break;
            case PlayerGunController.GunStateEnum.Finishing:
                gunTransitionFraction = _playerGunController.CurrentBlendFraction;
                gunTransitionState = BaseActionTransitionsEnum.ActionToBase;
                break;
        }
    }

    private void UpdateLayerTransitions(int layer,ref Utility.FractionBlockingValueTimer<BaseActionTransitionsEnum> layerState)
    {
        float weight = 0f;
        switch (layerState.Value)
        {
            case BaseActionTransitionsEnum.Base:
                weight = 0f;
                break;
            case BaseActionTransitionsEnum.Action:
                weight = 1f;
                break;
            case BaseActionTransitionsEnum.BaseToAction:
                if (layerState.CanBeChanged) 
                {
                    layerState.SetForce(BaseActionTransitionsEnum.Action);
                    weight = 1f;
                }
                else
                {
                    weight = Mathf.Clamp01(layerState.TimeFraction);
                }
                break;

            case BaseActionTransitionsEnum.ActionToBase:
                if (layerState.CanBeChanged) 
                {
                    layerState.SetForce(BaseActionTransitionsEnum.Base);
                    weight = 0f;
                }
                else
                {
                    weight = 1f - Mathf.Clamp01(layerState.TimeFraction);
                }
                break;
        }
        _layerMixer.SetInputWeight(layer, weight);
    }
    private void UpdateLayerTransitions(int layer, float fraction, ref BaseActionTransitionsEnum baseActionTransitionsState)
    {
        float weight = 0f;
        switch (baseActionTransitionsState)
        {
            case BaseActionTransitionsEnum.Base:
                weight = 0;
                break;
            case BaseActionTransitionsEnum.Action:
                weight = 1;
                break;
            case BaseActionTransitionsEnum.BaseToAction:
                weight = Mathf.Clamp01(fraction);
                break;
            case BaseActionTransitionsEnum.ActionToBase:
                weight = 1f - Mathf.Clamp01(fraction);
                break;
        }
        _layerMixer.SetInputWeight(layer, weight);
    }
    
    private void UpdateRig(ref Rig rig, float fraction, ref BaseActionTransitionsEnum baseActionTransitionsState)
    {
        float weight = 0f;
        switch (baseActionTransitionsState)
        {
            case BaseActionTransitionsEnum.Base:
                weight = 0;
                break;
            case BaseActionTransitionsEnum.Action:
                weight = 1;
                break;
            case BaseActionTransitionsEnum.BaseToAction:
                weight = Mathf.Clamp01(fraction);
                break;
            case BaseActionTransitionsEnum.ActionToBase:
                weight = 1f - Mathf.Clamp01(fraction);
                break;
        }
        rig.weight = weight;
    }

    private void UpdateRig(ref Rig rig, ref Utility.FractionBlockingValueTimer<BaseActionTransitionsEnum> baseActionTransitionsState)
    {
        float weight = 0f;
        switch (baseActionTransitionsState.Value)
        {
            case BaseActionTransitionsEnum.Base:
                weight = 0;
                break;
            case BaseActionTransitionsEnum.Action:
                weight = 1;
                break;
            case BaseActionTransitionsEnum.BaseToAction:
                weight = Mathf.Clamp01(baseActionTransitionsState.TimeFraction);
                if(baseActionTransitionsState.CanBeChanged)
                    baseActionTransitionsState.SetForce(BaseActionTransitionsEnum.Action);
                break;
            case BaseActionTransitionsEnum.ActionToBase:
                weight = 1f - Mathf.Clamp01(baseActionTransitionsState.TimeFraction);
                if(baseActionTransitionsState.CanBeChanged)
                    baseActionTransitionsState.SetForce(BaseActionTransitionsEnum.Base);
                break;
        }
        rig.weight = weight;
    }
    private void UpdateProp(ref GameObject hand, ref GameObject pocket, ref BaseActionTransitionsEnum baseActionTransitionsState, bool showDuringBlend, bool showAnyway = false)
    {
        if (showAnyway)
        {
            hand.SetActive(true);
            pocket.SetActive(false);
            return;
        }
        switch (baseActionTransitionsState)
        {
            case BaseActionTransitionsEnum.Base:
                hand.SetActive(false);
                pocket.SetActive(true);
                break;
            case BaseActionTransitionsEnum.BaseToAction or BaseActionTransitionsEnum.ActionToBase:
                if (showDuringBlend)
                {
                    hand.SetActive(true);
                    pocket.SetActive(false);
                }
                else
                {
                    hand.SetActive(false);
                    pocket.SetActive(true);
                }
                break;
            case BaseActionTransitionsEnum.Action:
                hand.SetActive(true);
                pocket.SetActive(false);
                break;
        }
    }



    


    private void UpdateBodyRotation()
    {
        var targetYAngle = 0f;
        var rotationSpeed = 90f;
        switch (_playerMovementController.SlidingPhase)
        {
            case 1:
                targetYAngle = 90f * _playerMovementController.SlidingPhaseFraction;
                rotationSpeed = _cachedRotationChangeSpeedForSlideStarting * Time.deltaTime;
                break;
            case 2:
                targetYAngle = 90f;
                break;
            case 3:
                targetYAngle = 90f * (1-_playerMovementController.SlidingPhaseFraction);
                rotationSpeed = _cachedRotationChangeSpeedForSlideEnding * Time.deltaTime;
                break;
        }
        
        if (_playerSensors.IsGrounded && _playerSensors.FoundGroundNormal && _playerMovementController.IsActiveSlidingPhase)
        {
           var localNormal = transform.InverseTransformDirection(_playerSensors.GroundNormal);

            var targetAngleX = Mathf.Atan2(-localNormal.z, localNormal.y) * Mathf.Rad2Deg;

            var targetAngleZ = Mathf.Atan2(-localNormal.x, localNormal.y) * Mathf.Rad2Deg;

           bodyHolder.localRotation = Quaternion.Euler(-targetAngleX, 0f, targetAngleZ);
            //Debug.Log(bodyHolder.up + ", " + _playerSensors.GroundNormal);
        }
        else
        {
            bodyHolder.localRotation = Quaternion.Euler(0, 0f, 0);
        }


        Quaternion targetRotation = _defaultBodyLocalRot * Quaternion.Euler(0, targetYAngle, 0);
        bodyTransform.localRotation = Quaternion.RotateTowards(
            bodyTransform.localRotation, 
            targetRotation, 
            rotationSpeed
        );
    }

    private void OnDestroy()
    {
        if (_graph.IsValid()) _graph.Destroy();
    }
}
