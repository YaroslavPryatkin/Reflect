using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class AnimationController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private GameObject body;

    [Header("Movement Clips")]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip walkClip;
    [SerializeField] private AnimationClip runClip;

    [Header("Action Clips")]
    [SerializeField] private AnimationClip dashClip;
    [SerializeField] private AnimationClip jumpClip;
    [SerializeField] private AnimationClip fallingIdleClip;
    [SerializeField] private AnimationClip rollingClip;

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

    [Header("Crossfade doration")]
    [SerializeField] private float crossfadeDuration = 0.15f;
    
    [Header("Settings & Thresholds for basic movement")]
    [SerializeField] private float walkThreshold = 0.2f;
    [SerializeField] private float normalSpeedForWalkAnimationSpeed = 3f;
    [SerializeField] private float runThreshold = 8f;
    [SerializeField] private float normalSpeedForRunAnimationSpeed = 8f;
    [SerializeField] private float normalSpeedForWallRunAnimationSpeed = 12f;
    
    [Header("Other Settings & Thresholds")]
    [SerializeField] private float rollingAnimationExitOffset = 0.05f;

    // --- References ---
    private Animator animator;
    private Transform bodyTransform;
    private PlayerSensors playerSensors;
    private MovementController movementController;
    private PlayerForwardJumpingController playerForwardJumpingController;
    private PlayerDashController playerDashController;

    // --- Playables Engine ---
    private PlayableGraph _graph;
    private AnimationMixerPlayable _mixer;
    private AnimationClip _currentClip;
    private int _currentPort = 0;
    private bool _isTransitioning;
    private float _transitionTimer;

    // --- Caches & Optimization ---
    private float _currentClipSpeed = -1f;
    private Quaternion _defaultBodyLocalRot;
    
    private float _cachedRollingSpeed;
    private float _cachedJumpSpeed;
    private float _cachedStartingSlideSpeed;
    private float _cachedEndingSlideSpeed;
    private float _cachedForwardLandingSpeed;
    private float _cachedRotationChangeSpeedForSlideStarting;
    private float _cachedRotationChangeSpeedForSlideEnding;

    private Utility.TemporaryValue<bool> jumpingAnimationBuffer = new(false, true);

    public void HandleJump()
    {
        jumpingAnimationBuffer.Activate(movementController.MaximalJumpDuration);
    }

    private void Awake()
    {
        animator = body.GetComponent<Animator>();
        bodyTransform = body.GetComponent<Transform>();
        playerSensors = GetComponent<PlayerSensors>();
        movementController = GetComponent<MovementController>();
        playerForwardJumpingController = GetComponent<PlayerForwardJumpingController>();
        playerDashController = GetComponent<PlayerDashController>();

        _defaultBodyLocalRot = bodyTransform.localRotation;

        InitPlayableGraph();
    }

    private void Start()
    {
        _cachedRollingSpeed = rollingClip.length / Mathf.Max(0.001f, movementController.RollDuration + rollingAnimationExitOffset);
        _cachedJumpSpeed = jumpClip.length / Mathf.Max(0.001f, movementController.MaximalJumpDuration);
        _cachedStartingSlideSpeed = startingSlideClip.length / Mathf.Max(0.001f, movementController.StartingSlideTime);
        _cachedEndingSlideSpeed = endingSlideClip.length / Mathf.Max(0.001f, movementController.EndingSlideTime);
        _cachedForwardLandingSpeed = forwardJumpingLandingClip.length / Mathf.Max(0.001f, playerForwardJumpingController.LandingTime);

        _cachedRotationChangeSpeedForSlideStarting = 90.0f / Mathf.Max(0.001f, movementController.StartingSlideTime);
        _cachedRotationChangeSpeedForSlideEnding = 90.0f / Mathf.Max(0.001f, movementController.EndingSlideTime);
    }

    private void InitPlayableGraph()
    {
        _graph = PlayableGraph.Create("DirectAnimationGraph");
        _mixer = AnimationMixerPlayable.Create(_graph, 2);
        
        var output = AnimationPlayableOutput.Create(_graph, "Animation", animator);
        output.SetSourcePlayable(_mixer);
        _graph.Play();
    }

    private void Update()
    {
        UpdateJumpLogic();
        UpdateStateLogic();
        UpdateCrossfade();
        UpdateBodyRotation();
    }

    private void UpdateJumpLogic()
    {
        if (movementController.IsHighJump && jumpingAnimationBuffer)
        {
            jumpingAnimationBuffer.Deactivate();
        }
    }

    private void UpdateStateLogic()
    {
        AnimationClip targetClip = idleClip;
        float targetSpeed = 1f;
        float currentSpeed = playerSensors.HorizontalSpeed;

        if (playerDashController.IsDashing)
        {
            targetClip = dashClip;
        }
        else if (movementController.IsRolling)
        {
            targetClip = rollingClip;
            targetSpeed = _cachedRollingSpeed;
        }
        else if (playerForwardJumpingController.IsForwardJumping)
        {
            if (playerForwardJumpingController.IsLanding)
            {
                targetClip = forwardJumpingLandingClip;
                targetSpeed = _cachedForwardLandingSpeed;
            }
            else
            {
                targetClip = forwardJumpingInAirClip;
                targetSpeed = forwardJumpingInAirClip.length / Mathf.Max(0.001f, playerForwardJumpingController.TimeToJump); 
            }
        }
        else if (movementController.WallRunningState != 0)
        {
            targetClip = movementController.WallRunningState > 0 ? rightWallRunClip : leftWallRunClip;
            targetSpeed = currentSpeed / normalSpeedForWallRunAnimationSpeed;
        }
        else if (jumpingAnimationBuffer)
        {
            targetClip = jumpClip;
            targetSpeed = _cachedJumpSpeed;
        }
        else if (movementController.SlidingPhase != 0)
        {
            if (movementController.SlidingPhase == 1) 
            {
                targetClip = startingSlideClip;
                targetSpeed = _cachedStartingSlideSpeed;
            }
            else if (movementController.SlidingPhase == 2) 
            {
                targetClip = slidingClip;
            }
            else 
            {
                targetClip = endingSlideClip;
                targetSpeed = _cachedEndingSlideSpeed;
            }
        }
        else if (movementController.IsHighJump)
        {
            targetClip = fallingIdleClip;
        }
        else if (playerSensors.IsGrounded)
        {
            if (currentSpeed > runThreshold)
            {
                targetClip = runClip;
                targetSpeed = currentSpeed / normalSpeedForRunAnimationSpeed;
            }
            else if (currentSpeed > walkThreshold)
            {
                targetClip = walkClip;
                targetSpeed = currentSpeed / normalSpeedForWalkAnimationSpeed;
            }
        }
        else
        {
            targetClip = fallingIdleClip; 
        }

        PlayClip(targetClip, targetSpeed);
    }

    private void PlayClip(AnimationClip newClip, float speed)
    {
        if (_currentClip == newClip)
        {
            _currentClipSpeed = speed;
            var currentPlayable = _mixer.GetInput(_currentPort);
            if (currentPlayable.IsValid()) currentPlayable.SetSpeed(speed);
            
            return;
        }

        _currentClip = newClip;
        _currentClipSpeed = speed;
        int newPort = 1 - _currentPort;

        var oldPlayable = _mixer.GetInput(newPort);
        if (oldPlayable.IsValid()) _graph.DestroySubgraph(oldPlayable);

        var newPlayable = AnimationClipPlayable.Create(_graph, newClip);
        newPlayable.SetSpeed(speed);
        _graph.Connect(newPlayable, 0, _mixer, newPort);

        _transitionTimer = 0f;
        _isTransitioning = crossfadeDuration > 0f;

        if (!_isTransitioning)
        {
            _mixer.SetInputWeight(newPort, 1f);
            _mixer.SetInputWeight(_currentPort, 0f);
            _currentPort = newPort;
        }
    }

    private void UpdateCrossfade()
    {
        if (!_isTransitioning) return;

        _transitionTimer += Time.deltaTime;
        float weight = Mathf.Clamp01(_transitionTimer / crossfadeDuration);
        int newPort = 1 - _currentPort;

        _mixer.SetInputWeight(newPort, weight);
        _mixer.SetInputWeight(_currentPort, 1f - weight);

        if (weight >= 1f)
        {
            _isTransitioning = false;
            
            var previousPlayable = _mixer.GetInput(_currentPort);
            if (previousPlayable.IsValid()) _graph.DestroySubgraph(previousPlayable);
            
            _currentPort = newPort;
        }
    }

    private void UpdateBodyRotation()
    {
        var targetAngle = 0f;
        var rotationSpeed = 90f;
        switch (movementController.SlidingPhase)
        {
            case 1:
                targetAngle = 90f * movementController.SlidingPhaseFraction;
                rotationSpeed = _cachedRotationChangeSpeedForSlideStarting * Time.deltaTime;
                break;
            case 2:
                targetAngle = 90f;
                break;
            case 3:
                targetAngle = 90f * (1-movementController.SlidingPhaseFraction);
                rotationSpeed = _cachedRotationChangeSpeedForSlideEnding * Time.deltaTime;
                break;
        }

        Quaternion targetRotation = _defaultBodyLocalRot * Quaternion.Euler(0, targetAngle, 0);
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
