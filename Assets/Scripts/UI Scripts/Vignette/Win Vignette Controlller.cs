using UnityEngine;

public class WinVignetteControlller : VignetteController
{
    protected override bool ShouldBeActive()
    {
        return UIManager.IsWinScreen;
    }
}
