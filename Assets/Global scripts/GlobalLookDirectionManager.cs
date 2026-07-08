using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-150)]
public class GlobalLookDirectionManager : MonoBehaviour
{
    public static GlobalLookDirectionManager Instance { get; private set; }

    [SerializeField] private float baseMouseSensitivityX = 0.8f;
    [SerializeField] private float baseMouseSensitivityY = 0.8f;
    [SerializeField] private float baseAimingMouseSensitivity= 0.6f;
    [SerializeField] private float minCamAngle = 10f;
    [SerializeField] private float maxCamAngle = 70f;
    [SerializeField] private PlayerGunController playerGunController;
    private InputSlider mouseSensSliderX;
    private InputSlider mouseSensSliderY;
    private InputSlider mouseSensSliderAiming;
    
    private Vector3 currentLookDirection  = Vector3.forward;

    public static Vector3 CurrentLookDirection => Instance.currentLookDirection;
    
    private float currentYaw;
    private float currentPitch = 20f;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        mouseSensSliderX = GlobalUIManager.Instance.EscapeMenuController.MouseSensX;
        mouseSensSliderY = GlobalUIManager.Instance.EscapeMenuController.MouseSensY;
        mouseSensSliderAiming = GlobalUIManager.Instance.EscapeMenuController.MouseSensAim;
    }
    
    private void Start()
    {
        currentPitch = (minCamAngle + maxCamAngle)/2;
    }
    
    private void OnEnable()
    {
        GlobalGameInputManager.Instance.OnLookEvent += HandleLook;
    }

    private void OnDisable()
    {
        GlobalGameInputManager.Instance.OnLookEvent -= HandleLook;
    }

    private void HandleLook(Vector2 lookVec)
    {
        
        currentYaw += lookVec.x * baseMouseSensitivityX * GetMouseSensitivityScale(mouseSensSliderX.Value) * GetAimingMouseSensitivityScale();
        currentPitch -= lookVec.y * baseMouseSensitivityY * GetMouseSensitivityScale(mouseSensSliderY.Value) * GetAimingMouseSensitivityScale();
        
        currentPitch = Mathf.Clamp(currentPitch, minCamAngle, maxCamAngle);


        CalculateLookDirection();
    }

    private void CalculateLookDirection()
    {
        currentLookDirection = Quaternion.Euler(currentPitch, currentYaw, 0f) * Vector3.forward;
    }

    private float GetMouseSensitivityScale(float userInput)
    {
        if (userInput <= 1) return userInput * userInput;
        else return Mathf.Sqrt(userInput);
    }

    private float GetAimingMouseSensitivityScale()
    {
        return playerGunController.GunState!=GunController.GunStateEnum.Non ? baseAimingMouseSensitivity * mouseSensSliderAiming.Value : 1;
    }
    
    public static Vector3 FromCameraLocalToGlobalByZX(Vector3 localVector)
    {
        return Utility.FromLocalToGlobalByZX(CurrentLookDirection, localVector);
    }
    public static Vector3 FromCameraLocalToGlobalByZX(Vector2 localVector)
    {
        var localVector3 = new Vector3(localVector.x, 0f, localVector.y);
        return Utility.FromLocalToGlobalByZX(CurrentLookDirection, localVector3);
    }

    public static float CurrentYaw => Instance.currentYaw;
    
    public static void SetNewYaw(float yaw)
    {
        Instance.currentYaw =  yaw;
        Instance.CalculateLookDirection();
    }
}
