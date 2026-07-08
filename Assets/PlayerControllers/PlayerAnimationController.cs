using System.Diagnostics;
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
    [SerializeField] private AnimationClip runForwardClip;
    [SerializeField] private AnimationClip runBackwardClip;
    [SerializeField] private AnimationClip runLeftClip;
    [SerializeField] private AnimationClip runRightClip;
    
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
    
    [Header("Sword Sheathe Clips")]
    [SerializeField] private AnimationClip unsheatheClip;
    [SerializeField] private AnimationClip sheatheClip;

    [Header("Sword Basic Movement Clips")]
    [SerializeField] private AnimationClip closeTurnClip;
    [SerializeField] private AnimationClip farTurnClip;
    [SerializeField] private AnimationClip unsheatheToCloseClip;
    [SerializeField] private AnimationClip closeToSheatheClip;
    [SerializeField] private AnimationClip farToSheatheClip;
    [SerializeField] private AnimationClip farTurnToSheatheClip;
    
    [Header("Sword Defence Clips")]
    [SerializeField] private AnimationClip unsheatheToParryClip;
    [SerializeField] private AnimationClip parryClip;
    [SerializeField] private AnimationClip blockExitToSheatheClip;
    [SerializeField] private AnimationClip blockExitToCloseClip;

    [Header("Sword Attack Clips")]
    [SerializeField] private AnimationClip frontAttackClip;
    [SerializeField] private AnimationClip backAttackClip;
    
    [Header("Sword Game objects")]
    [SerializeField] private GameObject backSword;
    [SerializeField] private GameObject handSword;
    
    [Header("Sword Layer Settings")]
    [SerializeField] private AvatarMask swordBodyMask;

    [Header("Sword Block Rigging Settings")]
    [SerializeField] private Rig rightHandRig;
    
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
    private PlayerSwordController _playerSwordController;
    private PlayerGunController _playerGunController;

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

    private float _cachedUnsheatheSpeed;
    private float _cachedSheatheSpeed;
    
    private float _cachedUnsheatheToCloseSpeed;
    private float _cachedFarToSheatheSpeed;
    private float _cachedCloseTurnSpeed;
    private float _cachedFarTurnSpeed;
    private float _cachedFarTurnToSheatheSpeed;
    private float _cachedCloseToSheatheSpeed;

    private float _cachedFrontAttackSpeed;
    private float _cachedBackAttackSpeed;

    private float _cachedSheatheToParrySpeed;
    private float _cachedAnyToParryTime;
    private float _cachedParrySpeed;
    private float _cachedBlockEnterTime;
    private float _cachedBlockExitTime;
    private float _cachedParryToCloseSpeed;
    private float _cachedParryToSheatheSpeed;

    
    private PlayableGraph _graph;
    private AnimationLayerMixerPlayable _layerMixer;
    private AnimationMixerPlayable _baseMixer;
    private AnimationMixerPlayable _swordMixer;
    private AnimationMixerPlayable _gunMixer;
    private PlayerTargetLockController _playerTargetLockController;

    private const int BaseMixerSize = 6;
    private float[] baseMixerWeights = new float[BaseMixerSize];
    
    private AnimationClip baseTargetClip;
    private float baseTargetSpeed;
    private float baseTargetCrossfadeDuration;
    
    private bool baseShouldUseMultiDirectionalInput;
    private Utility.FractionBlockingValueTimer<BaseActionTransitionsEnum> multiDirectionalTransitionBase =
        BaseActionTransitionsEnum.Base;
    private float[] baseMultiDirectionalSpeeds = new float[4];
    private float baseMultidirRotation = 0f;

    private float baseLeftRightCurrent = 0f;
    private float baseLeftRightSmoothVelocity = 0f;    
    
    private const int ActionMixerSize = 2;
    private float[] actionMixerWeights = new float[ActionMixerSize];
    
    private AnimationClip actionTargetClip;
    private float actionTargetSpeed;
    private float actionTargetCrossfadeDuration;

    private int _currentBasePort = 0;
    private AnimationClip _currentBaseClip = null;
    
    private int _currentActionPort = 0;
    private AnimationClip _currentActionClip = null;

    
    
    private Utility.FractionBlockingValueTimer<TransitionStateEnum> isAnimationTransitionBase =
        TransitionStateEnum.Current;

    private Utility.FractionBlockingValueTimer<TransitionStateEnum> isAnimationTransitionAction =
        TransitionStateEnum.Current;

    private BaseActionTransitionsEnum swordLayerState = BaseActionTransitionsEnum.Base;
    private float swordLayerFraction = 0f;
    
    private Utility.FractionBlockingValueTimer<BaseActionTransitionsEnum> swordBlockRigState =  BaseActionTransitionsEnum.Base;

    
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
        _playerSwordController = GetComponent<PlayerSwordController>();
        _playerGunController = GetComponent<PlayerGunController>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>(); 
        
        _defaultBodyLocalRot = bodyTransform.localRotation;

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
        
        _baseMixer = AnimationMixerPlayable.Create(_graph, BaseMixerSize);
        _swordMixer = AnimationMixerPlayable.Create(_graph, ActionMixerSize);
        _gunMixer = AnimationMixerPlayable.Create(_graph, 1);

        _graph.Connect(_baseMixer, 0, _layerMixer, 0);
        _graph.Connect(_swordMixer, 0, _layerMixer, 1);
        _graph.Connect(_gunMixer, 0, _layerMixer, 2);

        _layerMixer.SetInputWeight(0, 1f);
        _layerMixer.SetInputWeight(1, 0f);
        _layerMixer.SetInputWeight(2, 0f);


        if (swordBodyMask != null)
        {
            _layerMixer.SetLayerMaskFromAvatarMask(1, swordBodyMask);
        }
        else
        {
            Debug.LogError("Sword Body Mask is NULL in AnimationController!");
        }
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
        
        _cachedUnsheatheSpeed = AnimationUtility.GetAnimationSpeed(unsheatheClip, _playerSwordController.UnsheatheTime);
        _cachedSheatheSpeed = AnimationUtility.GetAnimationSpeed(sheatheClip, _playerSwordController.SheatheTime);
        
        _cachedUnsheatheToCloseSpeed = AnimationUtility.GetAnimationSpeed(unsheatheToCloseClip, _playerSwordController.UnsheatheToCloseTime);
        _cachedFarToSheatheSpeed = AnimationUtility.GetAnimationSpeed(farToSheatheClip, _playerSwordController.FarToSheatheTime);
        
        _cachedFarTurnToSheatheSpeed = AnimationUtility.GetAnimationSpeed(farTurnToSheatheClip, _playerSwordController.FarTurnToSheatheTime);
        
        _cachedCloseTurnSpeed = AnimationUtility.GetAnimationSpeed(closeTurnClip, _playerSwordController.CloseTurnTime);
        _cachedFarTurnSpeed = AnimationUtility.GetAnimationSpeed(farTurnClip, _playerSwordController.FarTurnTime);
        _cachedCloseToSheatheSpeed = AnimationUtility.GetAnimationSpeed(closeToSheatheClip, _playerSwordController.CloseToSheatheTime);

        _cachedFrontAttackSpeed = AnimationUtility.GetAnimationSpeed(frontAttackClip, _playerSwordController.FrontAttackTime);
        _cachedBackAttackSpeed = AnimationUtility.GetAnimationSpeed(backAttackClip, _playerSwordController.BackAttackTime);
        
        _cachedSheatheToParrySpeed = AnimationUtility.GetAnimationSpeed(unsheatheToParryClip,  _playerSwordController.UnsheatheToParryTime);
        _cachedParrySpeed=AnimationUtility.GetAnimationSpeed(parryClip, _playerSwordController.ParryTime);
        _cachedParryToCloseSpeed=AnimationUtility.GetAnimationSpeed(blockExitToCloseClip, _playerSwordController.BlockExitToCloseTime);
        _cachedParryToSheatheSpeed=AnimationUtility.GetAnimationSpeed(blockExitToSheatheClip, _playerSwordController.BlockExitToSheatheTime);
        _cachedAnyToParryTime = _playerSwordController.AnyToParryTime;
        _cachedBlockEnterTime = _playerSwordController.BlockEnterTime;
        _cachedBlockExitTime = _playerSwordController.BlockExitTime;
        //_cachedBlockSpeed = AnimationHandler.GetAnimationSpeed(blockClip,_playerSwordController.BlockSpeed);
        
        _cachedRotationChangeSpeedForSlideStarting = 90.0f / Mathf.Max(0.001f, _playerMovementController.StartingSlideTime);
        _cachedRotationChangeSpeedForSlideEnding = 90.0f / Mathf.Max(0.001f, _playerMovementController.EndingSlideTime);
        
        
        SetPersistentClipSettings();
        
        AnimationUtility.ConnectPersistentClip(ref _graph, _gunMixer, 0, leftHandGunFingersClip, 1f);
        
        _gunLocalPosOffset = leftHandBone.InverseTransformPoint(gunBarrelPoint.position);
        _gunLocalRotOffset = Quaternion.Inverse(leftHandBone.rotation) * gunBarrelPoint.rotation;
        
        handSword.SetActive(false);
        backSword.SetActive(true);
        
        handGun.SetActive(false);
        pocketGun.SetActive(true);
    }

    private void SetPersistentClipSettings()
    {
        AnimationUtility.ConnectPersistentClip(ref _graph, _baseMixer, 2, runForwardClip);
        AnimationUtility.ConnectPersistentClip(ref _graph, _baseMixer, 3, runBackwardClip);
        AnimationUtility.ConnectPersistentClip(ref _graph, _baseMixer, 4, runLeftClip);
        AnimationUtility.ConnectPersistentClip(ref _graph, _baseMixer, 5, runRightClip);

        baseMultiDirectionalSpeeds[0] = 1f;
        baseMultiDirectionalSpeeds[1] = AnimationUtility.GetSpeedFraction(runBackwardClip, runForwardClip);
        baseMultiDirectionalSpeeds[2] = AnimationUtility.GetSpeedFraction(runLeftClip, runForwardClip);
        baseMultiDirectionalSpeeds[3] = AnimationUtility.GetSpeedFraction(runRightClip, runForwardClip);
    }
    
    private void Update()
    {
        //dummy.position = hint.position;
        CalculateBaseLayerLogic();
        CalculateActionLayerLogic();
        CalculateGunLayerLogic();
        
        
        UpdateLayerTransitions(1, swordLayerFraction, ref swordLayerState);
        UpdateLayerTransitions(2, gunTransitionFraction, ref gunTransitionState);
        UpdateRig(ref leftHandRig, gunTransitionFraction, ref gunTransitionState);
        UpdateRig(ref headRig, gunTransitionFraction, ref gunTransitionState);
        UpdateRig(ref rightHandRig, ref swordBlockRigState);
        UpdateProp(ref handSword, ref backSword, ref swordLayerState, false, swordBlockRigState!=BaseActionTransitionsEnum.Base);
        UpdateProp(ref handGun, ref pocketGun, ref gunTransitionState, true);
        

        AnimationUtility.UpdateLayerAnimation(ref _graph,_baseMixer, ref baseMixerWeights, ref _currentBasePort, ref _currentBaseClip, 
            baseTargetClip, baseTargetSpeed, baseTargetCrossfadeDuration, ref isAnimationTransitionBase);
        AnimationUtility.UpdateMultiDirectionalAnimation(_playerSensors, transform,_baseMixer, ref baseMixerWeights, ref baseMultiDirectionalSpeeds, baseShouldUseMultiDirectionalInput,
            baseTargetSpeed, baseTargetCrossfadeDuration, ref baseLeftRightCurrent, ref baseLeftRightSmoothVelocity, ref multiDirectionalTransitionBase, out baseMultidirRotation);
        AnimationUtility.UseWeights(_baseMixer, ref baseMixerWeights, BaseMixerSize);
        
        

        if (swordLayerState != BaseActionTransitionsEnum.Base) 
        {
            AnimationUtility.UpdateLayerAnimation(ref _graph, _swordMixer, ref actionMixerWeights, ref _currentActionPort, ref _currentActionClip, 
                actionTargetClip, actionTargetSpeed, actionTargetCrossfadeDuration, ref isAnimationTransitionAction);
            AnimationUtility.UseWeights(_swordMixer, ref actionMixerWeights, ActionMixerSize);
        }
        
        

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
        baseTargetCrossfadeDuration = crossfadeDuration; 
        baseShouldUseMultiDirectionalInput = false;
        
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
            if (currentSpeed > walkThreshold)
            {
                if (_playerTargetLockController.IsLocked)
                {
                    baseShouldUseMultiDirectionalInput = true;
                    baseTargetSpeed = currentSpeed / normalSpeedForForwardRunAnimationSpeed;
                }
                else
                {
                    if (currentSpeed > runThreshold)
                    {
                        baseTargetClip = runClip;
                        baseTargetSpeed = currentSpeed / normalSpeedForRunAnimationSpeed;
                    }
                    else
                    {
                        baseTargetClip = walkClip;
                        baseTargetSpeed = currentSpeed / normalSpeedForWalkAnimationSpeed;
                    }
                        
                }
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

    private void CalculateActionLayerLogic()
    {
        actionTargetCrossfadeDuration = 0f;
        swordLayerFraction = 1f;
        swordLayerState = BaseActionTransitionsEnum.Action;
        
        var currentAttackState = _playerSwordController.SwordState;
        switch (currentAttackState)
        {
            case PlayerSwordController.SwordStateEnum.Non:
                swordLayerState = BaseActionTransitionsEnum.Base;
                swordLayerFraction = 0f;
                break;
            case PlayerSwordController.SwordStateEnum.BlendIn:
                actionTargetClip = unsheatheClip;
                actionTargetSpeed = 0f;
                swordLayerState = BaseActionTransitionsEnum.BaseToAction;
                swordLayerFraction = _playerSwordController.SwordStateFraction;
                break;
            case PlayerSwordController.SwordStateEnum.BlendOut:
                actionTargetClip = unsheatheClip;
                actionTargetSpeed = 0f;
                swordLayerState = BaseActionTransitionsEnum.ActionToBase;
                swordLayerFraction = _playerSwordController.SwordStateFraction;
                break;
            case PlayerSwordController.SwordStateEnum.Unsheathe:
                actionTargetClip = unsheatheClip;
                actionTargetSpeed =  _cachedUnsheatheSpeed;
                break;
            case PlayerSwordController.SwordStateEnum.UnsheatheToClose:
                actionTargetClip = unsheatheToCloseClip;
                actionTargetSpeed = _cachedUnsheatheToCloseSpeed;
                break;
            case PlayerSwordController.SwordStateEnum.FrontAttack:
                actionTargetClip = frontAttackClip;
                actionTargetSpeed = _cachedFrontAttackSpeed;
                break;
            case PlayerSwordController.SwordStateEnum.FarTurn:
                actionTargetClip = farTurnClip;
                actionTargetSpeed = _cachedFarTurnSpeed;
                break;
            case PlayerSwordController.SwordStateEnum.BackAttack:
                actionTargetClip = backAttackClip;
                actionTargetSpeed = _cachedBackAttackSpeed;
                break;
            case PlayerSwordController.SwordStateEnum.CloseTurn:
                actionTargetClip = closeTurnClip;
                actionTargetSpeed = _cachedCloseTurnSpeed;
                break;
            case PlayerSwordController.SwordStateEnum.CloseToSheathe:
                actionTargetClip = closeToSheatheClip;
                actionTargetSpeed = _cachedCloseToSheatheSpeed;
                break;
            case PlayerSwordController.SwordStateEnum.FarToSheathe:
                actionTargetClip =  farToSheatheClip;
                actionTargetSpeed =  _cachedFarToSheatheSpeed;
                break;
            case PlayerSwordController.SwordStateEnum.FarTurnToSheathe:
                actionTargetClip =  farTurnToSheatheClip;
                actionTargetSpeed =  _cachedFarTurnToSheatheSpeed;
                break;
            case PlayerSwordController.SwordStateEnum.Sheathe:
                actionTargetClip =  sheatheClip;
                actionTargetSpeed = _cachedSheatheSpeed;
                break;
            case PlayerSwordController.SwordStateEnum.SheatheToParry:
                actionTargetClip =  unsheatheToParryClip;
                actionTargetSpeed =  _cachedSheatheToParrySpeed;
                break;
            case PlayerSwordController.SwordStateEnum.AnyToParry:
                actionTargetClip =  parryClip;
                actionTargetSpeed =  0;
                actionTargetCrossfadeDuration = _cachedAnyToParryTime;
                break;
            case PlayerSwordController.SwordStateEnum.Parry:
                actionTargetClip =  parryClip;
                actionTargetSpeed =  _cachedParrySpeed;
                break;
            case PlayerSwordController.SwordStateEnum.Block:
                actionTargetClip =  blockExitToCloseClip;
                actionTargetSpeed =  0;
                actionTargetCrossfadeDuration = _cachedBlockExitTime;
                break;
            case PlayerSwordController.SwordStateEnum.BlockExit:
                actionTargetClip =  blockExitToCloseClip;
                actionTargetSpeed =  0;
                actionTargetCrossfadeDuration = _cachedBlockExitTime;
                break;
            case PlayerSwordController.SwordStateEnum.BlockExitToClose:
                actionTargetClip =  blockExitToCloseClip;
                actionTargetSpeed =  _cachedParryToCloseSpeed;
                break;
            case PlayerSwordController.SwordStateEnum.BlockExitToSheathe:
                actionTargetClip =  blockExitToSheatheClip;
                actionTargetSpeed =  _cachedParryToSheatheSpeed;
                break;
        }

        switch (currentAttackState)
        {
            case PlayerSwordController.SwordStateEnum.Block:
                if(swordBlockRigState!=BaseActionTransitionsEnum.Action && swordBlockRigState != BaseActionTransitionsEnum.BaseToAction)
                    swordBlockRigState.SetForce(BaseActionTransitionsEnum.BaseToAction, _cachedBlockEnterTime);
                if (swordBlockRigState == BaseActionTransitionsEnum.BaseToAction)
                {
                    swordLayerFraction = 1-swordBlockRigState.TimeFraction;
                    swordLayerState = BaseActionTransitionsEnum.ActionToBase;
                }
                else if (swordBlockRigState == BaseActionTransitionsEnum.Action)
                {
                    swordLayerState = BaseActionTransitionsEnum.Base;
                }
                break; 
            case PlayerSwordController.SwordStateEnum.Non:
                swordBlockRigState.SetForce(BaseActionTransitionsEnum.Base, 0);
                break;
            default:
                if(swordBlockRigState!=BaseActionTransitionsEnum.ActionToBase && swordBlockRigState != BaseActionTransitionsEnum.Base)
                    swordBlockRigState.SetForce(BaseActionTransitionsEnum.ActionToBase, _cachedBlockExitTime);
                
                if (swordBlockRigState == BaseActionTransitionsEnum.ActionToBase)
                {
                    swordLayerFraction = 1-swordBlockRigState.TimeFraction;
                    swordLayerState = BaseActionTransitionsEnum.BaseToAction;
                }
                break;
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

        if (multiDirectionalTransitionBase.Value != BaseActionTransitionsEnum.Base && _playerMovementController.CanLookToLockedTarget)
        {
            targetYAngle = baseMultidirRotation;
            rotationSpeed = 90f;
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
