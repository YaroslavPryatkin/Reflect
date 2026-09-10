using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class DashBlockVignetteController : VignetteController
{

    protected override bool ShouldBeActive()
    {
        return PlayerDashController.Instance.IsDashBlocked;
    }
}
