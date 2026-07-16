using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using Debug = UnityEngine.Debug;

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
    
    [Header("Settings & Thresholds for basic movement")]
    [SerializeField] private float walkThreshold = 0.2f;
    [SerializeField] private float normalSpeedForWalkAnimationSpeed = 3f;
    [SerializeField] private float runThreshold = 8f;
    [SerializeField] private float normalSpeedForRunAnimationSpeed = 8f;
    [SerializeField] private float normalSpeedForWallRunAnimationSpeed = 12f;
    
    [Header("Other Settings")] 
    [SerializeField] private float runLandingExitOffset = 0.2f;
    [SerializeField] private float rollLandingExitOffset = 0.05f;
    [SerializeField] private float crossfadeDuration = 0.15f;

    // --- References ---
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
    private PlayerTargetLockController _playerTargetLockController;

    // --- Caches ---
    private Quaternion _defaultBodyLocalRot;
    
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

    private AutomaticAnimationLayerController _animationLayerController;
    private MultidirectionalMovementController _multidirectionalMovementController;
    
    private AnimationClip targetClip;
    private float targetSpeed;
    
    private void Awake()
    {
        if (!TryGetComponent(out AnimationController controller))
        {
            Debug.LogError("No AnimationController found!", this);
            enabled = false;
            return;
        }
        
        var clips = new HashSet<AnimationClip>();
        clips.Add(idleClip);
        clips.Add(walkClip);
        clips.Add(runClip);
        clips.Add(dashClip);
        clips.Add(fallingIdleClip);
        clips.Add(standLandingClip);
        clips.Add(rollLandingClip);
        clips.Add(runLandingClip);
        clips.Add(startingGroundStandJumpClip);
        clips.Add(continuingGroundStandJumpClip);
        clips.Add(startingGroundRunJumpClip);
        clips.Add(continuingGroundRunJumpClip);
        clips.Add(startingWallJumpClip);
        clips.Add(continuingWallJumpClip);
        clips.Add(forwardJumpingInAirClip);
        clips.Add(forwardJumpingLandingClip);
        clips.Add(leftWallRunClip);
        clips.Add(rightWallRunClip);
        clips.Add(slidingClip);
        clips.Add(startingSlideClip);
        clips.Add(endingSlideClip);

        _animationLayerController = controller.GetAutomaticAnimationLayer(clips, 0);
        _animationLayerController.SetLayerWeight(1f);
        
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
        _multidirectionalMovementController = GetComponent<MultidirectionalMovementController>();
        
        _defaultBodyLocalRot = bodyTransform.localRotation;
    }
    



    private void Start()
    {
        _cachedStandLandingSpeed = Utility.GetAnimationSpeed(standLandingClip, _playerLandingController.StandLandingDuration);
        _cachedRunLandingSpeed = Utility.GetAnimationSpeed(runLandingClip,  _playerLandingController.RunLandingDuration + runLandingExitOffset);
        _cachedRollLandingSpeed = Utility.GetAnimationSpeed(rollLandingClip, _playerLandingController.RollLandingDuration + rollLandingExitOffset);
        
        
        _cachedStartingSlideSpeed = Utility.GetAnimationSpeed(startingSlideClip, _playerMovementController.StartingSlideTime);
        _cachedEndingSlideSpeed = Utility.GetAnimationSpeed(endingSlideClip, _playerMovementController.EndingSlideTime);
        _cachedForwardJumpLandingSpeed = Utility.GetAnimationSpeed(forwardJumpingLandingClip, _playerForwardJumpingController.LandingTime);
        _cachedStartingGroundStandJumpSpeed = Utility.GetAnimationSpeed(startingGroundStandJumpClip, _playerJumpController.StartingGroundStandJumpTime);
        _cachedContinuingGroundStandJumpSpeed = Utility.GetAnimationSpeed(continuingGroundStandJumpClip, _playerJumpController.ContinuingGroundStandJumpTime);
        _cachedStartingGroundRunJumpSpeed = Utility.GetAnimationSpeed(startingGroundRunJumpClip, _playerJumpController.StartingGroundRunJumpTime);
        _cachedContinuingGroundRunJumpSpeed = Utility.GetAnimationSpeed(continuingGroundRunJumpClip, _playerJumpController.ContinuingGroundRunJumpTime);
        _cachedStartingWallJumpSpeed = Utility.GetAnimationSpeed(startingWallJumpClip, _playerJumpController.StartingWallJumpTime);
        _cachedContinuingWallJumpSpeed = Utility.GetAnimationSpeed(continuingWallJumpClip, _playerJumpController.ContinuingWallJumpTime);
        
        _cachedRotationChangeSpeedForSlideStarting = 90.0f / Mathf.Max(0.001f, _playerMovementController.StartingSlideTime);
        _cachedRotationChangeSpeedForSlideEnding = 90.0f / Mathf.Max(0.001f, _playerMovementController.EndingSlideTime);
    }


    
    private void Update()
    {
        //dummy.position = hint.position;
        CalculateBaseLayerLogic();
        _animationLayerController.AutomaticUpdateCurrentPlayable(targetClip, targetSpeed, crossfadeDuration);
        UpdateBodyRotation();
    }

    private void CalculateBaseLayerLogic()
    {
        targetClip = fallingIdleClip;
        targetSpeed = 1f;
        _multidirectionalMovementController.ShouldUseMultiDirectionalAnimation = false;
        
        var currentSpeed = _playerSensors.SpeedAlignedWithGround;

        if (_playerDashController.IsDashing)
        {
            targetClip = dashClip;
        }
        else if (_playerLandingController.LandingState != 0)
        {
            switch (_playerLandingController.LandingState)
            {
                case 1:
                    targetClip = standLandingClip;
                    targetSpeed = _cachedStandLandingSpeed;
                    break;
                case 2:
                    targetClip = runLandingClip;
                    targetSpeed = _cachedRunLandingSpeed;
                    break;
                case 3:
                    targetClip = rollLandingClip;
                    targetSpeed = _cachedRollLandingSpeed;
                    break;
            }
        }
        else if (_playerForwardJumpingController.IsForwardJumping)
        {
            if (_playerForwardJumpingController.IsLanding)
            {
                targetClip = forwardJumpingLandingClip;
                targetSpeed = _cachedForwardJumpLandingSpeed;
            }
            else
            {
                targetClip = forwardJumpingInAirClip;
                targetSpeed = forwardJumpingInAirClip.length / Mathf.Max(0.001f, _playerForwardJumpingController.TimeToJump); 
            }
        }
        else if (_playerJumpController.JumpState != 0)
        {
            switch (_playerJumpController.JumpState)
            {
                case 1:
                    targetClip = startingGroundStandJumpClip;
                    targetSpeed = _cachedStartingGroundStandJumpSpeed;
                    break;
                case 2:
                    targetClip = continuingGroundStandJumpClip;
                    targetSpeed = _cachedContinuingGroundStandJumpSpeed;
                    break;
                case 3:
                    targetClip = startingGroundRunJumpClip;
                    targetSpeed = _cachedStartingGroundRunJumpSpeed;
                    break;
                case 4:
                    targetClip = continuingGroundRunJumpClip;
                    targetSpeed = _cachedContinuingGroundRunJumpSpeed;
                    break;
                case 5:
                    targetClip = startingWallJumpClip;
                    targetSpeed = _cachedStartingWallJumpSpeed;
                    break;
                case 6:
                    targetClip = continuingWallJumpClip;
                    targetSpeed = _cachedContinuingWallJumpSpeed;
                    break;
            }
        }
        else if (_playerMovementController.WallRunningState != 0)
        {
            targetClip = _playerMovementController.WallRunningState > 0 ? rightWallRunClip : leftWallRunClip;
            targetSpeed = currentSpeed / normalSpeedForWallRunAnimationSpeed;
        }
        else if (_playerMovementController.SlidingPhase != 0)
        {
            if (_playerMovementController.SlidingPhase == 1) 
            {
                targetClip = startingSlideClip;
                targetSpeed = _cachedStartingSlideSpeed;
            }
            else if (_playerMovementController.SlidingPhase == 2) 
            {
                targetClip = slidingClip;
            }
            else 
            {
                targetClip = endingSlideClip;
                targetSpeed = _cachedEndingSlideSpeed;
            }
        }
        else if (_playerSensors.IsGrounded)
        {
            if (_playerTargetLockController.IsLocked)
            {
                targetClip = idleClip;
                _multidirectionalMovementController.ShouldUseMultiDirectionalAnimation = 
                    currentSpeed > walkThreshold;
                //Debug.Log( currentSpeed + ", " + walkThreshold + ", "+_multidirectionalMovementController.ShouldUseMultiDirectionalAnimation);
            }
            else if (currentSpeed > runThreshold)
            {
                targetClip = runClip;
                targetSpeed = currentSpeed / normalSpeedForRunAnimationSpeed;
            }
            else if (currentSpeed > walkThreshold)
            {
                targetClip = walkClip;
                targetSpeed = currentSpeed / normalSpeedForWalkAnimationSpeed;
            }
            else
            {
                targetClip = idleClip;
            }
        }
        else
        {
            targetClip = fallingIdleClip; 
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

}
