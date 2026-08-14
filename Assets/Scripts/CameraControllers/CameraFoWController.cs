using UnityEngine;

public class CameraFoWController : MonoBehaviour
{
    [SerializeField] private float baseFOV = 60f;
    [SerializeField] private float maxFOVBonus = 20f;
    [SerializeField] private float fovTransitionSpeed = 8f;

    private Camera cam;
    private float targetFOV;
    private readonly Vector3[] frustumCorners = new Vector3[4];
    public float CurrentSphereCastRadius { get; private set; }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        targetFOV = baseFOV;
        cam.fieldOfView = baseFOV;
        
        RecalculateRadius();
    }

    private void Update()
    {
        if (!UIManager.IsGameActive) return;
        
        if (Mathf.Abs(cam.fieldOfView - targetFOV) > 0.01f)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
            
            RecalculateRadius();
        }
        else if (cam.fieldOfView != targetFOV)
        {
            cam.fieldOfView = targetFOV;
            RecalculateRadius();
        }
    }
    
    public void SetSpeedFactor(float normalizedSpeed)
    {
        targetFOV = baseFOV + (Mathf.Clamp01(normalizedSpeed) * maxFOVBonus);
    }
    
    public void SetSpeedFactorImmediate(float normalizedSpeed)
    {
        targetFOV = baseFOV + (Mathf.Clamp01(normalizedSpeed) * maxFOVBonus);
        cam.fieldOfView = targetFOV;
        RecalculateRadius();
    }

    private void RecalculateRadius()
    {
        cam.CalculateFrustumCorners(
            new Rect(0, 0, 1, 1), 
            cam.nearClipPlane, 
            Camera.MonoOrStereoscopicEye.Mono, 
            frustumCorners
        );
        CurrentSphereCastRadius = frustumCorners[0].magnitude * 1.1f;
    }
}
