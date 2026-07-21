using System;
using UnityEngine;

public class PlayerFixedDirectionMovementController : MonoBehaviour
{
    [Header("Speed settings")] 
    [SerializeField] private float startingSpeedThreshold = 0.5f;
    [SerializeField] private float endingSpeedThreshold = 2f;
    [Header("Wall Running")] 
    [SerializeField] private float angleToStartWallRun = 0.3f;
    [SerializeField] private float angleToFinishWallRun = 0.5f;
    
    
    [Header("Snapping")]
    [SerializeField] private float targetDistanceFromWall = 0.5f;
    [SerializeField] private float targetDistanceRailLine = 0.6f;
    [SerializeField] private float distanceError = 0.05f;
    [SerializeField] private float rotationError = 0.05f;

    [Header("Suppress")] 
    [SerializeField] private float suppressAfterInterruption = 0.5f;


    private Rigidbody _rb;
    private PlayerManager _playerManager;
    private PlayerSensors _playerSensors;
    private PlayerInputController _playerInputController;
    private PlayerJumpController _playerJumpController;
    
    public enum StateEnum
    {
        Non, LeftWall, RightWall, Line
    }

    public StateEnum State => _state.Value;
    public bool IsStateNon => _state.Value == StateEnum.Non;
    
    private readonly Utility.BlockingValueTimer<StateEnum> _state = StateEnum.Non;

    private bool _shouldStopBecauseOfSpeed = true;

    public Vector3 LastNormal { get; private set; } = Vector3.zero;
    private float _targetDistance=0f;

    private void Awake()
    {
        _playerManager = GetComponent<PlayerManager>();
        _playerSensors = GetComponent<PlayerSensors>();
        _playerInputController = GetComponent<PlayerInputController>();
        _playerJumpController = GetComponent<PlayerJumpController>();
        _rb = GetComponent<Rigidbody>();
    }
    private void Update()
    {
        HandleWallRunLogic();
    }

    private void HandleWallRunLogic()
    {
        if (!_playerManager.CanFixedMovement)
        {
            _state.SetForce(StateEnum.Non, 0);
            return;
        }
        
        
        var inputMoveVector = _playerInputController.InputMoveVector;
        var currentSpeed = _playerSensors.HorizontalSpeed;
        
        if (currentSpeed >= endingSpeedThreshold)
            _shouldStopBecauseOfSpeed = true;

        switch (_state.Value)
        {
            case StateEnum.Non:
                if (_state.CanBeChanged && currentSpeed >= startingSpeedThreshold)
                {
                    if (_playerSensors.IsRailLine)
                    {
                        _shouldStopBecauseOfSpeed = false;
                        _state.SetForce(StateEnum.Line, 0);
                        
                        
                        LastNormal = -_playerSensors.RailLine.up;
                        _targetDistance = targetDistanceRailLine;
                    }
                    else if (_playerSensors.IsLeftWallRun && 
                             Vector3.Dot(inputMoveVector, _playerSensors.LeftWallRunNormal) < angleToStartWallRun)
                    {
                        _shouldStopBecauseOfSpeed = false;
                        _state.SetForce(StateEnum.LeftWall, 0);
                        LastNormal = _playerSensors.LeftWallRunNormal;
                        _targetDistance = targetDistanceFromWall;
                    }
                    else if (_playerSensors.IsRightWallRun &&
                             Vector3.Dot(inputMoveVector, _playerSensors.RightWallRunNormal) < angleToStartWallRun)
                    {
                        _shouldStopBecauseOfSpeed = false;
                        _state.SetForce(StateEnum.RightWall, 0);
                        LastNormal = _playerSensors.RightWallRunNormal;
                        _targetDistance = targetDistanceFromWall;
                    }
                }
                break;
            case StateEnum.LeftWall:
                if (!_playerSensors.IsLeftWallRun)
                {
                    _state.SetForce(StateEnum.Non, 0);
                }
                else if (
                    Vector3.Dot(inputMoveVector, _playerSensors.LeftWallRunNormal) >= angleToFinishWallRun ||
                    (_shouldStopBecauseOfSpeed && currentSpeed < endingSpeedThreshold)
                )
                {
                    _state.SetForce(StateEnum.Non, suppressAfterInterruption);
                }
                else
                {
                    LastNormal = _playerSensors.LeftWallRunNormal;
                }
                break;
            case StateEnum.RightWall:
                if (!_playerSensors.IsRightWallRun)
                {
                    _state.SetForce(StateEnum.Non, 0);
                }
                else if (
                    Vector3.Dot(inputMoveVector, _playerSensors.RightWallRunNormal) >= angleToFinishWallRun || 
                    (_shouldStopBecauseOfSpeed && currentSpeed < endingSpeedThreshold)
                )
                {
                    _state.SetForce(StateEnum.Non, suppressAfterInterruption);
                }
                else
                {
                    LastNormal = _playerSensors.RightWallRunNormal;
                }
                break;
            case StateEnum.Line:
                if (!_playerSensors.IsRailLine)
                {
                    _state.SetForce(StateEnum.Non, 1f);
                    if(!_playerSensors.IsGrounded)
                        _playerJumpController.PerformLineJump();
                }
                else if (_shouldStopBecauseOfSpeed && currentSpeed < endingSpeedThreshold)
                {
                    _state.SetForce(StateEnum.Non, suppressAfterInterruption);
                }
                else
                {
                    LastNormal = -_playerSensors.RailLine.up;
                }
                break;
        }
    }

    private Vector3 GetOriginPoint()
    {
        return _state.Value switch
        {
            StateEnum.Line => Utility.ProjectPointOnLine(transform, _playerSensors.RailLine),
            StateEnum.LeftWall => _playerSensors.LeftWallRunPoint,
            StateEnum.RightWall => _playerSensors.RightWallRunPoint,
            _ => Vector3.zero
        };
    }

    public Vector3 GetMovementDirection()
    {
        return _state.Value switch
        {
            StateEnum.Line => _playerSensors.RailLineForward,
            StateEnum.LeftWall => Vector3.Cross(Vector3.up, -LastNormal),
            StateEnum.RightWall => Vector3.Cross(Vector3.up, LastNormal),
            _ => Vector3.zero
        };
    }
    
    
    public void SuppressAfterJump(float time)
    {
        _state.SetForce(StateEnum.Non, time);
    }
    
    public void SnapToPlace(float targetSpeed, Vector3 targetDir)
    {
        var targetPos = GetOriginPoint() + LastNormal * _targetDistance;
        if (Vector3.Distance(transform.position, targetPos) > distanceError)
        {
            //Debug.Log("Snapping distance, dist = " + dist + ", target dist = " + _targetDistance + ", error = " + Mathf.Abs(dist - _targetDistance) +" / "+ maxError);
            transform.position = targetPos;
        }
        
        if ( targetSpeed > 0.001f && 
             Vector3.Dot(_playerSensors.NormalizedHorizontalVelocity, targetDir) < 1 - rotationError)
        {
            var realTargetDir = Vector3.ProjectOnPlane(targetDir, LastNormal);
            realTargetDir.y = 0;
            realTargetDir = realTargetDir.normalized;
            _rb.linearVelocity = realTargetDir * _playerSensors.Speed;
            _rb.rotation = Quaternion.LookRotation(realTargetDir);
            _playerSensors.UpdateVelocity();
        }
    }

    public void SnapSpeed(Vector3 targetDir)
    {
        _rb.linearVelocity = targetDir * _playerSensors.Speed;
        _rb.rotation = Quaternion.LookRotation(targetDir);
        _playerSensors.UpdateVelocity();
    }
    
}
