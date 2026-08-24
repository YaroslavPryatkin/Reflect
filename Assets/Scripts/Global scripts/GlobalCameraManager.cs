using UnityEngine;

[DefaultExecutionOrder(-200 )]
public class GlobalCameraManager : SceneLocalSingleton<GlobalCameraManager>
{
    private CameraFoWController _cameraFoWController;
    private CameraTransformController _cameraTransformController;
    private Camera _playerCamera;
    
    private void Awake()
    {
        _cameraFoWController =  GetComponent<CameraFoWController>();
        _cameraTransformController = GetComponent<CameraTransformController>();
        _playerCamera = GetComponent<Camera>();
    }

    public static Vector3 PlayerCameraPosition => Instance.transform.position;

    public static Vector3 PlayerCameraForward=> Instance.transform.forward;

    public static Camera PlayerCamera => Instance._playerCamera;
    public static CameraFoWController FoWController => Instance._cameraFoWController;
    public static CameraTransformController TransformController => Instance._cameraTransformController;

}
