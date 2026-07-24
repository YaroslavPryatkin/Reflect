using UnityEngine;
using CustomAttributes;
using UnityEngine.AI;
using System.Collections.Generic;
using BaseActionTransitionsEnum = UtilityFunctions.BaseActionTransitionsEnum;

public class EnemyDodgeController : MonoBehaviour
{
    [Header("Dodging bullets")]
    [SerializeField] private bool canDodgeBullets = true;
    [SerializeField, EnableIf("canDodgeBullets")] 
    private int bulletsTakenToDodge = 3;
    [SerializeField, EnableIf("canDodgeBullets")]
    private float dodgeDistanceFromBullets = 3f;
    
    [Header("Dodging melee")]
    [SerializeField] private bool canDodgeMelee = true;
    [SerializeField, EnableIf("canDodgeMelee")] 
    private int hitsTakenToDodge = 2;
    [SerializeField, EnableIf("canDodgeMelee")]
    private bool dodgeMeleeToTheSide = false;
    [SerializeField, EnableIf("canDodgeMelee")]
    private float dodgeDistanceFromMelee = 5f;
    
    [Header("Settings")]
    [SerializeField, EnableIf("canDodgeBullets || canDodgeMelee")]
    private float dodgeDuration = 0.2f;
    [SerializeField, EnableIf("canDodgeBullets || canDodgeMelee")]
    private float dodgeRechargeTime = 0.5f;

    [Header("After dodge")] 
    [SerializeField, EnableIf("canDodgeBullets || canDodgeMelee")]
    private float timeToTurnToPlayerAfterDodge = 0.2f;
    [SerializeField, EnableIf("canDodgeBullets || canDodgeMelee")]
    private bool shouldShootAfterDodge = true;
    [SerializeField, EnableIf("canDodgeBullets && shouldShootAfterDodge || canDodgeMelee && shouldShootAfterDodge")]
    private float durationOfTelegraphTimeChange = 0.3f;
    [SerializeField, EnableIf("canDodgeBullets && shouldShootAfterDodge || canDodgeMelee && shouldShootAfterDodge")]
    private float telegraphTimeAfterDodge = 0.1f;
    [SerializeField, EnableIf("canDodgeBullets && shouldShootAfterDodge || canDodgeMelee && shouldShootAfterDodge")]
    private float shootingAngleIncreaseAfterDodge = 15f;
    
    [Header("Speed curve")]
    [SerializeField, EnableIf("canDodgeBullets || canDodgeMelee")] private AnimationCurve dodgeCurve = AnimationCurve.EaseInOut(0f, 1.5f, 1f, 0f);
    
    [Header("Animation")]
    [SerializeField, EnableIf("canDodgeBullets || canDodgeMelee")] private AnimationClip  dodgeClip;
    [SerializeField, EnableIf("canDodgeBullets || canDodgeMelee")] private float crossFadeDuration = 0.15f;
    
    
    private Rigidbody _rb;
    private NavMeshAgent _agent;
    private EnemySensors _enemySensors;
    private EnemyRotationController _enemyRotationController;
    private EnemyAI _enemyAI;
    private AnimationLayerController _animationLayerController;
    private HealthController _healthController;
    
    private EnemyGunController _enemyGunController;
    private bool _haveGunController;
    
    
    private enum StateEnum
    {
        Non, Dodging, Recharging
    }
    
    private readonly UtilityClasses.FractionBlockingValueTimer<StateEnum> _state = StateEnum.Non;
    private UtilityClasses.BaseActionAutomaticTransition _transitionState;
    
    private Vector3 _direction;
    private float _curveSpeedMultiplier;
    
    private int _bulletsTaken=0;
    private int _hitsTaken=0;

    private bool _tryingToSnapToNavMesh = false;


    private void Awake()
    {
        if (!canDodgeBullets && !canDodgeMelee) return;
        
        if (!TryGetComponent(out AnimationAndRigManager controller))
        {
            Debug.LogError("No AnimationController found!", this);
            enabled = false;
            return;
        }
        
        _rb =  GetComponent<Rigidbody>();
        _enemySensors = GetComponent<EnemySensors>();
        _agent = GetComponent<NavMeshAgent>();
        _enemyRotationController = GetComponent<EnemyRotationController>();
        _enemyAI = GetComponent<EnemyAI>();
        _healthController = GetComponent<HealthController>();
        _haveGunController = TryGetComponent(out _enemyGunController);

        var clips = new HashSet<AnimationClip>();
        clips.Add(dodgeClip);

        _animationLayerController = controller.GetAnimationLayer(clips, 6, "Enemy dodge");
        _animationLayerController.SetPlayableWeight(dodgeClip, 1f);
        _animationLayerController.SetPlayableSpeed(dodgeClip,
            UtilityFunctions.GetAnimationSpeed(dodgeClip, dodgeDuration));
        
        _curveSpeedMultiplier = 1f / (UtilityFunctions.EvaluateCurveAverage(dodgeCurve) * dodgeDuration);

        _transitionState = crossFadeDuration;
    }
    
    public void GettingHit(HealthController.DamageDealer damageDealer)
    {
        switch (damageDealer)
        {
            case HealthController.DamageDealer.Melee:
                if (canDodgeMelee && ++_hitsTaken >= hitsTakenToDodge)
                {
                    if (dodgeMeleeToTheSide)
                    {
                        var dir = Random.value > 0.5f ? 1 : -1;
                        StartDodge(dodgeDistanceFromMelee,Vector3.Cross(_enemySensors.NormalizedHorizontalDirectionToPlayer, Vector3.up) * dir);
                    }
                    else
                    {
                        StartDodge(dodgeDistanceFromMelee, -_enemySensors.NormalizedHorizontalDirectionToPlayer);
                    }
                }
                break;
            case HealthController.DamageDealer.Bullet:
                if (canDodgeBullets && ++_bulletsTaken >= bulletsTakenToDodge)
                {
                    var dir = Random.value > 0.5f ? 1 : -1;
                    StartDodge(dodgeDistanceFromBullets, Vector3.Cross(_enemySensors.NormalizedHorizontalDirectionToPlayer, Vector3.up) * dir);
                }

                break;
        }
    }
    
    
    private void StartDodge(float distance, Vector3 direction)
    {
        if (_state == StateEnum.Non)
        {
            _bulletsTaken=0;
            _hitsTaken=0;
            _rb.isKinematic = false;
            _agent.enabled = false;
            _enemyAI.TakeControls();
            _state.SetForce(StateEnum.Dodging, dodgeDuration);
            _direction = direction * (distance * _curveSpeedMultiplier);
            _enemyRotationController.UseSpecificDirection(direction);
            _animationLayerController.ResetPlayableTime(dodgeClip);
            _healthController.GrantIFrames();
            
            if(_haveGunController)
                _enemyGunController.InterruptAiming();
        }
    }

    private void FinishDodge()
    {
        _rb.linearVelocity = Vector3.zero;
        _tryingToSnapToNavMesh = true;
        
        if (_haveGunController)
        {
            _enemyGunController.StopInterruptingAiming();
            
            if(shouldShootAfterDodge)
                _enemyGunController.UseFastTelegraphTime(
                durationOfTelegraphTimeChange, 
                telegraphTimeAfterDodge, 
                shootingAngleIncreaseAfterDodge
                );
            
        }

        _enemyRotationController.FastRotate(timeToTurnToPlayerAfterDodge);
        _enemyRotationController.StopUsingSpecificDirection();
        _healthController.TakeIFrames();
    }


    private void Update()
    {
        if (_tryingToSnapToNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 2.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                Physics.SyncTransforms();
                _agent.enabled = true;
                _rb.isKinematic = true;
                _tryingToSnapToNavMesh = false;
                _agent.ResetPath();
                _enemyAI.ReturnControls();
            }
        }
        
        
        
        if (_state.CanBeChanged)
        {
            switch (_state.Value)
            {
                case StateEnum.Dodging:
                    FinishDodge();
                    _state.SetForce(StateEnum.Recharging, dodgeRechargeTime);
                    break;
                case StateEnum.Recharging:
                    _state.SetForce(StateEnum.Non);
                    break;
            }
        }
        
        _animationLayerController.SetLayerWeight(_transitionState.GetFraction(_state == StateEnum.Dodging));
    }

    private void FixedUpdate()
    {
        if (_state == StateEnum.Dodging)
        {
            _rb.linearVelocity = dodgeCurve.Evaluate(_state.TimeFraction) * _direction;
        }
    }
    


}
