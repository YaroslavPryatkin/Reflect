using UnityEngine;
using UnityEngine.InputSystem;

public class GlobalLookDirectionManager : MonoBehaviour
{
    public static GlobalLookDirectionManager Instance { get; private set; }

    [SerializeField] private float mouseSensitivityX = 1;
    [SerializeField] private float mouseSensitivityY = 1;
    [SerializeField] private float minCamAngle = 10f;
    [SerializeField] private float maxCamAngle = 70f;
    
    private Vector3 currentLookDirection  = Vector3.forward;

    public static Vector3 CurrentLookDirection => Instance != null ? Instance.currentLookDirection : Vector3.forward;
    
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
    }
    
    private void Start()
    {
        currentPitch = (minCamAngle + maxCamAngle)/2;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void OnEnable()
    {
        GlobalInputManager.Instance.OnLookEvent += HandleLook;
    }

    private void OnDisable()
    {
        GlobalInputManager.Instance.OnLookEvent -= HandleLook;
    }

    private void HandleLook(Vector2 lookVec)
    {
        currentYaw += lookVec.x * mouseSensitivityX;
        currentPitch -= lookVec.y * mouseSensitivityY;
        
        currentPitch = Mathf.Clamp(currentPitch, minCamAngle, maxCamAngle);
        

        currentLookDirection = Quaternion.Euler(currentPitch, currentYaw, 0f) * Vector3.forward;
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
}
