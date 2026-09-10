using System;
using System.Collections.Generic;
using System.Collections;
using CustomAttributes;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


public class EnemyAI : MonoBehaviour
{ 
    [Header("Distances")] 
    [SerializeField] private float shootingDistance = 20f;
    [SerializeField] private float preferredDistanceMax = 14;
    [SerializeField] private float preferredDistanceMin = 8;
    [SerializeField] private float wanderingDistance = 10f;
    [SerializeField] private float retreatDistance = 6f;

     private const float DirectionsStep = 10f;
     private const float DistancePresicion = 0.5f;
    
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

    [Header("Interrupt active shooting by movement")] 
    [SerializeField]
    private bool shouldInterruptActiveShootingByMovement = true;
    
    [Header("Shooting")] 
    [SerializeField] private bool shootWhileMoving = true;
    [SerializeField, EnableIf("shootWhileMoving")] 
    private float shootAngleIncreaseWhileMoving;
    [SerializeField, EnableIf("!shootWhileMoving")] 
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
    private float directionTimerMinDuration = 2f;
    [SerializeField]
    private float directionTimerMaxDuration = 5f;


    private bool _isActive = false;

    public bool IsMoving { get; private set; } = false;
    public bool IsAiming { get; private set; } = false;
    public float ShootingConeAngleIncrease { get; private set; } = 0f;
    
    private EnemySensors _enemySensors;
    private NavMeshAgent _agent;
    private EnemyGunController _enemyGunController;
    private EnemyRotationController _enemyRotationController;

    private int _computingIndex;
    
    private int _movementDirection = 0;
    
    private readonly UtilityTimers.TemporaryValue<bool> _orbitingDirectionTimer = new(false, true);
    private readonly float _orbitingAheadHalfAngle = 5f;
    private float _orbitingChord;

    private int _directionsToCheck;
    private float _preferredDistanceMid;
    
    private bool _goingToPosition=true;
    private readonly List<float> _directions = new ();
    //private readonly List<float> _distances = new ();
    
    private bool _lookingForPosition = false;
    private int _currentCheckedDirection;
    private Vector3 _currentTargetNavMeshPos;
    private NavMeshPath _cachedPath;
    private float _hMin, _hPref, _hMax;
    
    private int _controlsMovementAndShooting = 0;

    public void TakeControls()
    {
        ++_controlsMovementAndShooting;
    }

    public void ReturnControls()
    {
        --_controlsMovementAndShooting;
    }
    
    private void Awake()
    {
        _enemySensors = GetComponent<EnemySensors>();
        _agent = GetComponent<NavMeshAgent>();
        _enemyGunController = GetComponent<EnemyGunController>();
        _enemyRotationController = GetComponent<EnemyRotationController>();

        //_changeDirectionApproachDistance = approachDiagonallySpeed * approachChangeDirectionInterval;
        _orbitingChord = Mathf.Sin(_orbitingAheadHalfAngle * Mathf.Deg2Rad)*2;
        
        _directionsToCheck = (int)Math.Floor(360f / DirectionsStep);
        _preferredDistanceMid = (preferredDistanceMax + preferredDistanceMin) / 2;
        _cachedPath = new();
    }

    /// <summary>
    /// To call only from global computing time optimizer
    /// </summary>
    public void ChangeComputingIndex(int index)
    {
        _computingIndex = index;
    }
    
    public void Activate()
    {
        if (!_isActive)
        {
            _isActive = true;
            _computingIndex = GlobalEnemyComputingTimeOptimizer.AddEnemy(this);
        }
    }
    
    public void Deactivate()
    {
        if (_isActive)
        {
            _isActive = false;
            GlobalEnemyComputingTimeOptimizer.DeleteEnemy(_computingIndex);
        }
    }
    
    private void MakeRandomMovementDirection()
    {
        _movementDirection = Random.Range(0, 2);
        if (_movementDirection == 0) _movementDirection = -1;
    }

    private void ChangeState(float shootingSpeed, float speed, bool currentShootWhileMoving, float currentAngleIncreaseWhileMoving)
    {
        IsMoving = _agent.hasPath && !_enemyGunController.IsUsingFastTelegraphTime && (shouldInterruptActiveShootingByMovement || _enemyGunController.IsStateNon);
        IsAiming = (currentShootWhileMoving || !IsMoving || _enemySensors.SpeedAlignedWithGround < shootingSpeedThreshold) && 
                   _enemySensors.CanShootToPlayer &&
                   Vector3.Distance(transform.position, _enemySensors.PlayerPosition) <= shootingDistance;
        if (!IsAiming)
        {
            IsMoving = _agent.hasPath;
        }
        var isAimingSpeed = (currentShootWhileMoving || !IsMoving) && 
                            _enemySensors.CanShootToPlayer &&
                            Vector3.Distance(transform.position, _enemySensors.PlayerPosition) <= shootingDistance;

 
        
        _agent.speed = IsMoving ? (isAimingSpeed ? shootingSpeed : speed) : 0f;
        ShootingConeAngleIncrease = IsAiming && IsMoving ? currentAngleIncreaseWhileMoving : 0f;
    }
    
    
    private void Retreat()
    {
        var targetPos = transform.position - _enemySensors.NormalizedHorizontalDirectionToPlayer * 1f;
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
        var distToPlayer = _enemySensors.DistanceToPlayer;
        if (!_goingToPosition &&
            _enemySensors.CanShootToPlayer && 
            distToPlayer >= preferredDistanceMin &&
            distToPlayer <= preferredDistanceMax)
        {
            if (shouldOrbit)
            {
                Orbit(distToPlayer, orbitingSpeed);
            }
            else
            {
                ChangeState(0f, 0f, shootWhileMoving, shootAngleIncreaseWhileMoving);
            }
            return;
        }
        
        if(!_lookingForPosition && _goingToPosition)
            _goingToPosition = Vector3.Distance(_enemySensors.MyPosition, _currentTargetNavMeshPos) > 1.5f;

        
        if (GlobalEnemyComputingTimeOptimizer.CanStartCalculation(_computingIndex))
        {
            StartFindShootingPosition();
        }

        if (_lookingForPosition)
        {
            FindShootingPosition();
            if (_currentCheckedDirection == _directionsToCheck)
            {
               _lookingForPosition = false;
                ExecuteFallbacks();
            }
        }
    }

    private void StartFindShootingPosition()
    { 
        var deltaY = Mathf.Abs(_enemySensors.PlayerPosition.y - _enemySensors.MyPosition.y);

        if (deltaY > shootingDistance)
        {
            ExecuteFallbacks();
            return;
        }
        
        _hMax = Mathf.Sqrt(shootingDistance * shootingDistance - deltaY * deltaY);
        _hMin = 0f;
        if (retreatDistance > deltaY)
        {
            _hMin = Mathf.Sqrt(retreatDistance * retreatDistance - deltaY * deltaY);
        }
        
        var distToPlayer = _enemySensors.DistanceToPlayer;
        if (distToPlayer < preferredDistanceMin || distToPlayer > preferredDistanceMax)
        {
            _hPref = 0f;
            if (_preferredDistanceMid > deltaY)
            {
                _hPref = Mathf.Sqrt(_preferredDistanceMid * _preferredDistanceMid - deltaY * deltaY);
            }

            _hPref = Mathf.Min(_hMax, Mathf.Max(_hPref, 4f));
        }
        else
        {
            _hPref = Math.Clamp(_enemySensors.HorizontalDirectionToPlayer.magnitude, _hMin, _hMax);
        }
        
        _directions.Clear();
        
        for (var i = 0; i < _directionsToCheck; i++)
        {
            var multiplier = (i % 2 == 0) ? -(i / 2) : (i / 2) + 1;
            if (i == 0) multiplier = 0;
            var angle = multiplier * DirectionsStep;
            _directions.Add(angle);
        }
        _lookingForPosition = true;
        _currentCheckedDirection = 0;
    }

    private struct SearchSegment
    {
        public float Min;
        public float Max;
        public float Mid;
    }
    
    private readonly PriorityQueue.PriorityQueue<SearchSegment, float> _segmentsPool = new ();

    
    private void FindShootingPosition()
    {
        var baseDir = -_enemySensors.HorizontalDirectionToPlayer;
        if (baseDir.sqrMagnitude < 0.001f) baseDir = Vector3.forward;
        baseDir.Normalize();
    
        var end = Math.Min(
            _currentCheckedDirection + 5 + Mathf.CeilToInt(
                _directionsToCheck / GlobalEnemyComputingTimeOptimizer.CalculatingTargetDuration * Time.deltaTime), 
            _directionsToCheck);
        var startPos = _enemySensors.PlayerPosition;
        startPos.y = _enemySensors.MyPosition.y;
        for (; _currentCheckedDirection < end; ++_currentCheckedDirection)
        {
            var dir = Quaternion.Euler(0, _directions[_currentCheckedDirection], 0) * baseDir;

            if (CheckDirection(startPos, dir))
            {
                _lookingForPosition = false;
                _goingToPosition = true;
                ChangeState(shootingMovingSpeed, traversalSpeed, shootWhileMoving, shootAngleIncreaseWhileMoving);
                return;
            }
        }
    }
    
    private bool CheckDirection(in Vector3 start, in Vector3 dir)
    {
        _segmentsPool.Clear();
        
        var initialMid = (_hMin + _hMax) * 0.5f;
        _segmentsPool.Enqueue(new SearchSegment
        {
            Min = _hMin,
            Max = _hMax,
            Mid = initialMid
        }, Mathf.Abs(initialMid - _hPref));

        while (_segmentsPool.Count > 0)
        {
            var current = _segmentsPool.Dequeue();

            var halfLen = (current.Max - current.Min) * 0.5f;
            var radius = Mathf.Sqrt(halfLen * halfLen + 4f);
            
            var midPoint = start + dir * current.Mid;

            if (NavMesh.SamplePosition(midPoint, out var hit, radius, NavMesh.AllAreas))
            {
                if (current.Max - current.Min <= DistancePresicion)
                {
                    if (_enemySensors.HasLineOfSightToPlayer(midPoint)) 
                    {
                        if (NavMesh.CalculatePath(_enemySensors.MyPosition, hit.position, NavMesh.AllAreas, _cachedPath) &&
                            _cachedPath.status == NavMeshPathStatus.PathComplete)
                        {
                            _currentTargetNavMeshPos = hit.position; 
                            _agent.SetPath(_cachedPath);
                            return true; 
                        }
                    }
                }
                else
                {
                    var leftMid = (current.Min + current.Mid) * 0.5f;
                    var leftDist = Mathf.Abs(leftMid - _hPref);
                    _segmentsPool.Enqueue(new SearchSegment
                    {
                        Min = current.Min,
                        Max = current.Mid,
                        Mid = leftMid
                    }, leftDist);

                    var rightMid = (current.Mid + current.Max) * 0.5f;
                    var rightDist = Mathf.Abs(rightMid - _hPref);
                    _segmentsPool.Enqueue(new SearchSegment
                    {
                        Min = current.Mid,
                        Max = current.Max,
                        Mid = rightMid
                    }, rightDist);
                }
            }
        }

        return false;
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
            _orbitingDirectionTimer.Activate(Random.Range(directionTimerMinDuration, directionTimerMaxDuration));
        }
        var dir = _enemySensors.NormalizedHorizontalDirectionToPlayer;
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
        if(_enemySensors.HorizontalDistanceToPlayer < retreatDistance)
            Retreat();
        else
            TryToShoot();
    }

    private void Update()
    {
        if (!_isActive)
        {
            IsMoving = false;
            IsAiming = false;
            _agent.speed = 0f;
            ShootingConeAngleIncrease =  0f;
            return;
        }
        
        if(_controlsMovementAndShooting == 0)
            ControlMovementAndShooting();
        else
        {
            IsMoving = false;
            IsAiming = false;
            _agent.speed = 0f;
            ShootingConeAngleIncrease =  0f;
        }
    }
}
