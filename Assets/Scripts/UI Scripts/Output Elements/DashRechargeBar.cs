using UnityEngine;
using UnityEngine.UI;

public class DashRechargeBar : MonoBehaviour
{
    
    private PlayerDashController _playerDashController;
    [SerializeField] private Image bar;


    private void Awake()
    {
        _playerDashController = PlayerManager.Player.GetComponent<PlayerDashController>();
    }
    void Update()
    {
        var value = Mathf.Clamp01(_playerDashController.DashRechargeFraction);
        
        if (_playerDashController.IsDashing)
            value = 1 - value;
        
        bar.fillAmount = value;
    }
}
