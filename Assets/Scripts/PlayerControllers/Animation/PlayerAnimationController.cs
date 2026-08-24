using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

[DefaultExecutionOrder(100)]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private GameObject body;
    [SerializeField] private Transform bodyHolder;

    [Header("Movement Clips")]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip targetLockIdleClip;
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
    [Header("Ground standing")]
    [SerializeField] private AnimationClip startingGroundStandJumpClip;
    [SerializeField] private AnimationClip continuingGroundStandJumpClip;
    [Header("Ground run")]
    [SerializeField] private AnimationClip startingGroundRunJumpClip;
    [SerializeField] private AnimationClip continuingGroundRunJumpClip;
    [Header("Wall")]
    [SerializeField] private AnimationClip startingWallJumpClip;
    [SerializeField] private AnimationClip continuingWallJumpClip;
    [Header("Rail line")]
    [SerializeField] private AnimationClip startingRailJumpClip;
    [SerializeField] private AnimationClip continuingRailJumpClip;
    [Header("Jump pad player particles")]
    [SerializeField] private ParticleSystem jumpPadParticles;

    [Header("Forward Jump Clips")]
    [SerializeField] private AnimationClip forwardJumpingInAirClip;
    [SerializeField] private AnimationClip forwardJumpingLandingClip;

    [Header("Ledge Climb Clips")] 
    [SerializeField] private AnimationClip ledgeClimbActiveAnimationClip;
    [SerializeField] private AnimationClip ledgeClimbEndingAnimationClip;

    [Header("Wall Run Clips")]
    [SerializeField] private AnimationClip leftWallRunClip;
    [SerializeField] private AnimationClip rightWallRunClip;

    [Header("Rail line riding")]
    [SerializeField] private AnimationClip railLineRidingClip;
    
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

    [Header("Some Clip durations")] 
    [SerializeField] private float targetLockClipDuration = 1f;
    [SerializeField] private float railLineRidingClipDuration = 1f;
    
    [Header("Other Settings")] 
    [SerializeField] private float crossfadeDuration = 0.15f;

    // --- References ---
    private Transform _bodyTransform;
    private PlayerSensors _playerSensors;
    private PlayerSlidingController _playerSlidingController;
    private PlayerForwardJumpingController _playerForwardJumpingController;
    private PlayerDashController _playerDashController;
    private PlayerJumpController _playerJumpController;
    private PlayerLandingController _playerLandingController;
    private MeleeController _meleeController;
    private PlayerTargetLockController _playerTargetLockController;
    private AutomaticAnimationLayerController _animationLayerController;
    private MultidirectionalMovementController _multidirectionalMovementController;
    private PlayerFixedDirectionMovementController _playerFixedDirectionMovementController;
    private PlayerLedgeClimbController _playerLedgeClimbController;

    // --- Caches ---
    private Quaternion _defaultBodyLocalRot;
    
    private float _cachedStartingSlideSpeed;
    private float _cachedEndingSlideSpeed;
    
    private float _cachedForwardJumpLandingSpeed;
    private float _cachedRotationChangeSpeedForSlideStarting;
    private float _cachedRotationChangeSpeedForSlideEnding;
    
    private float _cachedStartingGroundStandJumpSpeed;
    private float _cachedStartingGroundRunJumpSpeed;
    private float _cachedStartingWallJumpSpeed;
    private float _cachedStartingRailJumpSpeed;

    private float _cachedTargetLockClipSpeed;
    private float _cachedLineRideClipSpeed;
    
    private float _cachedStandLandingSpeed;
    private float _cachedRunLandingSpeed;
    private float _cachedRollLandingSpeed;

    private float _cachedLedgeActiveSpeed;
    private float _cachedLedgeEndingSpeed;
    private float _cachedLedgeActiveClipDuration;

    
    private void Awake()
    {
        if (!TryGetComponent(out AnimationAndRigManager controller))
        {
            Debug.LogError("No AnimationController found!", this);
            enabled = false;
            return;
        }
        
        var clips = new HashSet<AnimationClip>();
        clips.Add(idleClip);
        clips.Add(targetLockIdleClip);
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
        clips.Add(startingRailJumpClip);
        clips.Add(continuingRailJumpClip);
        clips.Add(forwardJumpingInAirClip);
        clips.Add(forwardJumpingLandingClip);
        clips.Add(leftWallRunClip);
        clips.Add(rightWallRunClip);
        clips.Add(slidingClip);
        clips.Add(startingSlideClip);
        clips.Add(endingSlideClip);
        clips.Add(railLineRidingClip);
        clips.Add(ledgeClimbActiveAnimationClip);
        clips.Add(ledgeClimbEndingAnimationClip);

        _animationLayerController = controller.GetAutomaticAnimationLayer(clips, 0, "Player base");
        _animationLayerController.SetLayerWeight(1f);
        
        _bodyTransform = body.GetComponent<Transform>();
        _playerSensors = GetComponent<PlayerSensors>();
        _playerSlidingController = GetComponent<PlayerSlidingController>();
        _playerForwardJumpingController = GetComponent<PlayerForwardJumpingController>();
        _playerDashController = GetComponent<PlayerDashController>();
        _playerJumpController = GetComponent<PlayerJumpController>();
        _playerLandingController = GetComponent<PlayerLandingController>();
        _playerTargetLockController = GetComponent<PlayerTargetLockController>(); 
        _meleeController = GetComponent<MeleeController>();
        _multidirectionalMovementController = GetComponent<MultidirectionalMovementController>();
        _playerFixedDirectionMovementController = GetComponent<PlayerFixedDirectionMovementController>();
        _playerLedgeClimbController = GetComponent<PlayerLedgeClimbController>();
        
        _defaultBodyLocalRot = _bodyTransform.localRotation;
    }
    



    private void Start()
    {
        _cachedStandLandingSpeed = UtilityFunctions.GetAnimationSpeed(standLandingClip, _playerLandingController.StandLandingDuration, crossfadeDuration);
        _cachedRunLandingSpeed = UtilityFunctions.GetAnimationSpeed(runLandingClip,  _playerLandingController.RunLandingDuration, crossfadeDuration);
        _cachedRollLandingSpeed = UtilityFunctions.GetAnimationSpeed(rollLandingClip, _playerLandingController.RollLandingDuration, crossfadeDuration);
        
        
        _cachedStartingSlideSpeed = UtilityFunctions.GetAnimationSpeed(startingSlideClip, _playerSlidingController.StartingSlideTime);
        _cachedEndingSlideSpeed = UtilityFunctions.GetAnimationSpeed(endingSlideClip, _playerSlidingController.EndingSlideTime);
        _cachedForwardJumpLandingSpeed = UtilityFunctions.GetAnimationSpeed(forwardJumpingLandingClip, _playerForwardJumpingController.LandingTime, crossfadeDuration);
        _cachedStartingGroundStandJumpSpeed = UtilityFunctions.GetAnimationSpeed(startingGroundStandJumpClip, _playerJumpController.StartingGroundStandJumpTime, crossfadeDuration);
        _cachedStartingGroundRunJumpSpeed = UtilityFunctions.GetAnimationSpeed(startingGroundRunJumpClip, _playerJumpController.StartingGroundRunJumpTime, crossfadeDuration);
        _cachedStartingWallJumpSpeed = UtilityFunctions.GetAnimationSpeed(startingWallJumpClip, _playerJumpController.StartingWallJumpTime, crossfadeDuration);
        _cachedStartingRailJumpSpeed = UtilityFunctions.GetAnimationSpeed(startingRailJumpClip, _playerJumpController.StartingRailJumpTime, crossfadeDuration);
        _cachedTargetLockClipSpeed = UtilityFunctions.GetAnimationSpeed(targetLockIdleClip, targetLockClipDuration);
        _cachedLineRideClipSpeed = UtilityFunctions.GetAnimationSpeed(railLineRidingClip, railLineRidingClipDuration);
        _cachedLedgeActiveSpeed = UtilityFunctions.GetAnimationSpeed(ledgeClimbActiveAnimationClip, _playerLedgeClimbController.ActiveStateDuration);
        _cachedLedgeEndingSpeed = UtilityFunctions.GetAnimationSpeed(ledgeClimbEndingAnimationClip, _playerLedgeClimbController.EndingStateDuration, crossfadeDuration);
        _cachedLedgeActiveClipDuration = ledgeClimbActiveAnimationClip.length;
        
        _cachedRotationChangeSpeedForSlideStarting = 90.0f / Mathf.Max(0.001f, _playerSlidingController.StartingSlideTime);
        _cachedRotationChangeSpeedForSlideEnding = 90.0f / Mathf.Max(0.001f, _playerSlidingController.EndingSlideTime);
    }


    
    private void Update()
    {
        //dummy.position = hint.position;
        CalculateBaseLayerLogic();
        UpdateBodyRotation();
    }

    private void CalculateBaseLayerLogic()
    {
        
        var targetClip = fallingIdleClip;
        var targetSpeed = 1f;
        var isJumpPadParticles = false;
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
        else if (!_playerLedgeClimbController.IsStateNon)
        {
            switch (_playerLedgeClimbController.State)
            {
                case PlayerLedgeClimbController.StateEnum.Active:
                    targetClip = ledgeClimbActiveAnimationClip;
                    targetSpeed = _cachedLedgeActiveSpeed;
                    break;
                default:
                    targetClip = ledgeClimbEndingAnimationClip;
                    targetSpeed = _cachedLedgeEndingSpeed;
                    break;
            }
        }
        else if (!_playerFixedDirectionMovementController.IsStateNon)
        {
            switch (_playerFixedDirectionMovementController.State)
            {
                case PlayerFixedDirectionMovementController.StateEnum.RightWall:
                    targetClip = rightWallRunClip;
                    targetSpeed = currentSpeed / normalSpeedForWallRunAnimationSpeed;
                    break;
                case PlayerFixedDirectionMovementController.StateEnum.LeftWall:
                    targetClip = leftWallRunClip;
                    targetSpeed = currentSpeed / normalSpeedForWallRunAnimationSpeed;
                    break;
                case PlayerFixedDirectionMovementController.StateEnum.Line:
                    targetClip = railLineRidingClip;
                    targetSpeed = _cachedLineRideClipSpeed;
                    break;
            }
            
        }
        else if (!_playerJumpController.IsStateNon)
        {
            switch (_playerJumpController.State)
            {
                case PlayerJumpController.StateEnum.Starting:
                    switch (_playerJumpController.Type)
                    {
                        case PlayerJumpController.JumpTypeEnum.Standing:
                            targetClip = startingGroundStandJumpClip;
                            targetSpeed = _cachedStartingGroundStandJumpSpeed;
                            break;
                        case PlayerJumpController.JumpTypeEnum.Running:
                            targetClip = startingGroundRunJumpClip;
                            targetSpeed = _cachedStartingGroundRunJumpSpeed;
                            break;
                        case PlayerJumpController.JumpTypeEnum.Wall:
                            targetClip = startingWallJumpClip;
                            targetSpeed = _cachedStartingWallJumpSpeed;
                            break;
                        case PlayerJumpController.JumpTypeEnum.Line:
                            targetClip = startingRailJumpClip;
                            targetSpeed = _cachedStartingRailJumpSpeed;
                            break;
                    }
                    break;
                case PlayerJumpController.StateEnum.Continuing:
                    isJumpPadParticles = _playerJumpController.IsJumpPadJump;
                    switch (_playerJumpController.Type)
                    {
                        case PlayerJumpController.JumpTypeEnum.Standing:
                            targetClip = continuingGroundStandJumpClip;
                            targetSpeed = UtilityFunctions.GetAnimationSpeed(continuingGroundStandJumpClip, _playerJumpController.CurrentContinuingJumpTime, crossfadeDuration);
                            break;
                        case PlayerJumpController.JumpTypeEnum.Running:
                            targetClip = continuingGroundRunJumpClip;
                            targetSpeed = UtilityFunctions.GetAnimationSpeed(continuingGroundRunJumpClip, _playerJumpController.CurrentContinuingJumpTime, crossfadeDuration); ;
                            break;
                        case PlayerJumpController.JumpTypeEnum.Wall:
                            targetClip = continuingWallJumpClip;
                            targetSpeed = UtilityFunctions.GetAnimationSpeed(continuingWallJumpClip, _playerJumpController.CurrentContinuingJumpTime, crossfadeDuration);
                            break;
                        case PlayerJumpController.JumpTypeEnum.Line:
                            targetClip = continuingRailJumpClip;
                            targetSpeed =  UtilityFunctions.GetAnimationSpeed(continuingRailJumpClip, _playerJumpController.CurrentContinuingJumpTime, crossfadeDuration);
                            break;
                    }
                    break;
            }
        }
        else if (!_playerSlidingController.IsStateNon)
        {
            if (_playerSlidingController.State == PlayerSlidingController.StateEnum.Starting) 
            {
                targetClip = startingSlideClip;
                targetSpeed = _cachedStartingSlideSpeed;
            }
            else if (_playerSlidingController.State == PlayerSlidingController.StateEnum.InProcess) 
            {
                targetClip = slidingClip;
            }
            else 
            {
                targetClip = endingSlideClip;
                targetSpeed = _cachedEndingSlideSpeed;
                _multidirectionalMovementController.ShouldUseMultiDirectionalAnimation = 
                    _playerTargetLockController.IsLocked && currentSpeed > walkThreshold && 
                    _meleeController.State != MeleeController.MeleeStateEnum.Combo;
            }
        }
        else if (_playerSensors.IsGrounded)
        {
            if (_playerTargetLockController.IsLocked)
            {
                targetClip = targetLockIdleClip;
                targetSpeed = _cachedTargetLockClipSpeed;
                _multidirectionalMovementController.ShouldUseMultiDirectionalAnimation = 
                    currentSpeed > walkThreshold && 
                    _meleeController.State != MeleeController.MeleeStateEnum.Combo;
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
        
        _animationLayerController.AutomaticUpdateCurrentPlayable(targetClip, targetSpeed, crossfadeDuration);

        if (_playerLedgeClimbController.State == PlayerLedgeClimbController.StateEnum.Active && 
            _playerLedgeClimbController.ShouldUpdateAnimationTime)
        {
            _playerLedgeClimbController.ShouldUpdateAnimationTime = false;
            _animationLayerController.SetPlayableTime(ledgeClimbActiveAnimationClip,_playerLedgeClimbController.TimeFraction * _cachedLedgeActiveClipDuration);
        }

        if (isJumpPadParticles)
        {
            var partMain = jumpPadParticles.main;
            partMain.startColor = _playerJumpController.JumpPadColor;
            if (!jumpPadParticles.isPlaying)
            {
                jumpPadParticles.Play();
            }
        }
        else
        {
            if (jumpPadParticles.isPlaying)
            {
                jumpPadParticles.Stop();
            }
        }
    }
    
    
    
    


    private void UpdateBodyRotation()
    {
        var targetYAngle = 0f;
        var rotationSpeed = 90f;
        switch (_playerSlidingController.State)
        {
            case PlayerSlidingController.StateEnum.Starting:
                targetYAngle = 90f * _playerSlidingController.SlidingPhaseFraction;
                rotationSpeed = _cachedRotationChangeSpeedForSlideStarting * Time.deltaTime;
                break;
            case PlayerSlidingController.StateEnum.InProcess:
                targetYAngle = 90f;
                break;
            case PlayerSlidingController.StateEnum.Ending:
                targetYAngle = 90f * (1-_playerSlidingController.SlidingPhaseFraction);
                rotationSpeed = _cachedRotationChangeSpeedForSlideEnding * Time.deltaTime;
                break;
        }
        
        if (_playerSensors.IsGrounded && _playerSensors.FoundGroundNormal && _playerSlidingController.IsActiveSlidingPhase)
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
        _bodyTransform.localRotation = Quaternion.RotateTowards(
            _bodyTransform.localRotation, 
            targetRotation, 
            rotationSpeed
        );
    }

}
