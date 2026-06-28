using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{

    [SerializeField] private Camera playerCamera;
    [SerializeField] private float minSpeedToChangeFow = 10;
    [SerializeField] private float maxSpeedToChangeFow = 20;
    
    
    private PlayerSensors playerSensors;
    private Rigidbody rb;
    private CameraFoWController cameraFowController;
    private CameraTransformController cameraTransformController;

    private void Awake()
    {
        playerSensors = GetComponent<PlayerSensors>();
        rb = GetComponent<Rigidbody>();
        cameraFowController = playerCamera.GetComponent<CameraFoWController>();
        cameraTransformController = playerCamera.GetComponent<CameraTransformController>();
    }
    
    void Update()
    {
        ChangeFoWFromSpeed();
        UpdateCameraMode();
    }
    
    private void UpdateCameraMode()
    {
        float cameraMode = 0f;
        if (playerSensors.IsNearLeftWall)
        {
            cameraMode += playerSensors.LeftWallDistanceNormalized;
        }
        if (playerSensors.IsNearRightWall)
        {
            cameraMode -= playerSensors.RightWallDistanceNormalized;
        }
        cameraTransformController.CameraMode = cameraMode;
    }

    private void ChangeFoWFromSpeed()
    {
        float currentSpeed = rb.linearVelocity.magnitude;
        float speedFraction = (currentSpeed-minSpeedToChangeFow) / (maxSpeedToChangeFow - minSpeedToChangeFow);
        cameraFowController.SetSpeedFactor(speedFraction);
    }
}
