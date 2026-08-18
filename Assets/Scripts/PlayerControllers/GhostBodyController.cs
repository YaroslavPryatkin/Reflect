using UnityEngine;

public class GhostBodyController : MonoBehaviour
{
    [SerializeField] private Transform body;
    [SerializeField] private float vertDistance = 0.15f;
    [SerializeField] private float vertTime = 1f;
    [SerializeField] private float rotationTime = 1.5f;

    private Vector3 startLocalPosition;
    private Vector3 startLocalRotation;

    private void Start()
    {
        startLocalPosition = body.localPosition;
        startLocalRotation = body.localRotation.eulerAngles;
    }

    private void Update()
    {
        if (rotationTime <= 0f || vertTime <= 0f) return;

        float time = Time.unscaledTime;

        float currentAngle = (time * (360f / rotationTime)) % 360f;
        
        body.localRotation = Quaternion.Euler(startLocalRotation.x, currentAngle, startLocalRotation.z);

        float radians = (time / vertTime) * 2f * Mathf.PI;
        float currentVertOffset = Mathf.Sin(radians) * vertDistance;

        body.localPosition = startLocalPosition + Vector3.up * currentVertOffset;
    }
    
    
    
}
