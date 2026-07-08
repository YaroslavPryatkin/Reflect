using UnityEngine;

public class PlayerJumpController : MonoBehaviour
{

    [Header("Jumping from ground")]
    [SerializeField] private float jumpStrength = 20f;
    
    [Header("Jumping from wall")]
    [SerializeField] private float wallJumpUpStrength = 25f;
    [SerializeField] private float wallJumpSideStrength = 30f;
    [SerializeField] private float wallJumpBackStrength = 10f;
    [SerializeField] private float wallJumpBackSpeedThreshold = 5f;
    
    [Header("Coyote")]
    [SerializeField] private float groundCoyoteTime = 0.2f;
    [SerializeField] private float wallCoyoteTime = 0.1f;


    [Header("Ground stand timings")]
    [SerializeField] private float startingGroundStandJumpTime = 0.1f;
    [SerializeField] private float continuingGroundStandJumpTime = 0.7f;
    [SerializeField] private float groundStandJumpRechargeTime = 0.2f;
    
    [Header("Ground run timings")]
    [SerializeField] private float startingGroundRunJumpTime = 0.1f;
    [SerializeField] private float continuingGroundRunJumpTime = 0.7f;
    [SerializeField] private float groundRunSpeedThreshold = 2f;
    [SerializeField] private float groundRunJumpRechargeTime = 0.2f;
    
    [Header("Wall jumping timings")] 
    [SerializeField] private float startingWallJumpTime = 0.1f;
    [SerializeField] private float continuingWallJumpTime = 0.7f;
    [SerializeField] private float wallJumpRechargeTime = 0.4f;
    
    [Header("General")]
    [SerializeField] private float interruptionRechargeTime = 0.1f;


    public float StartingGroundStandJumpTime => startingGroundStandJumpTime;
    public float ContinuingGroundStandJumpTime => continuingGroundStandJumpTime;
    
    public float StartingGroundRunJumpTime => startingGroundRunJumpTime;
    public float ContinuingGroundRunJumpTime => continuingGroundRunJumpTime;
    
    public float StartingWallJumpTime => startingWallJumpTime;
    public float ContinuingWallJumpTime => continuingWallJumpTime;
    
    private Rigidbody rb;
    private PlayerMovementController _playerMovementController;
    private PlayerSensors _playerSensors;
    private PlayerInputController _playerInputController;
    private PlayerForwardJumpingController _playerForwardJumpingController;
    private PlayerDashController _playerDashController;
    private PlayerLandingController _playerLandingController;
    
    
    /// <summary>
    /// 0 - cant, 1 - from ground, 2 - from wall
    /// </summary>
    private Utility.DelayedValueTimer<int> havePlaceToJumpFrom = 0;
    
    /// <summary>
    ///  0 - not jumping,
    /// 1 - starting from ground standing, 2 - continuing from ground standing,
    /// 3 - starting from ground running, 2 - continuing from ground running,
    /// 5 - starting from wall, 6 - continuing from wall
    /// </summary>
    private Utility.BlockingValueTimer<int> jumpState = 0;

    private bool jumpStartBlocked = false;
    
    /// <summary>
    ///  0 - not jumping,
    /// 1 - starting from ground standing, 2 - continuing from ground standing,
    /// 3 - starting from ground running, 2 - continuing from ground running,
    /// 5 - starting from wall, 6 - continuing from wall
    /// </summary>
    public int JumpState => jumpState.Value;

    public bool StartingJump => jumpState.Value == 1 || jumpState.Value == 3 || jumpState.Value == 5;

    public void InterruptJump()
    {
        if(jumpState!=0)
            jumpState.SetForce(0, interruptionRechargeTime);
    }
    
    private void InterruptJumpIfRequired()
    {
        bool shouldInterrupt = false;
        
        if (_playerLandingController.LandingState != 0 ||
            _playerDashController.IsDashing ||
            _playerForwardJumpingController.IsForwardJumping ||
            _playerMovementController.SlidingPhase == 1)
        {
            shouldInterrupt = true;
            //Debug.Log("Interrupt any wat=y");
        }
        else
        {
            switch (jumpState.Value)
            {
                case 2 or 4 or 6:
                    shouldInterrupt = _playerMovementController.WallRunningState != 0;
                    //Debug.Log("Interrupt specific");
                    break;
            }
        }
        
        if(shouldInterrupt && jumpState!=0)
            jumpState.SetForce(0, interruptionRechargeTime);
    }
    
    private void ChangeJumpState()
    {
        if (jumpState.CanBeChanged)
        {
            switch (jumpState.Value)
            {
                case 1:
                    rb.linearVelocity = _playerSensors.VelocityAlignedWithGround;
                    if(_playerSensors.IsGrounded && _playerSensors.FoundGroundNormal)
                        rb.AddForce(_playerSensors.GroundNormal * jumpStrength, ForceMode.Impulse);
                    else
                        rb.AddForce(Vector3.up * jumpStrength, ForceMode.Impulse);
                    
                    _playerSensors.UpdateVelocity();
                    _playerMovementController.ChangeFlagsAfterJumpFromGround();

                    jumpState.SetForce(2, continuingGroundStandJumpTime);
                    break;
                case 2:
                    jumpState.SetForce(0, groundStandJumpRechargeTime);
                    break;
                case 3:
                    rb.linearVelocity = _playerSensors.VelocityAlignedWithGround;
                    if(_playerSensors.IsGrounded && _playerSensors.FoundGroundNormal)
                        rb.AddForce(_playerSensors.GroundNormal * jumpStrength, ForceMode.Impulse);
                    else
                        rb.AddForce(Vector3.up * jumpStrength, ForceMode.Impulse);
                    
                    _playerSensors.UpdateVelocity();
                    _playerMovementController.ChangeFlagsAfterJumpFromGround();

                    jumpState.SetForce(2, continuingGroundRunJumpTime);
                    break;
                case 4:
                    jumpState.SetForce(0, groundRunJumpRechargeTime);
                    break;
                case 5:
                    rb.linearVelocity = _playerSensors.VelocityAlignedWithGround;
                    var force = Vector3.up * wallJumpUpStrength + _playerMovementController.LastWallRunNormal * wallJumpSideStrength;
                    if(_playerSensors.HorizontalSpeed >= wallJumpBackSpeedThreshold)
                        force -= _playerSensors.NormalizedHorizontalVelocity * wallJumpBackStrength;
                    rb.AddForce( force, ForceMode.Impulse);
                    
                    _playerSensors.UpdateVelocity();
                    _playerMovementController.ChangeFlagsAfterJumpFromWall();
                    
                    jumpState.SetForce(4, continuingWallJumpTime);
                    break;
                case 6:
                    jumpState.SetForce(0, wallJumpRechargeTime);
                    break;
            }
        }
    }

    private void DetectPlaceToJumpFrom()
    {
        if (_playerMovementController.WallRunningState != 0)
        {
            havePlaceToJumpFrom.Set(2, 0);
        }
        else if (_playerSensors.IsGrounded)
        {
            havePlaceToJumpFrom.Set(1, 0);
        }
        else if (havePlaceToJumpFrom.RealValue == 2)
        {
            havePlaceToJumpFrom.Set(0, wallCoyoteTime);
        }
        else if (havePlaceToJumpFrom.RealValue == 1)
        {
            havePlaceToJumpFrom.Set(0, groundCoyoteTime);
        }
    }

    private void BlockJumpStart()
    {
        jumpStartBlocked = _playerLandingController.LandingState != 0 ||
                           _playerDashController.IsDashing ||
                           _playerForwardJumpingController.IsForwardJumping ||
                           _playerMovementController.SlidingPhase == 1;
    }
    
    private void StartJump()
    {
        if (!_playerInputController.IsJumpPressed  || jumpStartBlocked || jumpState!=0 || !jumpState.CanBeChanged) return;
        
        if (havePlaceToJumpFrom == 2)
        {
            jumpState.SetForce(5, startingWallJumpTime);
            _playerInputController.ResetJumpBuffer();
        }
        else if (havePlaceToJumpFrom == 1)
        {
            if (_playerSensors.FrontGroundState == 1)
            {
                _playerForwardJumpingController.PerformForwardJump(transform.position, _playerSensors.GetForwardGroundEndPoint(), _playerSensors.FrontGroundObstacleHeight);
            }
            else
            {
                if (_playerSensors.HorizontalSpeed < groundRunSpeedThreshold)
                {
                    jumpState.SetForce(1, startingGroundStandJumpTime);
                }
                else
                {
                    jumpState.SetForce(3, startingGroundRunJumpTime);
                }
            }
            _playerInputController.ResetJumpBuffer();
        }
    }

    private void Update()
    {
        DetectPlaceToJumpFrom();
        BlockJumpStart();
        StartJump();
    }

    private void FixedUpdate()
    {
        InterruptJumpIfRequired();
        ChangeJumpState();
    }
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _playerMovementController = GetComponent<PlayerMovementController>();
        _playerSensors = GetComponentInChildren<PlayerSensors>();
        _playerInputController = GetComponentInChildren<PlayerInputController>();
        _playerForwardJumpingController = GetComponentInChildren<PlayerForwardJumpingController>();
        _playerDashController =  GetComponentInChildren<PlayerDashController>();
        _playerLandingController = GetComponentInChildren<PlayerLandingController>();
    }
}
