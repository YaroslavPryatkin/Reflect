using System;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBarController : MonoBehaviour
{
    [SerializeField] private EnemyHealthController healthController;
    [SerializeField] private GameObject sliderObject;

    private Slider _slider;

    private void Awake()
    {
        _slider = sliderObject.GetComponent<Slider>();
    }
    private void Update()
    {
        if (healthController.IsFullHealth || healthController.IsDead)
        {
            sliderObject.SetActive(false);
        }
        else
        {
            sliderObject.SetActive(true);
            _slider.value = healthController.HealthFraction;
        }
    }
}
