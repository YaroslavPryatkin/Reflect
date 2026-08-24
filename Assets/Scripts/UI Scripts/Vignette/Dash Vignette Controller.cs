using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class DashVignetteController : VignetteController
{
    
    private PlayerDashController _playerDashController;
    protected override void Awake()
    {
        base.Awake();
        _playerDashController = PlayerManager.Player.GetComponent<PlayerDashController>();
    }

    protected override bool ShouldBeActive()
    {
        return _playerDashController.IsDashing;
    }
}
