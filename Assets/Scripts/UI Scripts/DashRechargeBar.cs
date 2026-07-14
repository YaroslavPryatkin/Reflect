using UnityEngine;
using UnityEngine.UI;

public class DashRechargeBar : MonoBehaviour
{
    
    private PlayerDashController playerDashController;
    [SerializeField] private Image bar;


    private void Awake()
    {
        playerDashController = GlobalGameManager.Player.GetComponent<PlayerDashController>();
    }
    void Update()
    {
        var value = playerDashController.DashRechargeFraction;
        bar.fillAmount = value;
    }
}
