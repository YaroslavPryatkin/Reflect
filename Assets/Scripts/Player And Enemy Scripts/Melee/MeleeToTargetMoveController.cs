using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Playables;
using System.Collections.Generic;

public class MeleeToTargetMoveController : MonoBehaviour
{
    [SerializeField] private List<AnimationClip> moveClips = new();
    [SerializeField] private AvatarMask avatarMask;
    [SerializeField] private float layerCrossFadeDuration = 0.1f;
    [SerializeField] private float clipCrossFadeDuration = 0.05f;
    
    private bool _haveTarget = false;
    private Transform _target;
    
    
    private bool _haveMoveRequest = false;
    private Utility.FractionTemporaryValue<bool> _moveRequest;
    
    private float _targetDistanceToTarget;
    private float _transformSpeed;
    private float _animationSpeed;
    private AnimationClip _animationClip;
    
    
    public Vector3 MoveDirection { get; private set; } = Vector3.zero;
    public bool IsMoving { get; private set; } = false;
    public bool IsLooking { get; private set; } = false;
    
    private Sensors _sensors;
    private Rigidbody rb;
    private AutomaticAnimationLayerController _automaticAnimationLayerController;
    private readonly Utility.FractionBlockingValueTimer<Utility.BaseActionTransitionsEnum> _layerState = Utility.BaseActionTransitionsEnum.Base;

    public void SetTarget(Transform target)
    {
        _target = target;
        _haveTarget = true;
    }

    public void UnsetTarget()
    {
        _haveTarget = false;
    }

    public void ActivateMoving(
        float targetDistance, 
        float transformSpeed,
        float duration,
        int  moveClipIndex,
        Utility.FractionTemporaryValue<bool> moveRequest)
    {
        if (!_haveTarget)
            return;
        
        _targetDistanceToTarget=targetDistance;
        _moveRequest = moveRequest;
        _haveMoveRequest = true;
        _transformSpeed = transformSpeed;
        _animationClip = moveClips[moveClipIndex];
        _animationSpeed = Utility.GetAnimationSpeed(_animationClip, duration);
    }

    public void StopMovingAndClearReferences()
    {
        _haveMoveRequest = false;
        if (IsMoving)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    private void UpdateFlags()
    {
        
        if (!_haveMoveRequest || !_moveRequest.Value)
        {
            IsMoving = false;
            IsLooking = false;
            return;
        }

        
        if (_haveTarget)
        {
            IsLooking = true;
            IsMoving = _sensors.IsGrounded && Vector3.Distance(_target.position, transform.position) > _targetDistanceToTarget;

            if (IsMoving)
            {
                var dir = transform.position - _target.position;
                dir = new Vector3(dir.x, 0f, dir.z);
                if (dir.sqrMagnitude < 0.00001f)
                {
                    dir = transform.forward;
                }
                else
                {
                    dir = dir.normalized;
                }
                var realTargetPosition = _target.position + dir * _targetDistanceToTarget;
                MoveDirection = (realTargetPosition - transform.position).normalized;
            }
        }
    }

    private void Move()
    {
        if (!IsMoving) return;
        
        rb.linearVelocity = MoveDirection * _transformSpeed;
        //rb.MovePosition(transform.position + MoveDirection * (_speed * Time.fixedDeltaTime));
        _sensors.UpdateVelocity();
    }

    
    
    private void SetAnimations()
    {
        _automaticAnimationLayerController.SetLayerWeightAndChangeState(_layerState, IsMoving, layerCrossFadeDuration);
        if (IsMoving)
            _automaticAnimationLayerController.AutomaticUpdateCurrentPlayable(_animationClip, _animationSpeed,
                clipCrossFadeDuration);
        else
            _automaticAnimationLayerController.ResetCurrentPlayable();
    }
    
    private void FixedUpdate()
    {
        UpdateFlags();
        
        
        
        Move();
        SetAnimations();
    }

    private void Awake()
    {
        if (!TryGetComponent(out AnimationController controller))
        {
            Debug.LogError("No AnimationController found!", this);
            enabled = false;
            return;
        }
        var clips = new HashSet<AnimationClip>();
        foreach (var clip in moveClips)
            clips.Add(clip);
        _automaticAnimationLayerController = controller.GetAutomaticAnimationLayer(clips, 4, avatarMask);
        
        _sensors = GetComponent<Sensors>();
        rb = GetComponent<Rigidbody>();
    }
}
