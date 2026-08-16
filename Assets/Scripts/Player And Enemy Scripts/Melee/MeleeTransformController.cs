using System;
using UnityEngine;

/// <summary>
/// This requires refactoring, but it is working and i dont want to bother
/// </summary>
public abstract class MeleeTransformController : MonoBehaviour
{
    [SerializeField] private float parabolaTargetFollowingSpeed = 20f;
    [SerializeField] private float targetDistanceToTarget = 0.9f;
    [SerializeField] private float targetSnapDistanceToTarget = 0.9f;
    public bool IsActive { get; private set; } = false;
    private readonly UtilityClasses.ChangeableFractionValueReference _isActiveTimer = new();
    
    public bool IsControllingMovement { get;private set; } = false;
    private bool _forceLookToTarget;
    
    private Vector3 _targetPosition;
    private Vector3 _targetDirection;
    private Vector3 _targetLookDirection;
    private bool _haveTarget;

    private enum Mode
    {
        Move, Standing, Parabola, Snap, MoveThrough
    }
    
    private Mode _mode = Mode.Move;
    
    
    /// <summary>
    /// different purpose for different modes <br/>
    /// move or moveThrough - speed multiplier<br/>
    /// parabola - angle up
    /// </summary>
    private float _currentFloatValue;
    /// <summary>
    /// different purpose for different modes <br/>
    /// move or moveThrough - speed curve<br/>
    /// parabola and snap - distance curve
    /// </summary>
    private AnimationCurve _currentCurve;
    /// <summary>
    /// different purpose for different modes <br/>
    /// parabola - target position on start<br/>
    /// snap - start transform pos
    /// </summary>
    private Vector3 _currentVector3Value;
    
    private float _rotationSpeed;
    private bool _wasKinematic=false;

    private LayerMask _wasExcludeLayers;
    
    private readonly UtilityClasses.ParabolaCurve _parabolaCurve = new();
    
    private Sensors _sensors;
    private Rigidbody _rb;
    
    /// <summary>
    /// Called if <br/> forceLookAtTarget == false or DoesHaveTarget() == false
    /// </summary>
    /// <param name="targetLookDirection">Should be normalized</param>
    protected abstract void SetTargetLookDirection(out Vector3 targetLookDirection);
    
    protected abstract bool DoesHaveTarget();
    
    /// <param name="targetPosition">World position</param>
    /// <param name="targetDirection">Should be normalized</param>
    protected abstract void SetTargetPositionAndDirection(out Vector3 targetPosition, out Vector3 targetDirection);

    public void ActivateMove(
        float transformSpeedMultiplier,
        AnimationCurve speedCurve,
        float rotationSpeed,
        bool forceLookToTarget,
        UtilityClasses.FractionTemporaryValue<bool> moveRequest)
    {
        if (IsActive)
        {
            InterruptMove();
        }
        _wasKinematic = _rb.isKinematic;
        _wasExcludeLayers = _rb.excludeLayers;
        
        _isActiveTimer.Set(moveRequest);
        IsActive = true;
        _mode = Mode.Move;
        
        _currentFloatValue = transformSpeedMultiplier;
        _currentCurve = speedCurve;
        _rotationSpeed = rotationSpeed;
        _forceLookToTarget= forceLookToTarget;
        IsControllingMovement = true;
        _haveTarget = false;
      
    }
    
    public void ActivateMoveThrough(
        float transformSpeedMultiplier,
        AnimationCurve speedCurve,
        float rotationSpeed,
        float angleHorizontal,
        bool forceLookToTarget,
        UtilityClasses.FractionTemporaryValue<bool> moveRequest)
    {
        if (IsActive)
        {
            InterruptMove();
        }
        _wasKinematic = _rb.isKinematic;
        _wasExcludeLayers = _rb.excludeLayers;


        if (forceLookToTarget && (_haveTarget = DoesHaveTarget()))
        {
            SetTargetPositionAndDirection(out _targetPosition, out _targetDirection);
            _targetLookDirection = _targetDirection;
        }
        else
        {
            SetTargetLookDirection(out _targetLookDirection);
        }

        _targetLookDirection = Quaternion.AngleAxis(angleHorizontal, Vector3.up) * _targetLookDirection;
        
        _isActiveTimer.Set(moveRequest);
        IsActive = true;
        _mode = Mode.MoveThrough;
        
        _currentFloatValue = transformSpeedMultiplier;
        _currentCurve = speedCurve;
        _rotationSpeed = rotationSpeed;
        _forceLookToTarget = forceLookToTarget;
        IsControllingMovement = true;
        
        _rb.excludeLayers = _wasExcludeLayers | _sensors.EnemyLayer;
        //Debug.Log("<color="+GetColor()+">Starting moving</color>");
    }
    
    public void ActivateStanding(
        float rotationSpeed,
        bool forceLookToTarget,
        bool standInPlace,
        UtilityClasses.FractionTemporaryValue<bool> moveRequest)
    {
        if (IsActive)
        {
            //Debug.Log("<color="+GetColor()+">Ending previous " + _type + "</color>");
            InterruptMove();
        }
        _wasKinematic = _rb.isKinematic;
        _wasExcludeLayers = _rb.excludeLayers;
        
        _isActiveTimer.Set(moveRequest);
        IsActive = true;
        _mode = Mode.Standing;
        
        _rotationSpeed = rotationSpeed;
        _forceLookToTarget= forceLookToTarget;
        IsControllingMovement = standInPlace;
        //Debug.Log("<color="+GetColor()+">Starting standing</color>");
    }
    
    public bool ActivateParabola(
        float angleUp,
        AnimationCurve distanceCurve,
        UtilityClasses.FractionTemporaryValue<bool> moveRequest)
    {
        if (IsActive)
        {
            //Debug.Log("<color="+GetColor()+">Ending previous " + _type + "</color>");
            InterruptMove();
        }
        _wasKinematic = _rb.isKinematic;
        _wasExcludeLayers = _rb.excludeLayers;
        
        
        
        _haveTarget = DoesHaveTarget();
        
        if (!_haveTarget)
        {
            return false;
        }
        
        SetTargetPositionAndDirection(out _currentVector3Value, out _targetDirection);

        _isActiveTimer.Set(moveRequest);
        
        IsActive = true;
        _mode = Mode.Parabola;

        _currentFloatValue = angleUp;
        _currentCurve = distanceCurve;
        IsControllingMovement = true;
        _parabolaCurve.StartPos = transform.position;
        _rotationSpeed = 30;
        
        _rb.isKinematic = true;
        return true;
        //Debug.Log("<color="+GetColor()+">Starting parabola</color>");
    }

    public bool ActivateSnapping(
        bool forceLookToTarget,
        AnimationCurve distanceCurve,
        UtilityClasses.FractionTemporaryValue<bool> moveRequest)
    {
        
        if (IsActive)
        {
            InterruptMove();
        }
        _wasKinematic = _rb.isKinematic;
        _wasExcludeLayers = _rb.excludeLayers;

        _haveTarget = DoesHaveTarget();
        
        if (!_haveTarget)
        {
            Debug.Log("No snap" + ", time = " + Time.frameCount);
            return false;
        }
        
        
        _isActiveTimer.Set(moveRequest);
        IsActive = true;
        _mode = Mode.Snap;

        
        SetTargetPositionAndDirection(out _targetPosition, out _targetDirection);
        
        _currentCurve = distanceCurve;
        _currentVector3Value = transform.position;
        
        _rotationSpeed = 400;
        _forceLookToTarget = forceLookToTarget;
        IsControllingMovement = true;
        
        
        _rb.isKinematic = true;
        return true;
    }
    
    public void StopMovingAndClearReferences()
    {
        _isActiveTimer.Unset();
    }

    private void UpdateFlags()
    {
        if (!IsActive) return;
        
        if (!_isActiveTimer.Value)
        {
            //Debug.Log("<color="+GetColor()+">Ending for timer ran out</color>");
            InterruptMove();
            return;
        }

        if (!_sensors.IsGrounded && (_mode is Mode.Move or Mode.Snap or Mode.MoveThrough))
        { 
            InterruptMove();
            return;
        }

        switch (_mode)
        {
            case Mode.Parabola:
                if (!DoesHaveTarget())
                {
                    InterruptMove();
                    return;
                }
                SetTargetPositionAndDirection(out _targetPosition, out _targetDirection);
                _targetLookDirection = _targetDirection;
                break;
            case Mode.Move or Mode.Standing:
                if (DoesHaveTarget())
                {
                    _haveTarget = true;
                    SetTargetPositionAndDirection(out _targetPosition, out _targetDirection);
                    if (_forceLookToTarget)
                    {
                        _targetLookDirection = _targetDirection;
                    }
                    else
                    {
                        SetTargetLookDirection(out _targetLookDirection);
                    }
                }
                else
                {
                    _haveTarget = false;
                    SetTargetLookDirection(out _targetLookDirection);
                }
                break;
            case Mode.Snap:
                if (!DoesHaveTarget())
                {
                    InterruptMove();
                    return;
                }
                SetTargetPositionAndDirection(out _targetPosition, out _targetDirection);
                if (_forceLookToTarget)
                {
                    _targetLookDirection = _targetDirection;
                }
                else
                {
                    SetTargetLookDirection(out _targetLookDirection);
                }
                break;
        }
    }

    public void InterruptMove()
    {
        if (IsActive)
        {
            _rb.isKinematic = _wasKinematic;
            IsActive = false;
            _rb.excludeLayers = _wasExcludeLayers;
            
            if (_mode is Mode.Move or Mode.MoveThrough)
                _rb.linearVelocity = Vector3.zero;
        }
    }
    
    private void Move()
    {
        var endPos = Vector3.zero;

        switch (_mode)
        {
            case Mode.Parabola or Mode.Move:
                endPos = _targetPosition - _targetDirection * targetDistanceToTarget;
                if (Vector3.Distance(transform.position, endPos) < 0.1f || 
                    Vector3.Distance(transform.position, _targetPosition) < targetDistanceToTarget)
                {
                    InterruptMove();
                    return;
                }
                break;
        }
        
        switch (_mode)
        {
            case Mode.Standing:
                _rb.linearVelocity=Vector3.zero;
                break;
            case Mode.Parabola:
                _currentVector3Value = Vector3.MoveTowards(_currentVector3Value, endPos,
                    parabolaTargetFollowingSpeed * Time.fixedDeltaTime);
                _parabolaCurve.EndPos = _currentVector3Value;
                _parabolaCurve.MakeStartAngleParabola(_currentFloatValue);
                _parabolaCurve.CalculateTotalLength();
                _rb.MovePosition(
                        _parabolaCurve.GetPositionByDistance(
                            _currentCurve.Evaluate(_isActiveTimer.TimeFraction)
                        )
                        );
                break;
            case Mode.Snap:
                if (Mathf.Abs(_targetDirection.y) > 0.995f)
                {
                    _targetDirection = transform.forward;
                }
                else
                {
                    _targetDirection.y = 0f;
                    _targetDirection = _targetDirection.normalized;
                }
                endPos =  _targetPosition - _targetDirection * targetSnapDistanceToTarget;
                
                _rb.MovePosition(
                    UtilityFunctions.LerpByDistance(
                        _currentVector3Value, endPos,
                        _currentCurve.Evaluate(_isActiveTimer.TimeFraction)
                        )
                    );
                break;
            case Mode.Move:
                if (_haveTarget)
                {
                    var res = transform.forward * (
                        _currentFloatValue * _currentCurve.Evaluate(_isActiveTimer.TimeFraction));
                    if (Vector3.Distance(_targetPosition, transform.position + res) < targetDistanceToTarget)
                    {
                        res = Vector3.ProjectOnPlane(res, _targetDirection);
                    }
                    _rb.linearVelocity = res;
                }
                else
                {
                    _rb.linearVelocity = transform.forward * (
                        _currentFloatValue * _currentCurve.Evaluate(_isActiveTimer.TimeFraction));
                }
                break;
            case Mode.MoveThrough:
                _rb.linearVelocity = transform.forward * (
                    _currentFloatValue * _currentCurve.Evaluate(_isActiveTimer.TimeFraction));
                break;
        }
        _sensors.UpdateVelocity();
    }

    private void Rotate()
    {
        var rot = Vector3.RotateTowards(transform.forward,
            _targetLookDirection, 
            _rotationSpeed * Time.fixedDeltaTime,
            0.0f);
        _rb.MoveRotation(Quaternion.LookRotation(rot, Vector3.up));
    }
    
    private void FixedUpdate()
    {
        UpdateFlags();
        
        if (!IsActive) return;
        
        Rotate();
        
        if(IsControllingMovement)
            Move();
    }

    protected virtual void Awake()
    {
        _sensors = GetComponent<Sensors>();
        _rb = GetComponent<Rigidbody>();
    }
}
