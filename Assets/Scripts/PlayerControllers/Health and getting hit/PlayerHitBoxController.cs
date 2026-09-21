using UnityEngine;

public class PlayerHitBoxController : MonoBehaviour
{

    [SerializeField] private float slideHitboxHeight = 1f;
    
    private PlayerSlidingController _playerSlidingController;
    private PlayerSensors _playerSensors;
    
    public Vector3 PlayerColliderPosition => transform.position + _playerSensors.ThisCollider.center; 

    private void Awake()
    {
        _playerSlidingController = GetComponent<PlayerSlidingController>();
        _playerSensors = GetComponent<PlayerSensors>();
    }

    private void Update()
    {
        if (_playerSlidingController.IsActiveSlidingPhase)
        {
            _playerSensors.ThisCollider.height = slideHitboxHeight;
            _playerSensors.ThisCollider.center =
                new Vector3(0, -(_playerSensors.ColliderHeight - slideHitboxHeight)/2, 0);
        }
        else
        {
            
            _playerSensors.ThisCollider.height = _playerSensors.ColliderHeight;
            _playerSensors.ThisCollider.center = new Vector3(0, 0, 0);
        }
    }
}
