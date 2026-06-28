using System;
using Unity.VisualScripting;
using UnityEngine;

public class CameraTransformController : MonoBehaviour
{
    [SerializeField] private Transform target;

    [SerializeField] private float farCameraDistance = 3f;
    [SerializeField] private float farSideShiftMultiplier = 0.5f;
    [SerializeField] private float closeCameraDistance = 1.5f;
    [SerializeField] private float closeSideShiftMultiplier = 0.7f;
    [SerializeField] private float cameraSpeed = 15f;
    [SerializeField] private float smoothTime = 0.05f;
    [SerializeField] private float rotationSmoothTime = 0.05f;
    [SerializeField] private LayerMask ignoreLayers;


    private float externalTargetCameraMode = 0;

    private bool isDashing = false;
    
    private float targetSideShiftMultiplier = 0; 
    private float cameraDistance = 0;
    private float sideShiftMultiplier = 0;
    
    private CameraFoWController _cameraFoWController;
    
    public float CameraMode
    {
        get => externalTargetCameraMode;
        set => externalTargetCameraMode = value;
    }
    
    public void ResetCameraMode(){externalTargetCameraMode = 0f;}

    private void MakeCameraSettings()
    {
        if (isDashing)
        {
            cameraDistance = closeCameraDistance;
            
            var externalSideShiftMultiplier = farSideShiftMultiplier * externalTargetCameraMode;
            
            if(externalSideShiftMultiplier <= 0.01f)
                targetSideShiftMultiplier = Math.Min(externalTargetCameraMode, -closeSideShiftMultiplier);
            else if (externalSideShiftMultiplier >= 0.01f)
                targetSideShiftMultiplier = Math.Max(externalTargetCameraMode, closeSideShiftMultiplier);
            else
                targetSideShiftMultiplier = closeSideShiftMultiplier;
        }
        else
        {
            cameraDistance = farCameraDistance;
            targetSideShiftMultiplier = farSideShiftMultiplier * externalTargetCameraMode;
        }
    }
    

    

    private void Start()
    {
        _cameraFoWController = GetComponent<CameraFoWController>();
    }
    
    private void OnEnable()
    {
        PlayerMapInputManager.Instance.OnDashPressEvent += HandleDashPress;
        PlayerMapInputManager.Instance.OnDashReleaseEvent += HandleDashRelease;
    }

    private void OnDisable()
    {
        PlayerMapInputManager.Instance.OnDashPressEvent -= HandleDashPress;
        PlayerMapInputManager.Instance.OnDashReleaseEvent -= HandleDashRelease;
    }

    private void HandleDashPress()
    {
        isDashing = true;
    }

    private void HandleDashRelease()
    {
        isDashing = false;
    }
    
    Vector3 camVelocity = Vector3.zero;
    private Vector3 smoothedLookDir = Vector3.forward;
    private Vector3 lookDirVelocity = Vector3.zero;

    
    private void LateUpdate()
    {
        MakeCameraSettings();
        
        sideShiftMultiplier = Mathf.Lerp(sideShiftMultiplier, targetSideShiftMultiplier, cameraSpeed * Time.unscaledDeltaTime);
        if (Mathf.Abs(sideShiftMultiplier - targetSideShiftMultiplier) < 0.001f) sideShiftMultiplier = targetSideShiftMultiplier;
        
        
        var lookDir = GlobalLookDirectionManager.CurrentLookDirection;
        if (lookDir == Vector3.zero) lookDir = target.forward;
        
        smoothedLookDir = Vector3.SmoothDamp(smoothedLookDir, lookDir, ref lookDirVelocity, rotationSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        smoothedLookDir = smoothedLookDir.normalized;
        
        var directionToCamera = -smoothedLookDir;
        
        var horizontalDir = new Vector3(directionToCamera.x, 0f, directionToCamera.z).normalized;

        if (horizontalDir == Vector3.zero)
        {
            horizontalDir = new Vector3(target.forward.x, 0f, target.forward.z).normalized;
        }
        
        var sideShift = Vector3.Cross(horizontalDir,Vector3.up) * sideShiftMultiplier;
        var realDirection = directionToCamera + sideShift;

        var finalPosition = Utility.GetSphereRayCastPoint(target.position, realDirection, cameraDistance, 
            _cameraFoWController.CurrentSphereCastRadius, ignoreLayers);
        
        transform.position = Vector3.SmoothDamp(transform.position, finalPosition, ref camVelocity,  smoothTime, Mathf.Infinity , Time.unscaledDeltaTime);

        transform.rotation = Quaternion.LookRotation(smoothedLookDir);
        
    }
    
    
}

