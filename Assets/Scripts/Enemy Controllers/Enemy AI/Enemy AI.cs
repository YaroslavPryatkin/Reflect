using System;
using System.Collections.Generic;
using System.Collections;
using CustomAttributes;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class EnemyAI : MonoBehaviour
{
     [SerializeField] private Transform marker;
    
    [Header("Distances")] 
    [SerializeField] private float shootingDistance = 20f;
    [SerializeField] private float preferredDistanceMax = 14;
    [SerializeField] private float preferredDistanceMin = 8;
    [SerializeField] private float wanderingDistance = 10f;
    [SerializeField] private float retreatDistance = 6f;

    [Header("Target calculating")]
    [SerializeField] private float calculatingTargetDuration = 1f;
    [SerializeField] private float directionsStep = 5f;
    [SerializeField] private float distanceStep = 0.5f;
    
    [Header("Speeds")] 
    [SerializeField] private float traversalSpeed = 6f;
    [SerializeField] private float wanderingSpeed = 4f;
    [SerializeField, EnableIf("shootWhileMoving")] 
    private float shootingMovingSpeed = 5f;
    [SerializeField] private float retreatSpeed = 8f;
    [SerializeField, EnableIf("shootWhileRetreating && shootWhileMoving")] 
    private float shootingRetreatSpeed = 6f;
    [SerializeField, EnableIf("shootWhileMoving && shouldOrbit")]
    private float orbitingSpeed = 2f;

    [Header("Shooting")] 
    [SerializeField] private bool shootWhileMoving = true;
    [SerializeField, EnableIf("shootWhileMoving")] 
    private float shootAngleIncreaseWhileMoving;
    [SerializeField, EnableIfNot("shootWhileMoving")] 
    private float shootingSpeedThreshold = 0.2f;

    [Header("Retreating")] 
    [SerializeField, EnableIf("shootWhileMoving")] 
    private bool shootWhileRetreating = false;
    [SerializeField, EnableIf("shootWhileRetreating && shootWhileMoving")] 
    private float shootAngleIncreaseWhileRetreating;

    [Header("Orbiting")] 
    [SerializeField, EnableIf("shootWhileMoving")]
    private bool shouldOrbit = true;
    
    
    [Header("Wandering")]
    [SerializeField]
    private float wanderingDirectionTimerMinDuration = 2f;
    [SerializeField]
    private float wanderingDirectionTimerMaxDuration = 5f;
    
    
    [Header("Dodging bullets")]
    [SerializeField] private bool canDodgeBullets = true;
    [SerializeField, EnableIf("canDodgeBullets")] 
    private int bulletsTakenToDodge = 1;
    [SerializeField, EnableIf("canDodgeBullets")]
    private float dodgeDuration = 0.2f;
    [SerializeField, EnableIf("canDodgeBullets")]
    private float dodgeRecharge = 1f;
    
    [Header("Step back from melee hits")]
    [SerializeField] private bool canStepBack = true;
    [SerializeField, EnableIf("canStepBack")] 
    private int hitsTakenToStepBack = 2;
    [SerializeField, EnableIf("canStepBack")]
    private float stepBackDuration = 0.2f;
    [SerializeField, EnableIf("canStepBack")]
    private float stepBackRecharge = 1f;

    public bool IsMoving { get; private set; } = false;
    public bool IsAiming { get; private set; } = false;
    public float ShootingConeAngleIncrease { get; private set; } = 0f;
    
    private EnemySensors _enemySensors;
    private NavMeshAgent _agent;
    private EnemyGunController _enemyGunController;
    
    
    private int _movementDirection = 0;
    
    private readonly Utility.TemporaryValue<bool> _orbitingDirectionTimer = new(false, true);
    private readonly Utility.TemporaryValue<bool> _calculatingTargetTime = new(false, true);
    private readonly float _orbitingAheadHalfAngle = 5f;
    private float _orbitingChord;

    private int _directionsToCheck;
    private float _preferredDistanceMid;
    private bool _goingToMid=true;
    private readonly List<float> _directions = new ();
    private readonly List<float> _distances = new ();
    
    private void Awake()
    {
        _enemySensors = GetComponent<EnemySensors>();
        _agent = GetComponent<NavMeshAgent>();
        _enemyGunController = GetComponent<EnemyGunController>();

        //_changeDirectionApproachDistance = approachDiagonallySpeed * approachChangeDirectionInterval;
        _orbitingChord = Mathf.Sin(_orbitingAheadHalfAngle * Mathf.Deg2Rad)*2;
        
        _directionsToCheck = (int)Math.Floor(360f / directionsStep);
        _preferredDistanceMid = (preferredDistanceMax + preferredDistanceMin) / 2;
    }
    
    private void MakeRandomMovementDirection()
    {
        _movementDirection = Random.Range(0, 2);
        if (_movementDirection == 0) _movementDirection = -1;
    }

    private void ChangeState(float shootingSpeed, float speed, bool currentShootWhileMoving, float currentAngleIncreaseWhileMoving)
    {
        IsMoving = _agent.hasPath;
        IsAiming = (currentShootWhileMoving || !IsMoving || _enemySensors.SpeedAlignedWithGround < shootingSpeedThreshold) && 
                   _enemySensors.IsSeeingPlayer &&
                   Vector3.Distance(transform.position, _enemySensors.PlayerPosition) <= shootingDistance;
        
        _agent.speed = IsMoving ? (IsAiming ? shootingSpeed : speed) : 0f;
        ShootingConeAngleIncrease = IsAiming && IsMoving ? currentAngleIncreaseWhileMoving : 0f;
    }
    
    
    private void Retreat()
    {
        _calculatingTargetTime.Deactivate();
        var targetPos = transform.position - _enemySensors.NormalizedHorizontalDirectionFromSeePointToPlayer * 1f;
        SetAgentDestination(targetPos);
        ChangeState(shootingRetreatSpeed, retreatSpeed, shootWhileRetreating && shootWhileMoving, shootAngleIncreaseWhileRetreating);
    }
    

    private void SetAgentDestination(Vector3 targetPos)
    {
        var path = new NavMeshPath();

        _agent.CalculatePath(targetPos, path);

        if (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial)
        {
            _agent.SetPath(path);
        }
        else
        {
            if (NavMesh.Raycast(transform.position, targetPos, out NavMeshHit hit, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
            }
            else
            {
                _agent.SetDestination(targetPos);
            }
        }
    }
    
    private void TryToShoot()
    {
        if (!_calculatingTargetTime)
        {
            _calculatingTargetTime.Activate(calculatingTargetDuration);
            FindShootingPosition();
        }
    }
    
    private void FindShootingPosition()
    {
        var path = new NavMeshPath();

        var deltaY = Mathf.Abs(_enemySensors.PlayerPosition.y - _enemySensors.SeePointPosition.y);

        if (deltaY > shootingDistance)
        {
            ExecuteFallbacks();
            return;
        }

        var distToPlayer = _enemySensors.DistanceFromSeePointToPlayer;
        if (!_goingToMid &&
             _enemySensors.IsSeeingPlayer && 
             distToPlayer >= preferredDistanceMin &&
             distToPlayer <= preferredDistanceMax)
        {
            if (shouldOrbit)
            {
                //Debug.Log("orbit");
                Orbit(distToPlayer, orbitingSpeed);
            }
            else
            {
                
                ChangeState(0f, 0f, shootWhileMoving, shootAngleIncreaseWhileMoving);
            }
            return;
        }

        
        _goingToMid = true;
        
        var hMax = Mathf.Sqrt(shootingDistance * shootingDistance - deltaY * deltaY);
        
        var hPref = 0f;
        if (_preferredDistanceMid > deltaY)
        {
            hPref = Mathf.Sqrt(_preferredDistanceMid * _preferredDistanceMid - deltaY * deltaY);
        }
        hPref = Mathf.Min(hMax, Mathf.Max(hPref, 4f));

        var hMin = 0f;
        if (retreatDistance > deltaY)
        {
            hMin = Mathf.Sqrt(retreatDistance * retreatDistance - deltaY * deltaY);
        }
        
        var baseDir = -_enemySensors.HorizontalDirectionFromSeePointToPlayer;
        if (baseDir.sqrMagnitude < 0.001f) baseDir = Vector3.forward;
        baseDir.Normalize();


        _directions.Clear();
        _distances.Clear();

        
        for (var i = 0; i < _directionsToCheck; i++)
        {
            var multiplier = (i % 2 == 0) ? -(i / 2) : (i / 2) + 1;
            if (i == 0) multiplier = 0;
            var angle = multiplier * directionsStep;
            _directions.Add(angle);
        }

        
        var distancesToCheck = Math.Max(1,(int)Math.Floor((hMax-hMin) / distanceStep));

        for (var i = 1; i <= distancesToCheck; i++)
        {
            _distances.Add(hMin + i * distanceStep);
        }
        _distances.Sort((a, b) => Mathf.Abs(a - hPref).CompareTo(Mathf.Abs(b - hPref)));
        
        
        foreach (var angle in _directions)
        {
            foreach (var dist in _distances)
            {
                var dir = Quaternion.Euler(0, angle, 0) * baseDir;
                var targetPos = _enemySensors.PlayerPosition + dir * dist;
                targetPos.y = _enemySensors.SeePointPosition.y;

                if (_enemySensors.HasLineOfSightToPlayer(targetPos))
                {
                    var rawDist = Vector3.Distance(_enemySensors.SeePointPosition, targetPos);
                    _enemySensors.GetTransformPositionFromSeePointPosition(ref targetPos);
                    if (NavMesh.SamplePosition(targetPos, out var hit, 2f, NavMesh.AllAreas))
                    {
                        if (NavMesh.CalculatePath(_enemySensors.SeePointPosition, hit.position, NavMesh.AllAreas, path) &&
                            path.status == NavMeshPathStatus.PathComplete)
                        {
                            //Debug.Log("Found position: angle =  " + angle + ", dist = " + dist);
                            //marker.position = targetPos;
                            _goingToMid =  rawDist > distanceStep * 1.2f;
                            //Debug.Log("Going, dist = " + rawDist + ", dist to player " + distToPlayer + ", hMax " + hMax  + ", hPref " + hPref + ", hMin " + hMin + ", dist count " + distancesCount);
                            _agent.SetPath(path);
                            _calculatingTargetTime.Deactivate();
                            ChangeState(shootingMovingSpeed, traversalSpeed, shootWhileMoving, shootAngleIncreaseWhileMoving);
                            return;
                        }
                    }
                }
            }
        }

        ExecuteFallbacks();
    }

    private void ExecuteFallbacks()
    {
        var path = new NavMeshPath();

        if (NavMesh.CalculatePath(transform.position, _enemySensors.PlayerPosition, NavMesh.AllAreas, path) 
            && path.status == NavMeshPathStatus.PathComplete)
        {
            _agent.SetDestination(_enemySensors.PlayerPosition);
            //Debug.Log("Going to player");
           //marker.position = _enemySensors.PlayerPosition;
            ChangeState(shootingMovingSpeed,traversalSpeed, shootWhileMoving, shootAngleIncreaseWhileMoving);
        }
        else
        {
            //Debug.Log("Wandering");
            Orbit(wanderingDistance, wanderingSpeed);
        }
    }
    
    private void Orbit(float distance, float speed)
    {
        if (!_orbitingDirectionTimer)
        {
            MakeRandomMovementDirection();
            _orbitingDirectionTimer.Activate(Random.Range(wanderingDirectionTimerMinDuration, wanderingDirectionTimerMaxDuration));
        }
        var dir = _enemySensors.NormalizedHorizontalDirectionFromSeePointToPlayer;
        var strafeDir = Vector3.Cross(dir, Vector3.up) * _movementDirection;
        strafeDir = Quaternion.Euler(0f, _orbitingAheadHalfAngle * -_movementDirection, 0f) * strafeDir;

        var point = new Vector3(_enemySensors.PlayerPosition.x, transform.position.y,
            _enemySensors.PlayerPosition.z);
        var targetPos = point - dir * distance  + strafeDir * (_orbitingChord * distance);
        SetAgentDestination(targetPos);
       // marker.position = targetPos;
        ChangeState(orbitingSpeed, speed, shootWhileMoving, shootAngleIncreaseWhileMoving);
    }
    
    private void ControlMovementAndShooting()
    {
        if (!_enemySensors.IsDetectingPlayer)
        {
            _agent.ResetPath();
            ChangeState( 0f, 0f, false, 0f);
            //Debug.Log("Isnt detecting");
            return;
        }
        if(_enemySensors.HorizontalDistanceFromSeePointToPlayer < retreatDistance)
            Retreat();
        else
            TryToShoot();
    }

    private void Update()
    {
        ControlMovementAndShooting();
    }
}
