using UnityEngine;
using UnityEngine.AI;

public class EnemySensors : Sensors
{
    [Header("Seeing player")] 
    [SerializeField] private Transform seePoint;
    [SerializeField] private float seeDistance;

    private GameObject _player;
    private PlayerHitBoxController _playerHitBoxController;
    private PlayerSensors _playerSensors;
    private NavMeshAgent _agent;
    
    public Vector3 PlayerPosition => _playerHitBoxController.PlayerColliderPosition;
    
    public Vector3 PlayerVelocity => _playerSensors.Velocity;

    public bool IsSeeingPlayer { get; private set; } = false;
    public bool IsDetectingPlayer { get; private set; } = false;
    
    public Vector3 HorizontalDirectionFromSeePointToPlayer{get; private set;} = Vector3.zero;
    public Vector3 NormalizedHorizontalDirectionFromSeePointToPlayer { get; private set; } =Vector3.zero;
    public float HorizontalDistanceFromSeePointToPlayer { get; private set; } = 0f;
    public Vector3 SeePointPosition => seePoint.position;
    public float DistanceFromSeePointToPlayer{get; private set;}=0f;
    
    protected override void Awake()
    {
        base.Awake();
        
        _player = GlobalGameManager.Player;
        _playerHitBoxController = _player.GetComponent<PlayerHitBoxController>();
        _playerSensors = _player.GetComponent<PlayerSensors>();
        _agent = GetComponent<NavMeshAgent>();

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
        if (Vector3.Distance(PlayerPosition, seePoint.position) > seeDistance)
        {
            IsSeeingPlayer = false;
            IsDetectingPlayer = false;
            return;
        }
        
        var seePointPlayerDir =  PlayerPosition - seePoint.position;
        DistanceFromSeePointToPlayer = seePointPlayerDir.magnitude;
        HorizontalDirectionFromSeePointToPlayer = new Vector3(seePointPlayerDir.x, 0, seePointPlayerDir.z);
        HorizontalDistanceFromSeePointToPlayer = HorizontalDirectionFromSeePointToPlayer.magnitude;
        NormalizedHorizontalDirectionFromSeePointToPlayer = HorizontalDirectionFromSeePointToPlayer.normalized;
        
        IsDetectingPlayer = true;
        IsSeeingPlayer = HasLineOfSightToPlayer(seePoint.position);
    }

    public bool HasLineOfSightToPlayer(Vector3 position)
    {
        var res = Utility.HasLineOfSight(position, PlayerPosition,
            IgnoreMyLayerMask, EnemyLayer);
        // Debug.Log("Has line of sight was called " + res);
        return res;
    }

    public void GetTransformPositionFromSeePointPosition(ref Vector3 position)
    {
        // var tmp = SeePointPosition - transform.position;
        // tmp = new Vector3(tmp.x, 0, tmp.z);
        // var dir = Vector3.Cross(NormalizedHorizontalDirectionFromSeePointToPlayer, Vector3.up);
        // position += dir * tmp.magnitude;
        position += transform.position - SeePointPosition;
    }
}
