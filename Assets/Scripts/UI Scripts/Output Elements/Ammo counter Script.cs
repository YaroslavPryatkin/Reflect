using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AmmoCounterController : MonoBehaviour
{
    [SerializeField] private List<GameObject> bullets;
    
    
    private PlayerGunController _playerGunController;

    private readonly List<Slider> _sliders = new ();

    private void Awake()
    {
        for(int i=0;i<bullets.Count;i++)
            _sliders.Add(bullets[i].GetComponent<Slider>());

        _playerGunController = PlayerManager.Player.GetComponent<PlayerGunController>();
    }
    
    private void Update()
    {
        var full = _playerGunController.CurrentAmountOfBullets;
        var i = 0;
        for (; i < full && i<bullets.Count; i++)
        {
            bullets[i].SetActive(true);
            _sliders[i].value = 1;
        }
        if (_playerGunController.IsRechargingBullet && i < bullets.Count)
        {
            bullets[i].SetActive(true);
            _sliders[i].value = _playerGunController.RechargeFraction;
            i++;
        }
        for (; i<bullets.Count; i++)
        {
            bullets[i].SetActive(false);
        }
    }
}
