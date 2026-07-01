using UnityEngine;
using UnityEngine.UI;

public class DashRechargeBar : MonoBehaviour
{
    
    [SerializeField] private PlayerDashController playerDashController;
    
    
    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }
    
    void Update()
    {
        var value = playerDashController.DashRechargeFraction;
        slider.value = value >= 1 ? 0 : value;

    }
}
