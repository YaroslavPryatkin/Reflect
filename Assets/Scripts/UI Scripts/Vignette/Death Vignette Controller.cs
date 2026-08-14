using UnityEngine;

public class DeathVignetteController : VignetteController
{
    protected override bool ShouldBeActive()
    {
        return UIManager.IsDeathScreen;
    }
}
