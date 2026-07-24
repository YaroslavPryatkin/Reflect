using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Playables;
using System.Collections.Generic;
using UnityEngine.UIElements;

public abstract class MeleeTransformController : MonoBehaviour
{
    [SerializeField] private float parabolaTargetFollowingSpeed = 20f;
    [SerializeField] private float targetDistanceToTarget = 0.9f;
    public bool IsActive { get; private set; } = false;
    private readonly UtilityClasses.ChangeableFractionValue _isActiveTimer = new();
    public bool IsControllingMovement { get;protected set; } = false;
    
    private Vector3 _targetPosition;
    private Vector3 _targetDirection;
    private Vector3 _targetLookDirection;
    private bool _haveTarget;

    private enum TypeEnum
    {
        Moving, Standing, Parabola
    }
    
    private TypeEnum _type = TypeEnum.Moving;
    
    
    private float _currentFloatValue;
    private AnimationCurve _currentCurve;

    private float _rotationSpeed;
    private bool _forceLookToTarget;
    
    private UtilityClasses.ParabolaCurve _parabolaCurve;
    private bool _wasKinematic=false;
    private Vector3 _currentParabolaEndPos;
    
    private Sensors _sensors;
    private Rigidbody _rb;

    protected abstract void SetTargetLookDirection( out Vector3 targetLookDirection);
    protected abstract bool DoesHaveTarget();
    protected abstract void SetTargetPositionAndDirection(out Vector3 targetPosition, out Vector3 targetDirection);

    public void ActivateMoving(
        float transformSpeedMultiplier,
        AnimationCurve speedCurve,
        float rotationSpeed,
        bool forceLookToTarget,
        UtilityClasses.FractionTemporaryValue<bool> moveRequest)
    {
        if (IsActive)
        {
            //Debug.Log("<color="+GetColor()+">Ending previous " + _type + "</color>");
            FinishMove();
        }
        
        _isActiveTimer.Set(moveRequest);
        IsActive = true;
        _type = TypeEnum.Moving;
        
        _currentFloatValue = transformSpeedMultiplier;
        _currentCurve = speedCurve;
        _rotationSpeed = rotationSpeed;
        _forceLookToTarget= forceLookToTarget;
        IsControllingMovement = true;
        _haveTarget = false;
      
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
            FinishMove();
        }
        
        _isActiveTimer.Set(moveRequest);
        IsActive = true;
        _type = TypeEnum.Standing;
        
        _rotationSpeed = rotationSpeed;
        _forceLookToTarget= forceLookToTarget;
        IsControllingMovement = standInPlace;
        //Debug.Log("<color="+GetColor()+">Starting standing</color>");
    }
    
    public void ActivateParabola(
        float angleUp,
        AnimationCurve distanceCurve,
        UtilityClasses.FractionTemporaryValue<bool> moveRequest)
    {
        if (IsActive)
        {
            //Debug.Log("<color="+GetColor()+">Ending previous " + _type + "</color>");
            FinishMove();
        }
        
        
        _isActiveTimer.Set(moveRequest);
        
        _haveTarget = DoesHaveTarget();
        
        if (!_haveTarget)
        {
            return;
        }
        
        SetTargetPositionAndDirection(out _currentParabolaEndPos, out _targetDirection);

        
        IsActive = true;
        _type = TypeEnum.Parabola;

        _currentFloatValue = angleUp;
        _currentCurve = distanceCurve;
        IsControllingMovement = true;
        _parabolaCurve.StartPos = transform.position;
        _rotationSpeed = 30;
        
        _wasKinematic = _rb.isKinematic;
        _rb.isKinematic = true;
        //Debug.Log("<color="+GetColor()+">Starting parabola</color>");
    }

    private string GetColor()
    {
        return _type switch
        {
            TypeEnum.Parabola => "red",
            TypeEnum.Moving => "green",
            TypeEnum.Standing => "cyan"
        };
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
            FinishMove();
            return;
        }

        if (!_sensors.IsGrounded && _type == TypeEnum.Moving)
        { 
            FinishMove();
            return;
        }

        switch (_type)
        {
            case TypeEnum.Parabola:
                if (!DoesHaveTarget())
                {
                    FinishMove();
                    return;
                }
                SetTargetPositionAndDirection(out _targetPosition, out _targetDirection);
                _targetLookDirection = _targetDirection;
                break;
            case TypeEnum.Moving or TypeEnum.Standing:
                if (DoesHaveTarget())
                {
                    
                    _haveTarget = true;
                    SetTargetPositionAndDirection(out _targetPosition, out _targetDirection);
                    if (_forceLookToTarget)
                    {
                        _targetLookDirection = _targetDirection;
                        break;
                    }
                }
                else
                {
                    _haveTarget = false;
                }
                SetTargetLookDirection(out _targetLookDirection);
                break;
        }
    }

    private void FinishMove()
    {
        _rb.isKinematic = _wasKinematic;
        _wasKinematic = false;
        IsActive = false;
        if(_type == TypeEnum.Moving)
            _rb.linearVelocity = Vector3.zero;
    }
    
    private void Move()
    {
        Vector3 endPos = Vector3.zero;

        switch (_type)
        {
            case TypeEnum.Parabola or TypeEnum.Moving:
                endPos = _targetPosition - _targetDirection * targetDistanceToTarget;
                if (Vector3.Distance(transform.position, endPos) < 0.1f || 
                    Vector3.Distance(transform.position, _targetPosition) < targetDistanceToTarget)
                {
                    FinishMove();
                    return;
                }
               
                break;
        }
        
        switch (_type)
        {
            case TypeEnum.Standing:
                _rb.linearVelocity=Vector3.zero;
                break;
            case TypeEnum.Parabola:
                _currentParabolaEndPos = Vector3.MoveTowards(_currentParabolaEndPos, endPos,
                    parabolaTargetFollowingSpeed * Time.fixedDeltaTime);
                _parabolaCurve.EndPos = _currentParabolaEndPos;
                    _parabolaCurve.MakeStartAngleParabola(_currentFloatValue);
                    _parabolaCurve.CalculateTotalLength();
                    _rb.MovePosition(
                        _parabolaCurve.GetPositionByTraveledDistance(
                            _currentCurve.Evaluate(_isActiveTimer.TimeFraction)
                        )
                    );
                break;
            case TypeEnum.Moving:
                if (_haveTarget)
                {
                    var res = transform.forward * (
                        _currentFloatValue * _currentCurve.Evaluate(_isActiveTimer.TimeFraction));
                    if (Vector3.Distance(_targetPosition, transform.position + res) < targetDistanceToTarget)
                    {
                        //Debug.Log("<color="+GetColor()+">Ended for reaching destination</color>");
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
        }
        _sensors.UpdateVelocity();
    }

    private void Rotate()
    {
        var rot = Vector3.RotateTowards(transform.forward,
            _targetLookDirection, 
            _rotationSpeed * Time.deltaTime,
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
