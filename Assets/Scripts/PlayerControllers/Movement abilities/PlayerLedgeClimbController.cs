using System;
using UnityEngine;
using UnityEngine.Shaders;

public class PlayerLedgeClimbController : MonoBehaviour
{
    [Header("Starting")]
    [SerializeField] private float maxAngleToStart;

    [Header("Sizes")] 
    [SerializeField] private float animationHighHandHeight;
    [SerializeField] private float distanceFromWallDuringActiveState=0.4f;
    [SerializeField] private float moveForwardDistanceDuringEnding;
    [SerializeField] private float animationLowHandHeightShiftFromColliderLow;
    
    [Header("Timing")] 
    [SerializeField] private float activeStateDuration;
    [SerializeField] private float endingStateDuration;


    private PlayerSensors _playerSensors;
    private PlayerInputController _playerInputController;
    private PlayerManager _playerManager;
    private Rigidbody _rb;
    
    public enum StateEnum
    {
        Non, Active, Ending
    }

    private readonly UtilityClasses.FractionBlockingValueTimer<StateEnum> _state = StateEnum.Non;

    private Vector3 _horizontalDir;
    private Vector3 _obstaclePoint;
    private Vector3 _endVelocity;
    private bool _wasKinematic;



    public bool IsStateNon => _state.Value == StateEnum.Non;
    public StateEnum State => _state.Value;
    public float TimeFraction => _state.TimeFraction;
    
    public float ActiveStateDuration => activeStateDuration;
    public float EndingStateDuration => endingStateDuration;
    
    private float _handHeightDiff;
    private float _endingSpeed;

    public bool ShouldUpdateAnimationTime { get; set; } = false;

    private void Awake()
    {
        _playerSensors = GetComponentInParent<PlayerSensors>();
        _rb=GetComponent<Rigidbody>();
        _playerInputController = GetComponent<PlayerInputController>();
        _playerManager = GetComponent<PlayerManager>();
        animationLowHandHeightShiftFromColliderLow -= _playerSensors.ColliderHalfHeight;
        _handHeightDiff = animationHighHandHeight - animationLowHandHeightShiftFromColliderLow;
        _endingSpeed = moveForwardDistanceDuringEnding /endingStateDuration;
    }

    private void Update()
    {
        if (_state.CanBeChanged)
        {
            switch (_state.Value)
            {
                case StateEnum.Non:
                    if (_playerSensors.LedgeState == PlayerSensors.FrontGroundStateEnum.Ledge && 
                        _playerManager.CanClimbLedge && _playerInputController.IsPlayerPressingWASD)
                    {
                        var fraction = (animationHighHandHeight - _playerSensors.LedgeObstacleHeight)/_handHeightDiff;
                        if (fraction is >=0f and <=1f)
                        {
                            _horizontalDir = -_playerSensors.LedgeNormal;
                            _horizontalDir.y = 0f;
                            var angle = Vector3.Angle(_horizontalDir, _playerInputController.InputMoveVector);
                            if (angle <= maxAngleToStart)
                            { 
                                _rb.angularVelocity = Vector3.zero;
                                _rb.rotation = Quaternion.LookRotation(_horizontalDir, Vector3.up);
                                
                                var movePlaneNormal = Vector3.Cross(_horizontalDir, Vector3.up);
                                _endVelocity = Vector3.ProjectOnPlane(_playerSensors.HorizontalVelocity, movePlaneNormal);

                                _obstaclePoint = _playerSensors.LedgeObstaclePoint;
                                _obstaclePoint.y = transform.position.y + _playerSensors.LedgeObstacleHeight;
                                
                                _wasKinematic = _rb.isKinematic;
                                _rb.isKinematic = true;

                                ShouldUpdateAnimationTime = true;
                                _state.SetForce(StateEnum.Active, activeStateDuration, fraction);
                            }
                        }
                    }
                    break;
                case StateEnum.Active:
                    ShouldUpdateAnimationTime = false;
                    _rb.isKinematic = _wasKinematic;
                    _state.SetForce(StateEnum.Ending, endingStateDuration);
                    break;
                case StateEnum.Ending:
                    _rb.linearVelocity = _endVelocity;
                    _state.SetForce(StateEnum.Non);
                    break;
            }
        }


        switch (_state.Value)
        {
            case StateEnum.Active:
                var vertDist = animationHighHandHeight - _state.TimeFraction * _handHeightDiff;
                var pos = _obstaclePoint - _horizontalDir * distanceFromWallDuringActiveState -
                          Vector3.up * vertDist;
               _rb.MovePosition(pos);
                break;
            case StateEnum.Ending:
                _rb.linearVelocity = _horizontalDir * _endingSpeed;
                break;
        }
    }
}
