using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class DashVignetteController : VignetteController
{
    protected override bool ShouldBeActive()
    {
        return PlayerDashController.Instance.IsDashing;
    }
}
