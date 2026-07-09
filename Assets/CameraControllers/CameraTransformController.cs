using System;
using Unity.VisualScripting;
using UnityEngine;

public class CameraTransformController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.05f;
    [SerializeField] private float rotationSmoothTime = 0.05f;
    [SerializeField] private float maxDistanceToSnap = 20f;
    [SerializeField] private LayerMask ignoreLayers;
    
    public float TargetSideShift { get; set; } = 0; 
    public float TargetUpShift { get; set; }= 0;
    public float TargetCameraDistance { get; set; }= 0;
    
    public Vector3 TargetDirection { get; set; } = Vector3.zero;
    
    private CameraFoWController _cameraFoWController;

    private void Start()
    {
        _cameraFoWController = GetComponent<CameraFoWController>();
    }
    
    Vector3 camVelocity = Vector3.zero;
    private Vector3 smoothedLookDir = Vector3.forward;
    private Vector3 lookDirVelocity = Vector3.zero;

    
    private void LateUpdate()
    {
        if (!GlobalUIManager.Instance.IsGameActive) return;
        
        
        var lookDir = TargetDirection;
        if (lookDir == Vector3.zero) lookDir = target.forward;
        
        smoothedLookDir = Vector3.SmoothDamp(smoothedLookDir, lookDir, ref lookDirVelocity, rotationSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        smoothedLookDir = smoothedLookDir.normalized;
        
        var directionToCamera = -smoothedLookDir;
        
        var horizontalDir = new Vector3(directionToCamera.x, 0f, directionToCamera.z).normalized;

        if (horizontalDir == Vector3.zero)
        {
            horizontalDir = new Vector3(target.forward.x, 0f, target.forward.z).normalized;
        }
        var sideShiftVector = Vector3.Cross(horizontalDir,Vector3.up) * TargetSideShift;
        var upShiftVector = Vector3.up * TargetUpShift;
        var realDirection = directionToCamera + sideShiftVector + upShiftVector;

        var finalPosition = Utility.GetSphereRayCastPoint(target.position, realDirection, TargetCameraDistance, 
            _cameraFoWController.CurrentSphereCastRadius, ignoreLayers);
        if (Vector3.Distance(transform.position, finalPosition) > maxDistanceToSnap)
        {
            transform.position = finalPosition;
            camVelocity = Vector3.zero;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, finalPosition, ref camVelocity, smoothTime,
                Mathf.Infinity, Time.unscaledDeltaTime);
        }

        transform.rotation = Quaternion.LookRotation(smoothedLookDir);
        
    }

    // private void ChangeShifts()
    // {
    //     sideShift = Mathf.Lerp(sideShift, TargetSideShift, sideShiftSpeed * Time.unscaledDeltaTime);
    //     if (Mathf.Abs(sideShift - TargetSideShift) < 0.001f) sideShift = TargetSideShift;
    //     
    //     upShift = Mathf.Lerp(upShift, TargetUpShift, upShiftSpeed * Time.unscaledDeltaTime);
    //     if (Mathf.Abs(upShift - TargetUpShift) < 0.001f) upShift = TargetUpShift;
    // }
    
}

