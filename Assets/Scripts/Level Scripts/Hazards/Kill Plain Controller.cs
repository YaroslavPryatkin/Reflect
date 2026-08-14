using System;
using UnityEngine;

public class KillPlainController : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out HealthController healthController))
        {
            healthController.FellOffTheMap();
        }
    }

    private void OnDrawGizmos()
    {
        transform.DrawColliderGizmo(Color.red);
    }
}
