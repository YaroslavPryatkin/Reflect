using UnityEngine;
using Vector3 = UnityEngine.Vector3;

[DefaultExecutionOrder(50)]
public class PlayerSlidingController : MonoBehaviour
{
    [Header("Sliding")]
    [SerializeField] private float slideSpeedHardCap = 20f;
    [SerializeField] private float slideSpeedGain = 4f;
    [SerializeField] private float slideSpeedGainMaxSpeed = 14f;
    [SerializeField] private float minSlideSpeedIfCanNotStandUp = 4f;
    [SerializeField] private float slideSpeedThreshold = 3f;
    [SerializeField] private float minimalSlideTime = 0.5f;
    [SerializeField] private float slideRechargeTime = 0.3f;
    [SerializeField] private float startingSlideTime = 0.1f;
    [SerializeField] private float endingSlideTime = 0.1f;




    private PlayerSensors _playerSensors;
    private PlayerInputController _playerInputController;
    private PlayerLandingController _playerLandingController;
    private Rigidbody rb;
    private PlayerManager _playerManager;


    public enum StateEnum
    {
        Non, Starting, InProcess, Ending
    }

    private Utility.FractionBlockingValueTimer<StateEnum> _state = StateEnum.Non;

    public StateEnum State => _state.Value;

    public float SlidingPhaseFraction => _state.TimeFraction;
    public float StartingSlideTime => startingSlideTime;
    public float EndingSlideTime => endingSlideTime;

    public bool IsActiveSlidingPhase => _state.Value == StateEnum.Starting || _state.Value == StateEnum.InProcess;
    public bool IsStateNon => _state.Value == 0;
    public float SlideSpeedHardCap => slideSpeedHardCap;
    public float MinSlideSpeedIfCanNotStandUp => minSlideSpeedIfCanNotStandUp;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _playerSensors = GetComponent<PlayerSensors>();
        _playerInputController = GetComponent<PlayerInputController>();
        _playerLandingController = GetComponent<PlayerLandingController>();
        _playerManager = GetComponent<PlayerManager>();
    }



    private void Update()
    {
        if (!_playerManager.CanSlide)
        {
            if (_state.Value != StateEnum.Non)
            {
                _state.SetForce(StateEnum.Non, slideRechargeTime);
            }
        }
        else if (IsActiveSlidingPhase && _playerSensors.HorizontalSpeed < slideSpeedThreshold && !_playerSensors.HasSomethingInTheCollider)
        {
            _state.SetForce(StateEnum.Ending, endingSlideTime);
        }
        else
        {
            if (_state.CanBeChanged)
            {
                switch (_state.Value)
                {
                    case StateEnum.Non:
                        if (_playerInputController.IsSlidePressed && _playerSensors.HorizontalSpeed >= slideSpeedThreshold)
                        {
                            _playerLandingController.InterruptLanding();
                            _state.SetForce(StateEnum.Starting, startingSlideTime);
                            ApplySlideSpeedGain();
                        }
                        break;
                    case StateEnum.Starting:
                        if (_playerInputController.IsSlidePressed || _playerSensors.HasSomethingInTheCollider)
                        {
                            _state.SetForce(StateEnum.InProcess, minimalSlideTime);
                        }
                        else
                        {
                            _state.SetForce(StateEnum.Ending, endingSlideTime);
                        }
                        break;
                    case StateEnum.InProcess:
                        if (!_playerInputController.IsSlidePressed && !_playerSensors.HasSomethingInTheCollider)
                        {
                            _state.SetForce(StateEnum.Ending, endingSlideTime);
                        }
                        break;
                    case StateEnum.Ending:
                        _state.SetForce(StateEnum.Non, slideRechargeTime);
                        break;
                }
            }
        }
    }
    
    private void ApplySlideSpeedGain()
    {
        float currentSpeed = _playerSensors.HorizontalSpeed;
        
        if (currentSpeed < slideSpeedGainMaxSpeed)
        {
            float newSpeed = Mathf.Min(currentSpeed + slideSpeedGain, slideSpeedGainMaxSpeed);
            
            var currentDir = _playerSensors.NormalizedHorizontalVelocity;
            
            rb.linearVelocity = new Vector3(currentDir.x * newSpeed, _playerSensors.Velocity.y, currentDir.z * newSpeed);
        }
    }
}