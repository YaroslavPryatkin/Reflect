using UnityEngine;

public class DashBlockTriggerController : TriggerController
{
    protected override void Entered()
    {
        PlayerDashController.SetDashBlocked();
    }
    
    protected override void Exited()
    {
        PlayerDashController.UnsetDashBlocked();
    }
}
