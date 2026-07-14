using UnityEngine;

public class PlayerLandingController : MonoBehaviour
{

    [Header("Standing")] 
    [SerializeField] private float standLandingDuration = 0.3f;
    
    [Header("Running")] 
    [SerializeField] private float runLandingDuration = 0.3f;
    [SerializeField] private float runLandingHorizontalSpeedThreshold = 2f;
    
    [Header("Rolling")] 
    [SerializeField] private float rollLandingDuration = 0.5f;
    [SerializeField] private float rollLandingMinimalHorizontalSpeedToSet = 6f;
    [SerializeField] private float rollLandingVerticalSpeedThreshold = 8f;
    
    public float StandLandingDuration => standLandingDuration;
    public float RunLandingDuration => runLandingDuration;
    public float RollLandingDuration => rollLandingDuration;
    
    public float RollLandingMinimalSpeed => rollLandingMinimalHorizontalSpeedToSet;
    
    private PlayerSensors _playerSensors;
    private PlayerJumpController _playerJumpController;
    private PlayerForwardJumpingController _playerForwardJumpingController;
    private Rigidbody rb;
    
    
    /// <summary>
    /// 0 - not landing, 1 - standing landing, 2 - running landing, 3 - rolling landing
    /// </summary>
    private Utility.TemporaryValue<int> landingState = 0;
    private bool wasOnGround = true;
    
    /// <summary>
    /// 0 - not landing, 1 - standing landing, 2 - running landing, 3 - rolling landing
    /// </summary>
    public int LandingState => landingState.Value;

    public bool CanBeInterrupted => landingState.Value != 3;
    
    public void InterruptLanding()
    {
        landingState.Deactivate();
    }
    
    public void ToCallFromMovementController()
    {
        if (!_playerSensors.IsGrounded)
        {
            wasOnGround = false;
            landingState.Deactivate();
        }
        else if (!wasOnGround)
        {
            wasOnGround =  true;
            if (_playerForwardJumpingController.IsForwardJumping) return;
            
            _playerJumpController.InterruptJump();

            var normalSpeed = -_playerSensors.Velocity.y;

            if (_playerSensors.FoundGroundNormal)
            {
                normalSpeed = Vector3.Dot(_playerSensors.Velocity, -_playerSensors.GroundNormal);
            }
            
            
            if (normalSpeed >= rollLandingVerticalSpeedThreshold)
            {
                if (_playerSensors.HorizontalSpeed < rollLandingMinimalHorizontalSpeedToSet)
                {
                    var newHorizontalVelocity = _playerSensors.NormalizedHorizontalVelocity *
                                                rollLandingMinimalHorizontalSpeedToSet;
                    rb.linearVelocity = new Vector3(newHorizontalVelocity.x, _playerSensors.Velocity.y,
                        newHorizontalVelocity.z);
                    _playerSensors.UpdateVelocity();
                }
                
                landingState.Activate(3,rollLandingDuration);
            }
            else
            {
                if (_playerSensors.HorizontalSpeed > runLandingHorizontalSpeedThreshold)
                {
                    landingState.Activate(2,runLandingDuration);
                }
                else
                {
                    landingState.Activate(1,standLandingDuration);
                }
            }
        }
    }

    private void Awake()
    {
        _playerSensors = GetComponent<PlayerSensors>();
        _playerJumpController = GetComponent<PlayerJumpController>();
        _playerForwardJumpingController = GetComponent<PlayerForwardJumpingController>();
        rb = GetComponent<Rigidbody>();
    }
}
