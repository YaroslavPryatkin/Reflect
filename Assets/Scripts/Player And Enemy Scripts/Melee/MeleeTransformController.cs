using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Playables;
using System.Collections.Generic;
using UnityEngine.UIElements;

public abstract class MeleeTransformController : MonoBehaviour
{
    public bool IsActive { get; private set; } = false;
    private readonly Utility.ChangeableFractionValue _isActiveTimer = new();
    public bool IsControllingMovement { get;protected set; } = false;
    
    private Vector3 _targetPosition;
    private Vector3 _targetDirection;
    private Vector3 _inputDirection;
    private bool _haveTarget;
    
    private bool _canMove = false;
    private float _targetDistanceToTarget;
    private float _transformSpeedMultiplier;
    private AnimationCurve _speedCurve;

    private float _rotationSpeed;
    private bool _forceLookToTarget;
    
    
    
    private Sensors _sensors;
    private Rigidbody rb;

    protected abstract void SetInputDirection( out Vector3 inputDirection);
    protected abstract bool DoesHaveTarget();
    protected abstract void SetTargetPositionAndDirection(out Vector3 targetPosition, out Vector3 targetDirection);

    public void ActivateMoving(
        float targetDistance, 
        float transformSpeedMultiplier,
        AnimationCurve speedCurve,
        float rotationSpeed,
        bool forceLookToTarget,
        Utility.FractionTemporaryValue<bool> moveRequest)
    {
        _targetDistanceToTarget=targetDistance;
        _isActiveTimer.Set(moveRequest);
        _transformSpeedMultiplier = transformSpeedMultiplier;
        _speedCurve = speedCurve;
        _rotationSpeed = rotationSpeed;
        _canMove = true;
        _forceLookToTarget= forceLookToTarget;
        IsControllingMovement = true;
    }
    
    public void ActivateStandingAndLooking(
        float rotationSpeed,
        bool forceLookToTarget,
        bool standInPlace,
        Utility.FractionTemporaryValue<bool> moveRequest)
    {
        _isActiveTimer.Set(moveRequest);
        _canMove = false;
        _rotationSpeed = rotationSpeed;
        _forceLookToTarget= forceLookToTarget;
        IsControllingMovement = standInPlace;
    }

    public void StopMovingAndClearReferences()
    {
        _isActiveTimer.Unset();
    }

    private void UpdateFlags()
    {
        if (_isActiveTimer.Value && _sensors.IsGrounded)
        {
            IsActive = true;
            SetInputDirection(out _inputDirection);
        }
        else if (IsActive)
        {
            IsActive = false;
            rb.linearVelocity = Vector3.zero;
        }

        _haveTarget = DoesHaveTarget();
        if (_haveTarget)
        {
            SetTargetPositionAndDirection(out _targetPosition, out _targetDirection);
            if (_forceLookToTarget)
            {
                _inputDirection = _targetDirection;
            }
        }
    }
    
    private void Move()
    {
        if (_canMove)
        {
            if (_haveTarget)
            {
                var res = transform.forward * (
                    _transformSpeedMultiplier * _speedCurve.Evaluate(_isActiveTimer.TimeFraction));
                if (Vector3.Distance(_targetPosition, transform.position + res) < _targetDistanceToTarget)
                {
                    res = Vector3.ProjectOnPlane(res, _targetDirection);
                }
                rb.linearVelocity = res;
            }
            else
            {
                rb.linearVelocity = transform.forward * (
                    _transformSpeedMultiplier * _speedCurve.Evaluate(_isActiveTimer.TimeFraction));
            }
        }
        else
        {
            rb.linearVelocity=Vector3.zero;
        }
        _sensors.UpdateVelocity();
    }

    private void Rotate()
    {
        var rot = Vector3.RotateTowards(transform.forward,
            _inputDirection, 
            _rotationSpeed * Time.deltaTime,
            0.0f);
        transform.rotation = Quaternion.LookRotation(rot,Vector3.up);
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
        rb = GetComponent<Rigidbody>();
    }
}
