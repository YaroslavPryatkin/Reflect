using UnityEngine;

public class CrossHairAndTargetCircleController : MonoBehaviour
{
    [SerializeField] private ProjectedImage crosshair;
    [SerializeField] private ProjectedImage targetLockCircle;
    
    private PlayerTargetLockController _playerTargetLockController;
    private PlayerGunController _playerGunController;

    private void Awake()
    {
        _playerTargetLockController = GlobalGameManager.Player.GetComponent<PlayerTargetLockController>();
        _playerGunController = GlobalGameManager.Player.GetComponent<PlayerGunController>();
    }

    private void Update()
    {
        crosshair.SetShowing(_playerGunController.GunState!=GunController.GunStateEnum.Non);
        crosshair.WorldTarget = _playerGunController.TargetPoint;
        targetLockCircle.SetShowing(_playerTargetLockController.IsLocked);
        targetLockCircle.WorldTarget = _playerTargetLockController.TargetPosition;
    }
}
