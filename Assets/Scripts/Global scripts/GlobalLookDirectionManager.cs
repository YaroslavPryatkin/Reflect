using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-150)]
public class GlobalLookDirectionManager : SceneLocalSingleton<GlobalLookDirectionManager>
{
    [SerializeField] private float baseMouseSensitivityX = 0.8f;
    [SerializeField] private float baseMouseSensitivityY = 0.8f;
    [SerializeField] private float baseAimingMouseSensitivity= 0.6f;
    [SerializeField] private float minCamAngle = 10f;
    [SerializeField] private float maxCamAngle = 70f;
    [SerializeField] private float startingYaw = 0f;


    private PlayerGunController _playerGunController;
    private FloatSettingsValue _mouseSensX;
    private FloatSettingsValue _mouseSensY;
    private FloatSettingsValue _mouseSensAim;
    
    private Vector3 _currentLookDirection  = Vector3.forward;

    public static Vector3 CurrentLookDirection => Instance._currentLookDirection;
    
    private float _currentYaw;
    private float _currentPitch = 20f;
    
    private void Awake()
    {
        _currentYaw = startingYaw;
        
        _mouseSensX = GameSettings.Get<FloatSettingsValue>("mouseSensX");
        _mouseSensY = GameSettings.Get<FloatSettingsValue>("mouseSensY");
        _mouseSensAim = GameSettings.Get<FloatSettingsValue>("mouseSensAim");

        _playerGunController = PlayerManager.Player.GetComponent<PlayerGunController>();
        
        CalculateLookDirection();
    }
    
    private void Start()
    {
        _currentPitch = (minCamAngle + maxCamAngle)/2;
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
        
        _currentYaw += lookVec.x * baseMouseSensitivityX * GetMouseSensitivityScale(_mouseSensX.Value) * GetAimingMouseSensitivityScale();
        _currentPitch -= lookVec.y * baseMouseSensitivityY * GetMouseSensitivityScale( _mouseSensY.Value) * GetAimingMouseSensitivityScale();
        
        _currentPitch = Mathf.Clamp(_currentPitch, minCamAngle, maxCamAngle);


        CalculateLookDirection();
    }

    private void CalculateLookDirection()
    {
        _currentLookDirection = Quaternion.Euler(_currentPitch, _currentYaw, 0f) * Vector3.forward;
    }

    private float GetMouseSensitivityScale(float userInput)
    {
        if (userInput <= 1) return userInput * userInput;
        else return Mathf.Sqrt(userInput);
    }

    private float GetAimingMouseSensitivityScale()
    {
        return _playerGunController.GunStateValue!=UtilityFunctions.BaseActionTransitionsEnum.Base ? baseAimingMouseSensitivity * _mouseSensAim.Value : 1;
    }
    
    public static Vector3 FromCameraLocalToGlobalByZX(Vector3 localVector)
    {
        return UtilityFunctions.FromLocalToGlobalByZX(CurrentLookDirection, localVector);
    }

    public static float CurrentYaw => Instance._currentYaw;
    
    public static void SetNewYaw(float yaw)
    {
        Instance._currentYaw =  yaw;
        Instance.CalculateLookDirection();
    }
}
