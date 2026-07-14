using System;
using UnityEngine;

public class EnemyCanvasController : MonoBehaviour
{
    private Transform cameraTransform;

    private void Awake()
    {
        cameraTransform = GlobalCameraManager.PlayerCamera.transform;
    }

    private void Update()
    {
        transform.LookAt(2 * transform.position - cameraTransform.position);
    }
}
