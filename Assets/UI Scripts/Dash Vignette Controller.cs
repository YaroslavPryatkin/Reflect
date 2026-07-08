using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class DashVignetteController : MonoBehaviour
{
    
    private PlayerDashController playerDashController;
    [SerializeField] private float fadeInTime = 0.1f;
    [SerializeField] private float fadeOutTime = 0.1f;
    [SerializeField] private Image image;
    
    private Utility.FractionBlockingValueTimer<bool> isShowing = false;


    private void Awake()
    {
        playerDashController = GlobalGameManager.Player.GetComponent<PlayerDashController>();
    }
    
    void Update()
    {
        if (isShowing)
        {
            if(!playerDashController.IsDashing)
                isShowing.SetForce(false, fadeOutTime);
            else
                image.canvasRenderer.SetAlpha(Mathf.Clamp01(isShowing.TimeFraction));
        }
        else
        {
            if(playerDashController.IsDashing)
                isShowing.SetForce(true, fadeInTime);
            else
                image.canvasRenderer.SetAlpha(1 - Mathf.Clamp01(isShowing.TimeFraction));
        }
    }
}
