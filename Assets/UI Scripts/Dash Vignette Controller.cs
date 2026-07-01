using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class DashVignetteController : MonoBehaviour
{
    
    [SerializeField] private PlayerDashController playerDashController;
    [SerializeField] private float fadeInTime = 0.1f;
    [SerializeField] private float fadeOutTime = 0.1f;
    [SerializeField] private GameObject imageObject;

    
    private Image image;
    private Utility.FractionValueTimer<bool> isShowing = false;


    private void Awake()
    {
        image = imageObject.GetComponent<Image>();
    }
    private void Start()
    {
        imageObject.SetActive(true);
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
