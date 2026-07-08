using UnityEngine;

public class EnemySensors : Sensors
{
    [Header("Seeing player")] 
    [SerializeField] private Transform seePoint;
    [SerializeField] private float seeDistance;

    private DamageController _damageController;
    private GameObject _player;
    private PlayerHitBoxController _playerHitBoxController;
    private PlayerSensors _playerSensors;
    
    public Vector3 PlayerPosition => _playerHitBoxController.PlayerColliderPosition;
    
    public Vector3 PlayerVelocity => _playerSensors.Velocity;

    public bool IsSeeingPlayer { get; private set; } = false;
    
    public Vector3 HorizontalDirectionToPlayer { get; private set; } =Vector3.zero;
    
    protected override void Awake()
    {
        base.Awake();

        _damageController = GetComponent<DamageController>();
        
        _player = GlobalGameManager.Player;
        _playerHitBoxController = _player.GetComponent<PlayerHitBoxController>();
        _playerSensors = _player.GetComponent<PlayerSensors>();
    }

    protected override void Update()
    {
        base.Update();

        ChangeSeePlayer();
    }

    private void ChangeSeePlayer()
    {
        if (Vector3.Distance(PlayerPosition, seePoint.position) > seeDistance)
        {
            IsSeeingPlayer = false;
            return;
        }
        
        if(!Utility.HasLineOfSight(transform.position, PlayerPosition, seeDistance, IgnoreMyLayerMask, _damageController.EnemyLayer))
        {
            IsSeeingPlayer = false;
            return;
        }
        
        IsSeeingPlayer = true;
        
        var playerDir =  PlayerPosition - seePoint.position;
        HorizontalDirectionToPlayer = new Vector3(playerDir.x, 0, playerDir.z).normalized;
    }
}
