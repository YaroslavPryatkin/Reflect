using UnityEngine;

public class FanRotationController : MonoBehaviour
{
    [Header("Everything is in degrees")]
    [SerializeField] private float duration360 = 1f;
    [SerializeField] private Vector3 rotationMultipliers = Vector3.zero;
    
    private float _degreesPerSecond;

    private void Awake()
    {
        if (duration360 <= 0f)
        {
            enabled = false;
            return;
        }

        _degreesPerSecond = 360f / duration360;
    }
    
    private void Update()
    {

        transform.Rotate(rotationMultipliers * (_degreesPerSecond * Time.deltaTime), Space.Self);
    }
}
