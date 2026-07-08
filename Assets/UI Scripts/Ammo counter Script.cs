using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AmmoCounterController : MonoBehaviour
{
    [SerializeField] private List<GameObject> bullets;
    
    
    private PlayerGunController playerGunController;

    private List<Slider> sliders = new ();

    private void Awake()
    {
        for(int i=0;i<bullets.Count;i++)
            sliders.Add(bullets[i].GetComponent<Slider>());

        playerGunController = GlobalGameManager.Player.GetComponent<PlayerGunController>();
    }
    
    private void Update()
    {
        var full = playerGunController.CurrentAmountOfBullets;
        var i = 0;
        for (; i < full && i<bullets.Count; i++)
        {
            bullets[i].SetActive(true);
            sliders[i].value = 1;
        }
        if (playerGunController.IsRechargingBullet && i < bullets.Count)
        {
            bullets[i].SetActive(true);
            sliders[i].value = playerGunController.RechargeFraction;
            i++;
        }
        for (; i<bullets.Count; i++)
        {
            bullets[i].SetActive(false);
        }
    }
}
