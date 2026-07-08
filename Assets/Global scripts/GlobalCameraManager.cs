using UnityEngine;

[DefaultExecutionOrder(-200 )]
public class GlobalCameraManager : MonoBehaviour
{
    private static GlobalCameraManager _instance;
    [SerializeField] private GameObject playerCameraObject;

    private CameraFoWController _cameraFoWController;
    private CameraTransformController _cameraTransformController;
    private Camera _playerCamera;
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        _cameraFoWController =  playerCameraObject.GetComponent<CameraFoWController>();
        _cameraTransformController = playerCameraObject.GetComponent<CameraTransformController>();
        _playerCamera = playerCameraObject.GetComponent<Camera>();
    }

    public static Vector3 GetPlayerCameraPosition()
    {
        return _instance.playerCameraObject.transform.position;
    }

    public static Vector3 GetPlayerCameraForward()
    {
        return _instance.playerCameraObject.transform.forward;
    }

    public static Camera PlayerCamera => _instance._playerCamera;
    public static CameraFoWController FoWController => _instance._cameraFoWController;
    public static CameraTransformController TransformController => _instance._cameraTransformController;

}
