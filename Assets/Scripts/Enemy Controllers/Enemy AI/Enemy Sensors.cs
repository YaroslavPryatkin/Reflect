using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemySensors : Sensors
{
    [Header("Seeing player")] 
    [SerializeField] private Transform shoulderPoint;
    [SerializeField] private List<Transform> seePoints;
    [SerializeField] private float seeDistance;

    private GameObject _player;
    private PlayerHitBoxController _playerHitBoxController;
    private PlayerSensors _playerSensors;
    private NavMeshAgent _agent;
    
    public Vector3 PlayerPosition => _playerHitBoxController.PlayerColliderPosition;
    
    public Vector3 PlayerVelocity => _playerSensors.Velocity;

    public bool CanShootToPlayer { get; private set; } = false;
    public bool IsDetectingPlayer { get; private set; } = false;
    
    public Vector3 MyPosition => transform.position;
    public Vector3 HorizontalDirectionToPlayer{get; private set;} = Vector3.forward;
    public Vector3 NormalizedHorizontalDirectionToPlayer { get; private set; } =Vector3.forward;
    public float HorizontalDistanceToPlayer { get; private set; } = 0f;
    public float DistanceToPlayer{get; private set;}=0f;

    private readonly List<Vector3> _shiftVectors = new ();
    
    protected override void Awake()
    {
        base.Awake();
        
        _player = GlobalGameManager.Player;
        _playerHitBoxController = _player.GetComponent<PlayerHitBoxController>();
        _playerSensors = _player.GetComponent<PlayerSensors>();
        _agent = GetComponent<NavMeshAgent>();

        foreach (var seePoint in seePoints)
        {
            var shift = seePoint.position - transform.position;
            _shiftVectors.Add(transform.InverseTransformDirection(shift));
        }
    }

    protected override void Update()
    {
        base.Update();
        ChangeSeePlayer();
        // Debug.Log(IsGrounded  + ", " + FoundGroundNormal+", "+ Velocity +", "+ VelocityAlignedWithGround);
    }
    
    protected override Vector3 GetVelocity()
    {
        return _agent.velocity;
    }

    private void ChangeSeePlayer()
    {
        if (Vector3.Distance(PlayerPosition, transform.position) > seeDistance)
        {
            CanShootToPlayer = false;
            IsDetectingPlayer = false;
            return;
        }
        
        var playerDir =  PlayerPosition - transform.position;
        DistanceToPlayer = playerDir.magnitude;
        HorizontalDirectionToPlayer = new Vector3(playerDir.x, 0, playerDir.z);
        HorizontalDistanceToPlayer = HorizontalDirectionToPlayer.magnitude;
        NormalizedHorizontalDirectionToPlayer = HorizontalDirectionToPlayer.normalized;
        
        IsDetectingPlayer = true;
        CanShootToPlayer = UtilityFunctions.HasLineOfSight(shoulderPoint.position, PlayerPosition,
            IgnoreMyLayerMask, EnemyLayer);
    }

    public bool HasLineOfSightToPlayer(Vector3 position)
    {
        var rot = 
            Quaternion.LookRotation(NormalizedHorizontalDirectionToPlayer, transform.up);
        foreach (var shift in _shiftVectors)
        {
            if (!UtilityFunctions.HasLineOfSight(position + rot * shift, PlayerPosition,
                    IgnoreMyLayerMask, EnemyLayer))
                return false;
        }
        // Debug.Log("Has line of sight was called " + res);
        return true;
    }
}
